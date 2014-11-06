using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DDay.iCal;
using eMotive.Managers.Interfaces;
using eMotive.SCE.Common;
using eMotive.SCE.Common.ActionFilters;
using eMotive.SCE.Common.Providers;
using eMotive.Models.Objects.Account;
using eMotive.Models.Objects.Certificates;
using eMotive.Models.Objects.Search;
using eMotive.Models.Objects.Signups;
using eMotive.Models.Objects.StatusPages;
using eMotive.Models.Objects.Users;
using eMotive.Services.Interfaces;
using Extensions;
//using Ninject;
using Novacode;
using Rotativa;
using ServiceStack.Mvc;

namespace eMotive.SCE.Areas.Admin.Controllers
{
    //http://stackoverflow.com/questions/11461142/parse-json-string-into-an-array

    public class UsersController : ServiceStackController
    {
        private readonly IUserManager userManager;
        private readonly IRoleManager roleManager;
        private readonly ISessionManager signupManager;
        private readonly IGroupManager groupManager;
        private readonly IFormManager formManager;

        private readonly Dictionary<string,string> searchFilter;
        public UsersController(IUserManager _userManager, IRoleManager _roleManager, ISessionManager _signupManager, IGroupManager _groupManager, IFormManager _formManager)
        {
            userManager = _userManager;
            roleManager = _roleManager;
            signupManager = _signupManager;

            groupManager = _groupManager;
            formManager = _formManager;

            searchFilter = new Dictionary<string, string> { { "Type", "User" } };
        }

       // [Inject]
        public IEmailService EmailService { get; set; }

       // [Inject]
        public IeMotiveConfigurationService configurationService { get; set; }

       // [Inject]
        public INotificationService NotificationService { get; set; }

        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult Index(UserSearch userSearch)
        {
            if (string.IsNullOrEmpty(userSearch.SortBy))
            {
                userSearch.SortBy = "Surname";
                userSearch.OrderBy = SortDirection.DESC;
            }

            ViewBag.LoggedInUser = userManager.Fetch(User.Identity.Name);

            if (!string.IsNullOrEmpty(userSearch.SelectedRoleFilter) && userSearch.SelectedRoleFilter != "0")
                searchFilter.Add("RoleID", userSearch.SelectedRoleFilter);

            userSearch.Filter = searchFilter;

            //todo: PAGING BREAKS WITH FILTER, FIX THIS!!!

            var searchItem = userManager.DoSearch(userSearch);

            if (searchItem.Items.HasContent())
            {
                userSearch.Page = searchItem.CurrentPage;
                userSearch.NumberOfResults = searchItem.NumberOfResults;
                userSearch.Users = userManager.FetchRecordsFromSearch(searchItem); 
            }
            else
            {
                userSearch = new UserSearch();
            }
            var roles = roleManager.FetchAll();

            if (roles.HasContent())
            {
                var roleFilter = new Collection<KeyValuePair<string, string>> { new KeyValuePair<string, string>("0", string.Empty)};

                roleFilter.AddRange(roles.Select(n => new KeyValuePair<string, string>(n.ID.ToString(CultureInfo.InvariantCulture), n.Name)));
                userSearch.RoleFilter = roleFilter;

                //roles.Select(n => new KeyValuePair<string, string>(n.ID.ToString(CultureInfo.InvariantCulture), n.Name));
                //   userSearch.SelectedRoleFilter = "2";
            }

            return View(userSearch);
        }

        //                        

      //  [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public CustomJsonResult FetchSCETrainingInformation(string username)
        {
            var trainingSessions = signupManager.FetchAllTraining();

            var trainingInfo = new Collection<SCETrainingInformation>();
            var issues = NotificationService.FetchIssues();

            foreach (var session in trainingSessions)
            {
                trainingInfo.Add(new SCETrainingInformation
                {
                    Date = session.Date.ToString("D"),
                    ID = session.ID,
                    AttendedThisSession = false
                });
            }

            return new CustomJsonResult
            {
                Data = new { success = !issues.HasContent(), message = issues, results = trainingInfo }
            };
        }

       // [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public CustomJsonResult SessionAttendanceCertificate(string username, int trainingSession)
        {
            var pdf = new ActionAsPdf("SessionAttendanceCertificateLayout", new { username = username, id = trainingSession }) { FileName = "Certificate.pdf" };

            bool emailSent = false;

            if (pdf != null)
            {
                var user = userManager.Fetch(username);
                var sceData = userManager.FetchSCEData(user.ID);
                var replacements = new Dictionary<string, string>(4)
                    {
                        {"#title#", sceData.Title},
                        {"#forename#", user.Forename},
                        {"#surname#", user.Surname},
                        {"#username#", user.Username},
                        {"#sitename#", configurationService.SiteName()},
                        {"#siteurl#", configurationService.SiteURL()}
                    };

                const string key = "TrainingCertificate";

                var pdfBin = pdf.BuildPdf(ControllerContext);

                emailSent = EmailService.SendMail(key, user.Email, replacements, pdfBin, "TrainingCertificate.pdf", "pdf");
                if (emailSent)
                {
                    EmailService.SendEmailLog(key, user.Username);
                }

            }

            var issues = NotificationService.FetchIssues();

            return new CustomJsonResult
            {
                Data = new { success = emailSent, message = issues, results = "" }
            };
        }

       // [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult SessionAttendanceCertificateLayout(string username, int id)
        {
            var user = userManager.Fetch(username);
            if (signupManager.RegisterAttendanceToSession(new SessionAttendance {SessionID = id, UserID = user.ID}))
            {
                var sceData = userManager.FetchSCEData(user.ID);

                //    var sessions = signupManager.FetchSignupInformation(username);

                var session = signupManager.Fetch(id);

                var RCPCode = signupManager.FetchRCPActivityCode(id);

                var formData = new SCEFormData();
                var certificate = new SCECertificate
                {
                    Title = sceData.Title,
                    Forename = user.Forename,
                    Surname = user.Surname,
                    DateOfCourse = string.Format(new MyCustomDateProvider(), "{0}", session.Date),
                    Grade = formData.Grades[sceData.Grade],
                    RCPNumber = RCPCode.ToString(CultureInfo.InvariantCulture),
                    Trust = formData.Trusts[sceData.Trust],
                    DateSigned = string.Format(new MyCustomDateProvider(), "{0}", DateTime.Now)
                };

                return View(certificate);
            }
            return null;
        }

        [HttpGet]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult CreateInterviewer()
        {
            var allGroups = groupManager.FetchGroups();
            var dropDowns = new SCEFormData();
            ViewBag.GradesDropDown = dropDowns.Grades;
            ViewBag.TrustsDropDown = dropDowns.Trusts;
            ViewBag.GroupDropDown = allGroups;

            var sce = new SCEData { AllGroups = allGroups, Enabled = true};

            return View(sce);
        }

        [HttpPost]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult CreateInterviewer(SCEData sce)
        {
            if (ModelState.IsValid)
            {
                int id;

                if (userManager.CreateSCE(sce, out id))
                {
                    var successView = new SuccessView
                    {
                        Message = "The new User was successfully created.",
                        Links = new[]
                            {
                                new SuccessView.Link {Text = string.Format("Edit {0}", sce.Username), URL = @Url.Action("EditUser", "Users", new {username = sce.Username})},
                                new SuccessView.Link {Text = "Return to Users Home", URL = @Url.Action("Index", "Users")}
                            }
                    };

                    TempData["SuccessView"] = successView;

                    return RedirectToAction("Success", "Home", new { area = "Admin" });
                }

                var errors = NotificationService.FetchIssues();
                if (errors.HasContent())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("error", error);
                    }
                }
            }
            var allGroups = groupManager.FetchGroups();
            var dropDowns = new SCEFormData();
            ViewBag.GradesDropDown = dropDowns.Grades;
            ViewBag.TrustsDropDown = dropDowns.Trusts;
            ViewBag.GroupDropDown = allGroups;
            sce.AllGroups = allGroups;

            return View(sce);
        }

        [HttpGet]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult Create()
        {
            var user = new UserWithRoles();
            ViewBag.Roles = new SelectList(roleManager.FetchAll(), "Id", "Name");

            return View(user);
        }

        [HttpPost]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult Create(UserWithRoles user)
        {
            var roles = roleManager.FetchAll();
            if (ModelState.IsValid)
            {
                var role = roles.Single(n => n.ID.ToString() == user.SelectedRole);
                user.Roles = new[] { role };

                int id;

                if (userManager.Create(user, out id))
                {
                    var successView = new SuccessView
                    {
                        Message = "The new User was successfully created.",
                        Links = new[]
                            {
                                new SuccessView.Link {Text = string.Format("Edit {0}", user.Username), URL = @Url.Action("Edit", "Users", new {username = user.Username})},
                                new SuccessView.Link {Text = "Return to Users Home", URL = @Url.Action("Index", "Users")}
                            }
                    };

                    TempData["SuccessView"] = successView;

                    return RedirectToAction("Success", "Home", new { area = "Admin" });
                }

                var errors = NotificationService.FetchIssues();
                if (errors.HasContent())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("error", error);
                    }
                }
            }

            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(user);
        }

        [HttpGet]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult EditUser(string username)
        {
            var user = userManager.Fetch(username);
            var profile = userManager.FetchProfile(username);
            var sce = userManager.FetchSCEData(user.ID);

            sce.BelongsToGroups = profile.Groups.Select(n => n.ID.ToString(CultureInfo.InvariantCulture)).ToArray();
            sce.Username = user.Username;
            sce.Forename = user.Forename;
            sce.Surname = user.Surname;
            sce.Email = user.Email;
            sce.Enabled = user.Enabled;
            var allGroups = groupManager.FetchGroups();

            ViewBag.GradesDropDown = formManager.FetchFormList("Grade").Collection.ToDictionary(k => k.Value, v => v.Text);
            ViewBag.TrustsDropDown = formManager.FetchFormList("Trusts").Collection.ToDictionary(k => k.Value, v => v.Text);
            ViewBag.GroupDropDown = allGroups;

            sce.AllGroups = allGroups;
            ViewBag.LoggedInUser = userManager.Fetch(User.Identity.Name);

            return View(sce);
        }

        [HttpPost]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult EditUser(SCEData sce)
        {
            if (ModelState.IsValid)
            {
                if (userManager.UpdateSCE(sce))
                {
                    var successView = new SuccessView
                    {
                        Message = "The User was successfully updated.",
                        Links = new[]
                            {
                                new SuccessView.Link {Text = string.Format("Edit {0}", sce.Username), URL = @Url.Action("EditUser", "Users", new {username = sce.Username})},
                                new SuccessView.Link {Text = "Return to Users Home", URL = @Url.Action("Index", "Users")}
                            }
                    };

                    TempData["SuccessView"] = successView;

                    return RedirectToAction("Success", "Home", new { area = "Admin" });
                }

                var errors = NotificationService.FetchIssues();
                if (errors.HasContent())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("error", error);
                    }
                }
            }
            var allGroups = groupManager.FetchGroups();

            ViewBag.GradesDropDown = formManager.FetchFormList("Grade").Collection.ToDictionary(k => k.Value, v => v.Text);
            ViewBag.TrustsDropDown = formManager.FetchFormList("Trusts").Collection.ToDictionary(k => k.Value, v => v.Text);
            ViewBag.GroupDropDown = allGroups;
            sce.AllGroups = allGroups;

            return View(sce);
        }

        [HttpGet]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult Edit(string username)
        {
            var user = userManager.Fetch(username);

            var userWithRoles = new UserWithRoles
                {
                    Username = user.Username,
                    Archived = user.Archived,
                    Email = user.Email,
                    Enabled = user.Enabled,
                    Forename = user.Forename,
                    Surname = user.Surname,
                    ID = user.ID,
                    Roles = user.Roles,
                    SelectedRole = user.Roles.HasContent() ? user.Roles.FirstOrDefault().ID.ToString(CultureInfo.InvariantCulture) : "0"
                };

            ViewBag.Roles = new SelectList(roleManager.FetchAll(), "Id", "Name");//userWithRoles.AllRoles.Select(m => new SelectListItem {Text = m.Name, Value = m.ID.ToString()});

            return View(userWithRoles);
        }

        [HttpPost]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult Edit(UserWithRoles user)
        {
            var roles = roleManager.FetchAll();
            if (ModelState.IsValid)
            {
                var role = roles.Single(n => n.ID.ToString() == user.SelectedRole);
                user.Roles = new[] { role };

                if (userManager.Update(user))
                {
                    var successView = new SuccessView
                    {
                        Message = "The User was successfully updated.",
                        Links = new[]
                            {
                                new SuccessView.Link {Text = string.Format("Edit {0}", user.Username), URL = @Url.Action("Edit", "Users", new {username = user.Username})},
                                new SuccessView.Link {Text = "Return to Users Home", URL = @Url.Action("Index", "Users")}
                            }
                    };

                    TempData["SuccessView"] = successView;

                    return RedirectToAction("Success", "Home", new { area = "Admin" });
                }

                var errors = NotificationService.FetchIssues();
                if (errors.HasContent())
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError("error", error);
                    }
                }
            }

            return View(user);
        }

        [AjaxOnly]
        public CustomJsonResult DeleteUser(string username)
        {
            var user = userManager.Fetch(username);

            var signups = signupManager.FetchSignupInformation(username);

            var success = false;
            var errors = new string[] { };

            var userHasSignups = false;
            int signupCount = 0;

            if (signups.SignupInformation.HasContent())
            {
                signupCount = signups.SignupInformation.Count(n => n.SignedUp);
                userHasSignups = signups.SignupInformation.Count(n => n.SignedUp) > 0;
            }

            if (!userHasSignups)
            {
                success = userManager.Delete(user);
                var returnedErrors = NotificationService.FetchIssues();
                if (returnedErrors.HasContent())
                    errors = returnedErrors.ToArray();
            }
            else
            {
                errors = new[] { string.Format("{0} {1} is currently signed up to {2} {3}. Please remove this user from all of their sessions before deleting.", user.Forename, user.Surname, signupCount, signupCount > 1 ? "sessions" : "session") };
            }


            return new CustomJsonResult
            {
                Data = new { success = success, message = errors, results = string.Empty }
            };
        }

        public CustomJsonResult FetchUserNotes(string username)
        {
            var notes = userManager.FetchUserNotes(username);

            var success = !string.IsNullOrEmpty(notes);

            var errors = success ? NotificationService.FetchIssues() : new string[] { };

            return new CustomJsonResult
            {
                Data = new { success = success, message = errors, results = notes }
            };
        }

        public CustomJsonResult SaveUserNotes(string username, string notes)
        {
            var success = userManager.SaveUserNotes(username, notes);

            var errors = success ? NotificationService.FetchIssues() : new string[] { };

            return new CustomJsonResult
            {
                Data = new { success = success, message = errors, results = string.Empty }
            };
        }

        public FileStreamResult GenerateApplicantLetter(string username)
        {
            var user = userManager.Fetch(username);

            if (user == null)
                throw new HttpException(404, "The requested letter could not be generated.");

            var docName = string.Format("{0}_{1}_letter.docx", user.Forename, user.Surname);

            using (var document = DocX.Create(docName))
            {

                var p = document.InsertParagraph();
                p.Append(DateTime.Now.ToString("dddd d MMMM yyyy")).FontSize(8).AppendLine().AppendLine()
                    .Append(string.Format("Dear {0} {1}", user.Forename, user.Surname)).AppendLine().AppendLine()
                    .Append("INVITATION FOR MEDICINE INTERVIEW").Bold().FontSize(14).AppendLine().AppendLine()
                    .Append("Thank you for your application to study Medicine and Surgery at the University of Birmingham. I am pleased to invite you for an interview.").AppendLine().AppendLine()
                    .Append("Please log in to our web based interview sign up system using the link and details below. Once you have selected your chosen date and time you will receive further information by email.").AppendLine().AppendLine()
                    .Append("Username:").AppendLine().AppendLine()
                    .Append("Password:").AppendLine().AppendLine()
                    .Append("http://mymds.bham.ac.uk/MMIApplicants").UnderlineStyle(UnderlineStyle.singleLine).AppendLine().AppendLine()
                    .Append("Yours sincerely").AppendLine();

                var img = document.AddImage(Server.MapPath("~/Content/images/Signature.jpg"));
                var pic = img.CreatePicture();
                pic.Width = 100;
                pic.Height = 35;
                p = document.InsertParagraph();
                p.InsertPicture(pic);
                p.AppendLine().AppendLine().Append("Dr Austen Spruce").AppendLine()
                .Append("Medicine Admissions Tutor").AppendLine()
                .Append("Telephone: 0121 414 9044 / 4046").FontSize(8).AppendLine()
                .Append("Email: medicineinterviews@contacts.bham.ac.uk").FontSize(8);


                var ms = new MemoryStream();
                document.SaveAs(ms);
                ms.Position = 0;

                var file = new FileStreamResult(ms, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                {
                    FileDownloadName = docName
                };

                return file;

            }
        }

    }
}
