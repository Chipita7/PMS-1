import { apiClient } from '@/lib/api';

// Backend-aligned DTOs
export interface ProjectTaskCreateDto {
  title: string;
  description?: string;
  projectAssignmentId: number;
  assignedMemberId?: string;
  parentTaskId?: number;
  weight: number; // 1-100
  estimatedHours?: number; // 0-1000
  startDate?: string; // ISO string
  priority: 'Low' | 'Medium' | 'High' | 'Urgent';
  status: 'Todo' | 'InProgress' | 'Done';
  dueDate?: string; // ISO string
  dependencies?: number[];
  milestoneId?: number;
  isAutoCreateTodo?: boolean;
}

export interface ProjectTaskReadDto {
  id: number;
  title: string;
  description: string;
  projectAssignmentId: number;
  assignedMemberId?: string;
  parentTaskId?: number;
  depth?: number;
  isLeaf?: boolean;
  progress: number;
  weight: number;
  priority?: string; // may be undefined depending on backend mapping
  status?: string; // may be undefined depending on backend mapping
  estimatedHours?: number;
  actualHours?: number;
  dueDate?: string;
  createdAt?: string;
  subTasks: ProjectTaskReadDto[];
}

export class TaskApi {
  async getAllTasks(): Promise<ProjectTaskReadDto[]> {
    const res = await apiClient.get<ProjectTaskReadDto[]>('/ProjectTask/Get-all-tasks');
    if (!res.success || !res.data) throw new Error(res.message || 'Failed to fetch tasks');
    return res.data;
  }

  async getTaskById(id: number): Promise<ProjectTaskReadDto> {
    const res = await apiClient.get<ProjectTaskReadDto>(`/ProjectTask/Get-task-by-id/${id}`);
    if (!res.success || !res.data) throw new Error(res.message || 'Failed to fetch task');
    return res.data;
  }

  async createTask(payload: ProjectTaskCreateDto): Promise<ProjectTaskReadDto> {
    const res = await apiClient.post<ProjectTaskReadDto>('/ProjectTask/create-task', payload);
    if (!res.success || !res.data) throw new Error(res.message || 'Failed to create task');
    return res.data;
  }

  async updateTask(id: number, payload: Partial<ProjectTaskCreateDto>): Promise<ProjectTaskReadDto> {
    const res = await apiClient.put<ProjectTaskReadDto>(`/ProjectTask/update-task/${id}`, payload);
    if (!res.success || !res.data) throw new Error(res.message || 'Failed to update task');
    return res.data;
  }

  async deleteTask(id: number): Promise<void> {
    const res = await apiClient.delete(`/ProjectTask/Delete-task?id=${id}`);
    if (!res.success) throw new Error(res.message || 'Failed to delete task');
  }

  async updateProgress(id: number, progress: number): Promise<void> {
    const res = await apiClient.put(`/ProjectTask/${id}/progress`, { progress });
    if (!res.success) throw new Error(res.message || 'Failed to update progress');
  }
}

export const taskApi = new TaskApi();
