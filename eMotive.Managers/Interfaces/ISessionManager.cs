using System.Collections.Generic;
using eMotive.Models.Objects.Signups;
using eMotive.Search.Interfaces;
using eMotive.Search.Objects;
using mod = eMotive.Models.Objects.SignupsMod;

namespace eMotive.Managers.Interfaces
{
    public interface ISessionManager : ISearchable
    {
        Signup Fetch(int _id);
        Signup Fetch(int[] _ids);

        IEnumerable<Signup> FetchAll();

        bool Save(mod.Signup signup);

        IEnumerable<Signup> FetchAllTraining();

        IEnumerable<SessionDay> FetchAllBrief();
        UserHomeView FetchHomeView(string _username);

        bool RegisterAttendanceToSession(SessionAttendance _session);

        UserSignupView FetchSignupInformation(string _username);
        UserSignupView FetchSignupInformation(string _username, int _idGroup);

        IEnumerable<SignupState> FetchSignupStates(string _username);
        UserSlotView FetchSlotInformation(int _signup, string _username);

        bool SignupToSlot(int _signupID, int _slotId, string _username);
        bool CancelSignupToSlot(int _signupID, int _slotId, string _username);

        int FetchRCPActivityCode(int _signupID);

        IEnumerable<Group> FetchAllGroups();

        IEnumerable<mod.Signup> FetchRecordsFromSearch(SearchResult _searchResult);

        #region TESTING PULLING OUT SIGNUPS STRAIGHT FROM REP
        IEnumerable<mod.Signup> FetchAllM();
        mod.Signup FetchM(int _id);
        IEnumerable<mod.Signup> FetchM(IEnumerable<int> _ids);
        mod.UserSignup FetchUserSignup(int _userId, IEnumerable<int> _groupIds);
        IEnumerable<mod.UserSignup> FetchUserSignups(int _userId, IEnumerable<int> _groupIds);
        #endregion
    }
}
