import { apiClient } from '@/lib/api';

class ReviewTaskService {
  baseUrl = '/ReviewTasks';

  async getMyTasks() {
    const res = await apiClient.get<any>(`${this.baseUrl}/my-tasks`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch my tasks');
    return (res.data as any)?.Data || res.data || [];
  }

  async completeTask(taskId: number, completionRemarks?: string) {
    const res = await apiClient.put<any>(`${this.baseUrl}/${taskId}/complete`, {
      completionRemarks: completionRemarks || undefined
    });
    if (!res.success) throw new Error(res.message || 'Failed to complete task');
    return res.data;
  }

  async reassignTask(taskId: number, newAssigneeId: string, newAssigneeName: string) {
    const res = await apiClient.put<any>(`${this.baseUrl}/${taskId}/reassign`, {
      newAssigneeId,
      newAssigneeName
    });
    if (!res.success) throw new Error(res.message || 'Failed to reassign task');
    return res.data;
  }

  async updateTask(taskId: number, taskDescription: string, dueDate: string) {
    const res = await apiClient.put<any>(`${this.baseUrl}/${taskId}`, { taskDescription, dueDate });
    if (!res.success) throw new Error(res.message || 'Failed to update task');
    return res.data;
  }

  async getOverdue() {
    const res = await apiClient.get<any>(`${this.baseUrl}/overdue`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch overdue tasks');
    return (res.data as any)?.Data || res.data || [];
  }

  async getTemplates() {
    const res = await apiClient.get(`${this.baseUrl}/templates`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch task templates');
    return res.data || [];
  }

  async createManualTask(
    requestId: number,
    assigneeId: string,
    assigneeName: string,
    assigneeRole: string,
    taskDescription: string,
    dueDate?: string
  ) {
    const payload: any = { assigneeId, assigneeName, assigneeRole, taskDescription };
    if (dueDate) payload.dueDate = dueDate;
    const res = await apiClient.post<any>(`${this.baseUrl}/request/${requestId}/manual`, payload);
    if (!res.success) throw new Error(res.message || 'Failed to create reviewer task');
    return res.data;
  }
}

export const reviewTaskService = new ReviewTaskService();
