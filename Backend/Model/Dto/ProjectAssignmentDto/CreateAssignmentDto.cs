namespace ProjectManagementSystem1.Model.Dto.ProjectAssignmentDto
{
    public class CreateAssignmentDto
    {
        public int ProjectId { get; set; }
        public string EmployeeId { get; set; }
        public string MemberRole { get; set; } // Supervisor or User
    }

}
