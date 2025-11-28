import { apiClient } from '@/lib/api';

/**
 * Project Request Summary interface matching backend ProjectRequestDto
 */
export interface ProjectRequestSummary {
  id: number;
  requestID: string;
  requestTitle: string;
  requestDescription: string;
  referenceNo?: string;
  requestType: string;
  priority: string;
  status: string;
  priorityColor?: string;
  requestedByName: string;
  businessDepartment: string;
  strategicAlignment: string;
  createdDate: string;
  requestDurationDays: number;
  totalScore?: number;
  assignedTo?: string;
  timeToDeliveryDays?: number;
  daysUntilDelivery?: number;
  headReviewerId?: string;
  headReviewerName?: string;
  headAssignedAt?: string;
}

export interface AssignmentRole {
  id: number;
  name: string;
  code: string;
  priority: number;
  canBeMultiple: boolean;
  sortOrder: number;
  description?: string;
  isActive?: boolean;
}

export interface WorkflowHistoryEntry {
  id: number;
  workflowStage: string;
  status: string;
  changedBy: string;
  startDate: string;
  endDate?: string | null;
  durationDays?: number | null;
}

export interface StatusHistoryEntry {
  id: number;
  fromStatus: string;
  toStatus: string;
  changedBy: string;
  changedByName?: string;
  changedAt: string;
  remarks?: string;
  durationInPreviousStatus?: number;
}

export interface WorkflowHistoryResponse {
  workflowHistory: WorkflowHistoryEntry[];
  statusHistory: StatusHistoryEntry[];
}

/**
 * Project Request Service
 * Provides methods to interact with project request APIs
 */
class ProjectRequestService {
  /**
   * Get all project requests
   */
  async getAll(): Promise<ProjectRequestSummary[]> {
    try {
      const response = await apiClient.get<ProjectRequestSummary[]>('/ProjectRequests');
      if (!response.success || !response.data) {
        throw new Error(response.message || 'Failed to fetch requests');
      }
      return Array.isArray(response.data) ? response.data : [];
    } catch (error: any) {
      // Provide more helpful error messages
      if (error.message?.includes('Network error') || error.message?.includes('Cannot connect')) {
        throw new Error(
          'Cannot connect to backend server. Please ensure:\n' +
          '1. The backend is running on http://localhost:8080\n' +
          '2. CORS is properly configured\n' +
          '3. The API endpoint /api/ProjectRequests exists'
        );
      }
      if (error.response?.status === 404) {
        throw new Error(
          'API endpoint not found (404). Please verify:\n' +
          '1. The backend controller is registered\n' +
          '2. The route matches: /api/ProjectRequests\n' +
          '3. The backend is running and accessible'
        );
      }
      throw error;
    }
  }

  /**
   * Get configured assignment roles for idea intake workflows
   */
  async getAssignmentRoles(): Promise<AssignmentRole[]> {
    const response = await apiClient.get<{ success: boolean; roles: AssignmentRole[] }>('/ProjectRequests/assignment-roles');
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch assignment roles');
    }
    const payload = response.data;
    if (Array.isArray(payload)) {
      return payload;
    }
    return payload.roles || [];
  }

  /**
   * Get a single project request by ID
   */
  async getById(id: number): Promise<ProjectRequestSummary> {
    const response = await apiClient.get<ProjectRequestSummary>(`/ProjectRequests/${id}`);
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch request');
    }
    return response.data;
  }

  /**
   * Assign reviewers to a request
   */
  async assignReviewers(
    requestId: number,
    assignDto: {
      assignedTeam: string;
      assignedTo: string;
      assigneeRole: string;
      setAsPrimary?: boolean;
      reviewerType?: string;
      autoCreateTasks?: boolean; // ✅ NEW: Option to auto-create review tasks
    }
  ): Promise<{ success: boolean; message: string; primaryAssignee?: string }> {
    const response = await apiClient.put<{ success: boolean; message: string; primaryAssignee?: string }>(
      `/ProjectRequests/${requestId}/assign`,
      assignDto
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to assign reviewers');
    }
    return (response.data || { success: true, message: 'Assigned' }) as {
      success: boolean;
      message: string;
      primaryAssignee?: string;
    };
  }

  /**
   * Assign a head reviewer (owner) for a request
   */
  async assignHeadReviewer(
    requestId: number,
    payload: { headUserId?: string; headUsername?: string; notify?: boolean }
  ): Promise<{ success: boolean; message: string; headReviewerId?: string; headReviewerName?: string }> {
    const response = await apiClient.put<{ success: boolean; message: string; headReviewerId?: string; headReviewerName?: string }>(
      `/ProjectRequests/${requestId}/assign-head`,
      payload
    );
    if (!response.success) {
      throw new Error(response.message || 'Failed to assign head reviewer');
    }
    return (response.data || { success: true, message: 'Head assigned' }) as {
      success: boolean;
      message: string;
      headReviewerId?: string;
      headReviewerName?: string;
    };
  }

  /**
   * Get IDs of requests where the current user is the head reviewer
   */
  async getHeadAssignmentsForCurrentUser(): Promise<number[]> {
    const response = await apiClient.get<{ requestIds: number[] }>(`/ProjectRequests/head-assignments/me`);
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch head assignments');
    }
    return response.data.requestIds || [];
  }

  /**
   * Get requests needing evaluation
   */
  async getNeedingEvaluation(): Promise<ProjectRequestSummary[]> {
    const response = await apiClient.get<{ requests: ProjectRequestSummary[] }>('/ProjectRequests/needing-evaluation');
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch requests');
    }
    return response.data.requests || [];
  }

  /**
   * Get requests pending approval
   */
  async getPendingApproval(): Promise<ProjectRequestSummary[]> {
    const response = await apiClient.get<{ requests: ProjectRequestSummary[] }>('/ProjectRequests/pending-approval');
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch requests');
    }
    return response.data.requests || [];
  }

  /**
   * Get review tasks for a request
   */
  async getReviewTasks(requestId: number): Promise<any[]> {
    const response = await apiClient.get<{ tasks: any[] }>(`/ProjectRequests/${requestId}/review-tasks`);
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch review tasks');
    }
    return response.data.tasks || [];
  }

  /**
   * Generate tasks for a specific reviewer type (role) for a request
   * Uses ReviewTasksController: POST /ReviewTasks/request/{requestId}/generate-for-reviewer
   */
  async generateTasksForReviewerType(
    requestId: number,
    reviewerType: string,
    assigneeId: string,
    customDueDays?: number
  ): Promise<{ Success: boolean; Message?: string }> {
    const payload = { reviewerType, assigneeId, customDueDays };
    const res = await apiClient.post<{ Success: boolean; Message?: string }>(
      `/ReviewTasks/request/${requestId}/generate-for-reviewer`,
      payload
    );
    if (!res.success) {
      throw new Error(res.message || 'Failed to generate reviewer tasks');
    }
    return res.data || { Success: true };
  }

  /**
   * Check if a request can be approved (server-side gating)
   */
  async getCanApprove(requestId: number): Promise<{ success: boolean; canApprove: boolean; message?: string }> {
    const res = await apiClient.get<{ success: boolean; canApprove: boolean; message?: string }>(
      `/ProjectRequests/${requestId}/can-approve`
    );
    if (!res.success) {
      throw new Error(res.message || 'Failed to check approval eligibility');
    }
    return (res.data || { success: false, canApprove: false, message: res.message }) as {
      success: boolean;
      canApprove: boolean;
      message?: string;
    };
  }

  /**
   * Approve a request
   */
  async approveRequest(
    requestId: number,
    remarks?: string,
    minimumScoreThreshold?: number
  ): Promise<{ success: boolean; message: string }> {
    const response = await apiClient.put<{ success: boolean; message: string }>(`/ProjectRequests/${requestId}/approve`, {
      remarks,
      minimumScoreThreshold,
    });
    if (!response.success) {
      throw new Error(response.message || 'Failed to approve request');
    }
    return (response.data || { success: true, message: 'Approved' }) as { success: boolean; message: string };
  }

  /**
   * Reject a request
   */
  async rejectRequest(requestId: number, remarks: string, reason: string): Promise<{ success: boolean; message: string }> {
    const response = await apiClient.put<{ success: boolean; message: string }>(`/ProjectRequests/${requestId}/reject`, { remarks, reason });
    if (!response.success) {
      throw new Error(response.message || 'Failed to reject request');
    }
    return (response.data || { success: true, message: 'Rejected' }) as { success: boolean; message: string };
  }

  /**
   * Get workflow and status history for a request
   */
  async getWorkflowHistory(requestId: number): Promise<WorkflowHistoryResponse> {
    const response = await apiClient.get<{
      success: boolean;
      requestId: number;
      workflowHistory: WorkflowHistoryEntry[];
      statusHistory: StatusHistoryEntry[];
    }>(`/ProjectRequests/${requestId}/workflow-history`);
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch workflow history');
    }
    const payload = response.data;
    return {
      workflowHistory: payload.workflowHistory || [],
      statusHistory: payload.statusHistory || [],
    };
  }

  /**
   * Get owner history (who has been head/owner for the request)
   */
  async getOwnerHistory(requestId: number): Promise<any[]> {
    const response = await apiClient.get<{ success: boolean; requestId: number; history: any[] }>(`/ProjectRequests/${requestId}/owner-history`);
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch owner history');
    }
    return response.data.history || [];
  }

  /**
   * Start execution (move to project creation)
   */
  async startExecution(requestId: number): Promise<{ success: boolean; message: string }> {
    const response = await apiClient.post<{ success: boolean; message: string }>(`/ProjectRequests/${requestId}/start-execution`);
    if (!response.success) {
      throw new Error(response.message || 'Failed to start execution');
    }
    return (response.data || { success: true, message: 'Started' }) as { success: boolean; message: string };
  }
}

// Export singleton instance
export const projectRequestService = new ProjectRequestService();

