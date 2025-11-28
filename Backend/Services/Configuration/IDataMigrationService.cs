namespace ProjectManagementSystem1.Services.Configuration
{
    public interface IDataMigrationService
    {
        Task<bool> MigrateEnumDataToConfigAsync();
        Task<bool> MigrateExistingProjectRequestsAsync();
        Task<MigrationReport> GetMigrationReportAsync();
        Task<bool> ResetAndRegenerateRolesAsync();
        //Task<bool> AddApprovalReadyStageAsync();
    }

    public class MigrationReport
    {
        public int TotalConfigEntriesCreated { get; set; }
        public int TotalProjectRequestsMigrated { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime MigrationDate { get; set; }
    }
}