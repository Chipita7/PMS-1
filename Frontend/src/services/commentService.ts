import { apiClient } from '@/lib/api';

export interface CreateCommentPayload {
  projectRequestId: number;
  commentText: string;
}

export interface UpdateCommentPayload {
  commentText: string;
}

class CommentService {
  baseUrl = '/RequestComments';

  async addComment(payload: CreateCommentPayload) {
    const res = await apiClient.post(`${this.baseUrl}`, payload);
    if (!res.success) throw new Error(res.message || 'Failed to add comment');
    return res.data;
  }

  async getCommentsByRequest(requestId: number) {
    const res = await apiClient.get(`${this.baseUrl}/request/${requestId}`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch comments');
    return res.data?.data || [];
  }

  async getCommentById(commentId: number) {
    const res = await apiClient.get(`${this.baseUrl}/${commentId}`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch comment');
    return res.data?.data;
  }

  async updateComment(commentId: number, payload: UpdateCommentPayload) {
    const res = await apiClient.put(`${this.baseUrl}/${commentId}`, payload);
    if (!res.success) throw new Error(res.message || 'Failed to update comment');
    return res.data?.data;
  }

  async deleteComment(commentId: number) {
    const res = await apiClient.delete(`${this.baseUrl}/${commentId}`);
    if (!res.success) throw new Error(res.message || 'Failed to delete comment');
    return res.data;
  }

  async getStats(requestId: number) {
    const res = await apiClient.get(`${this.baseUrl}/request/${requestId}/stats`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch comment stats');
    return res.data?.data;
  }
}

export const commentService = new CommentService();
