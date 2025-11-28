import { apiClient } from '@/lib/api';

export interface EvaluationDto {
  id: number;
  projectRequestId: number;
  reviewerId: string;
  reviewerName: string;
  reviewerRole: string;
  strategicAlignmentScore?: number;
  feasibilityScore?: number;
  businessValueScore?: number;
  technicalComplexityScore?: number;
  evaluationStatus: 'Draft' | 'Submitted';
  evaluationRemarks?: string;
  createdAt: string;
  submittedAt?: string;
  overallScore?: number; // ✅ Added: Individual evaluation's overall score
}

export interface SubmitEvaluationDto {
  strategicAlignmentScore?: number;
  feasibilityScore?: number;
  businessValueScore?: number;
  technicalComplexityScore?: number;
  evaluationRemarks?: string;
}

class EvaluationService {
  /**
   * Get evaluations for a request
   */
  async getEvaluationsByRequest(requestId: number): Promise<EvaluationDto[]> {
    const response = await apiClient.get<{ evaluations: EvaluationDto[] }>(`/Evaluations/request/${requestId}`);
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch evaluations');
    }
    return response.data.evaluations || [];
  }

  /**
   * Get a single evaluation by ID
   */
  async getEvaluationById(evaluationId: number): Promise<EvaluationDto> {
    const response = await apiClient.get<{ evaluation: EvaluationDto }>(`/Evaluations/${evaluationId}`);
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch evaluation');
    }
    return response.data.evaluation;
  }

  /**
   * Submit an evaluation
   */
  async submitEvaluation(evaluationId: number, evaluationDto: SubmitEvaluationDto): Promise<{ success: boolean; message: string; newTotalScore?: number }> {
    const response = await apiClient.post(`/Evaluations/${evaluationId}/submit`, evaluationDto);
    if (!response.success) {
      throw new Error(response.message || 'Failed to submit evaluation');
    }
    return response.data;
  }

  /**
   * Get my evaluations (current user's evaluations)
   */
  async getMyEvaluations(): Promise<EvaluationDto[]> {
    const response = await apiClient.get<{ evaluations: EvaluationDto[] }>('/Evaluations/my-evaluations');
    if (!response.success || !response.data) {
      throw new Error(response.message || 'Failed to fetch evaluations');
    }
    return response.data.evaluations || [];
  }
}

export const evaluationService = new EvaluationService();

