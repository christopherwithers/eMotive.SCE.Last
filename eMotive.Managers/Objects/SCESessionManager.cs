using System;
using System.Linq;
using eMotive.Managers.Objects.Signups;
using eMotive.Models.Objects;
using eMotive.Search.Interfaces;
using Extensions;
using eMotive.Managers.Interfaces;
using eMotive.Models.Objects.Signups;
using eMotive.Repository.Interfaces;

using rep = eMotive.Repository.Objects.Signups;

namespace eMotive.Managers.Objects
{
    public class SCESessionManager : SessionManager
    {

        //todo: before htting this, we need to know if the user has signed up ANYWHERE - another query? ########################
        public SCESessionManager(ISessionRepository _signupRepository, IUserManager _userManager, ISearchManager _searchManager, IFormManager _formManager)
            : base(_signupRepository, _userManager, _searchManager, _formManager)
        {
        }

        override public SlotStatus GenerateSlotStatus(Slot _slot, GenerateSlotStatusDTO _params)
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

            if (userIsSignnedUpToCurrentSignup)
                return SlotStatus.AlreadySignedUp;

            if (_params.SignupID != 2078 && _params.SignupID != 2079 && _params.SignupID != 2080 && _params.SignupID != 2081)
                if (_params.UsersSignups.Any(n => n.Date.Date == _params.SignupDate.Date && n.IdSignUp != _params.SignupID))
                {
                    return SlotStatus.Clash;
                }

            if (!_params.MultipleSignupsPerGroup)
            {
                //if (_params.UserHasSignup && !_params.MultipleSignupsPerSignup)
                 //   return SlotStatus.Clash;

                return SlotStatus.Signup;
            }

            if (!_params.MultipleSignupsPerSignup)
            {
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
    }
}
