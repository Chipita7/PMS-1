import { apiClient } from '@/lib/api';

export const progressRecalculationService = {
  /**
   * ✅ Recalculate ALL milestone and project progress
   * Use this once to update all existing data
   */
  recalculateAll: async (): Promise<{
    success: boolean;
    message: string;
    milestonesUpdated: number;
    projectsUpdated: number;
    totalMilestones: number;
    totalProjects: number;
  }> => {
    console.log('🔄 Calling recalculate-all API...');
    const response = await apiClient.post<any>('/ProgressRecalculation/recalculate-all', {});
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to recalculate all');
    }
    console.log('✅ Recalculate-all response:', response.data);
    return response.data;
  },

  /**
   * Recalculate progress for a specific project
   */
  recalculateProject: async (projectId: number): Promise<{
    success: boolean;
    message: string;
    milestonesUpdated: number;
  }> => {
    console.log(`🔄 Recalculating project ${projectId}...`);
    const response = await apiClient.post<any>(`/ProgressRecalculation/recalculate-project/${projectId}`, {});
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to recalculate project');
    }
    console.log(`✅ Project ${projectId} recalculated:`, response.data);
    return response.data;
  },

  /**
   * Recalculate progress for a specific milestone
   */
  recalculateMilestone: async (milestoneId: number): Promise<{
    success: boolean;
    message: string;
  }> => {
    console.log(`🔄 Recalculating milestone ${milestoneId}...`);
    const response = await apiClient.post<any>(`/ProgressRecalculation/recalculate-milestone/${milestoneId}`, {});
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to recalculate milestone');
    }
    console.log(`✅ Milestone ${milestoneId} recalculated:`, response.data);
    return response.data;
  }
};

