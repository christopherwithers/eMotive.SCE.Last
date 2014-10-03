using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using DDay.iCal;
using eMotive.Managers.Interfaces;
using eMotive.SCE.Common;
using eMotive.SCE.Common.ActionFilters;
using eMotive.Models.Objects.Signups;
using eMotive.Models.Objects.SignupsMod;
using eMotive.Models.Objects.StatusPages;
using eMotive.Models.Objects.Uploads;
using eMotive.Models.Objects.Users;
using eMotive.SCE.Common;
using eMotive.SCE.Infrastructure;
using eMotive.Search.Objects;
using eMotive.Services.Interfaces;
using Extensions;
using Newtonsoft.Json.Linq;
//using Ninject;
using OfficeOpenXml;
using ServiceStack.Mvc;
using Group = eMotive.Models.Objects.Signups.Group;
using Signup = eMotive.Models.Objects.Signups.Signup;

namespace eMotive.SCE.Areas.Admin.Controllers
{
    public class SignupsController : ServiceStackController
    {
        private readonly ISessionManager signupManager;
        private readonly IGroupManager groupManager;
        private readonly IDocumentManagerService documentManager;
        private readonly IUserManager userManager;

        private readonly Dictionary<string, string> searchFilter;

        public SignupsController(ISessionManager _signupManager, IGroupManager _groupManager, IDocumentManagerService _documentManager, IUserManager _userManager)
        {
            signupManager = _signupManager;
            groupManager = _groupManager;
            documentManager = _documentManager;
            userManager = _userManager;

            searchFilter = new Dictionary<string, string> { { "Type", "Signup" } };
        }

       // [Inject]
        public INotificationService NotificationService { get; set; }

      //  [Inject]
        public IeMotiveConfigurationService ConfigurationService { get; set; }
        
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult Index(SignupSearch signupSearch)
        {
           /* var signupAdminView = new AdminSignupView
                {
                    Signups = signupManager.FetchAllM()
                };*/

            if (!string.IsNullOrEmpty(signupSearch.SelectedGroupFilter) && signupSearch.SelectedGroupFilter != "0")
                searchFilter.Add("GroupID", signupSearch.SelectedGroupFilter);

            signupSearch.Filter = searchFilter;
        //    signupSearch.PageSize = 20;
            var searchItem = signupManager.DoSearch(signupSearch);

            if (searchItem.Items.HasContent())
            {
                signupSearch.Page = searchItem.CurrentPage;
                signupSearch.NumberOfResults = searchItem.NumberOfResults;
                signupSearch.Signups = signupManager.FetchRecordsFromSearch(searchItem); 
            }
            else
            {
              //  signupSearch.Signups = signupManager.FetchAllM();
                signupSearch = new SignupSearch();
            }

            var groups = groupManager.FetchGroups();

            if (groups.HasContent())
            {
                var groupFilter = new Collection<KeyValuePair<string, string>> { new KeyValuePair<string, string>("0", string.Empty) };

                groupFilter.AddRange(groups.Select(n => new KeyValuePair<string, string>(n.ID.ToString(CultureInfo.InvariantCulture), n.Name)));
                signupSearch.GroupFilter = groupFilter;
            }

            return View(signupSearch);
        }
        /*
        public ActionResult Details(int id)
        {
            var signup = signupManager.FetchM(id);

            return View(signup);
        }*/

        public ActionResult Edit(int id)
        {
            var signup = signupManager.Fetch(id);

            return View(signup);
        }



        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult SignupDetails(int? id)
        {
            var signup = signupManager.FetchM(id.Value);

            return View(signup);
        }


        public class FineUploaderResult : ActionResult
        {
            public const string ResponseContentType = "text/plain";

            private readonly bool _success;
            private readonly string _error;
            private readonly bool? _preventRetry;
            private readonly JObject _otherData;

            public FineUploaderResult(bool success, object otherData = null, string error = null, bool? preventRetry = null)
            {
                _success = success;
                _error = error;
                _preventRetry = preventRetry;

                if (otherData != null)
                    _otherData = JObject.FromObject(otherData);
            }

            public override void ExecuteResult(ControllerContext context)
            {
                var response = context.HttpContext.Response;
                response.ContentType = ResponseContentType;

                response.Write(BuildResponse());
            }

            public string BuildResponse()
            {
                var response = _otherData ?? new JObject();
                response["success"] = _success;

                if (!string.IsNullOrWhiteSpace(_error))
                    response["error"] = _error;

                if (_preventRetry.HasValue)
                    response["preventRetry"] = _preventRetry.Value;

                return response.ToString();
            }
        }

        [HttpGet]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult BoxiUpload()
        {
            var lastUpload = documentManager.FetchLastUploaded(UploadReference.BOXI);

            return View(lastUpload);
        }


        public ActionResult UserSessions(string username)
        {
            var user = userManager.Fetch(username);
            var signups = signupManager.FetchSignupInformation(username);

            var sessionView = new ThirdPartySignupSessions {Signup = signups, User = user};

            return View(sessionView);
        }

        public ActionResult UserSlots(string username, int? id)
        {
            var user = userManager.Fetch(username);
            var slots = signupManager.FetchSlotInformation(id.HasValue ? id.Value : -1, username);

            var slotView = new ThirdPartySignupSlots {Slots = slots, User = user};

            return View(slotView);
        }

        [HttpPost]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public FineUploaderResult BoxiUpload(FineUpload upload)
        {
            // asp.net mvc will set extraParam1 and extraParam2 from the params object passed by Fine-Uploader

            var uploadLocation = documentManager.FetchUploadLocation(UploadReference.BOXI);
            var dir = uploadLocation.Directory;

            var filename = Path.GetFileName(upload.Filename);
            var modifiedFilename = string.Format("{0}_{1}", DateTime.Now.ToString("hhmmss"), filename);
            var path = Server.MapPath(dir);

            var filePath = Path.Combine(path, modifiedFilename);
            try
            {
                upload.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                return new FineUploaderResult(false, error: ex.Message);
            }

            documentManager.SaveDocumentInformation(new UploadedDocument
            {
                Name = filename,
                DateUploaded = DateTime.Now,
                Extension = Path.GetExtension(upload.Filename),
                Location = path,
                ModifiedName = modifiedFilename,
                Reference = UploadReference.BOXI,
                UploadedByUsername = User.Identity.Name

            });

            var workBook = new FileInfo(filePath);

            using (var xlPackage = new ExcelPackage(workBook))
            {
                ExcelWorksheet ws
                    = xlPackage.Workbook.Worksheets[1];

                var applicants = new List<ApplicantData>();

                for (var rowNum = 2; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    applicants.Add(new ApplicantData
                    {
                        TermCode = ws.Cells[rowNum, 1].GetValue<string>(),
                        ApplicationDate = DateTime.FromOADate(ws.Cells[rowNum, 2].GetValue<double>()),
                        ApplicantID = ws.Cells[rowNum, 3].GetValue<string>(),
                        PersonalID = ws.Cells[rowNum, 4].GetValue<string>(),
                        ApplicantPrefix = ws.Cells[rowNum, 5].GetValue<string>(),
                        Surname = ws.Cells[rowNum, 6].GetValue<string>(),
                        Firstname = ws.Cells[rowNum, 7].GetValue<string>(),
                        DateOfBirth = DateTime.FromOADate(ws.Cells[rowNum, 8].GetValue<double>()),
                        Age = ws.Cells[rowNum, 9].GetValue<int>(),
                        Gender = ws.Cells[rowNum, 10].GetValue<string>(),
                        DisabilityCode = ws.Cells[rowNum, 11].GetValue<string>(),
                        DisabilityDesc = ws.Cells[rowNum, 12].GetValue<string>(),
                        ResidenceCode = ws.Cells[rowNum, 13].GetValue<string>(),
                        NationalityDesc = ws.Cells[rowNum, 14].GetValue<string>(),
                        CorrespondenceAddr1 = ws.Cells[rowNum, 15].GetValue<string>(),
                        CorrespondenceAddr2 = ws.Cells[rowNum, 16].GetValue<string>(),
                        CorrespondenceAddr3 = ws.Cells[rowNum, 17].GetValue<string>(),
                        CorrespondenceCity = ws.Cells[rowNum, 18].GetValue<string>(),
                        CorrespondenceNationDesc = ws.Cells[rowNum, 19].GetValue<string>(),
                        CorrespondencePostcode = ws.Cells[rowNum, 20].GetValue<string>(),
                        EmailAddress = ws.Cells[rowNum, 21].GetValue<string>(),
                        PreviousSchoolDesc = ws.Cells[rowNum, 22].GetValue<string>(),
                        SchoolAddressCity = ws.Cells[rowNum, 23].GetValue<string>(),
                        SchoolLEADescription = ws.Cells[rowNum, 24].GetValue<string>()
                    });


                }

                if (!applicants.HasContent())
                    return new FineUploaderResult(true, error: "An error occurred. The applicant data could not be saved.");


                userManager.SaveApplicantData(applicants);


                return new FineUploaderResult(true, new { extraInformation = 12345 });
            }
        }

        [HttpGet]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult A100ApplicantUpload()
        {
            var applicantUploadView = new ApplicantUploadView
            {
                Groups = null,
                LastUploadedDocument = documentManager.FetchLastUploaded(UploadReference.A100Applicants)
            };


            return View(applicantUploadView);
        }

        [HttpPost]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public FineUploaderResult A100ApplicantUpload(FineUpload upload)
        {
            // asp.net mvc will set extraParam1 and extraParam2 from the params object passed by Fine-Uploader

            var uploadLocation = documentManager.FetchUploadLocation(UploadReference.A100Applicants);
            var dir = uploadLocation.Directory;

            var filename = Path.GetFileName(upload.Filename);
            var modifiedFilename = string.Format("{0}_{1}", DateTime.Now.ToString("hhmmss"), filename);
            var path = Server.MapPath(dir);

            var filePath = Path.Combine(path, modifiedFilename);
            try
            {
                upload.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                return new FineUploaderResult(false, error: ex.Message);
            }

            documentManager.SaveDocumentInformation(new UploadedDocument
            {
                Name = filename,
                DateUploaded = DateTime.Now,
                Extension = Path.GetExtension(upload.Filename),
                Location = path,
                ModifiedName = modifiedFilename,
                Reference = UploadReference.A100Applicants,
                UploadedByUsername = User.Identity.Name

            });

            var workBook = new FileInfo(filePath);

            using (var xlPackage = new ExcelPackage(workBook))
            {
                ExcelWorksheet ws
                    = xlPackage.Workbook.Worksheets[1];

                var applicants = new List<ApplicantUploadData>();

                for (var rowNum = 2; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    if (!string.IsNullOrEmpty(ws.Cells[rowNum, 3].GetValue<string>()))
                    {
                        applicants.Add(new ApplicantUploadData
                        {
                            InviteForInterview = ws.Cells[rowNum, 3].GetValue<string>(),
                            NonUkResident = ws.Cells[rowNum, 4].GetValue<string>(),
                            PersonID = ws.Cells[rowNum, 12].GetValue<string>(),

                        });

                    }
                }

                if (!applicants.HasContent())
                    return new FineUploaderResult(true, error: "An error occurred. The applicant data could not be saved.");

                var success = userManager.CreateApplicantAccounts(applicants, new[] { 1, 2 });


                //  userManager.SaveApplicantData(applicants);


                return new FineUploaderResult(true, new { extraInformation = 12345 });
            }

            // the anonymous object in the result below will be convert to json and set back to the browser
            return new FineUploaderResult(true, new { extraInformation = 12345 });
        }

        [HttpGet]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult A101ApplicantUpload()
        {
            var applicantUploadView = new ApplicantUploadView
            {
                Groups = null,
                LastUploadedDocument = documentManager.FetchLastUploaded(UploadReference.A101Applicants)
            };


            return View(applicantUploadView);
        }

        [HttpPost]
        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public FineUploaderResult A101ApplicantUpload(FineUpload upload)
        {
            // asp.net mvc will set extraParam1 and extraParam2 from the params object passed by Fine-Uploader

            var uploadLocation = documentManager.FetchUploadLocation(UploadReference.A101Applicants);
            var dir = uploadLocation.Directory;

            var filename = Path.GetFileName(upload.Filename);
            var modifiedFilename = string.Format("{0}_{1}", DateTime.Now.ToString("hhmmss"), filename);
            var path = Server.MapPath(dir);

            var filePath = Path.Combine(path, modifiedFilename);
            try
            {
                upload.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                return new FineUploaderResult(false, error: ex.Message);
            }

            documentManager.SaveDocumentInformation(new UploadedDocument
            {
                Name = filename,
                DateUploaded = DateTime.Now,
                Extension = Path.GetExtension(upload.Filename),
                Location = path,
                ModifiedName = modifiedFilename,
                Reference = UploadReference.A101Applicants,
                UploadedByUsername = User.Identity.Name

            });

            var workBook = new FileInfo(filePath);

            using (var xlPackage = new ExcelPackage(workBook))
            {
                ExcelWorksheet ws
                    = xlPackage.Workbook.Worksheets[1];

                var applicants = new List<ApplicantUploadData>();

                for (var rowNum = 2; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    if (!string.IsNullOrEmpty(ws.Cells[rowNum, 3].GetValue<string>()))
                    {
                        applicants.Add(new ApplicantUploadData
                        {
                            InviteForInterview = ws.Cells[rowNum, 3].GetValue<string>(),
                            NonUkResident = ws.Cells[rowNum, 4].GetValue<string>(),
                            PersonID = ws.Cells[rowNum, 12].GetValue<string>(),

                        });

                    }
                }

                if (!applicants.HasContent())
                    return new FineUploaderResult(true, error: "An error occurred. The applicant data could not be saved.");

                var success = userManager.CreateApplicantAccounts(applicants, new[] { 2, 3 });


                //  userManager.SaveApplicantData(applicants);


                return new FineUploaderResult(true, new { extraInformation = 12345 });
            }

            // the anonymous object in the result below will be convert to json and set back to the browser
            return new FineUploaderResult(true, new { extraInformation = 12345 });
        }


        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult Groups()
        {
            var groups = groupManager.FetchGroups();

            return View(groups);
        }

        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        [HttpGet]
        public ActionResult GroupEdit(int? id)
        {
            var group = groupManager.FetchGroup(id.HasValue ? id.Value : -1);

            return View(group);
        }

        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        [HttpPost]
        public ActionResult GroupEdit(Group group)
        {
            if (ModelState.IsValid)
            {
                if (groupManager.UpdateGroup(group))
                {
                    var successView = new SuccessView
                    {
                        Message = "The Group was successfully updated.",
                        Links = new[]
                            {
                                new SuccessView.Link {Text = string.Format("Edit {0}", group.Name), URL = @Url.Action("GroupEdit", "Signups", new {id = group.ID})},
                                new SuccessView.Link {Text = "Return to Group Home", URL = @Url.Action("Groups")}
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

            return View(group);
        }

        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult EditSignup(int id)
        {
            return View();
        }

        [Common.ActionFilters.Authorize(Roles = "Super Admin, Admin")]
        public ActionResult OpenClose()
        {
            var signups = signupManager.FetchAllBrief();

            var signupDict = signups.GroupBy(
        m =>
        string.Format("{1} {0}", m.Date.Year,
                      System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(
                          m.Date.Month))).ToDictionary(k => k.Key, g => g.OrderBy(n => n.Date).ToList());

            return View(signupDict);
        }


    //    [AjaxOnly]
        public JsonResult FetchSignup(int idSignup)
        {
            var signup = signupManager.Fetch(idSignup);

            var issues = NotificationService.FetchIssues();

          //  return new CustomJsonResult
          //  {
            return new JsonResult {Data =  new {success = signup != null, message = issues, result = signup}, JsonRequestBehavior = JsonRequestBehavior.AllowGet};
            // };

        }

      //  [AjaxOnly]
        public CustomJsonResult FetchAllGroups()
        {
            //  var signup = signupManager.sa
            var groups = signupManager.FetchAllGroups();

            var issues = NotificationService.FetchIssues();

            return new CustomJsonResult
            {
                Data = new { success = false, message = issues, result = groups }
            };

        }

        [AjaxOnly]
        public CustomJsonResult SaveSignup(Signup signup)
        {
          //  var signup = signupManager.sa

            var issues = NotificationService.FetchIssues();

            return new CustomJsonResult
            {
                Data = new { success = false, message = issues }
            };

        }


        [AjaxOnly]
        public CustomJsonResult SignupToSlot(int idSignup, int idSlot, string username)
        {
            if (signupManager.SignupToSlot(idSignup, idSlot, username))
            {
                var signup = signupManager.Fetch(idSignup);
                var slot = signup.Slots.Single(n => n.ID == idSlot);

                ApplicantSignupPush(signup.ID, signup.Slots.Sum(n => n.TotalPlacesAvailable),
                    signup.Slots.Sum(n => n.ApplicantsSignedUp.HasContent() ? n.TotalPlacesAvailable - n.ApplicantsSignedUp.Count() : n.TotalPlacesAvailable));

                ApplicantSlotPush(slot.ID, slot.TotalPlacesAvailable,
                    slot.ApplicantsSignedUp.HasContent() ? slot.TotalPlacesAvailable - slot.ApplicantsSignedUp.Count() : slot.TotalPlacesAvailable);

                return new CustomJsonResult
                {
                    Data = new { success = true, message = "successfully signed up." }
                };
            }

            var issues = NotificationService.FetchIssues();


            return new CustomJsonResult
            {
                Data = new { success = false, message = issues }
            };

        }

        [AjaxOnly]
        public CustomJsonResult CancelSignupToSlot(int idSignup, int idSlot, string username)
        {
            if (signupManager.CancelSignupToSlot(idSignup, idSlot, username))
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
        }

        private void ApplicantSignupPush(int _signupID, int _totalPlaces, int _remainingPlaces)
        {
         /*   var pusher = new PusherServer.Pusher(ConfigurationService.PusherID(), ConfigurationService.PusherKey(), ConfigurationService.PusherSecret());

            var result = pusher.Trigger("SignupSelection", "PlacesChanged",
                                                    new
                                                    {
                                                        SignUpId = _signupID,
                                                        TotalPlaces = _totalPlaces,
                                                        PlacesAvailable = _remainingPlaces
                                                    });*/
        }

        private void ApplicantSlotPush(int _slotID, int _totalPlaces, int _remainingPlaces)
        {
            /*var pusher = new PusherServer.Pusher(ConfigurationService.PusherID(), ConfigurationService.PusherKey(), ConfigurationService.PusherSecret());

            var result = pusher.Trigger("SignupSelection", "SlotChanged",
                                                    new
                                                    {
                                                        SlotId = _slotID,
                                                        TotalPlaces = _totalPlaces,
                                                        PlacesAvailable = _remainingPlaces
                                                    });*/
        }

    }
}
