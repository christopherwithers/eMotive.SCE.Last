using System.Collections.Generic;
using eMotive.Repository.Objects.Users;

namespace eMotive.Repository.Interfaces
{
    //TODO: NEED TO CREATE A PROFILE CLASS WHICH PULLS IN USER GROUP AND OTHER NEEDED INFO!
    public interface IUserRepository
    {
        User New();
        User Fetch(int _id);
        User Fetch(string _value, UserField _field);

        string FetUserNotes(string _username);
        bool SaveUserNotes(string _username, string _notes);

        IEnumerable<User> FetchAll();
        IEnumerable<User> Fetch(IEnumerable<int> _ids);
        IEnumerable<User> Fetch(IEnumerable<string> _usernames);

        bool SaveApplicantData(IEnumerable<ApplicantData> _applicantData);
        IDictionary<string, List<ApplicantData>> FetchApplicantData(IEnumerable<string> _username);
        IEnumerable<ApplicantData> FetchApplicantData(string _username);

       // CreateUser Create(User _user);

       /* void AddUserToRoles(int _id, IEnumerable<int> _ids);
        void RemoveUserFromRoles(int _userId, IEnumerable<int> _ids);
        IEnumerable<int> FindUsersInRole(int _userId, int _id);*/
        
        bool Create(User _user, out int _id);
        bool Update(User _user);
        bool Delete(User _user);
        
        bool ValidateUser(string _username, string _password);

        bool SavePassword(int _id, string _salt, string _password);
        string GetSalt(string _username);

        Profile FetchProfile(string _username);

        SCEData FetchSceData(int _id);
        bool Create(SCEData _sce);
        bool Update(SCEData _sce);
        IEnumerable<SCEData> FetchAllSceData();

        bool UserHasWithdrawn(int userId);
        bool WithdrawUser(int userId);

        
    }
}
