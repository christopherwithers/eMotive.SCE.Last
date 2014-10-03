namespace eMotive.Models.Objects
{
    public enum SlotStatus { Signup, Full, Closed, Clash, AlreadySignedUp, Interested, Reserve }
    public enum CreateUser { Success, Error, DuplicateUsername, DuplicateEmail, Deletedaccount }
    public enum SlotType { Main, Reserve, Interested }
}
