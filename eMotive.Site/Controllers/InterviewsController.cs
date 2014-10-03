using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using eMotive.Managers.Interfaces;
using eMotive.Services.Interfaces;
using ServiceStack.Mvc;

namespace eMotive.SCE.Controllers
{
    [Common.ActionFilters.Authorize(Roles = "Interviewer,Applicant")]
    public class InterviewsController : ServiceStackController
    {
        private readonly ISessionManager signupManager;
        private readonly IPartialPageManager pageManager;
        private readonly IUserManager userManager;

        public InterviewsController(ISessionManager _signupManager, IPartialPageManager _pageManager, IUserManager _userManager)
        {
            signupManager = _signupManager;
            pageManager = _pageManager;
            userManager = _userManager;
        }

        //[Inject]
        public IeMotiveConfigurationService ConfigurationService { get; set; }
       // [Inject]
        public INotificationService NotificationService { get; set; }

       /* public ActionResult Disability()
        {
            var signups = signupManager.FetchSignupInformation(User.Identity.Name);

            var pageText = pageManager.FetchPartials(new[] { "Disability-Session-List-header", "Disability-Session-List-Footer" }).ToDictionary(k => k.Key, v => v.Text);
            signups.HeaderText = pageText["Disability-Session-List-header"];
            signups.FooterText = pageText["Disability-Session-List-Footer"];

       //     signups.HeaderText = pageManager.Fetch("Disability-Session-List-header").Text;
            
            return View(signups);
        }*/

        public ActionResult Test()
        {
            return View();
        }

        public ActionResult Signups()
        {
            var signups = signupManager.FetchSignupInformation(User.Identity.Name);
            var user = userManager.Fetch(User.Identity.Name);

            Dictionary<string, string> pageText = null;

            if (user.Roles.Any(n => n.Name == "Applicant"))
            {
                pageText = pageManager.FetchPartials(new[] { "Applicant-Session-List-Header", "Applicant-Session-List-Footer" }).ToDictionary(k => k.Key, v => v.Text);
                signups.HeaderText = pageText["Applicant-Session-List-Header"];
                signups.FooterText = pageText["Applicant-Session-List-Footer"];
            }
            else
            {
                pageText= pageManager.FetchPartials(new[] { "Session-List-header", "Session-List-Footer" }).ToDictionary(k => k.Key, v => v.Text);
                signups.HeaderText = pageText["Session-List-header"];
                signups.FooterText = pageText["Session-List-Footer"];
            }


            return View(signups);
        }

        public ActionResult Withdraw()
        {
            return View();
        }

        public ActionResult TestPage()
        {
            var signups = signupManager.FetchSignupInformation(User.Identity.Name);
            var user = userManager.Fetch(User.Identity.Name);

            Dictionary<string, string> pageText = null;

            if (user.Roles.Any(n => n.Name == "Applicant"))
            {
                pageText = pageManager.FetchPartials(new[] {"Session-List-header", "Session-List-Footer"}).ToDictionary(k => k.Key, v => v.Text);
                signups.HeaderText = pageText["Session-List-header"];
                signups.FooterText = pageText["Session-List-Footer"];
            }
            else
            {
                pageText = pageManager.FetchPartials(new[] { "Session-List-header", "Session-List-Footer" }).ToDictionary(k => k.Key, v => v.Text);
                signups.HeaderText = pageText["Session-List-header"];
                signups.FooterText = pageText["Session-List-Footer"];
            }

            return View(signups);
        }

        public ActionResult Slots(int? id)
        {
            var slots = signupManager.FetchSlotInformation(id.HasValue ? id.Value : -1, User.Identity.Name);
            var user = userManager.Fetch(User.Identity.Name);

            Dictionary<string, string> pageText;

            if (user.Roles.Any(n => n.Name == "Applicant"))
            {
                pageText = pageManager.FetchPartials(new[] { "Applicant-Interview-Date-Page", "Applicant-Interview-Date-Page-Footer" }).ToDictionary(k => k.Key, v => v.Text);
                slots.HeaderText = pageText["Applicant-Interview-Date-Page"];
                slots.FooterText = pageText["Applicant-Interview-Date-Page-Footer"];
            }
            else
            {
                pageText = pageManager.FetchPartials(new[] { "Interview-Date-Page", "Interview-Date-Page-Footer" }).ToDictionary(k => k.Key, v => v.Text);
                slots.HeaderText = pageText["Interview-Date-Page"];
                slots.FooterText = pageText["Interview-Date-Page-Footer"];
            }

            if (slots != null)
            {
                var replacements = new Dictionary<string, string>(4)
                {
                    {"#interviewdate#", slots.Date.ToString("dddd d MMMM yyyy")},
                    {"#description#", slots.Description},
                    {"#group#", slots.Group.Name}
                };
                //Disability-Interview-Date-Page
                var sbHead = new StringBuilder(slots.HeaderText);
                var sbFoot = new StringBuilder(slots.FooterText);

                foreach (var replacment in replacements)
                {
                    sbHead.Replace(replacment.Key, replacment.Value);
                    sbFoot.Replace(replacment.Key, replacment.Value);
                }

                slots.HeaderText = sbHead.ToString();
                slots.FooterText = sbFoot.ToString();
                /*slots.HeaderText = sb.ToString();
                slots.FooterText = pageManager.Fetch("Interview-Date-Page-Footer").Text;*/
                slots.LoggedInUser = User.Identity.Name ?? string.Empty;
            }

            return View(slots);
        }

       /* [AjaxOnly]
        public CustomJsonResult SignupToSlot(int idSignup, int idSlot)
        {
            if (signupManager.SignupToSlot(idSignup, idSlot, User.Identity.Name))
            {
                var signup = signupManager.Fetch(idSignup);
                var slot = signup.Slots.Single(n => n.ID == idSlot);

                ApplicantSignupPush(signup.ID, signup.Slots.Sum(n => n.TotalPlacesAvailable),
                    signup.Slots.Sum(n => n.ApplicantsSignedUp.HasContent() ? n.TotalPlacesAvailable - n.ApplicantsSignedUp.Count() : n.TotalPlacesAvailable));

                ApplicantSlotPush(slot.ID, slot.TotalPlacesAvailable,
                    slot.ApplicantsSignedUp.HasContent() ? slot.TotalPlacesAvailable - slot.ApplicantsSignedUp.Count() : slot.TotalPlacesAvailable);

                return new CustomJsonResult
                    {
                        Data = new {success = true, message = "successfully signed up."}
                    };
            }

            var issues = NotificationService.FetchIssues();


                return new CustomJsonResult
                {
                    Data = new { success = false, message = issues }
                };
            
        }

        [AjaxOnly]
        public CustomJsonResult CancelSignupToSlot(int idSignup, int idSlot)
        {
            if (signupManager.CancelSignupToSlot(idSignup, idSlot, User.Identity.Name))
            {
                var signup = signupManager.Fetch(idSignup);
                var slot = signup.Slots.Single(n => n.ID == idSlot);

                ApplicantSignupPush(signup.ID, signup.Slots.Sum(n => n.TotalPlacesAvailable),
                    signup.Slots.Sum(n => n.ApplicantsSignedUp.HasContent() ? n.TotalPlacesAvailable - n.ApplicantsSignedUp.Count() : n.TotalPlacesAvailable));

                ApplicantSlotPush(slot.ID, slot.TotalPlacesAvailable,
                    slot.ApplicantsSignedUp.HasContent() ? slot.TotalPlacesAvailable - slot.ApplicantsSignedUp.Count() : slot.TotalPlacesAvailable);

                return new CustomJsonResult
                {
                    Data = new { success = true, message = "successfully cancelled appointment." }
                };
            }

            return new CustomJsonResult
            {
                Data = new { success = false, message = "An error occurred. The signup could not be cancelled." }
            };
        }*/
        /*
        private void ApplicantSignupPush(int _signupID, int _totalPlaces, int _remainingPlaces)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<MMIHub>();

            hubContext.Clients.Group("SignupSelection").placesChanged(new
            {
                SignUpId = _signupID,
                TotalPlaces = _totalPlaces,
                PlacesAvailable = _remainingPlaces
            });
        }

        private void ApplicantSlotPush(int _slotID, int _totalPlaces, int _remainingPlaces)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<MMIHub>();

            hubContext.Clients.Group("SignupSelection").slotChanged(new
            {
                SlotId = _slotID,
                TotalPlaces = _totalPlaces,
                PlacesAvailable = _remainingPlaces
            });
        }*/

    }
}
