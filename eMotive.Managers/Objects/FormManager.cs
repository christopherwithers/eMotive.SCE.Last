using System.Collections.Generic;
using AutoMapper;
using eMotive.Managers.Interfaces;
using eMotive.Models.Objects.Forms;
using eMotive.Repository.Interfaces;
using eMotive.Services.Interfaces;
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
            throw new System.NotImplementedException();
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
