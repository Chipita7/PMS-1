// RequestDetail.tsx - restyled with purple/gold theme and Times New Roman font
import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, User, Building2, Mail, Phone, DollarSign, TrendingUp, Clock, AlertCircle, CheckCircle, XCircle, FileText } from 'lucide-react';
import { getProjectRequestById, type ProjectRequestDetailDto } from '@/services/requestService';
import { projectRequestService } from '@/services/projectRequestService';
import RequestDetailsTabs from '@/components/RequestDetailsTabs';

import { useAuth } from "@/context/AuthContext";

const RequestDetail: React.FC<{ darkMode?: boolean }> = ({ darkMode = false }) => {
  const navigate = useNavigate();
  const { role, id } = useParams<{ role: string; id: string }>();
  const { user } = useAuth();
  const [request, setRequest] = useState<ProjectRequestDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isHead, setIsHead] = useState<boolean>(false);
  const [ownerHistory, setOwnerHistory] = useState<any[] | null>(null);

  useEffect(() => {
    const fetchRequest = async () => {
      if (!id) {
        setError('Request ID is required');
        setLoading(false);
        return;
      }
      try {
        setLoading(true);
        setError(null);
        const data = await getProjectRequestById(parseInt(id));
        setRequest(data);

        // Load owner history first to help with head check
        let hist: any[] = [];
        try {
          hist = await projectRequestService.getOwnerHistory(parseInt(id));
          setOwnerHistory(Array.isArray(hist) ? hist : []);
        } catch (e) {
          setOwnerHistory(null);
        }

        // Determine if current user is head for this request
        // Check 1: API endpoint for assignments
        let isAssigned = false;
        try {
          const myHeadIds = await projectRequestService.getHeadAssignmentsForCurrentUser();
          if (Array.isArray(myHeadIds) && myHeadIds.includes(parseInt(id))) {
            isAssigned = true;
          }
        } catch (e) {
          console.warn("Failed to check head assignments:", e);
        }

        // Check 2: Owner history (fallback if API fails or for specific roles like Director)
        if (!isAssigned && user && Array.isArray(hist)) {
          const activeHead = hist.find((h: any) =>
            (h.ownerRole || h.OwnerRole || '').toLowerCase().includes('head') &&
            (h.status || h.Status || '').toLowerCase() === 'active'
          );

          if (activeHead) {
            const ownerId = activeHead.ownerId || activeHead.OwnerID;
            const ownerName = activeHead.ownerName || activeHead.OwnerName;
            const ownerEmail = activeHead.ownerEmail || activeHead.OwnerEmail; // Assuming email might be in history

            const currentUsername = (user.username || '').toLowerCase();
            const currentName = (user.name || '').toLowerCase();
            const currentEmail = (user.email || '').toLowerCase();

            // Robust comparison: Check ID, Name, and Email
            if (ownerId && String(ownerId).toLowerCase() === currentUsername) {
              isAssigned = true;
            } else if (ownerName && String(ownerName).toLowerCase() === currentName) {
              isAssigned = true;
            } else if (ownerEmail && String(ownerEmail).toLowerCase() === currentEmail) {
              isAssigned = true;
            }

            // Special case for Directors/VPs: If they are the ones who created the assignment or if the name matches partially
            if (!isAssigned && (user.role === 'director' || user.role === 'vice_president')) {
              // If the owner name contains the user's name or vice versa (handling "First Last" vs "Last, First" etc)
              if (ownerName && (String(ownerName).toLowerCase().includes(currentName) || currentName.includes(String(ownerName).toLowerCase()))) {
                isAssigned = true;
              }
            }
          }
        }

        setIsHead(isAssigned);

      } catch (err: any) {
        console.error('Failed to fetch request:', err);
        setError(err.message || 'Failed to load request details');
      } finally {
        setLoading(false);
      }
    };
    fetchRequest();
  }, [id, user]);

  const handleBack = () => {
    navigate(`/dashboard/${role}/requests`);
  };

  const getStatusBadgeClass = (statusName: string) => {
    const statusColors: Record<string, string> = {
      Submitted: 'bg-blue-100 text-blue-800 border-blue-200',
      'Under Evaluation': 'bg-yellow-100 text-yellow-800 border-yellow-200',
      Approved: 'bg-green-100 text-green-800 border-green-200',
      Rejected: 'bg-red-100 text-red-800 border-red-200',
      Backlogged: 'bg-gray-100 text-gray-800 border-gray-200',
      'In Progress': 'bg-purple-100 text-purple-800 border-purple-200',
      Completed: 'bg-green-100 text-green-800 border-green-200',
      Delivered: 'bg-emerald-100 text-emerald-800 border-emerald-200',
      Closed: 'bg-gray-100 text-gray-800 border-gray-200',
    };
    return statusColors[statusName] || 'bg-gray-100 text-gray-800 border-gray-200';
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center font-['Times_New_Roman',_Times,_serif]">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-purple-900 mb-4" />
          <p className="text-gray-600 text-lg">Loading request details...</p>
        </div>
      </div>
    );
  }

  if (error || !request) {
    return (
      <div className="min-h-screen bg-gray-50 font-['Times_New_Roman',_Times,_serif]">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="bg-red-50 border border-red-200 rounded-lg p-6">
            <div className="flex items-center">
              <XCircle className="w-6 h-6 text-red-600 mr-3" />
              <div>
                <h3 className="text-lg font-semibold text-red-800">Error Loading Request</h3>
                <p className="text-sm text-red-600 mt-1">{error || 'Request not found'}</p>
              </div>
            </div>
            <button onClick={handleBack} className="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors">
              Back to Requests
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
      {/* Header */}
      <header className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30 shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button onClick={handleBack} className="p-2 hover:bg-[#B351A9]/10 rounded-xl transition-colors">
              <ChevronLeft className="w-5 h-5 text-[#B351A9]" />
            </button>
            <div>
            <h1 className="text-2xl font-bold text-[#B351A9] fl">Request Details</h1>
            <p className="text-sm text-[#85257C]">View complete request information</p>
            </div>
          </div>

          <div className="text-right">
            <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
            <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Request ID & Status */}
        <section className="mb-6 bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 rounded-lg border border-[#B351A9]/30 shadow-sm p-6">
          <div className="flex items-center justify-between">
            <div>
              <div className="flex items-center space-x-4">
                <div>
                  <span className="text-sm font-medium text-[#85257C]">Request ID</span>
                  <div className="text-2xl font-mono font-bold text-[#B351A9] mt-1">
                    {request.requestID || `PR-${request.id}`}
                  </div>
                </div>
                {request.referenceNo && (
                  <div className="pl-4 border-l border-[#CDA352]/40">
                    <span className="text-sm font-medium text-[#85257C]">Reference No</span>
                    <div className="text-lg font-semibold text-[#B351A9] mt-1">{request.referenceNo}</div>
                  </div>
                )}
              </div>
            </div>
            <div className="text-right">
              <span className="text-sm font-medium text-[#85257C] block mb-2">Status</span>
              <span className={`px-4 py-2 rounded-lg text-sm font-semibold border ${getStatusBadgeClass(request.status.name)}`}>
                {request.status.name}
              </span>
            </div>
          </div>
        </section>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left Column – Main Content */}
          <div className="lg:col-span-2 space-y-6">
            {/* Core Information */}
            <section className="bg-gradient-to-br from-[#B351A9]/5 via-white to-[#E4CA86]/5 rounded-lg border border-[#CDA352]/30 shadow-sm p-6">
              <h2 className="text-lg font-semibold text-[#B351A9] mb-4 pb-3 border-b border-[#CDA352]/40 flex items-center">
                <FileText className="w-5 h-5 mr-2 text-[#CDA352]" />
                Core Information
              </h2>
              <div className="space-y-4">
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Request Title</span>
                  <p className="text-[#B351A9] text-base font-medium">{request.requestTitle}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Description</span>
                  <p className="text-gray-900 text-base whitespace-pre-wrap">{request.requestDescription}</p>
                </div>
              </div>
            </section>

            {/* Classification */}
            <section className="bg-gradient-to-br from-[#E4CA86]/5 via-white to-[#B351A9]/5 rounded-lg border border-[#B351A9]/30 shadow-sm p-6">
              <h2 className="text-lg font-semibold text-[#B351A9] mb-4 pb-3 border-b border-[#CDA352]/40 flex items-center">
                <AlertCircle className="w-5 h-5 mr-2 text-[#CDA352]" />
                Classification &amp; Categorization
              </h2>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Request Type</span>
                  <p className="text-gray-900">{request.requestType.name || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Request Category</span>
                  <p className="text-gray-900">{request.requestCategory.name || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Service Category</span>
                  <p className="text-gray-900">{request.serviceCategory.name || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Product Category</span>
                  <p className="text-gray-900">{request.productCategory.name || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Business Impact</span>
                  <p className="text-gray-900">{request.businessImpact.name || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Request Urgency</span>
                  <p className="text-gray-900">{request.requestUrgency.name || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Priority Level</span>
                  <span className="inline-block px-3 py-1 rounded text-sm font-semibold" style={{
                    backgroundColor: request.priority.color ? `${request.priority.color}20` : '#f3f4f620',
                    color: request.priority.color || '#6b7280',
                    border: `1px solid ${request.priority.color || '#e5e7eb'}`,
                  }}>
                    {request.priority.name || '—'}
                  </span>
                </div>
                {request.riskLevel && (
                  <div>
                    <span className="text-sm font-medium text-[#85257C] block mb-1">Risk Level</span>
                    <p className="text-gray-900">{request.riskLevel.name}</p>
                  </div>
                )}
                {request.complexityLevel && (
                  <div>
                    <span className="text-sm font-medium text-[#85257C] block mb-1">Complexity Level</span>
                    <p className="text-gray-900">{request.complexityLevel.name}</p>
                  </div>
                )}
              </div>
            </section>

            {/* Additional Metadata */}
            <section className="bg-gradient-to-br from-[#B351A9]/5 via-white to-[#E4CA86]/5 rounded-lg border border-[#CDA352]/30 shadow-sm p-6">
              <h2 className="text-lg font-semibold text-[#B351A9] mb-4 pb-3 border-b border-[#CDA352]/40 flex items-center">
                <TrendingUp className="w-5 h-5 mr-2 text-[#CDA352]" />
                Additional Metadata
              </h2>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Strategic Alignment</span>
                  <p className="text-gray-900">{request.strategicAlignment || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Requested Delivery Date</span>
                  <p className="text-gray-900">
                    {request.requestedDeliveryDate
                      ? new Date(request.requestedDeliveryDate).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })
                      : '—'}
                  </p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Estimated Cost (ETB)</span>
                  <p className="text-gray-900">
                    {request.estimatedCost
                      ? new Intl.NumberFormat('en-ET', { style: 'currency', currency: 'ETB' }).format(request.estimatedCost)
                      : '—'}
                  </p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Estimated Benefit (ETB)</span>
                  <p className="text-gray-900">
                    {request.estimatedBenefit
                      ? new Intl.NumberFormat('en-ET', { style: 'currency', currency: 'ETB' }).format(request.estimatedBenefit)
                      : '—'}
                  </p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Benefit Capture Duration</span>
                  <p className="text-gray-900">
                    {request.benefitCaptureDuration
                      ? `${request.benefitCaptureDuration} month${request.benefitCaptureDuration !== 1 ? 's' : ''}`
                      : '—'}
                  </p>
                </div>
              </div>
            </section>

            {/* Evaluation Scores */}
            {(request.totalScore !== null && request.totalScore !== undefined) || request.feasibilityScore || request.businessValueScore || request.technicalComplexityScore ? (
              <section className="bg-gradient-to-br from-[#E4CA86]/5 via-white to-[#B351A9]/5 rounded-lg border border-[#B351A9]/30 shadow-sm p-6">
                <h2 className="text-lg font-semibold text-[#B351A9] mb-4 pb-3 border-b border-[#CDA352]/40 flex items-center">
                  <CheckCircle className="w-5 h-5 mr-2 text-[#CDA352]" />
                  Evaluation Scores
                </h2>
                <div className="grid grid-cols-2 gap-4">
                  {request.feasibilityScore !== null && request.feasibilityScore !== undefined && (
                    <div>
                      <span className="text-sm font-medium text-[#85257C] block mb-1">Feasibility Score</span>
                      <p className="text-[#B351A9] text-lg font-semibold">{request.feasibilityScore}</p>
                    </div>
                  )}
                  {request.businessValueScore !== null && request.businessValueScore !== undefined && (
                    <div>
                      <span className="text-sm font-medium text-[#85257C] block mb-1">Business Value Score</span>
                      <p className="text-[#B351A9] text-lg font-semibold">{request.businessValueScore}</p>
                    </div>
                  )}
                  {request.technicalComplexityScore !== null && request.technicalComplexityScore !== undefined && (
                    <div>
                      <span className="text-sm font-medium text-[#85257C] block mb-1">Technical Complexity Score</span>
                      <p className="text-[#B351A9] text-lg font-semibold">{request.technicalComplexityScore}</p>
                    </div>
                  )}
                  {request.totalScore !== null && request.totalScore !== undefined && (
                    <div>
                      <span className="text-sm font-medium text-[#85257C] block mb-1">Total Score</span>
                      <p className="text-gray-900 text-2xl font-bold text-[#B351A9]">{request.totalScore.toFixed(1)}</p>
                    </div>
                  )}
                  {request.evaluationRemarks && (
                    <div className="col-span-2">
                      <span className="text-sm font-medium text-[#85257C] block mb-1">Evaluation Remarks</span>
                      <p className="text-gray-900 whitespace-pre-wrap">{request.evaluationRemarks}</p>
                    </div>
                  )}
                </div>
              </section>
            ) : null}
          </div>

          {/* Right Column – Requestor Details & Workflow */}
          <div className="space-y-6">
            {/* Requestor Details */}
            <section className="bg-gradient-to-br from-[#B351A9]/5 via-white to-[#E4CA86]/5 rounded-lg border border-[#CDA352]/30 shadow-sm p-6">
              <h2 className="text-lg font-semibold text-[#B351A9] mb-4 pb-3 border-b border-[#CDA352]/40 flex items-center">
                <User className="w-5 h-5 mr-2 text-[#CDA352]" />
                Requestor Details
              </h2>
              <div className="space-y-4">
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Requested By (AD User ID)</span>
                  <p className="text-gray-900">{request.requestedBy || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Requested By Name</span>
                  <p className="text-gray-900">{request.requestedByName || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Organization</span>
                  <p className="text-gray-900">{request.organizationName || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Business Sector</span>
                  <p className="text-gray-900">{request.businessSector || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Business Division</span>
                  <p className="text-gray-900">{request.businessDivision || '—'}</p>
                </div>
                <div>
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Business Department</span>
                  <p className="text-gray-900">{request.businessDepartment || '—'}</p>
                </div>
                <div className="pt-3 border-t border-[#CDA352]/40">
                  <div className="flex items-center mb-2">
                    <Mail className="w-4 h-4 text-[#CDA352] mr-2" />
                    <span className="text-sm font-medium text-[#85257C]">Primary Email</span>
                  </div>
                  <p className="text-gray-900 ml-6">{request.primaryContactEmail || '—'}</p>
                </div>
                {request.secondaryContactEmail && (
                  <div className="pt-3 border-t border-[#CDA352]/40">
                    <div className="flex items-center mb-2">
                      <Mail className="w-4 h-4 text-[#CDA352] mr-2" />
                      <span className="text-sm font-medium text-[#85257C]">Secondary Email</span>
                    </div>
                    <p className="text-gray-900 ml-6">{request.secondaryContactEmail}</p>
                  </div>
                )}
                <div className="pt-3 border-t border-[#CDA352]/40">
                  <div className="flex items-center mb-2">
                    <Phone className="w-4 h-4 text-[#CDA352] mr-2" />
                    <span className="text-sm font-medium text-[#85257C]">Primary Phone</span>
                  </div>
                  <p className="text-gray-900 ml-6">{request.primaryContactPhone || '—'}</p>
                </div>
                {request.secondaryContactPhone && (
                  <div className="pt-3 border-t border-[#CDA352]/40">
                    <div className="flex items-center mb-2">
                      <Phone className="w-4 h-4 text-[#CDA352] mr-2" />
                      <span className="text-sm font-medium text-[#85257C]">Secondary Phone</span>
                    </div>
                    <p className="text-gray-900 ml-6">{request.secondaryContactPhone}</p>
                  </div>
                )}
              </div>
            </section>

            {/* Workflow & Assignment */}
            <section className="bg-gradient-to-br from-[#E4CA86]/5 via-white to-[#B351A9]/5 rounded-lg border border-[#B351A9]/30 shadow-sm p-6">
              <h2 className="text-lg font-semibold text-[#B351A9] mb-4 pb-3 border-b border-[#CDA352]/40 flex items-center">
                <Building2 className="w-5 h-5 mr-2 text-[#CDA352]" />
                Workflow &amp; Assignment
              </h2>
              <div className="space-y-4">
                {/* Always show Request Creator */}
                <div className="bg-gradient-to-r from-[#B351A9]/10 to-[#E4CA86]/10 rounded-lg p-3 border border-[#CDA352]/40">
                  <span className="text-sm font-medium text-[#85257C] block mb-1">Request Creator</span>
                  <p className="text-[#B351A9] font-semibold">
                    {request.requestedByName || request.requestedBy || '—'}
                  </p>
                  {request.requestedByName && request.requestedBy && (
                    <p className="text-xs text-[#85257C] mt-1">ID: {request.requestedBy}</p>
                  )}
                </div>

                {request.workflowStage && (
                  <div>
                    <span className="text-sm font-medium text-[#85257C] block mb-1">Workflow Stage</span>
                    <p className="text-gray-900">{request.workflowStage.name || '—'}</p>
                  </div>
                )}
                {request.assignedTeam && (
                  <div>
                    <span className="text-sm font-medium text-[#85257C] block mb-1">Assigned Team</span>
                    <p className="text-gray-900">{request.assignedTeam}</p>
                  </div>
                )}

                {/* Only show Head Reviewer if explicitly assigned (check ownerHistory) */}
                {(() => {
                  if (ownerHistory && ownerHistory.length > 0) {
                    const activeHead = ownerHistory.find((h: any) =>
                      (h.ownerRole || h.OwnerRole || '').toLowerCase().includes('head') &&
                      (h.status || h.Status || '').toLowerCase() === 'active'
                    );
                    const recentHead = ownerHistory.slice().reverse().find((h: any) =>
                      (h.ownerRole || h.OwnerRole || '').toLowerCase().includes('head')
                    );
                    const headInfo = activeHead || recentHead;

                    if (headInfo) {
                      return (
                        <div>
                          <span className="text-sm font-medium text-[#85257C] block mb-1">Head Reviewer</span>
                          <p className="text-gray-900">
                            {headInfo.ownerName || headInfo.OwnerName || headInfo.ownerId || headInfo.OwnerID || '—'}
                          </p>
                        </div>
                      );
                    }
                  }
                  return null;
                })()}

                {/* Only show Assigned To if it exists and is different from creator */}
                {request.assignedTo && request.assignedTo !== request.requestedBy && (
                  <div>
                    <span className="text-sm font-medium text-[#85257C] block mb-1">Head of Request</span>
                    <p className="text-gray-900">{request.assignedTo}</p>
                  </div>
                )}

                {request.evaluatorID && (
                  <div>
                    <span className="text-sm font-medium text-[#85257C] block mb-1">Evaluator ID</span>
                    <p className="text-gray-900">{request.evaluatorID}</p>
                  </div>
                )}

                {isHead && (
                  <div className="pt-3 border-t border-[#CDA352]/40">
                    <button
                      onClick={() => navigate(`/dashboard/${role}/requests/${id}/head`)}
                      className="w-full inline-flex items-center justify-center px-4 py-2.5 rounded-lg bg-gradient-to-r from-[#B351A9] to-[#85257C] text-white text-sm font-semibold shadow-md hover:from-[#85257C] hover:to-[#B351A9] transition-all"
                    >
                      Go to Head Console (Assign Reviewers)
                    </button>
                    <p className="text-xs text-[#85257C] mt-2 text-center">
                      Use this to assign reviewers. Approval/rejection is available in the Evaluations tab below.
                    </p>
                  </div>
                )}
              </div>
            </section>
          </div>
        </div>

        {/* Request Summary navigation */}
        <div className="mt-8 flex justify-end">
          <button
            onClick={() => navigate(`/dashboard/${role}/requests/${id}/summary`)}
            className="inline-flex items-center px-5 py-2.5 rounded-lg bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] text-white text-sm font-semibold shadow-md hover:shadow-lg transition-all"
          >
            View Request Summary
          </button>
        </div>
      </main>
    </div>
  );
};

export default RequestDetail;
