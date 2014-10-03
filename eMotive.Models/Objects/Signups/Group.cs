using eMotive.Models.Validation.Sessions;
using ServiceStack.FluentValidation.Attributes;

namespace eMotive.Models.Objects.Signups
{
    [Validator(typeof(GroupValidator))]
    public class Group
    {
        public int ID { get; set; }
        public string Name { get; set; }
      //  public bool DisabilitySignups { get; set; }
        public bool AllowMultipleSignups { get; set; }
        public string Description { get; set; }
    }
}
