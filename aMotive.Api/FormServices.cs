using System.Collections.Generic;
using eMotive.Managers.Interfaces;
using eMotive.Models.Objects.Forms;
using eMotive.Services.Interfaces;
using Extensions;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;


namespace eMotive.Api
{
    [Route("/Forms/FormList/New", "GET")]
    public class NewFormList
    {
    }

    [Route("/Forms/FormList")]
    [Route("/Forms/FormList/{Ids}")]
    public class GetFormLists
    {
        public int[] Ids { get; set; }
    }

    [Route("/Forms/FormList", "DELETE")]
    public class DeleteFormList
    {
        public int Id { get; set; }
    }

    [Route("/Forms/FormList", "POST")]
    [Route("/Forms/FormList", "PUT")]
    public class SaveFormList
    {
        public FormList formList { get; set; }
    }

    public class FormService : Service
    {
        private readonly IFormManager _formManager;
        private readonly INotificationService _notificationService;

        public FormService(IFormManager formManager, INotificationService notificationService)
        {
            _formManager = formManager;
            _notificationService = notificationService;
        }

        public INotificationService NotificationService { get; set; }

        public object Get(NewFormList request)
        {
            return new ServiceResult<FormList>
            {
                Success = true,
                Result = _formManager.NewFormList(),
                Errors = new string[] { }
            };
        }

        public object Get(GetFormLists request)
        {
            var result = request.Ids.IsEmpty()
                ? _formManager.FetchFormList()
                : _formManager.FetchFormList(request.Ids);

            var success = result.HasContent();

            var issues = _notificationService.FetchIssues(); //TODO: how to deal with errors when going directly into the api?? perhaps organise messages better?

            return new ServiceResult<IEnumerable<FormList>>
            {
                Success = success,
                Result = result,
                Errors = issues
            };

        }

    }
}
