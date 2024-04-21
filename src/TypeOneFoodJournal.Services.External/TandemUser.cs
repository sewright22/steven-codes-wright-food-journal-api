namespace TypeOneFoodJournal.Services.External
{
    [Serializable]
    public class TandemUser
    {
        public string Id { get; set; } = string.Empty;
        public string PatientObjectId { get; set; } = string.Empty;
        public string PatientObjectType { get; set; } = string.Empty;
    }
}