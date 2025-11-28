using System.Threading.Tasks;
using ProjectManagementSystem1.Models.Entities.ProjectRequestEntities;

namespace ProjectManagementSystem1.Services.IdeaIntake
{
    public interface IIdeaIntakeDecisionService
    {
        Task<bool> ShouldGoToIdeaRefinementAsync(ProjectRequest request);
    }
}
