using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using eMotive.Managers.Interfaces;
using eMotive.Models.Objects.Forms;
using eMotive.Repository.Interfaces;
using eMotive.Services.Interfaces;
using Extensions;
using rForm = eMotive.Repository.Objects.Forms;

namespace eMotive.Managers.Objects
{
    public class FormManager : IFormManager
    {
        private readonly IFormRepository _formRepository;
         
        public FormManager(IFormRepository formRepository)
        {
            _formRepository = formRepository;
            AutoMapperManagerConfiguration.Configure();
        }

        public IMappingEngine Mapper { get; set; }
        public INotificationService NotificationService { get; set; }

        public FormList NewFormList()
        {
            return Mapper.Map<rForm.FormList, FormList>(_formRepository.NewFormList());
        }

        public bool CreateFormList(FormList formList, out int id)
        {
            id = -1;

            if (string.IsNullOrEmpty(formList.Name))
            {
                if (!formList.Collection.HasContent())
                {
                    NotificationService.AddIssue("The form list has no name.");
                    return false;
                }
            }

            var formCheck = _formRepository.FetchFormList(formList.Name);

            if (formCheck != null)
            {
                if (String.Equals(formList.Name, formCheck.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    NotificationService.AddIssue(string.Format("A drop down list named '{0}' already exists.", formList.Name));
                    return false;
                }
            }

            if (!formList.Collection.HasContent())
            {
                NotificationService.AddIssue("The form list has no items.");
                return false;
            }

            var duplicatesText = formList.Collection.GroupBy(n => n.Text).SelectMany(g => g.Skip(1));

            if (duplicatesText.HasContent())
            {
                NotificationService.AddIssue(
                    string.Format("Drop down list item text must be unique. Duplications detected: '{0}'",
                        string.Join(",", duplicatesText.Select(n => n.Text))));
                return false;
            }

            var duplicateValues = formList.Collection.GroupBy(n => n.Text).SelectMany(g => g.Skip(1));

            if (duplicateValues.HasContent())
            {
                NotificationService.AddIssue(
                    string.Format("Drop down list item values must be unique. Duplications detected: '{0}'",
                        string.Join(",", duplicateValues.Select(n => n.Value))));
                return false;
            }

            formList.Collection = formList.Collection.GroupBy(x => x.Text).Select(y => y.First()); //todo: need to remove empty 

            if (_formRepository.CreateFormList(Mapper.Map<FormList, rForm.FormList>(formList), out id))
            {
                return true;
            }

            return false;
        }

        public bool UpdateFormList(FormList formList)
        {
            throw new System.NotImplementedException();
        }

        public bool DeleteFormList(FormList formList)
        {
            throw new System.NotImplementedException();
        }

        public FormList FetchFormList(int id)
        {
            return Mapper.Map<rForm.FormList, FormList>(_formRepository.FetchFormList(id));
        }

        public FormList FetchFormList(string name)
        {
            return Mapper.Map<rForm.FormList, FormList>(_formRepository.FetchFormList(name));
        }

        public IEnumerable<FormList> FetchFormList()
        {
            return Mapper.Map<IEnumerable<rForm.FormList>, IEnumerable<FormList>>(_formRepository.FetchFormList());
        }

        public IEnumerable<FormList> FetchFormList(IEnumerable<int> ids)
        {
            return Mapper.Map<IEnumerable<rForm.FormList>, IEnumerable<FormList>>(_formRepository.FetchFormList(ids));
        }
    }
}
