import { apiClient } from '@/lib/api';

// Configuration option from backend
export interface ConfigOption {
  id: number;
  name: string;
  code?: string;
  color?: string;
  description?: string;
  sortOrder: number;
}

export interface ConfigOptionCreateDto {
  name: string;
  code: string;
  description?: string;
  color?: string;
  sortOrder: number;
}

// Configuration dictionary from backend
export interface RequestConfigurations {
  RequestTypes?: ConfigOption[];
  RequestCategories?: ConfigOption[];
  ServiceCategories?: ConfigOption[];
  ProductCategories?: ConfigOption[];
  Priorities?: ConfigOption[];
  ImpactUrgencies?: ConfigOption[]; // Legacy - kept for backward compatibility
  ImpactLevels?: ConfigOption[]; // For Business Impact
  UrgencyLevels?: ConfigOption[]; // For Request Urgency
  RiskLevels?: ConfigOption[];
  ComplexityLevels?: ConfigOption[];
  Statuses?: ConfigOption[];
  StrategicAlignments?: ConfigOption[];
}

// Backend DTO for creating a request
export interface CreateProjectRequestDto {
  requestTitle: string;
  requestDescription: string;
  referenceNo?: string;
  requestTypeConfigId: number;
  requestCategoryConfigId: number;
  serviceCategoryConfigId: number;
  productCategoryConfigId: number;
  priorityConfigId: number;
  businessImpactConfigId: number;
  requestUrgencyConfigId: number;
  strategicAlignment: string;
  strategicAlignmentConfigId?: number;
  estimatedCost?: number;
  estimatedBenefit?: number;
  benefitCaptureDuration?: number;
  requestedDeliveryDate?: string;
  riskLevelConfigId?: number;
  complexityLevelConfigId?: number;
  primaryContactEmail: string;
  secondaryContactEmail?: string;
  primaryContactPhone: string;
  secondaryContactPhone?: string;
  requestedByName?: string;
  businessSector?: string;
  businessDivision?: string;
  businessDepartment?: string;
  organizationName?: string;
}

// Config Info DTO from backend
export interface ConfigInfoDto {
  id: number;
  name: string;
  code?: string;
  color?: string;
}

// Response DTO from backend (list view)
export interface ProjectRequestDto {
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
  [key: string]: any;
}

// Detail DTO from backend (detailed view)
export interface ProjectRequestDetailDto {
  id: number;
  requestID: string;
  requestTitle: string;
  requestDescription: string;
  referenceNo?: string;
  requestType: ConfigInfoDto;
  requestCategory: ConfigInfoDto;
  serviceCategory: ConfigInfoDto;
  productCategory: ConfigInfoDto;
  priority: ConfigInfoDto;
  businessImpact: ConfigInfoDto;
  requestUrgency: ConfigInfoDto;
  status: ConfigInfoDto;
  workflowStage?: ConfigInfoDto;
  riskLevel?: ConfigInfoDto;
  complexityLevel?: ConfigInfoDto;
  strategicAlignmentInfo?: ConfigInfoDto;
  requestedBy: string;
  requestedByName: string;
  businessSector: string;
  businessDivision: string;
  businessDepartment: string;
  organizationName: string;
  primaryContactEmail: string;
  secondaryContactEmail?: string;
  primaryContactPhone: string;
  secondaryContactPhone?: string;
  strategicAlignment: string;
  estimatedCost?: number;
  estimatedBenefit?: number;
  benefitCaptureDuration?: number;
  requestedDeliveryDate?: string;
  evaluatorID?: string;
  feasibilityScore?: number;
  businessValueScore?: number;
  technicalComplexityScore?: number;
  totalScore?: number;
  evaluationRemarks?: string;
  assignedTeam?: string;
  assignedTo?: string;
  createdDate: string;
  lastUpdatedDate: string;
  createdBy: string;
  approvalDate?: string;
  requestDurationDays: number;
  timeToDeliveryDays?: number;
  daysUntilDelivery?: number;
}

/**
 * Fetch all configuration options for request forms
 */
export const getRequestConfigurations = async (): Promise<RequestConfigurations> => {
  const response = await apiClient.get<RequestConfigurations>('/ProjectRequests/configurations');
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch configurations');
  }
  return response.data;
};

/**
 * Fetch all project requests
 */
export const getAllProjectRequests = async (): Promise<ProjectRequestDto[]> => {
  try {
    const response = await apiClient.get<ProjectRequestDto[]>('/ProjectRequests');
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
};

/**
 * Fetch a single project request by ID
 */
export const getProjectRequestById = async (id: number): Promise<ProjectRequestDetailDto> => {
  const response = await apiClient.get<ProjectRequestDetailDto>(`/ProjectRequests/${id}`);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to fetch request');
  }
  return response.data;
};

/**
 * Create a new project request
 */
export const createProjectRequest = async (
  requestData: CreateProjectRequestDto
): Promise<ProjectRequestDto> => {
  const response = await apiClient.post<ProjectRequestDto>('/ProjectRequests', requestData);
  if (!response.success || !response.data) {
    throw new Error(response.message || 'Failed to create request');
  }
  return response.data;
};

/**
 * Helper function to find config ID by name
 */
export const findConfigIdByName = (
  configs: ConfigOption[] | undefined,
  name: string
): number | undefined => {
  if (!configs) return undefined;
  const config = configs.find(c => c.name.toLowerCase() === name.toLowerCase());
  return config?.id;
};

/**
 * Helper function to find config ID by code
 */
export const findConfigIdByCode = (
  configs: ConfigOption[] | undefined,
  code: string
): number | undefined => {
  if (!configs) return undefined;
  const config = configs.find(c => c.code?.toLowerCase() === code.toLowerCase());
  return config?.id;
};

/**
 * Calculate priority based on urgency and impact matrix
 * Priority Matrix (matches backend expectations):
 * - High Urgency + High Impact = P1
 * - High Urgency + Medium Impact = P2
 * - High Urgency + Low Impact = P3
 * - Medium Urgency + High Impact = P2
 * - Medium Urgency + Medium Impact = P3
 * - Medium Urgency + Low Impact = P4
 * - Low Urgency + High Impact = P3
 * - Low Urgency + Medium Impact = P4
 * - Low Urgency + Low Impact = P5
 * 
 * Backend uses PriorityConfig with Name values like "P1", "P2", "P3", "P4", "P5"
 */
export const calculatePriorityFromUrgencyAndImpact = (
  urgency: string | undefined,
  impact: string | undefined,
  priorities: ConfigOption[] | undefined
): string | undefined => {
  if (!urgency || !impact || !priorities || priorities.length === 0) return undefined;

  const urgencyLower = urgency.toLowerCase().trim();
  const impactLower = impact.toLowerCase().trim();

  // Helper to find priority by name or code (backend uses "P1", "P2", etc.)
  const findPriority = (targetName: string): ConfigOption | undefined => {
    return priorities.find(p =>
      p.name.toLowerCase() === targetName.toLowerCase() ||
      p.code?.toLowerCase() === targetName.toLowerCase()
    );
  };

  // Priority matrix logic - backend expects P1, P2, P3, P4, P5
  let targetPriority: string | undefined;

  if (urgencyLower === 'high' && impactLower === 'high') {
    targetPriority = 'P1';
  } else if (
    (urgencyLower === 'high' && impactLower === 'medium') ||
    (urgencyLower === 'medium' && impactLower === 'high')
  ) {
    targetPriority = 'P2';
  } else if (
    (urgencyLower === 'high' && impactLower === 'low') ||
    (urgencyLower === 'medium' && impactLower === 'medium') ||
    (urgencyLower === 'low' && impactLower === 'high')
  ) {
    targetPriority = 'P3';
  } else if (
    (urgencyLower === 'medium' && impactLower === 'low') ||
    (urgencyLower === 'low' && impactLower === 'medium')
  ) {
    targetPriority = 'P4';
  } else if (urgencyLower === 'low' && impactLower === 'low') {
    targetPriority = 'P5';
  }

  // Find the actual priority config that matches (by name or code)
  if (targetPriority) {
    const found = findPriority(targetPriority);
    if (found) {
      return found.name; // Return the actual name from backend (might be "P1" or "Critical" etc.)
    }
    // Fallback: try to find any priority that matches the pattern
    const fallback = priorities.find(p =>
      p.name.toLowerCase().includes(targetPriority.toLowerCase()) ||
      p.code?.toLowerCase() === targetPriority.toLowerCase()
    );
    return fallback?.name || targetPriority;
  }

  return undefined;
};

/**
 * Add a new configuration option
 */
export const addConfigOption = async (type: string, option: ConfigOptionCreateDto): Promise<void> => {
  const response = await apiClient.post(`/RequestConfig/${type}`, option);
  if (!response.success) {
    throw new Error(response.message || 'Failed to add option');
  }
};

/**
 * Delete a configuration option
 */
export const deleteConfigOption = async (type: string, id: number): Promise<void> => {
  const response = await apiClient.delete(`/RequestConfig/${type}/${id}`);
  if (!response.success) {
    throw new Error(response.message || 'Failed to delete option');
  }
};
