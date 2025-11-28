import { apiClient } from '@/lib/api';

export interface SubmitFeedbackPayload {
  projectRequestId: number;
  feedbackText: string;
  rating?: number;
}

export interface UpdateFeedbackPayload {
  feedbackText?: string;
  rating?: number;
}

class FeedbackService {
  baseUrl = '/Feedback';

  async submitFeedback(payload: SubmitFeedbackPayload) {
    const res = await apiClient.post(`${this.baseUrl}`, payload);
    if (!res.success) throw new Error(res.message || 'Failed to submit feedback');
    return res.data;
  }

  async getFeedbackByRequest(requestId: number) {
    const res = await apiClient.get(`${this.baseUrl}/request/${requestId}`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch feedback');
    return res.data?.data || [];
  }

  async getFeedbackById(feedbackId: number) {
    const res = await apiClient.get(`${this.baseUrl}/${feedbackId}`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch feedback');
    return res.data?.data;
  }

  async updateFeedback(feedbackId: number, payload: UpdateFeedbackPayload) {
    const res = await apiClient.put(`${this.baseUrl}/${feedbackId}`, payload);
    if (!res.success) throw new Error(res.message || 'Failed to update feedback');
    return res.data?.data;
  }

  async deleteFeedback(feedbackId: number) {
    const res = await apiClient.delete(`${this.baseUrl}/${feedbackId}`);
    if (!res.success) throw new Error(res.message || 'Failed to delete feedback');
    return res.data;
  }

  async getStats(requestId: number) {
    const res = await apiClient.get(`${this.baseUrl}/request/${requestId}/stats`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch feedback stats');
    return res.data?.data;
  }

  async getSummary(requestId: number) {
    const res = await apiClient.get(`${this.baseUrl}/request/${requestId}/summary`);
    if (!res.success) throw new Error(res.message || 'Failed to fetch feedback summary');
    return res.data?.data;
  }

  async hasUserGivenFeedback(requestId: number) {
    const res = await apiClient.get(`${this.baseUrl}/request/${requestId}/has-given`);
    if (!res.success) throw new Error(res.message || 'Failed to check user feedback status');
    return res.data?.data?.hasGivenFeedback as boolean;
  }
}

export const feedbackService = new FeedbackService();
