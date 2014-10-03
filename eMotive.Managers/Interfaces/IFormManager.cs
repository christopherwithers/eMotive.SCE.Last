using System.Collections.Generic;
using eMotive.Models.Objects.Forms;

namespace eMotive.Managers.Interfaces
{
    public interface IFormManager
    {
        FormList NewFormList();
        bool CreateFormList(FormList formList, out int id);
        bool UpdateFormList(FormList formList);
        bool DeleteFormList(FormList formList);

        FormList FetchFormList(int id);
        FormList FetchFormList(string name);

        IEnumerable<FormList> FetchFormList();
        IEnumerable<FormList> FetchFormList(IEnumerable<int> ids);
    }
}
