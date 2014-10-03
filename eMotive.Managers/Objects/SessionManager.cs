using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper;
using Cache.Interfaces;
using eMotive.Managers.Objects.Search;
using eMotive.Managers.Objects.Signups;
using eMotive.Models.Objects;
using eMotive.Models.Objects.Search;
using eMotive.Models.Objects.Users;
using eMotive.Search.Interfaces;
using eMotive.Search.Objects;
using Extensions;
using eMotive.Managers.Interfaces;
using eMotive.Models.Objects.Signups;
using eMotive.Repository.Interfaces;
using eMotive.Services.Interfaces;
using Lucene.Net.Search;
using rep = eMotive.Repository.Objects.Signups;
using emSearch = eMotive.Search.Objects.Search;

namespace eMotive.Managers.Objects
{
    public class SessionManager : ISessionManager
    {
        private readonly ISessionRepository signupRepository;
        private readonly IUserManager userManager;
        private readonly ISearchManager searchManager;

        public SessionManager(ISessionRepository _signupRepository, IUserManager _userManager, ISearchManager _searchManager)
        {
            signupRepository = _signupRepository;
            userManager = _userManager;
            searchManager = _searchManager;

            AutoMapperManagerConfiguration.Configure();
        }

        public IeMotiveConfigurationService configurationService { get; set; }
        public INotificationService notificationService { get; set; }
        public IEmailService emailService { get; set; }
        public ICache cache { get; set; }

        readonly Dictionary<int, object> dictionary = new Dictionary<int, object>();
        readonly object dictionaryLock = new object();

        private IEnumerable<Repository.Objects.Signups.Signup> FetchSignupsByGroup(IEnumerable<int> _groups)
        {
            var cacheId = string.Format("RepSignups_{0}", string.Join("_", _groups));

            var signups = cache.FetchCollection<Repository.Objects.Signups.Signup>(cacheId, n => n.id, null);

            if (signups.HasContent())
                return signups;

            signups = signupRepository.FetchSignupsByGroup(_groups);

            cache.PutCollection(signups, n => n.id, cacheId);

            return signups;
        }

        //todo: closed date logic here
        public Signup Fetch(int _id)
        {
            var cacheId = string.Format("ModelSignup_{0}", _id);

            var signup = cache.FetchItem<Signup>(cacheId);

            if (signup != null)
                return signup;

            var repSignup = signupRepository.Fetch(_id);


            if (repSignup == null)
            {
                notificationService.AddError("The requested signup could not be found.");
                return null;
            }

            signup = Mapper.Map<rep.Signup, Signup>(repSignup);

            IDictionary<int, User> usersDict = null;

            if (repSignup.Slots.Any(n => n.UsersSignedUp.HasContent()))
            {
                //  usersDict = new Dictionary<int, User>();
                var UsersSignedUp = repSignup.Slots.Where(n => n.UsersSignedUp != null).SelectMany(m => m.UsersSignedUp);//.Select(u => u.IdUser);
                var userIds = UsersSignedUp.Select(u => u.IdUser);
                var users = userManager.Fetch(userIds);
                usersDict = users.ToDictionary(k => k.ID, v => v);
            }

            foreach (var repSlot in repSignup.Slots)
            {
                foreach (var slot in signup.Slots)
                {
                    if (repSlot.id != slot.ID) continue;

                    if (!repSlot.UsersSignedUp.HasContent()) continue;

                    slot.ApplicantsSignedUp = new Collection<UserSignup>();
                    foreach (var user in repSlot.UsersSignedUp)
                    {
                        slot.ApplicantsSignedUp.Add(new UserSignup { User = usersDict[user.IdUser], SignupDate = user.SignUpDate, ID = user.ID });
                    }
                }
            }


            return signup;
        }


        public Signup Fetch(int[] _ids)
        {
            throw new NotImplementedException();
            /*   var cacheId = string.Format("ModelSignup_{0}", string.Join("-", _ids));

               var signup = cache.FetchItem<IEnumerable<Signup>>(cacheId);

               if (signup != null)
                   return signup;

               var repSignup = signupRepository.FetchSignups(_ids);


               if (repSignup == null)
               {
                   notificationService.AddError("The requested signup could not be found.");
                   return null;
               }

               signup = Mapper.Map<rep.Signup, Signup>(repSignup);

               IDictionary<int, User> usersDict = null;

               if (repSignup.Slots.Any(n => n.UsersSignedUp.HasContent()))
               {
                   //  usersDict = new Dictionary<int, User>();
                   var UsersSignedUp = repSignup.Slots.Where(n => n.UsersSignedUp != null).SelectMany(m => m.UsersSignedUp);//.Select(u => u.IdUser);
                   var userIds = UsersSignedUp.Select(u => u.IdUser);
                   var users = userManager.Fetch(userIds);
                   usersDict = users.ToDictionary(k => k.ID, v => v);
               }

               foreach (var repSlot in repSignup.Slots)
               {
                   foreach (var slot in signup.Slots)
                   {
                       if (repSlot.id != slot.ID) continue;

                       if (!repSlot.UsersSignedUp.HasContent()) continue;

                       slot.ApplicantsSignedUp = new Collection<UserSignup>();
                       foreach (var user in repSlot.UsersSignedUp)
                       {
                           slot.ApplicantsSignedUp.Add(new UserSignup { User = usersDict[user.IdUser], SignupDate = user.SignUpDate, ID = user.ID });
                       }
                   }
               }


               return signup;*/
        }


        //TODO: need a new signup admin obj which contains full user + signup date etc! Then map to it!
        public IEnumerable<Signup> FetchAll()
        {
            var cacheId = "ModelSignupCollection";

            var signupModels = cache.FetchCollection<Signup>(cacheId, i => i.ID, null);//TODO: need a fetch (ids) func to push in here?

            if (signupModels.HasContent())
                return signupModels;

            var signups = signupRepository.FetchAll();

            if (!signups.HasContent())
                return null;

            signupModels = Mapper.Map<IEnumerable<rep.Signup>, IEnumerable<Signup>>(signups);

            var usersDict = userManager.Fetch(signups.SelectMany(u => u.Slots.Where(n => n.UsersSignedUp.HasContent()).SelectMany(n => n.UsersSignedUp).Select(m => m.IdUser))).ToDictionary(k => k.ID, v => v);

            foreach (var repSignup in signups)
            {
                foreach (var modSignup in signupModels)
                {
                    foreach (var repSlot in repSignup.Slots)
                    {
                        foreach (var slot in modSignup.Slots)
                        {
                            if (repSlot.id != slot.ID) continue;

                            if (!repSlot.UsersSignedUp.HasContent()) continue;

                            slot.ApplicantsSignedUp = new Collection<UserSignup>();
                            foreach (var user in repSlot.UsersSignedUp)
                            {
                                slot.ApplicantsSignedUp.Add(new UserSignup { User = usersDict[user.IdUser], SignupDate = user.SignUpDate, ID = user.ID });
                            }
                        }
                    }
                }
            }

            cache.PutCollection(signupModels, i => i.ID, cacheId);

            return signupModels;
        }

        public bool Save(Models.Objects.SignupsMod.Signup signup)
        {
            var success = signupRepository.Save(Mapper.Map<Models.Objects.SignupsMod.Signup, rep.Signup>(signup));

            if(!success)
                notificationService.AddIssue("The session changes could not be saved.");

            return success;
        }

        public IEnumerable<Signup> FetchAllTraining()
        {
            var cacheId = "ModelSignupCollectionTraining";

            var signupModels = cache.FetchCollection<Signup>(cacheId, i => i.ID, null);//TODO: need a fetch (ids) func to push in here?

            if (signupModels.HasContent())
                return signupModels;

            var signups = signupRepository.FetchAllTraining();

            if (!signups.HasContent())
                return null;

            signupModels = Mapper.Map<IEnumerable<rep.Signup>, IEnumerable<Signup>>(signups);

            var usersDict = userManager.Fetch(signups.SelectMany(u => u.Slots.Where(n => n.UsersSignedUp.HasContent()).SelectMany(n => n.UsersSignedUp).Select(m => m.IdUser))).ToDictionary(k => k.ID, v => v);

            foreach (var repSignup in signups)
            {
                foreach (var modSignup in signupModels)
                {
                    foreach (var repSlot in repSignup.Slots)
                    {
                        foreach (var slot in modSignup.Slots)
                        {
                            if (repSlot.id != slot.ID) continue;

                            if (!repSlot.UsersSignedUp.HasContent()) continue;

                            slot.ApplicantsSignedUp = new Collection<UserSignup>();
                            foreach (var user in repSlot.UsersSignedUp)
                            {
                                slot.ApplicantsSignedUp.Add(new UserSignup { User = usersDict[user.IdUser], SignupDate = user.SignUpDate, ID = user.ID });
                            }
                        }
                    }
                }
            }

            cache.PutCollection(signupModels, i => i.ID, cacheId);

            return signupModels;
        }

        public IEnumerable<SessionDay> FetchAllBrief()
        {
            var groups = signupRepository.FetchGroups();

            if (!groups.HasContent())
            {
                notificationService.AddError("An error occurred. Groups could not be found.");
                return null;
            }

            var signups = FetchAll();

            return Mapper.Map<IEnumerable<Signup>, IEnumerable<SessionDay>>(signups);
        }


        public IEnumerable<Group> FetchAllGroups()
        {
            return Mapper.Map<IEnumerable<rep.Group>, IEnumerable<Group>>(signupRepository.FetchGroups());
        }

        public IEnumerable<Models.Objects.SignupsMod.Signup> FetchRecordsFromSearch(SearchResult _searchResult)
        {
            return FetchM(_searchResult.Items.Select(n => n.ID).ToList());
        }

        public UserHomeView FetchHomeView(string _username)
        {
            //todo: fetch user and group
            var user = userManager.Fetch(_username);

            if (user == null)
            {
                //TODo: ERROR MESSAGE HERE!
                return null;
            }

            var profile = userManager.FetchProfile(_username);

            if (!profile.Groups.HasContent())
                return null;

            var signups = FetchSignupsByGroup(profile.Groups.Select(n => n.ID));

            if (signups == null)
                return null;

            var homeView = new UserHomeView
            {
                User = user
            };

            foreach (var signup in signups)
            {
                if (!signup.Slots.HasContent())
                    continue;

                foreach (var slot in signup.Slots)
                {
                    if (!slot.UsersSignedUp.HasContent())
                        continue;

                    foreach (var userSignup in slot.UsersSignedUp)
                    {
                        if (userSignup.IdUser != user.ID) continue;

                        var signupDetails = new UserSignupDetails
                        {
                            SignUpDate = signup.Date.AddHours(slot.Time.Hour).AddMinutes(slot.Time.Minute),
                            SignUpDetails = slot.Description,
                            SignedUpSlotID = slot.id,
                            SignupID = signup.id,
                            SignupGroup = new Group { Description = signup.Group.Description, AllowMultipleSignups = signup.Group.AllowMultipleSignups, ID = signup.Group.ID, Name = signup.Group.Name },
                            SignupDescription = signup.Description,
                            Type = GenerateHomeViewSlotStatus(slot, user.ID)// slot.UsersSignedUp.Where(n => n.IdUser == user.ID).Single(t => t.Type)
                        };

                        homeView.SignupDetails.Add(signupDetails);
                    }
                }
            }

            return homeView;
        }

        public bool RegisterAttendanceToSession(SessionAttendance _session)
        {
            return signupRepository.RegisterAttendanceToSession(Mapper.Map<SessionAttendance, rep.SessionAttendance>(_session));
        }


        public UserSignupView FetchSignupInformation(string _username)
        {
            var signupCollection = new Collection<SignupState>();
            var user = userManager.Fetch(_username);

            if (user == null)
            {//TODO: ERROR MESSAGE HERE!!
                return null;
            }

            var profile = userManager.FetchProfile(_username);
            var userSignUp = FetchuserSignups(user.ID, profile.Groups.Select(n => n.ID));
            var signups = FetchSignupsByGroup(profile.Groups.Select(n => n.ID));

            bool signedup = false;
            int signupId = 0;
            if (signups.HasContent())
            {
                //signupCollection
                foreach (var item in signups)
                {
                    //Logic to deal with applicants and closed signups
                    //if a signup is closed, we hide it from applicants UNLESS they are signed up to a slot in that signup
                    if (!item.Closed || userSignUp != null && userSignUp.Any(n => n.IdSignUp == item.id))
                    {
                        var signup = new SignupState
                        {
                            ID = item.id,
                            Date = item.Date,
                            SignedUp =
                                item.Slots.Any(
                                    n =>
                                    n.UsersSignedUp.HasContent() &&
                                    n.UsersSignedUp.Any(m => m != null && m.IdUser == user.ID)),
                            TotalSlotsAvailable = item.Slots.Sum(n => n.PlacesAvailable),
                            TotalReserveAvailable = item.Slots.Sum(n => n.ReservePlaces),
                            TotalInterestedAvaiable = item.Slots.Sum(n => n.InterestedPlaces),

                            NumberSignedUp = item.Slots.Sum(n => n.UsersSignedUp.HasContent() ? n.UsersSignedUp.Count() : 0),

                            MergeReserve = item.MergeReserve,
                            OverrideClose = item.OverrideClose,
                            DisabilitySignup = item.Group.DisabilitySignups,
                            Closed = item.Closed || item.CloseDate < DateTime.Now,
                            Description = item.Description,
                            //       SignupType = item.
                            Group = new Group { AllowMultipleSignups = item.Group.AllowMultipleSignups, Description = item.Group.Description, ID = item.Group.ID, Name = item.Group.Name }
                        };


                        foreach (var slot in item.Slots ?? new rep.Slot[] { })
                        {
                            signup.SignupNumbers.Add(new SignupState.SignupSlotState
                            {
                                SlotID = slot.id,
                                TotalSlotsAvailable = slot.PlacesAvailable,
                                TotalInterestedAvaiable = slot.InterestedPlaces,
                                TotalReserveAvailable = slot.ReservePlaces,
                                NumberSignedUp = slot.UsersSignedUp.HasContent() ? slot.UsersSignedUp.Count : 0
                            });
                        }

                        if (signup.SignedUp)
                        {
                            signup.SignupTypes = new Collection<SlotType>();
                            signedup = true;
                            signupId = signup.ID;
                            // var usersSlots = item.Slots.Where(n => n.UsersSignedUp.HasContent()).Select(m => m.UsersSignedUp.Where(o => o.IdUser == user.ID));

                            foreach (var userSignup in item.Slots)
                            {
                                if (userSignup.UsersSignedUp.HasContent() && userSignup.UsersSignedUp.Any(n => n.IdUser == user.ID))
                                {
                                    //   var usersIndex = userSignup.UsersSignedUp.FindIndex(n => n.IdUser == user.ID);

                                    // if (usersIndex != null)
                                    // {




                                    signup.SignupTypes.Add(GenerateHomeViewSlotStatus(userSignup, user.ID));//usersIndex.ToString());
                                    // }
                                }
                            }
                        }

                        signupCollection.Add(signup);
                    }
                }
            }

            var signupView = new UserSignupView
            {
                SignupInformation = signupCollection,
                SignupID = signupId,
                SignedUp = signedup,
            };

            return signupView;

        }



        /*
        public UserSignupView FetchSignupInformation(string _username)
        {
            var signupCollection = new Collection<SignupState>();
            var user = userManager.Fetch(_username);

            if (user == null)
            {//TODO: ERROR MESSAGE HERE!!
                return null;
            }

            var profile = userManager.FetchProfile(_username);
            var userSignUp = FetchuserSignups(user.ID, profile.Groups.Select(n => n.ID));
            var signups = FetchSignupsByGroup(profile.Groups.Select(n => n.ID));

            bool signedup = false;
            int signupId = 0;
            if (signups.HasContent())
            {
                //signupCollection
                foreach (var item in signups)
                {//TODO: NEED TO CHECK IF SLOT IS NULL! I>E ANY SLOTS ASSIGNED TO SIGNUP
                    //Logic to deal with applicants and closed signups
                    //if a signup is closed, we hide it from applicants UNLESS they are signed up to a slot in that signup
                    if (!item.Closed || userSignUp != null && userSignUp.Any(n => n.IdSignUp == item.id))
                    {
                        var signup = new SignupState
                        {
                            ID = item.id,
                            Date = item.Date,
                            SignedUp =
                                item.Slots.Any(
                                    n =>
                                    n.UsersSignedUp.HasContent() &&
                                    n.UsersSignedUp.Any(m => m != null && m.IdUser == user.ID)),
                            TotalSlotsAvailable = item.Slots.Sum(n => n.PlacesAvailable),
                            TotalReserveAvailable = item.Slots.Sum(n => n.ReservePlaces),
                            TotalInterestedAvaiable = item.Slots.Sum(n => n.InterestedPlaces),
                            NumberSignedUp = item.Slots.Sum(n => n.UsersSignedUp.HasContent() ? n.UsersSignedUp.Count() : 0),
                            MergeReserve = item.MergeReserve,
                            OverrideClose = item.OverrideClose,
                            DisabilitySignup = item.Group.DisabilitySignups,
                            Closed = item.Closed || item.CloseDate < DateTime.Now,
                            Description = item.Description,
                     //       SignupType = item.
                            Group = new Group { AllowMultipleSignups = item.Group.AllowMultipleSignups, Description = item.Group.Description, ID = item.Group.ID, Name = item.Group.Name}
                        };

                        if (signup.SignedUp)
                        {
                            signup.SignupTypes = new Collection<SlotType>();
                            signedup = true;
                            signupId = signup.ID;
                           // var usersSlots = item.Slots.Where(n => n.UsersSignedUp.HasContent()).Select(m => m.UsersSignedUp.Where(o => o.IdUser == user.ID));

                            foreach (var userSignup in item.Slots)
                            {
                                if (userSignup.UsersSignedUp.HasContent() && userSignup.UsersSignedUp.Any(n => n.IdUser == user.ID))
                                {
                                 //   var usersIndex = userSignup.UsersSignedUp.FindIndex(n => n.IdUser == user.ID);

                                    // if (usersIndex != null)
                                    // {




                                    signup.SignupTypes.Add(GenerateHomeViewSlotStatus(userSignup, user.ID));//usersIndex.ToString());
                                    // }
                                }
                            }
                        }

                        signupCollection.Add(signup);
                    }
                }
            }

            var signupView = new UserSignupView
            {
                SignupInformation = signupCollection,
                SignupID = signupId,
                SignedUp = signedup,
            };

            return signupView;

        }
*/
        public UserSignupView FetchSignupInformation(string _username, int _idGroup)
        {
            var signupCollection = new Collection<SignupState>();
            var user = userManager.Fetch(_username);

            if (user == null)
            {//TODO: ERROR MESSAGE HERE!!
                return null;
            }

            var profile = userManager.FetchProfile(_username);


            if (profile.Groups.All(n => n.ID != _idGroup))
            {
                notificationService.AddError("You do not have access to the requested session signup.");
                return null;
            }

            var userSignUp = FetchuserSignups(user.ID, profile.Groups.Select(n => n.ID));
            var signups = FetchSignupsByGroup(profile.Groups.Select(n => n.ID));

            bool signedup = false;
            int signupId = 0;

            if (signups.HasContent())
            {
                //signupCollection
                foreach (var item in signups)
                {
                    //Logic to deal with applicants and closed signups
                    //if a signup is closed, we hide it from applicants UNLESS they are signed up to a slot in that signup
                    if (!item.Closed || userSignUp != null && userSignUp.Any(n => n.IdSignUp == item.id))
                    {
                        var signup = new SignupState
                        {
                            ID = item.id,
                            Date = item.Date,
                            SignedUp =
                                item.Slots.Any(
                                    n =>
                                    n.UsersSignedUp.HasContent() &&
                                    n.UsersSignedUp.Any(m => m != null && m.IdUser == user.ID)),
                            TotalSlotsAvailable = item.Slots.Sum(n => n.PlacesAvailable),
                            TotalReserveAvailable = item.Slots.Sum(n => n.ReservePlaces),
                            TotalInterestedAvaiable = item.Slots.Sum(n => n.InterestedPlaces),
                            NumberSignedUp = item.Slots.Sum(n => n.UsersSignedUp.HasContent() ? n.UsersSignedUp.Count() : 0),
                            MergeReserve = item.MergeReserve,
                            OverrideClose = item.OverrideClose,
                            DisabilitySignup = item.Group.DisabilitySignups,
                            Closed = item.Closed || item.CloseDate < DateTime.Now,
                            Description = item.Description,
                            Group = new Group { AllowMultipleSignups = item.Group.AllowMultipleSignups, Description = item.Group.Description, ID = item.Group.ID, Name = item.Group.Name }
                        };

                        if (signup.SignedUp)
                        {
                            signedup = true;
                            signupId = signup.ID;
                        }

                        signupCollection.Add(signup);
                    }
                }
            }

            var signupView = new UserSignupView
            {
                SignupInformation = signupCollection,
                SignupID = signupId,
                SignedUp = signedup
            };

            return signupView;

        }


        public IEnumerable<SignupState> FetchSignupStates(string _username)
        {
            var signupCollection = new Collection<SignupState>();
            var user = userManager.Fetch(_username);

            if (user == null)
            {//TODO: ERROR MESSAGE HERE!!
                return null;
            }

            var profile = userManager.FetchProfile(_username);
            var userSignUp = FetchuserSignup(user.ID, profile.Groups.Select(n => n.ID));
            var signups = FetchSignupsByGroup(profile.Groups.Select(n => n.ID));

            bool signedup = false;
            int signupId = 0;

            if (signups.HasContent())
            {
                //signupCollection
                foreach (var item in signups)
                {
                    //Logic to deal with applicants and closed signups
                    //if a signup is closed, we hide it from applicants UNLESS they are signed up to a slot in that signup
                    if (!item.Closed || userSignUp != null && userSignUp.IdSignUp == item.id)
                    {
                        var signup = new SignupState
                        {
                            ID = item.id,
                            Date = item.Date,
                            SignedUp =
                                item.Slots.Any(
                                    n =>
                                    n.UsersSignedUp.HasContent() &&
                                    n.UsersSignedUp.Any(m => m != null && m.IdUser == user.ID)),
                            TotalSlotsAvailable = item.Slots.Sum(n => n.PlacesAvailable),
                            TotalReserveAvailable = item.Slots.Sum(n => n.ReservePlaces),
                            TotalInterestedAvaiable = item.Slots.Sum(n => n.InterestedPlaces),
                            NumberSignedUp = item.Slots.Sum(n => n.UsersSignedUp.HasContent() ? n.UsersSignedUp.Count() : 0),
                            MergeReserve = item.MergeReserve,
                            OverrideClose = item.OverrideClose,
                            DisabilitySignup = item.Group.DisabilitySignups,
                            Closed = item.Closed || item.CloseDate < DateTime.Now
                        };

                        if (signup.SignedUp)
                        {
                            signedup = true;
                            signupId = signup.ID;
                        }

                        signupCollection.Add(signup);
                    }
                }
            }

            return signupCollection;
        }

        public UserSlotView FetchSlotInformation(int _signup, string _username)
        {
            var user = userManager.Fetch(_username);

            //TODO need id of slot!
            var signup = Fetch(_signup);

            var signupGroup = signupRepository.FetchSignupGroup(_signup);

            var userProfile = userManager.FetchProfile(_username);

            if (signupGroup == null || userProfile == null || !userProfile.Groups.HasContent())
            {
                notificationService.AddError("An error occurred. The selected interview date could not be loaded.");

                if (signupGroup == null)
                    notificationService.Log(
                        string.Format("SignupManager: FetchSlotInformation: The signupGroup was null for signup: {0}.",
                            _signup));

                if (userProfile == null)
                    notificationService.Log(
                        string.Format(
                            "SignupManager: FetchSlotInformation: The userProfile was null for username: {0}", _username));

                return null;
            } //why isn't this working now?!?

            var hasAccess = userProfile.Groups.Any(@group => signupGroup.ID == @group.ID);

            if (!hasAccess) //signupGroup.ID != 
            {
                notificationService.AddError("You do not have permission to view the requested interview.");
                return null;
            }

            if (signup == null)
            {
                notificationService.AddError("The requested interview date could not be found.");
                return null;
            }

            var userSignup = FetchuserSignups(user.ID, userProfile.Groups.Select(n => n.ID));

            if (signup.Closed && (userSignup == null || userSignup.Any(n => n.IdSignUp == signup.ID)))
            {
                notificationService.AddError("The requested interview date is clsoed.");
                return null;
            }

            var slotCollection = new Collection<SlotState>();

            //TODO: COULD HAVE A BOOL HERE TO CHECK FOR 1 SIGNUP AGAINST ALL GROUPS OR A SIGNUP PER GROUP? #########################################
            var userSignUp = signupRepository.FetchUserSignups(user.ID, userProfile.Groups.Select(n => n.ID));

            var slotView = new UserSlotView(signup);

            foreach (var item in signup.Slots)
            {
                var slot = new SlotState
                {
                    ID = item.ID,
                    Description = item.Description,
                    Enabled = item.Enabled,
                    NumberSignedUp = item.ApplicantsSignedUp.HasContent() ? item.ApplicantsSignedUp.Count() : 0,
                    TotalPlacesAvailable = item.TotalPlacesAvailable,
                    Status = GenerateSlotStatus(item, new GenerateSlotStatusDTO
                    {
                        Closed = signup.Closed || signup.CloseDate < DateTime.Now,
                        MergeReserve = signup.MergeReserve,
                        MultipleSignupsPerSignup = signup.AllowMultipleSignups,
                        MultipleSignupsPerGroup = signupGroup.AllowMultipleSignups,
                        Group = signup.Group.Name,
                        OverrideClose = signup.OverrideClose,
                        Username = _username,
                        UsersSignups = userSignUp,
                        SignupID = signup.ID,
                        SignupDate = signup.Date
                    }),
                    SignupType = GenerateUserSignupType(item, user.ID),
                    Closed = signup.Closed || signup.CloseDate < DateTime.Now,
                    OverrideClose = signup.OverrideClose,
                    MergeReserve = signup.MergeReserve,
                    TotalInterestedAvaiable = item.InterestedPlaces,
                    TotalReserveAvailable = item.ReservePlaces
                };


                slotCollection.Add(slot);
            }

            slotView.Description = signup.Description;
            slotView.SlotState = slotCollection;
            slotView.HasSignedUp = signup.Slots.FirstOrDefault(n => n.ApplicantsSignedUp != null && n.ApplicantsSignedUp.Any(u => String.Equals(u.User.Username, _username, StringComparison.CurrentCultureIgnoreCase))) != null;


            return slotView;
        }

        //todo: reindex signup
        public bool SignupToSlot(int _signupID, int _slotId, string _username)
        {
            var signup = Fetch(_signupID);

            var slot = signup.Slots.SingleOrDefault(n => n.ID == _slotId);

            if (slot == null)
            {
                notificationService.AddError(string.Format("The requested slot ({0}) could not be found for signup {1}.", _slotId, _signupID));
                return false;
            }

            //TODO: Check for null here?
            var user = userManager.Fetch(_username);
            var profile = userManager.FetchProfile(_username);


            object bodyLock;
            lock (dictionaryLock)
            {

                if (!dictionary.TryGetValue(_slotId, out bodyLock))
                {
                    bodyLock = new object();
                    dictionary[_slotId] = bodyLock;
                }
            }

            if (signup.Closed)
            {
                notificationService.AddIssue("You cannot sign up to this slot. The sign up is closed.");

                return false;
            }

            if (DateTime.Now > signup.CloseDate)
            {
                notificationService.AddIssue(string.Format("You cannot sign up to this slot. The sign up closed on {0}.", signup.CloseDate.ToString("dddd d MMMM yyyy")));

                return false;
            }

            lock (bodyLock)
            {
                var signupDate = DateTime.Now;

                string error;

                if (slot.ApplicantsSignedUp.HasContent())
                {
                    if (slot.ApplicantsSignedUp.Any(n => String.Equals(n.User.Username, user.Username, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        notificationService.AddIssue("You have already signed up to this slot.");
                        return false;
                    }

                    if (slot.ApplicantsSignedUp.Count() >= slot.TotalPlacesAvailable + slot.ReservePlaces + slot.InterestedPlaces) //TODO: look into this logic, what happens if no interested places have been generated??
                    {
                        notificationService.AddIssue("The selected slot is now full.");
                        return false;
                    }
                }

                int id;

                //   bool interestedSlot = false;

                // if (slot.ApplicantsSignedUp.HasContent())
                // interestedSlot = slot.ApplicantsSignedUp.Count() >= slot.TotalPlacesAvailable + slot.ReservePlaces;

                if (signupRepository.SignupToSlot(_slotId, user.ID, signupDate, out id))
                {
                    //+1 to account for the signup we are currently processing
                    var reserveSignup = slot.ApplicantsSignedUp.HasContent() && slot.ApplicantsSignedUp.Count() + 1 > slot.TotalPlacesAvailable;
                    // slot.ApplicantsSignedUp.Single(n => n.ID == 0).ID = id;
                    var replacements = new Dictionary<string, string>(4)
                    {
                        {"#forename#", user.Forename},
                        {"#surname#", user.Surname},
                        {"#SignupDate#", signup.Date.ToString("dddd d MMMM yyyy")},
                        {"#SlotDescription#", slot.Description},
                        {"#SignupDescription#", signup.Description},
                        {"#GroupDescription#", signup.Group.Name},
                        {"#username#", user.Username},
                        {"#sitename#", configurationService.SiteName()},
                        {"#siteurl#", configurationService.SiteURL()}
                    };

                    // string key = interestedSlot ? "InterestedSignup" : "UserSessionSignup";


                    var key = string.Empty;

                    if (user.Roles.Any(n => n.Name == "Applicant"))
                        key = "ApplicantSessionSignup";

                    if (user.Roles.Any(n => n.Name == "Interviewer"))
                    {
                        if (signup.Group.Name == "Observer")
                        {
                            key = "ObserverSessionSignup";
                        }
                        else
                        {

                            key = reserveSignup ? "ReserveSessionSignup" : "InterviewerSessionSignup";
                        }
                    }


                    if (emailService.SendMail(key, user.Email, replacements))
                    {
                        emailService.SendEmailLog(key, user.Username);
                        return true;
                    }
                    return true;
                }

                notificationService.AddError("An error occured. ");
                return false;

            }


        }
        //todo: reindex signup
        public bool CancelSignupToSlot(int _signupID, int _slotId, string _username)
        {
            var signup = Fetch(_signupID);

            var slot = signup.Slots.SingleOrDefault(n => n.ID == _slotId);

            if (slot == null)
            {
                notificationService.AddError(string.Format("The requested slot ({0}) could not be found for signup {1}.", _slotId, _signupID));
                return false;
            }

            //TODO: check for null here??
            var user = userManager.Fetch(_username);
            // var profile = userManager.FetchProfile(_username);

            object bodyLock;
            lock (dictionaryLock)
            {
                if (!dictionary.TryGetValue(_slotId, out bodyLock))
                {
                    bodyLock = new object();
                    dictionary[_slotId] = bodyLock;
                }
            }

            lock (bodyLock)
            {//todo: NOTE that SCE bumps from interested to reserve. MMI bumps from reserve to main
                var BumpUser = slot.ApplicantsSignedUp.FindIndex(n => n.User.ID == user.ID) + 1 <= slot.TotalPlacesAvailable && slot.ApplicantsSignedUp.Count() > slot.TotalPlacesAvailable;//slot.ApplicantsSignedUp.Count() > slot.TotalPlacesAvailable;// + slot.ReservePlaces;

                if (signupRepository.CancelSignupToSlot(_slotId, user.ID))
                {

                    var userIndex = slot.ApplicantsSignedUp.FindIndex(n => n.User.ID == user.ID) + 1;

                    var reserveCancel = userIndex > slot.TotalPlacesAvailable;
                    /*                    var userSignup = signup.Slots.SingleOrDefault(n => n.ID == _slotId).ApplicantsSignedUp.SingleOrDefault(n => n.Applicant.Username == _username);

                    signup.Slots.SingleOrDefault(n => n.ID == _slotId).ApplicantsSignedUp.Remove(userSignup);
*/
                    /*if (slot.ApplicantsSignedUp().Count() >= slot.ApplicantsSignedUp + slot.ReservePlaces)
                    {
                        
                    }*/
                    var replacements = new Dictionary<string, string>(4)
                    {
                        {"#forename#", user.Forename},
                        {"#surname#", user.Surname},
                        {"#SignupDate#", signup.Date.ToString("dddd d MMMM yyyy")},
                        {"#SlotDescription#", slot.Description},
                        {"#username#", user.Username},
                        {"#SignupDescription#", signup.Description},
                        {"#GroupDescription#", signup.Group.Name},
                        {"#sitename#", configurationService.SiteName()},
                        {"#siteurl#", configurationService.SiteURL()}
                    };

                    //  string key = "UserSessionCancel";

                    var key = string.Empty;

                    if (user.Roles.Any(n => n.Name == "Applicant"))
                        key = "ApplicantSessionCancel";

                    if (user.Roles.Any(n => n.Name == "Interviewer"))
                    {
                        if (signup.Group.Name == "Observer")
                            key = "ObserverSessionCancel";
                        else
                            key = reserveCancel ? "ReserveSessionCancel" : "InterviewerSessionCancel";
                    }


                    if (emailService.SendMail(key, user.Email, replacements))
                    {
                        emailService.SendEmailLog(key, user.Username);


                        if (BumpUser)
                        {
                            var users = slot.ApplicantsSignedUp.Select(n => n).OrderBy(n => n.SignupDate).ToArray();

                            var UserToBump = users[slot.TotalPlacesAvailable /*+ slot.ReservePlaces*/].User;


                            key = "SlotUpgrade";

                            if (emailService.SendMail(key, UserToBump.Email, replacements))
                            {
                                emailService.SendEmailLog(key, UserToBump.Username);
                                return true;
                            }
                        }




                        return true;
                    }



                    notificationService.AddError("An error occured. ");
                    return true;
                }

                return false;
            }
        }

        public int FetchRCPActivityCode(int _signupID)
        {
            return signupRepository.FetchRCPActivityCode(_signupID);
        }

        //cache this!
        private rep.UserSignup FetchuserSignup(int _iduser, IEnumerable<int> _groupIds)
        {
            return signupRepository.FetchUserSignup(_iduser, _groupIds);
        }

        private IEnumerable<rep.UserSignup> FetchuserSignups(int _iduser, IEnumerable<int> _groupIds)
        {
            return signupRepository.FetchUserSignups(_iduser, _groupIds);
        }

        virtual public SlotType GenerateHomeViewSlotStatus(rep.Slot _slot, int _userId)
        {
            //   var userPosition = _slot.UsersSignedUp.ToList().FindIndex(n => n.Type ==)
            // throw new NotImplementedException();

            //    if(_slot)

            var userSignup = _slot.UsersSignedUp.SingleOrDefault(n => n.IdUser == _userId);

            //todo: error check incase userSignup is null??

            return (SlotType)userSignup.Type;
        }

        virtual public SlotType GenerateUserSignupType(Slot _slot, int _userId)
        {
            //   var userPosition = _slot.UsersSignedUp.ToList().FindIndex(n => n.Type ==)
            // throw new NotImplementedException();

            //    if(_slot)

            if (_slot.ApplicantsSignedUp.HasContent())
            {
                var userSignup = _slot.ApplicantsSignedUp.SingleOrDefault(n => n.User.ID == _userId);

                if (userSignup != null)
                {
                    var usersIndex = _slot.ApplicantsSignedUp.FindIndex(n => n.User.ID == _userId) + 1;

                    if (usersIndex <= _slot.TotalPlacesAvailable)
                        return SlotType.Main;

                    if (usersIndex <= _slot.TotalPlacesAvailable + _slot.ReservePlaces)
                        return SlotType.Reserve;

                    return SlotType.Interested;
                }

                //todo: error check incase userSignup is null??
            }

            return SlotType.Interested; //todo: need an error slot?
        }

        virtual public SlotStatus GenerateSlotStatus(Slot _slot, GenerateSlotStatusDTO _params)
        {

            if (!_slot.Enabled)
                return SlotStatus.Closed;

            if (_params.Closed && !_params.OverrideClose)//checked for overide? - centralise closed logic??
                return SlotStatus.Closed;

            var userIsSignnedUpToCurrentSignup = false;
            var applicantsSignedUp = 0;

            if (_slot.ApplicantsSignedUp.HasContent())
            {
                userIsSignnedUpToCurrentSignup = _slot.ApplicantsSignedUp.Any(n => String.Equals(n.User.Username, _params.Username, StringComparison.CurrentCultureIgnoreCase));
                applicantsSignedUp = _slot.ApplicantsSignedUp.Count();
            }

            var currentSignup = _params.UsersSignups.Select(n => n.IdSlot == _slot.ID);

            if (!_params.MultipleSignupsPerGroup)
            {
                if (userIsSignnedUpToCurrentSignup)
                    return SlotStatus.AlreadySignedUp;

                if (_params.UserHasSignup && !_params.MultipleSignupsPerSignup)
                    return SlotStatus.Clash;

                return SlotStatus.Signup;
            }


            if (!_params.MultipleSignupsPerSignup)
            {
                if (userIsSignnedUpToCurrentSignup)
                    return SlotStatus.AlreadySignedUp;

                if (_params.UserHasSignup)
                    return SlotStatus.Clash;

                return SlotStatus.Signup;
            }


            if (applicantsSignedUp < _slot.TotalPlacesAvailable)
            {
                return SlotStatus.Signup;
            }

            if (applicantsSignedUp < _slot.TotalPlacesAvailable + _slot.ReservePlaces)
            {
                return _params.MergeReserve ? SlotStatus.Signup : SlotStatus.Reserve;
            }

            if (applicantsSignedUp < _slot.TotalPlacesAvailable + _slot.ReservePlaces + _slot.InterestedPlaces)
                return SlotStatus.Interested;

            if (applicantsSignedUp >= _slot.TotalPlacesAvailable + _slot.ReservePlaces + _slot.InterestedPlaces)
                return SlotStatus.Full;

            /*if (_userSignup != null)
                return SlotStatus.Clash;*/

            if (!_slot.ApplicantsSignedUp.HasContent())
                return SlotStatus.Signup;



            return SlotStatus.Signup;//todo: need ERROR here?

        }




        #region TESTING STRAIGHT SIGNUP PULLTHROUGH
        public IEnumerable<Models.Objects.SignupsMod.Signup> FetchAllM()
        {
            var signups = Mapper.Map<IEnumerable<rep.Signup>, IEnumerable<Models.Objects.SignupsMod.Signup>>(signupRepository.FetchAll());

            var users = userManager.Fetch(signups.SelectMany(n => n.Slots).SelectMany(m => m.UsersSignedUp).Select(o => o.IdUser)).ToDictionary(k => k.ID, v => v);

            foreach (var signup in signups)
            {
                foreach (var slot in signup.Slots)
                {
                    slot.MergeReserve = signup.MergeReserve;
                    foreach (var user in slot.UsersSignedUp)
                    {
                        user.User = users[user.IdUser];
                        // break;
                    }
                }
            }

            return signups;
        }

        public Models.Objects.SignupsMod.Signup FetchM(int _id)
        {
            var signup = Mapper.Map<rep.Signup, Models.Objects.SignupsMod.Signup>(signupRepository.Fetch(_id));

            var users = userManager.Fetch(signup.Slots.SelectMany(m => m.UsersSignedUp).Select(o => o.IdUser)).ToDictionary(k => k.ID, v => v);

            foreach (var slot in signup.Slots)
            {
                slot.MergeReserve = signup.MergeReserve;
                foreach (var user in slot.UsersSignedUp)
                {
                    user.User = users[user.IdUser];
                    // break;
                }
            }

            return signup;
        }

        public IEnumerable<Models.Objects.SignupsMod.Signup> FetchM(IEnumerable<int> _ids)
        {
            var signups = Mapper.Map<IEnumerable<rep.Signup>, IEnumerable<Models.Objects.SignupsMod.Signup>>(signupRepository.FetchSignups(_ids));

            var users = userManager.Fetch(signups.SelectMany(n => n.Slots).SelectMany(m => m.UsersSignedUp).Select(o => o.IdUser)).ToDictionary(k => k.ID, v => v);

            foreach (var signup in signups)
            {
                foreach (var slot in signup.Slots)
                {
                    slot.MergeReserve = signup.MergeReserve;
                    foreach (var user in slot.UsersSignedUp)
                    {
                        user.User = users[user.IdUser];
                        // break;
                    }
                }
            }

            return signups;
        }

        public Models.Objects.SignupsMod.UserSignup FetchUserSignup(int _userId, IEnumerable<int> _groupIds)
        {
            return Mapper.Map<rep.UserSignup, Models.Objects.SignupsMod.UserSignup>(signupRepository.FetchUserSignup(_userId, _groupIds));
        }

        public IEnumerable<Models.Objects.SignupsMod.UserSignup> FetchUserSignups(int _userId, IEnumerable<int> _groupIds)
        {
            return Mapper.Map<IEnumerable<rep.UserSignup>, IEnumerable<Models.Objects.SignupsMod.UserSignup>>(signupRepository.FetchUserSignups(_userId, _groupIds));
        }
        #endregion

        public SearchResult DoSearch(BasicSearch _search)
        {
            var newSearch = Mapper.Map<BasicSearch, emSearch>(_search);

            if (_search.Filter.HasContent())
            {
                foreach (var filter in _search.Filter)
                {
                    newSearch.Filters.Add(filter.Key, new emSearch.SearchTerm { Field = filter.Value, Term = Occur.MUST });
                }
            }

            if (!string.IsNullOrEmpty(newSearch.Query))
            {
                newSearch.CustomQuery = new Dictionary<string, emSearch.SearchTerm>
                {
                    {"Username", new emSearch.SearchTerm {Field = _search.Query, Term = Occur.SHOULD}},
                    {"Forename", new emSearch.SearchTerm {Field = _search.Query, Term = Occur.SHOULD}},
                    {"Surname", new emSearch.SearchTerm {Field = _search.Query, Term = Occur.SHOULD}},
                    {"Email", new emSearch.SearchTerm {Field = _search.Query, Term = Occur.SHOULD}},
                    {"SignupDescription", new emSearch.SearchTerm {Field = _search.Query, Term = Occur.SHOULD}},
                    {"SlotDescription", new emSearch.SearchTerm {Field = _search.Query, Term = Occur.SHOULD}},
                    {"Group", new emSearch.SearchTerm {Field = _search.Query, Term = Occur.SHOULD}},
                };

            }

            return searchManager.DoSearch(newSearch);
        }

        public void ReindexSearchRecords()
        {
            var records = FetchAllM();

            if (!records.HasContent())
            {
                //todo: send an error message here
                return;
            }

            foreach (var item in records)
            {
                searchManager.Add(new SignupSearchDocument(item));
            }
        }
    }
}
