namespace TypeOneFoodJournal.Services
{
    using TypeOneFoodJournal.Data;

    public interface IInsulinPumpService
    {
        string Token { get; }
        Task Login(string username, string password);
        void UpdateToken(string token, string userId);
        Task<ReadingList> GetInsulinPumpDataAsync(DateTime startDate, DateTime endDate);
    }
}
