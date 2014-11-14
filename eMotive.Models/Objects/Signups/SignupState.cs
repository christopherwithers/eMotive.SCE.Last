using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace eMotive.Models.Objects.Signups
{
    public class SignupState
    {
        public SignupState()
        {
            SignupNumbers = new Collection<SignupSlotState>();
        }

        public int ID { get; set; }
        public DateTime Date { get; set; }
        //public int SlotsAvailable { get; set; }
        public bool SignedUp { get; set; }
        public int NumberSignedUp { get; set; }
        
        public int TotalSlotsAvailable { get; set; }
        public int TotalReserveAvailable { get; set; }
        public int TotalInterestedAvaiable { get; set; }

        public SlotType TypeAvailable { get; set; }
        public bool DisabilitySignup { get; set; }
        public bool Closed { get; set; }
        public Location Location { get; set; }

        public Collection<SlotType> SignupTypes { get; set; } // public SlotType SignupType { get; set; }

        public Collection<SignupSlotState> SignupNumbers { get; set; }

        public Group Group { get; set; }

        public string Description { get; set; }

        public bool OverrideClose { get; set; }
        public bool MergeReserve { get; set; }

        public string SlotsAvailableString()
        {
            if (!OverrideClose && Closed)
                return "Sign up closed";

            int placesAvailable;

            if (!MergeReserve)
            {
                var inMain = false;
              //  var inReserve = false;

                var totalMainRemaining = 0;
                var totalReserveRemaining = 0;

                foreach (var slot in SignupNumbers)
                {

                    totalMainRemaining += slot.TotalSlotsAvailable - slot.NumberSignedUp < 0 ? 0 : slot.TotalSlotsAvailable - slot.NumberSignedUp;

                    if (totalMainRemaining >= slot.NumberSignedUp)
                    {
                        totalReserveRemaining += slot.TotalReserveAvailable;
                    }
                    else
                    {
                        totalReserveRemaining += (slot.TotalSlotsAvailable + slot.TotalReserveAvailable) - slot.NumberSignedUp;
                    }
                    // if (slot.NumberSignedUp < slot.TotalSlotsAvailable)
                    // {
                    //    inMain = true;
                    // break;
                    // inReserve &= true;
                    // }
                }

                if (totalMainRemaining > 0)
                {

                   // totalMainRemaining = SignupNumbers.Sum(n => n.TotalSlotsAvailable - n.NumberSignedUp);
                   // totalReserveRemaining = SignupNumbers.Sum(n => n.TotalReserveAvailable);

                    return string.Format("{1} {0} Available ({2} Main, {3} Reserve)",
                        "PLACE".SingularOrPlural(TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp),
                        TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp,
                        totalMainRemaining,
                        totalReserveRemaining);
                }

               // totalMainRemaining = SignupNumbers.Sum(n => n.TotalSlotsAvailable - n.NumberSignedUp);
                totalReserveRemaining = SignupNumbers.Sum(n => n.TotalReserveAvailable + n.TotalSlotsAvailable - n.NumberSignedUp);

                return string.Format("{1} {0} Available",
                    "RESERVE".SingularOrPlural(totalReserveRemaining - NumberSignedUp),
                    totalReserveRemaining);

              //  if (inReserve)
               // {
               //     
               // }

                return string.Format("{1} {0} Available", "Place".SingularOrPlural(TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp), TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp);

             //   if (NumberSignedUp < TotalSlotsAvailable + TotalReserveAvailable)
              //  var availabilityDetails = string.Empty;

              //  if(NumberSignedUp > TotalSlotsAvailable)
               //     availabilityDetails = string.Format(" ({0} Reserve) ", TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp);
               // else
                  //  availabilityDetails = string.Format(" ({0} Main, {1} Reserve) ", TotalSlotsAvailable - NumberSignedUp, TotalReserveAvailable);

               // return string.Format("{1} {2} {0} Available", "Place".SingularOrPlural(TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp), TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp, availabilityDetails);

                //if (NumberSignedUp < TotalSlotsAvailable + TotalReserveAvailable)
               //     return string.Format("{1} {0} Available", "Reserve".SingularOrPlural(TotalSlotsAvailable - NumberSignedUp - TotalReserveAvailable), TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp);

            }
            else
            {
                if (NumberSignedUp < TotalSlotsAvailable + TotalReserveAvailable)
                {
                    placesAvailable = TotalSlotsAvailable + TotalReserveAvailable - NumberSignedUp;
                    return string.Format("{1} {0} Available", "PLACE".SingularOrPlural(placesAvailable), placesAvailable);
                }
            }

            if (NumberSignedUp < TotalSlotsAvailable + TotalReserveAvailable + TotalInterestedAvaiable)
            {
                placesAvailable = TotalSlotsAvailable + TotalReserveAvailable + TotalInterestedAvaiable - NumberSignedUp;
                return string.Format("{1} {0} Available", "INTERESTED", placesAvailable);
            }

            return "No Places Available";
        }


        public class SignupSlotState
        {
            public int SlotID { get; set; }
            public int NumberSignedUp { get; set; }
            public int TotalSlotsAvailable { get; set; }
            public int TotalReserveAvailable { get; set; }
            public int TotalInterestedAvaiable { get; set; }
        }
    }

    public static class StringExtensions
    {
        public static string SingularOrPlural(this string _term, int _count)
        {
            return _count == 1 ? _term : _term + "S";
        }
    }
}
