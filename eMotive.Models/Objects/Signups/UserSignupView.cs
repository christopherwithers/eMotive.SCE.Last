using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace eMotive.Models.Objects.Signups
{
    public class UserSignupView
    {
        public string HeaderText { get; set; }
        public string FooterText { get; set; }
        public bool SignedUp { get; set; }
        public int SignupID { get; set; }
        
        public ICollection<SignupState> SignupInformation { get; set; }


        public IDictionary<string, List<SignupState>> GetSignupsByGroup()
        {
            if (!SignupInformation.HasContent())
                return null;

            return SignupInformation.GroupBy(m => m.Group.Name).ToDictionary(k => k.Key, v => v.ToList());
        }

        public string GetDescription(string _key)
        {
            return SignupInformation.First(n => n.Group.Name == _key).Group.Description;
        }
    }
}
