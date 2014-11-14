using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using eMotive.Managers.Interfaces;
using eMotive.Models.Objects.Account;
using eMotive.Models.Objects.Reports.Users;
using eMotive.Models.Objects.Signups;
using eMotive.Services.Interfaces;
using Extensions;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ServiceStack.Mvc;

namespace eMotive.SCE.Areas.Admin.Controllers
{
    [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
    public class ReportsController : ServiceStackController
    {
        private readonly IReportService reportService;
        private readonly IDocumentManagerService documentManager;
        private readonly IGroupManager groupManager;
        private readonly ISessionManager signupManager;
        private readonly IUserManager userManager;
        private readonly INotificationService notificationService;
        private readonly IFormManager formManager;

        private readonly string CONTENT_TYPE;

        public ReportsController(IReportService _reportService, IDocumentManagerService _documentManager, ISessionManager _signupManager, IGroupManager _groupManager, IUserManager _userManager, INotificationService _notificationService, IFormManager _formManager)
        {
            reportService = _reportService;
            documentManager = _documentManager;
            signupManager = _signupManager;
            userManager = _userManager;
            notificationService = _notificationService;
            groupManager = _groupManager;
            formManager = _formManager;

            CONTENT_TYPE = documentManager.FetchMimeTypeForExtension("xlxs").Type;
        }

        public ActionResult Index()
        {
            var signupAdminView = new AdminSignupView
            {
                Signups = signupManager.FetchAllM()
            };

           // var sites = formManager.FetchFormList("Sites");

           // ViewBag.Sites = sites;

            return View(signupAdminView);
        }

        public FileStreamResult AllInterviewers()
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);

            var users = reportService.FetchAllInterviewers();

            if (users.HasContent())
            {
                using (var xlPackage = new ExcelPackage())
                {
                    const string REPORT_NAME = "All Interviewers Report";
                    var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);

                    int x = 1;
                    using (var r = worksheet.Cells["A1:S1"])
                    {
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                        r.Style.Font.Bold = true;
                        r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //18
                    int i = 0;

                    worksheet.Cells[x, ++i].Value = "Username";
                    worksheet.Cells[x, ++i].Value = "Title";
                    worksheet.Cells[x, ++i].Value = "Forename";
                    worksheet.Cells[x, ++i].Value = "Surname";
                    worksheet.Cells[x, ++i].Value = "Email";
                    worksheet.Cells[x, ++i].Value = "SecretaryEmail";
                    worksheet.Cells[x, ++i].Value = "OtherEmail";
                    worksheet.Cells[x, ++i].Value = "PhoneWork";
                    worksheet.Cells[x, ++i].Value = "PhoneMobile";
                    worksheet.Cells[x, ++i].Value = "PhoneOther";
                    worksheet.Cells[x, ++i].Value = "Trained";
                    worksheet.Cells[x, ++i].Value = "Enabled";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, ++i].Value = "Notes";
                    }

                    x++;

                    foreach (var user in users)
                    {
                        if (!user.Enabled)
                        {
                            using (var r = worksheet.Cells[string.Concat("A", x, ":S", x)])
                            {
                                r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 203, 203));
                            }
                        }

                        i = 0;
                        worksheet.Cells[x, ++i].Value = user.Username;
                        worksheet.Cells[x, ++i].Value = user.Title;
                        worksheet.Cells[x, ++i].Value = user.Forename;
                        worksheet.Cells[x, ++i].Value = user.Surname;
                        worksheet.Cells[x, ++i].Value = user.Email;
                        worksheet.Cells[x, ++i].Value = user.SecretaryEmail;
                        worksheet.Cells[x, ++i].Value = user.OtherEmail;
                        worksheet.Cells[x, ++i].Value = user.PhoneWork;
                        worksheet.Cells[x, ++i].Value = user.PhoneMobile;
                        worksheet.Cells[x, ++i].Value = user.PhoneOther;
                        worksheet.Cells[x, ++i].Value = user.Trained ? "Yes" : "No";
                        worksheet.Cells[x, ++i].Value = user.Enabled ? "Yes" : "No";

                        if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                        {
                            worksheet.Cells[x, ++i].Value = user.Notes;
                        }

                        x++;
                    }

                    return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE) { FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME) };

                }
            }

            return null;
        }

        public FileStreamResult AllObservers()
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);

            var users = reportService.FetchAllObservers();
          //  var groupsDict = groupManager.FetchGroups().ToDictionary(k => k.ID, v => v.Name);
            if (users.HasContent())
            {
                using (var xlPackage = new ExcelPackage())
                {
                    const string REPORT_NAME = "All Observers Report";
                    var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);

                    int x = 1;
                    using (var r = worksheet.Cells["A1:S1"])
                    {
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                        r.Style.Font.Bold = true;
                        r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //18
                    int i = 0;

                    worksheet.Cells[x, ++i].Value = "Username";
                    worksheet.Cells[x, ++i].Value = "Title";
                    worksheet.Cells[x, ++i].Value = "Forename";
                    worksheet.Cells[x, ++i].Value = "Surname";
                    worksheet.Cells[x, ++i].Value = "Email";
                    worksheet.Cells[x, ++i].Value = "SecretaryEmail";
                    worksheet.Cells[x, ++i].Value = "OtherEmail";
                    worksheet.Cells[x, ++i].Value = "PhoneWork";
                    worksheet.Cells[x, ++i].Value = "PhoneMobile";
                    worksheet.Cells[x, ++i].Value = "PhoneOther";
                    worksheet.Cells[x, ++i].Value = "Trained";
                    worksheet.Cells[x, ++i].Value = "Enabled";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, ++i].Value = "Notes";
                    }

                    x++;

                    foreach (var user in users)
                    {
                        if (!user.Enabled)
                        {
                            using (var r = worksheet.Cells[string.Concat("A", x, ":S", x)])
                            {
                                r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 203, 203));
                            }
                        }

                     //   var specialty = "Unknown";
                       // groupsDict.TryGetValue(user.MainSpecialty, out specialty);

                        i = 0;
                        worksheet.Cells[x, ++i].Value = user.Username;
                        worksheet.Cells[x, ++i].Value = user.Title;
                        worksheet.Cells[x, ++i].Value = user.Forename;
                        worksheet.Cells[x, ++i].Value = user.Surname;
                        worksheet.Cells[x, ++i].Value = user.Email;
                        worksheet.Cells[x, ++i].Value = user.SecretaryEmail;
                        worksheet.Cells[x, ++i].Value = user.OtherEmail;
                        worksheet.Cells[x, ++i].Value = user.PhoneWork;
                        worksheet.Cells[x, ++i].Value = user.PhoneMobile;
                        worksheet.Cells[x, ++i].Value = user.PhoneOther;
                        worksheet.Cells[x, ++i].Value = user.Trained ? "Yes" : "No";
                        worksheet.Cells[x, ++i].Value = user.Enabled ? "Yes" : "No";

                        if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                        {
                            worksheet.Cells[x, ++i].Value = user.Notes;
                        }

                        x++;
                    }

                    return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE) { FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME) };

                }
            }

            return null;
        }

        public FileStreamResult AllInterviewersAndObservers()
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);

            var users = reportService.FetchAllInterviewersAndObservers();
            //  var groupsDict = groupManager.FetchGroups().ToDictionary(k => k.ID, v => v.Name);
            if (users.HasContent())
            {
                using (var xlPackage = new ExcelPackage())
                {
                    const string REPORT_NAME = "All Interviewers and Observers Report";
                    var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);

                    int x = 1;
                    using (var r = worksheet.Cells["A1:S1"])
                    {
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                        r.Style.Font.Bold = true;
                        r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //18
                    var i = 0;

                    worksheet.Cells[x, ++i].Value = "Username";
                    worksheet.Cells[x, ++i].Value = "Groups";
                    worksheet.Cells[x, ++i].Value = "Title";
                    worksheet.Cells[x, ++i].Value = "Forename";
                    worksheet.Cells[x, ++i].Value = "Surname";
                    worksheet.Cells[x, ++i].Value = "Email";
                    worksheet.Cells[x, ++i].Value = "SecretaryEmail";
                    worksheet.Cells[x, ++i].Value = "OtherEmail";
                    worksheet.Cells[x, ++i].Value = "PhoneWork";
                    worksheet.Cells[x, ++i].Value = "PhoneMobile";
                    worksheet.Cells[x, ++i].Value = "PhoneOther";
                    worksheet.Cells[x, ++i].Value = "Trained";
                    worksheet.Cells[x, ++i].Value = "Enabled";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, ++i].Value = "Notes";
                    }

                    x++;

                    foreach (var user in users)
                    {
                        if (!user.Enabled)
                        {
                            using (var r = worksheet.Cells[string.Concat("A", x, ":S", x)])
                            {
                                r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 203, 203));
                            }
                        }

                        //   var specialty = "Unknown";
                        // groupsDict.TryGetValue(user.MainSpecialty, out specialty);

                        i = 0;
                        worksheet.Cells[x, ++i].Value = user.Username;
                        worksheet.Cells[x, ++i].Value = user.Groups;
                        worksheet.Cells[x, ++i].Value = user.Title;
                        worksheet.Cells[x, ++i].Value = user.Forename;
                        worksheet.Cells[x, ++i].Value = user.Surname;
                        worksheet.Cells[x, ++i].Value = user.Email;
                        worksheet.Cells[x, ++i].Value = user.SecretaryEmail;
                        worksheet.Cells[x, ++i].Value = user.OtherEmail;
                        worksheet.Cells[x, ++i].Value = user.PhoneWork;
                        worksheet.Cells[x, ++i].Value = user.PhoneMobile;
                        worksheet.Cells[x, ++i].Value = user.PhoneOther;
                        worksheet.Cells[x, ++i].Value = user.Trained ? "Yes" : "No";
                        worksheet.Cells[x, ++i].Value = user.Enabled ? "Yes" : "No";

                        if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                        {
                            worksheet.Cells[x, ++i].Value = user.Notes;
                        }

                        x++;
                    }

                    return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE) { FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME) };

                }
            }

            return null;
        }


        public FileStreamResult InterviewersNotSignedUp()
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);
            var users = reportService.FetchInterviewersNotSignedUp();

            if (!users.HasContent()) return null;


            using (var xlPackage = new ExcelPackage())
            {
                const string REPORT_NAME = "Interviewers not signed up Report";
                var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);

                int x = 1;
                using (var r = worksheet.Cells["A1:S1"])
                {
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                    r.Style.Font.Bold = true;
                    r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                var i = 0;

                worksheet.Cells[x, ++i].Value = "Username";
                worksheet.Cells[x, ++i].Value = "Title";
                worksheet.Cells[x, ++i].Value = "Forename";
                worksheet.Cells[x, ++i].Value = "Surname";
                worksheet.Cells[x, ++i].Value = "Email";
                worksheet.Cells[x, ++i].Value = "SecretaryEmail";
                worksheet.Cells[x, ++i].Value = "OtherEmail";
                worksheet.Cells[x, ++i].Value = "PhoneWork";
                worksheet.Cells[x, ++i].Value = "PhoneMobile";
                worksheet.Cells[x, ++i].Value = "PhoneOther";
                worksheet.Cells[x, ++i].Value = "Trained";
                worksheet.Cells[x, ++i].Value = "Enabled";

                if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                {
                    worksheet.Cells[x, ++i].Value = "Notes";
                }

                x++;

                foreach (var user in users)
                {
                    if (!user.Enabled)
                    {
                        using (var r = worksheet.Cells[string.Concat("A", x, ":S", x)])
                        {
                            r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 203, 203));
                        }
                    }

                    i = 0;
                    worksheet.Cells[x, ++i].Value = user.Username;
                    worksheet.Cells[x, ++i].Value = user.Title;
                    worksheet.Cells[x, ++i].Value = user.Forename;
                    worksheet.Cells[x, ++i].Value = user.Surname;
                    worksheet.Cells[x, ++i].Value = user.Email;
                    worksheet.Cells[x, ++i].Value = user.SecretaryEmail;
                    worksheet.Cells[x, ++i].Value = user.OtherEmail;
                    worksheet.Cells[x, ++i].Value = user.PhoneWork;
                    worksheet.Cells[x, ++i].Value = user.PhoneMobile;
                    worksheet.Cells[x, ++i].Value = user.PhoneOther;
                    worksheet.Cells[x, 13].Value = user.Trained ? "Yes" : "No";
                    worksheet.Cells[x, ++i].Value = user.Enabled ? "Yes" : "No";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, 15].Value = user.Notes;
                    }

                    x++;
                }

                return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE) { FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME) };

            }
        }

        public FileStreamResult ObserversNotSignedUp()
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);
            var users = reportService.FetchInterviewersNotSignedUp();

            if (!users.HasContent()) return null;


            using (var xlPackage = new ExcelPackage())
            {
                const string REPORT_NAME = "Observers not signed up Report";
                var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);

                int x = 1;
                using (var r = worksheet.Cells["A1:S1"])
                {
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                    r.Style.Font.Bold = true;
                    r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                var i = 0;

                worksheet.Cells[x, ++i].Value = "Username";
                worksheet.Cells[x, ++i].Value = "Title";
                worksheet.Cells[x, ++i].Value = "Forename";
                worksheet.Cells[x, ++i].Value = "Surname";
                worksheet.Cells[x, ++i].Value = "Email";
                worksheet.Cells[x, ++i].Value = "SecretaryEmail";
                worksheet.Cells[x, ++i].Value = "OtherEmail";
                worksheet.Cells[x, ++i].Value = "PhoneWork";
                worksheet.Cells[x, ++i].Value = "PhoneMobile";
                worksheet.Cells[x, ++i].Value = "PhoneOther";
                worksheet.Cells[x, ++i].Value = "Trained";
                worksheet.Cells[x, ++i].Value = "Enabled";

                if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                {
                    worksheet.Cells[x, ++i].Value = "Notes";
                }

                x++;

                foreach (var user in users)
                {
                    if (!user.Enabled)
                    {
                        using (var r = worksheet.Cells[string.Concat("A", x, ":S", x)])
                        {
                            r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 203, 203));
                        }
                    }

                    i = 0;
                    worksheet.Cells[x, ++i].Value = user.Username;
                    worksheet.Cells[x, ++i].Value = user.Title;
                    worksheet.Cells[x, ++i].Value = user.Forename;
                    worksheet.Cells[x, ++i].Value = user.Surname;
                    worksheet.Cells[x, ++i].Value = user.Email;
                    worksheet.Cells[x, ++i].Value = user.SecretaryEmail;
                    worksheet.Cells[x, ++i].Value = user.OtherEmail;
                    worksheet.Cells[x, ++i].Value = user.PhoneWork;
                    worksheet.Cells[x, ++i].Value = user.PhoneMobile;
                    worksheet.Cells[x, ++i].Value = user.PhoneOther;
                    worksheet.Cells[x, 13].Value = user.Trained ? "Yes" : "No";
                    worksheet.Cells[x, ++i].Value = user.Enabled ? "Yes" : "No";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, 15].Value = user.Notes;
                    }

                    x++;
                }

                return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE) { FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME) };

            }
        }

        public FileStreamResult InterviewersAndObserversNotSignedUp()
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);
            var users = reportService.FetchInterviewersAndObserversNotSignedUp();

            if (!users.HasContent()) return null;


            using (var xlPackage = new ExcelPackage())
            {
                const string REPORT_NAME = "Interviwers and Observers not signed up Report";
                var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);

                int x = 1;
                using (var r = worksheet.Cells["A1:S1"])
                {
                    r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                    r.Style.Font.Bold = true;
                    r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }
                var i = 0;

                worksheet.Cells[x, ++i].Value = "Username";
                worksheet.Cells[x, ++i].Value = "Groups";
                worksheet.Cells[x, ++i].Value = "Title";
                worksheet.Cells[x, ++i].Value = "Forename";
                worksheet.Cells[x, ++i].Value = "Surname";
                worksheet.Cells[x, ++i].Value = "Email";
                worksheet.Cells[x, ++i].Value = "SecretaryEmail";
                worksheet.Cells[x, ++i].Value = "OtherEmail";
                worksheet.Cells[x, ++i].Value = "PhoneWork";
                worksheet.Cells[x, ++i].Value = "PhoneMobile";
                worksheet.Cells[x, ++i].Value = "PhoneOther";
                worksheet.Cells[x, ++i].Value = "Trained";
                worksheet.Cells[x, ++i].Value = "Enabled";

                if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                {
                    worksheet.Cells[x, ++i].Value = "Notes";
                }

                x++;

                foreach (var user in users)
                {
                    if (!user.Enabled)
                    {
                        using (var r = worksheet.Cells[string.Concat("A", x, ":S", x)])
                        {
                            r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 203, 203));
                        }
                    }

                    i = 0;
                    worksheet.Cells[x, ++i].Value = user.Username;
                    worksheet.Cells[x, ++i].Value = user.Groups;
                    worksheet.Cells[x, ++i].Value = user.Title;
                    worksheet.Cells[x, ++i].Value = user.Forename;
                    worksheet.Cells[x, ++i].Value = user.Surname;
                    worksheet.Cells[x, ++i].Value = user.Email;
                    worksheet.Cells[x, ++i].Value = user.SecretaryEmail;
                    worksheet.Cells[x, ++i].Value = user.OtherEmail;
                    worksheet.Cells[x, ++i].Value = user.PhoneWork;
                    worksheet.Cells[x, ++i].Value = user.PhoneMobile;
                    worksheet.Cells[x, ++i].Value = user.PhoneOther;
                    worksheet.Cells[x, 13].Value = user.Trained ? "Yes" : "No";
                    worksheet.Cells[x, ++i].Value = user.Enabled ? "Yes" : "No";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, 15].Value = user.Notes;
                    }

                    x++;
                }

                return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE) { FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME) };

            }
        }

        public FileStreamResult NotSignedUp()
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);
            var users = reportService.FetchUsersNotSignedUp();
            var groupsDict = groupManager.FetchGroups().ToDictionary(k => k.ID, v => v.Name);
            if (users.HasContent())
            {
                using (var xlPackage = new ExcelPackage())
                {
                    const string REPORT_NAME = "Inactive SCE Report";
                    var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);

                    int x = 1;
                    using (var r = worksheet.Cells["A1:S1"])
                    {
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                        r.Style.Font.Bold = true;
                        r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    //18
                    worksheet.Cells[x, 1].Value = "Username";
                    worksheet.Cells[x, 2].Value = "Title";
                    worksheet.Cells[x, 3].Value = "Forename";
                    worksheet.Cells[x, 4].Value = "Surname";
                    worksheet.Cells[x, 5].Value = "GMCNumber";
                    worksheet.Cells[x, 6].Value = "Specialty";
                    worksheet.Cells[x, 7].Value = "Email";
                    worksheet.Cells[x, 8].Value = "SecretaryEmail";
                    worksheet.Cells[x, 9].Value = "OtherEmail";
                    worksheet.Cells[x, 10].Value = "PhoneWork";
                    worksheet.Cells[x, 11].Value = "PhoneMobile";
                    worksheet.Cells[x, 12].Value = "PhoneOther";
                    worksheet.Cells[x, 13].Value = "Trained";
                    worksheet.Cells[x, 14].Value = "Enabled";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, 15].Value = "Notes";
                    }

                    x++;

                    foreach (var user in users)
                    {
                        if (!user.Enabled)
                        {
                            using (var r = worksheet.Cells[string.Concat("A", x, ":S", x)])
                            {
                                r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 203, 203));
                            }
                        }

                        var specialty = "Unknown";
                        groupsDict.TryGetValue(user.MainSpecialty, out specialty);

                        worksheet.Cells[x, 1].Value = user.Username;
                        worksheet.Cells[x, 2].Value = user.Title;
                        worksheet.Cells[x, 3].Value = user.Forename;
                        worksheet.Cells[x, 4].Value = user.Surname;
                        worksheet.Cells[x, 5].Value = user.GMCNumber;
                        worksheet.Cells[x, 6].Value = specialty;
                        worksheet.Cells[x, 7].Value = user.Email;
                        worksheet.Cells[x, 8].Value = user.SecretaryEmail;
                        worksheet.Cells[x, 9].Value = user.OtherEmail;
                        worksheet.Cells[x, 10].Value = user.PhoneWork;
                        worksheet.Cells[x, 11].Value = user.PhoneMobile;
                        worksheet.Cells[x, 12].Value = user.PhoneOther;
                        worksheet.Cells[x, 13].Value = user.Trained ? "Yes" : "No";
                        worksheet.Cells[x, 14].Value = user.Enabled ? "Yes" : "No";

                        if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                        {
                            worksheet.Cells[x, 15].Value = user.Notes;
                        }

                        x++;
                    }

                    return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE) { FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME) };

                }
            }

            return null;
        }


        public FileStreamResult FullInterviewReport()
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);
            var signups = signupManager.FetchAll();
            var sceFormData = new SCEFormData();

            if (signups != null)
            {


                using (var xlPackage = new ExcelPackage())
                {
                    string REPORT_NAME = "Full Session Report";
                    var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);

                    int x = 1;
                    using (var r = worksheet.Cells["A1:U1"])
                    {
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                        r.Style.Font.Bold = true;
                        r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                    worksheet.Cells[x, 1].Value = "Date";
                    worksheet.Cells[x, 2].Value = "Specialty";
                    worksheet.Cells[x, 3].Value = "Location";
                    worksheet.Cells[x, 4].Value = "Slot";
                    worksheet.Cells[x, 5].Value = "Place";
                    worksheet.Cells[x, 6].Value = "Date Signed Up";
                    worksheet.Cells[x, 7].Value = "Username";
                    worksheet.Cells[x, 8].Value = "Title";
                    worksheet.Cells[x, 9].Value = "Forename";
                    worksheet.Cells[x, 10].Value = "Surname";
                    worksheet.Cells[x, 11].Value = "GMCNumber";
                    worksheet.Cells[x, 12].Value = "Home Trust";
                    worksheet.Cells[x, 13].Value = "Email";
                    worksheet.Cells[x, 14].Value = "SecretaryEmail";
                    worksheet.Cells[x, 15].Value = "OtherEmail";
                    worksheet.Cells[x, 16].Value = "PhoneWork";
                    worksheet.Cells[x, 17].Value = "PhoneMobile";
                    worksheet.Cells[x, 18].Value = "PhoneOther";
                    worksheet.Cells[x, 19].Value = "Trained";
                    worksheet.Cells[x, 20].Value = "Enabled";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, 21].Value = "Notes";
                    }

                    x++;
                
                    var userIDs = signups.Where(o => o.Slots != null).SelectMany(n => n.Slots.Where(m => m.ApplicantsSignedUp != null).SelectMany(m => m.ApplicantsSignedUp).Select(o => o.User.ID));

                    IDictionary<string, SCEReportItem> userDict;

                    if (!userIDs.HasContent())
                        userDict = new Dictionary<string, SCEReportItem>();
                    else
                        userDict = reportService.FetchSCEData(userIDs).ToDictionary(k => k.Username, v => v);

                    foreach (var signup in signups)
                    {
                        foreach (var slot in signup.Slots.OrderBy(n => n.Time))
                        {
                            var users = slot.ApplicantsSignedUp.HasContent() ? slot.ApplicantsSignedUp.OrderBy(n => n.SignupDate).ToArray() : new UserSignup[] { };
                            var slotCount = 0;
                            for (int i = 1; i <= slot.TotalPlacesAvailable + slot.ReservePlaces + slot.InterestedPlaces; i++)
                            {
                                string slotType = "Error";

                                if (slotCount + 1 <= slot.TotalPlacesAvailable) slotType = "Main";
                                if (slotCount + 1 > slot.TotalPlacesAvailable && slotCount + 1 <= slot.TotalPlacesAvailable + slot.ReservePlaces) slotType = "Reserve";
                                if (slotCount + 1 > slot.TotalPlacesAvailable + slot.ReservePlaces && slotCount + 1 <= slot.TotalPlacesAvailable + slot.ReservePlaces + slot.InterestedPlaces) slotType = "Interested";

                                if (users.Length > slotCount)
                                {
                                    var sceData = userDict[users[slotCount].User.Username];

                                    worksheet.Cells[x, 1].Value = signup.Date.ToString("D");
                                    worksheet.Cells[x, 2].Value = signup.Group.Name;
                                    worksheet.Cells[x, 3].Value = signup.Description;
                                    worksheet.Cells[x, 4].Value = slot.Description;
                                    worksheet.Cells[x, 5].Value = slotType;
                                    worksheet.Cells[x, 6].Value = users[slotCount].SignupDate.ToString("f");
                                    worksheet.Cells[x, 7].Value = sceData.Username;
                                    worksheet.Cells[x, 8].Value = sceData.Title;
                                    worksheet.Cells[x, 9].Value = sceData.Forename;
                                    worksheet.Cells[x, 10].Value = sceData.Surname;
                                    worksheet.Cells[x, 11].Value = sceData.GMCNumber;


                                    string trust;

                                    /*if (!sceFormData.Trusts.TryGetValue(sceData.Trust, out trust))
                                    {
                                        trust = "Unknwon";
                                    }

                                    worksheet.Cells[x, 12].Value = trust;*/
                                    worksheet.Cells[x, 13].Value = sceData.Email;
                                    worksheet.Cells[x, 14].Value = sceData.SecretaryEmail;
                                    worksheet.Cells[x, 15].Value = sceData.OtherEmail;
                                    worksheet.Cells[x, 16].Value = sceData.PhoneWork;
                                    worksheet.Cells[x, 17].Value = sceData.PhoneMobile;
                                    worksheet.Cells[x, 18].Value = sceData.PhoneOther;
                                    worksheet.Cells[x, 19].Value = sceData.Trained ? "Yes" : "No";
                                    worksheet.Cells[x, 20].Value = sceData.Enabled ? "Yes" : "No";

                                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                                    {
                                        worksheet.Cells[x, 21].Value = sceData.Notes;
                                    }

                                }
                                else
                                {
                                    worksheet.Cells[x, 1].Value = signup.Date.ToString("D");
                                    worksheet.Cells[x, 2].Value = signup.Group.Name;
                                    worksheet.Cells[x, 3].Value = signup.Description;
                                    worksheet.Cells[x, 4].Value = slot.Description;
                                    worksheet.Cells[x, 5].Value = slotType;
                                }
                                slotCount++;
                                x++;
                            }
                        }

                    }

                    worksheet.Cells.AutoFitColumns();

                    return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE)
                    {
                        FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME)
                    };
                }
            }

            throw new HttpException(404, "File not found.");
        }


        public FileStreamResult InterviewReport(int id)
        {
            var loggedInUser = userManager.Fetch(User.Identity.Name);
            var signup = signupManager.Fetch(id);
            if (signup != null)
            {
                var userIDs = signup.Slots.Where(o => o.ApplicantsSignedUp != null).SelectMany(n => n.ApplicantsSignedUp.Select(m => m.User.ID));

                IDictionary<string, SCEReportItem> userDict;

                if (!userIDs.HasContent())
                    userDict = new Dictionary<string, SCEReportItem>();
                else
                    userDict = reportService.FetchSCEData(userIDs).ToDictionary(k => k.Username, v => v);

                using (var xlPackage = new ExcelPackage())
                {
                    string REPORT_NAME = string.Format("{0} {1} Report", signup.Group.Name, signup.Date.ToString("F"));
                    var worksheet = xlPackage.Workbook.Worksheets.Add(REPORT_NAME);
                  
                    int x = 1;
                    using (var r = worksheet.Cells["A1:S1"])
                    {
                        r.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(171, 205, 250));
                        r.Style.Font.Bold = true;
                        r.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    var j = 0;

                    worksheet.Cells[x, ++j].Value = "Slot";
                    worksheet.Cells[x, ++j].Value = "Place";
                    worksheet.Cells[x, ++j].Value = "Date Signed Up";
                    worksheet.Cells[x, ++j].Value = "Username";
                    worksheet.Cells[x, ++j].Value = "Title";
                    worksheet.Cells[x, ++j].Value = "Forename";
                    worksheet.Cells[x, ++j].Value = "Surname";
                    worksheet.Cells[x, ++j].Value = "Email";
                    worksheet.Cells[x, ++j].Value = "SecretaryEmail";
                    worksheet.Cells[x, ++j].Value = "OtherEmail";
                    worksheet.Cells[x, ++j].Value = "PhoneWork";
                    worksheet.Cells[x, ++j].Value = "PhoneMobile";
                    worksheet.Cells[x, ++j].Value = "PhoneOther";
                    worksheet.Cells[x, ++j].Value = "Trained";
                    worksheet.Cells[x, ++j].Value = "Enabled";

                    if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                    {
                        worksheet.Cells[x, 18].Value = "Notes";
                    }

                    x++;

                    foreach (var slot in signup.Slots.OrderBy(n => n.Time))
                    {
                        var users = slot.ApplicantsSignedUp.HasContent() ? slot.ApplicantsSignedUp.OrderBy(n => n.SignupDate).ToArray() : new UserSignup[] { };
                        var slotCount = 0;
                        for (int i = 1; i <= slot.TotalPlacesAvailable + slot.ReservePlaces + slot.InterestedPlaces; i++)
                        {
                            string slotType = "Error";

                            if (slotCount + 1 <= slot.TotalPlacesAvailable) slotType = "Main";
                            if (slotCount + 1 > slot.TotalPlacesAvailable && slotCount + 1 <= slot.TotalPlacesAvailable + slot.ReservePlaces) slotType = "Reserve";
                            if (slotCount + 1 > slot.TotalPlacesAvailable + slot.ReservePlaces && slotCount + 1 <= slot.TotalPlacesAvailable + slot.ReservePlaces + slot.InterestedPlaces) slotType = "Interested";

                            if (users.Length > slotCount)
                            {
                               // PopulateWorksheetForSingleReport(ref worksheet, x, slot, userDict[users[slotCount].User.Username], slotType,
                                 //   users[slotCount]);
                                j = 0;
                                var sceData = userDict[users[slotCount].User.Username];
                                worksheet.Cells[x, ++j].Value = slot.Description;
                                worksheet.Cells[x, ++j].Value = slotType;
                                worksheet.Cells[x, ++j].Value = users[slotCount].SignupDate.ToString("f");
                                worksheet.Cells[x, ++j].Value = sceData.Username;
                                worksheet.Cells[x, ++j].Value = sceData.Title;
                                worksheet.Cells[x, ++j].Value = sceData.Forename;
                                worksheet.Cells[x, ++j].Value = sceData.Surname;
                                worksheet.Cells[x, ++j].Value = sceData.Email;
                                worksheet.Cells[x, ++j].Value = sceData.SecretaryEmail;
                                worksheet.Cells[x, ++j].Value = sceData.OtherEmail;
                                worksheet.Cells[x, ++j].Value = sceData.PhoneWork;
                                worksheet.Cells[x, ++j].Value = sceData.PhoneMobile;
                                worksheet.Cells[x, ++j].Value = sceData.PhoneOther;
                                worksheet.Cells[x, ++j].Value = sceData.Trained ? "Yes" : "No";
                                worksheet.Cells[x, ++j].Value = sceData.Enabled ? "Yes" : "No";

                                if (loggedInUser != null && loggedInUser.Roles.Any(n => n.Name == "Admin" || n.Name == "Super Admin"))
                                {
                                    worksheet.Cells[x, 18].Value = sceData.Notes;
                                }

                            }
                            else
                            {
                                worksheet.Cells[x, 1].Value = slot.Description;
                                worksheet.Cells[x, 2].Value = slotType;
                            }
                            slotCount++;
                            x++;
                        }
                    }

                    worksheet.Cells.AutoFitColumns();

                    return new FileStreamResult(new MemoryStream(xlPackage.GetAsByteArray()), CONTENT_TYPE)
                        {
                            FileDownloadName = string.Format("{0}.xlsx", REPORT_NAME)
                        };
                }
            }

            throw new HttpException(404, "File not found.");
        }
    }
}
