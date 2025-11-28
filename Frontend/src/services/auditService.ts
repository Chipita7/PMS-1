import { apiClient } from '@/lib/api';

export interface AuditEntry {
  id: number;
  projectRequestId: number;
  action: string;
  description: string;
  changedBy: string;
  changedOn: string;
  fieldName?: string;
  oldValue?: string;
  newValue?: string;
  entityType?: string;
  relatedEntityId?: number;
  additionalData?: string;
}

export interface AuditSummary {
  totalAuditEntries: number;
  createdCount: number;
  updatedCount: number;
  statusChangeCount: number;
  ownerChangeCount: number;
  firstActivity?: string;
  lastActivity?: string;
  activitiesByUser?: Record<string, number>;
  activitiesByType?: Record<string, number>;
}

class AuditService {
  async getAuditTrail(requestId: number): Promise<AuditEntry[]> {
    const res = await apiClient.get<AuditEntry[]>(`/project-requests/${requestId}/audit`);
    if (!res.success || !res.data) {
      throw new Error(res.message || 'Failed to fetch audit trail');
    }
    return Array.isArray(res.data) ? res.data : [];
  }

  async getAuditSummary(requestId: number): Promise<AuditSummary> {
    const res = await apiClient.get<AuditSummary>(`/project-requests/${requestId}/audit/summary`);
    if (!res.success || !res.data) {
      throw new Error(res.message || 'Failed to fetch audit summary');
    }
    return res.data;
  }

  async getRecentActivity(requestId: number, count = 50): Promise<AuditEntry[]> {
    const res = await apiClient.get<AuditEntry[]>(`/project-requests/${requestId}/audit/recent?count=${count}`);
    if (!res.success || !res.data) {
      throw new Error(res.message || 'Failed to fetch recent activity');
    }
    return Array.isArray(res.data) ? res.data : [];
  }
}

export const auditService = new AuditService();
