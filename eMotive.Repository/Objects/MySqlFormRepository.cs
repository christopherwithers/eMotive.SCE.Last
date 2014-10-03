using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using Dapper;
using eMotive.Repository.Interfaces;
using eMotive.Repository.Objects.Forms;
using Extensions;
using MySql.Data.MySqlClient;

namespace eMotive.Repository.Objects
{
    public class MySqlFormRepository : IFormRepository
    {        
        private readonly string _connectionString;
        private IDbConnection _connection;

        public MySqlFormRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        internal IDbConnection Connection
        {
            get
            {
                return _connection ?? new MySqlConnection(_connectionString);
            }
        }

        public FormList NewFormList()
        {
            return new FormList {Collection = new List<FormListItem>()};
        }

        public bool CreateFormList(FormList formList, out int id)
        {
            using (var cn = Connection)
            {
                using (var transaction = new TransactionScope())
                {
                    var success = true;
                    cn.Open();
                    id = -1;

                    var sql = "INSERT INTO `FormLists` (`Name`) Values (@Name);";

                    success &= cn.Execute(sql, formList) > 0;

                    var newId = cn.Query<ulong>("SELECT CAST(LAST_INSERT_ID() AS UNSIGNED INTEGER);").SingleOrDefault();
                    var convId = id = Convert.ToInt32(newId);

                    if (id > 0)
                    {
                        var formListItems = formList.Collection.Select(n => new
                        {
                            FormListID = newId,
                            Value = n.Value,
                            Text = n.Text
                        }).ToList();


                        sql = "INSERT INTO `FormListItems` (`FormListID`, `Value`, `Text`) Values (@ApplicationID, @Name);";

                        success &= cn.Execute(sql, formListItems) > 0;
                    }

                    transaction.Complete();

                    return success | newId > 0;
                }

            }
        }
        //TODO: find a way of passing around connection and transaction scope for all internal queries? To do this, we need a way of wrapping using statements around all calls, rather than each call having their own using statements?
        public bool UpdateFormList(FormList formList)
        {
            using (var cn = Connection)
            {
                using (var transaction = new TransactionScope())
                {
                    cn.Open();
                    var success = true;
                    #region fetch old FormList
                    var sql = "SELECT `ID`,`Name` FROM `FormLists` a INNER JOIN `FormListItems` b ON a.`ID`=b.`FormListID` WHERE a.`ID`=@Id;";

                    var oldFormList = cn.Query<FormList, IEnumerable<FormListItem>, FormList>(sql, (oFormList, formListItems) =>
                    {
                        oFormList.Collection = formListItems;
                        return oFormList;
                    }, new {id = formList.ID}).SingleOrDefault();
                    #endregion

                    sql = "UPDATE `FormLists` SET `Name`=@Name WHERE `ID`=@Id;";
                    success &= cn.Execute(sql, new { Name = formList.Name, Id = formList.ID }) > 0;

                    if (formList.Collection.HasContent() && !oldFormList.Collection.HasContent())
                    {
                        sql = "DELETE FROM `FormListItems` WHERE `FormListID`=@Id;";
                        success &= cn.Execute(sql, new { Id = formList.ID }) > 0;
                    }
                    else
                    {
                        if (formList.Collection.HasContent() && !oldFormList.Collection.HasContent())
                        {
                            var formListItems = formList.Collection.Select(n => new
                            {
                                FormListID = formList.ID,
                                Value = n.Value,
                                Text = n.Text
                            }).ToList();


                            sql = "INSERT INTO `FormListItems` (`FormListID`, `Value`, `Text`) Values (@ApplicationID, @Name);";

                            success &= cn.Execute(sql, formListItems) > 0;
                        }
                        else if(formList.Collection.HasContent() && oldFormList.Collection.HasContent())
                        {
                            var toDelete = oldFormList.Collection.Where(n => !formList.Collection.Any(m => n.ID == m.ID && m.ID > 0));
                            var toUpdate = formList.Collection.Where(n => oldFormList.Collection.Any(m => n.ID == m.ID));//todo: need to check for greater than 0 here?
                            var toCreate = formList.Collection.Where(n => n.ID == 0).Select(m => new {FormListID = formList.ID, Value = m.Value, Text = m.Text});

                            if (toDelete.HasContent())
                            {
                                sql = "DELETE FROM `FormListItems` WHERE `FormListID`=@id";
                                success &= cn.Execute(sql, toDelete) > 0;
                            }

                            if (toUpdate.HasContent())
                            {
                                sql = "UPDATE `FormListItems` SET `Value`=@Value, `Text`=@Text WHERE `FormListID`=@ID";
                                success &= cn.Execute(sql, toUpdate) > 0;
                            }

                            if (toCreate.HasContent())
                            {
                                sql = "INSERT INTO `FormListItems` (`VALUE`,`TEXT`) VALUES (@Value,@Text) WHERE `FormListID`=@id";
                                success &= cn.Execute(sql, toCreate) > 0;
                            }
                        }
                    }

                    if (!success)
                        transaction.Dispose();
                    else
                        transaction.Complete();

                    return success;
                }
            }
        }

        public bool DeleteFormList(FormList formList)
        {
            using (var cn = Connection)
            {
                using (var transaction = new TransactionScope())
                {
                    cn.Open();
                    var success = true;

                    var sql = "DELETE FROM `FormLists` WHERE `Id`=@id;";
                    success &= cn.Execute(sql, new {id = formList.ID}) > 0;

                    sql = "DELETE FROM `FormListItems` WHERE `FormListID`=@id;";
                    success &= cn.Execute(sql, new {id = formList.ID}) > 0;

                    if (!success)
                        transaction.Dispose();
                    else
                        transaction.Complete();

                    return success;
                }
            }
        }

        public FormList FetchFormList(int id)
        {
            using (var cn = Connection)
            {
                cn.Open();
                
                const string sql = "SELECT `ID`,`Name` FROM `FormLists` a INNER JOIN `FormListItems` b ON a.`ID`=b.`FormListID` WHERE a.`ID`=@id;";

                var collection = cn.Query<FormList, IEnumerable<FormListItem>, FormList>(sql, (formList, formListItems) =>
                {
                    formList.Collection = formListItems;
                    return formList;
                }, new {id = id}).SingleOrDefault();

                return collection;
            }
        }

        public FormList FetchFormList(string name)
        {
            using (var cn = Connection)
            {
                cn.Open();

                const string sql = "SELECT `ID`,`Name` FROM `FormLists` a INNER JOIN `FormListItems` b ON a.`ID`=b.`FormListID` WHERE a.`Name`=@name;";

                var collection = cn.Query<FormList, IEnumerable<FormListItem>, FormList>(sql, (formList, formListItems) =>
                {
                    formList.Collection = formListItems;
                    return formList;
                }, new {name = name}).SingleOrDefault();

                return collection;
            }
        }

        public IEnumerable<FormList> FetchFormList()
        {
            using (var cn = Connection)
            {
                cn.Open();

                const string sql = "SELECT `ID`,`Name` FROM `FormLists` a INNER JOIN `FormListItems` b ON a.`ID`=b.`FormListID`;";

                var collection = cn.Query<FormList, IEnumerable<FormListItem>, FormList>(sql, (formList, formListItems) =>
                {
                    formList.Collection = formListItems;
                    return formList;
                });

                return collection;
            }
        }

        public IEnumerable<FormList> FetchFormList(IEnumerable<int> ids)
        {
            using (var cn = Connection)
            {
                cn.Open();

                const string sql = "SELECT `ID`,`Name` FROM `FormLists` a INNER JOIN `FormListItems` b ON a.`ID`=b.`FormListID` WHERE a.`ID` in @ids;";

                var collection = cn.Query<FormList, IEnumerable<FormListItem>, FormList>(sql, (formList, formListItems) =>
                {
                    formList.Collection = formListItems;
                    return formList;
                }, new {ids = ids});

                return collection;
            }
        }
    }
}
