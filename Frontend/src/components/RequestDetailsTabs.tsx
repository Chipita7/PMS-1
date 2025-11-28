import React, { useEffect, useMemo, useState } from "react";
import {
  ClipboardList,
  GitPullRequest,
  History,
  Layers,
  MessageSquare,
  Shield,
  Star,
  UserCircle2,
} from "lucide-react";
import {
  projectRequestService,
  ProjectRequestSummary,
  WorkflowHistoryEntry,
  StatusHistoryEntry,
} from "@/services/projectRequestService";
import { evaluationService, EvaluationDto } from "@/services/evaluationService";
import { commentService } from "@/services/commentService";
import { feedbackService } from "@/services/feedbackService";
import { auditService, AuditEntry, AuditSummary } from "@/services/auditService";
import { reviewTaskService } from "@/services/reviewTaskService";
import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import UserSelect from "@/components/UserSelect";
import { toast } from "react-hot-toast";

interface CommentDto {
  id: number;
  authorName: string;
  authorID: string;
  commentText: string;
  timestamp: string;
}

interface FeedbackDto {
  id: number;
  authorName: string;
  rating: number;
  feedbackText: string;
  timestamp: string;
}

interface ReviewTask {
  id: number;
  title: string;
  assignee: string;
  assigneeRole: string;
  status: string;
  dueDate?: string;
  submittedAt?: string;
}

interface OwnerHistoryEntry {
  id: number;
  ownerId?: string;
  ownerName?: string;
  ownerRole?: string;
  startDate: string;
  endDate?: string;
  status: string;
}

type TabKey =
  | "summary"
  | "evaluations"
  | "tasks"
  | "comments"
  | "feedback"
  | "workflow"
  | "owner"
  | "audit";

interface Props {
  requestId: number;
}

const formatDateTime = (date?: string | null) => {
  if (!date) return "—";
  return new Date(date).toLocaleString();
};

const formatDate = (date?: string | null) => {
  if (!date) return "—";
  return new Date(date).toLocaleDateString();
};

const renderConfigValue = (value: unknown) => {
  if (!value) return "—";
  if (typeof value === "string") return value;
  if (typeof value === "object" && value !== null) {
    const maybeName = (value as { name?: string }).name;
    if (maybeName) return maybeName;
  }
  return String(value);
};

const formatDuration = (days?: number | null) => {
  if (days === null || days === undefined) return "—";
  if (days === 0) return "<1 day";
  return `${days} day${days === 1 ? "" : "s"}`;
};

const EmptyState: React.FC<{ icon: React.ReactNode; title: string; description: string }> = ({
  icon,
  title,
  description,
}) => (
  <div className="border border-dashed border-[#CDA352]/30 rounded-xl p-6 text-center text-gray-500">
    <div className="flex justify-center mb-3 text-[#B351A9]/60">{icon}</div>
    <p className="font-medium text-[#273238]">{title}</p>
    <p className="text-sm text-gray-500">{description}</p>
  </div>
);

const RequestDetailsTabs: React.FC<Props> = ({ requestId }) => {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<TabKey>("summary");
  const [summary, setSummary] = useState<ProjectRequestSummary | null>(null);
  const [evaluations, setEvaluations] = useState<EvaluationDto[]>([]);
  const [tasks, setTasks] = useState<ReviewTask[]>([]);
  const [comments, setComments] = useState<CommentDto[]>([]);
  const [feedback, setFeedback] = useState<FeedbackDto[]>([]);
  const [workflowHistory, setWorkflowHistory] = useState<WorkflowHistoryEntry[]>([]);
  const [statusHistory, setStatusHistory] = useState<StatusHistoryEntry[]>([]);
  const [ownerHistory, setOwnerHistory] = useState<OwnerHistoryEntry[]>([]);
  const [auditTrail, setAuditTrail] = useState<AuditEntry[]>([]);
  const [auditSummary, setAuditSummary] = useState<AuditSummary | null>(null);
  const [recentAudit, setRecentAudit] = useState<AuditEntry[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [progressing, setProgressing] = useState(false);
  const [progressError, setProgressError] = useState<string | null>(null);

  const [newComment, setNewComment] = useState<string>("");
  const [submittingComment, setSubmittingComment] = useState(false);
  const [showCommentsList, setShowCommentsList] = useState(false);

  const [rating, setRating] = useState<number | null>(null);
  const [feedbackRemark, setFeedbackRemark] = useState<string>("");
  const [submittingFeedback, setSubmittingFeedback] = useState(false);
  const [showFeedbackList, setShowFeedbackList] = useState(false);

  const [hasGivenFeedback, setHasGivenFeedback] = useState<boolean>(false);
  const [editingCommentId, setEditingCommentId] = useState<number | null>(null);
  const [editCommentText, setEditCommentText] = useState<string>("");
  const [editingFeedbackId, setEditingFeedbackId] = useState<number | null>(null);
  const [editRating, setEditRating] = useState<number | null>(null);
  const [editRemark, setEditRemark] = useState<string>("");

  // Add a local state for form for the logged-in user's draft evaluation
  const [editingEvalId, setEditingEvalId] = useState<number | null>(null);
  const [localForm, setLocalForm] = useState<Partial<EvaluationDto>>({});
  const [submittingEval, setSubmittingEval] = useState(false);
  const [evalError, setEvalError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const [
          req,
          evals,
          reviewTasks,
          comm,
          fb,
          workflow,
          owners,
          auditEntries,
          auditSummaryData,
          auditRecent,
          hasGiven
        ] = await Promise.all([
          projectRequestService.getById(requestId),
          evaluationService.getEvaluationsByRequest(requestId),
          projectRequestService.getReviewTasks(requestId),
          commentService.getCommentsByRequest(requestId),
          feedbackService.getFeedbackByRequest(requestId),
          projectRequestService.getWorkflowHistory(requestId),
          projectRequestService.getOwnerHistory(requestId),
          auditService.getAuditTrail(requestId),
          auditService.getAuditSummary(requestId),
          auditService.getRecentActivity(requestId, 10),
          feedbackService.hasUserGivenFeedback(requestId),
        ]);

        if (!mounted) return;
        setSummary(req);
        setEvaluations(Array.isArray(evals) ? evals : []);
        setTasks(Array.isArray(reviewTasks) ? reviewTasks : []);
        setComments(Array.isArray(comm) ? comm : []);
        setFeedback(Array.isArray(fb) ? fb : []);
        setWorkflowHistory(workflow.workflowHistory || []);
        setStatusHistory(workflow.statusHistory || []);
        setOwnerHistory(Array.isArray(owners) ? owners : []);
        setAuditTrail(Array.isArray(auditEntries) ? auditEntries : []);
        setAuditSummary(auditSummaryData);
        setRecentAudit(Array.isArray(auditRecent) ? auditRecent : []);
        setHasGivenFeedback(Boolean(hasGiven));
      } catch (e: any) {
        if (!mounted) return;
        setError(e.message || "Failed to load request details");
      } finally {
        if (mounted) setLoading(false);
      }
    })();

    return () => {
      mounted = false;
    };
  }, [requestId]);

  const evaluationStats = useMemo(() => {
    if (!evaluations.length) {
      return { submitted: 0, total: 0, average: null as number | null };
    }
    const submitted = evaluations.filter((e) => e.evaluationStatus === "Submitted");

    // ✅ FIXED: Calculate average of each evaluator's individual average
    // Per user requirement: If 3 evaluators, calculate each evaluator's average (of their 3 scores),
    // then average those 3 individual averages to get TotalScore
    const validScores = submitted.filter(e =>
      e.feasibilityScore != null &&
      e.businessValueScore != null &&
      e.technicalComplexityScore != null
    );

    if (validScores.length === 0) {
      return { submitted: submitted.length, total: evaluations.length, average: null };
    }

    // Calculate each evaluator's individual average (of their 3 core scores)
    const individualAverages = validScores.map(e =>
      (e.feasibilityScore! + e.businessValueScore! + e.technicalComplexityScore!) / 3.0
    );

    // TotalScore = Average of all evaluators' individual averages
    const average = individualAverages.reduce((sum, val) => sum + val, 0) / individualAverages.length;

    return { submitted: submitted.length, total: evaluations.length, average };
  }, [evaluations]);

  const feedbackStats = useMemo(() => {
    if (!feedback.length) {
      return { average: null as number | null, total: 0 };
    }
    const average = feedback.reduce((sum, item) => sum + (item.rating || 0), 0) / feedback.length;
    return { average, total: feedback.length };
  }, [feedback]);

  const isHeadReviewer = summary && (user?.id === summary.headReviewerId || user?.username === summary.headReviewerName);
  const canProceed = evaluationStats.total > 0 && evaluationStats.submitted === evaluationStats.total;

  const handleEvalField = (name: string, value: string | number) => {
    setLocalForm(prev => ({ ...prev, [name]: value }));
  };

  const handleEvalEdit = (e: EvaluationDto) => {
    setEditingEvalId(e.id);
    setLocalForm({
      strategicAlignmentScore: e.strategicAlignmentScore,
      feasibilityScore: e.feasibilityScore,
      businessValueScore: e.businessValueScore,
      technicalComplexityScore: e.technicalComplexityScore,
      evaluationRemarks: e.evaluationRemarks,
    });
  };

  const handleEvalSubmit = async (evaluationId: number, asDraft: boolean = false) => {
    setSubmittingEval(true);
    setEvalError(null);
    try {
      // Only submit selected criteria (if specified by head); otherwise submit all provided fields
      const allowed = getSelectedCriteriaSet();
      const payload: any = {};
      const keys: Array<keyof EvaluationDto> = [
        'strategicAlignmentScore',
        'feasibilityScore',
        'businessValueScore',
        'technicalComplexityScore',
      ];
      keys.forEach((k) => {
        const include = allowed.size === 0 || allowed.has(k as string);
        if (include && (localForm as any)[k] != null && (localForm as any)[k] !== '') {
          (payload as any)[k] = (localForm as any)[k];
        }
      });
      if (localForm.evaluationRemarks != null) payload.evaluationRemarks = localForm.evaluationRemarks;
      await evaluationService.submitEvaluation(evaluationId, payload);
      setEditingEvalId(null);
      setLocalForm({});
      // ✅ FIXED: Refresh both evaluations AND summary to show updated average totalScore
      const [evals, updatedSummary] = await Promise.all([
        evaluationService.getEvaluationsByRequest(requestId),
        projectRequestService.getById(requestId)
      ]);
      setEvaluations(Array.isArray(evals) ? evals : []);
      setSummary(updatedSummary);
    } catch (err: any) {
      setEvalError(err.message || 'Failed to submit evaluation');
    } finally {
      setSubmittingEval(false);
    }
  };

  const [showApprovalModal, setShowApprovalModal] = React.useState(false);
  const [showRejectionModal, setShowRejectionModal] = React.useState(false);
  const [approvalRemarks, setApprovalRemarks] = React.useState('');
  const [rejectionReason, setRejectionReason] = React.useState('');
  const [rejectionRemarks, setRejectionRemarks] = React.useState('');
  const [showCompleteTaskModal, setShowCompleteTaskModal] = React.useState<number | null>(null);
  const [showReassignTaskModal, setShowReassignTaskModal] = React.useState<number | null>(null);
  const [taskCompletionRemarks, setTaskCompletionRemarks] = React.useState('');
  const [reassignUserId, setReassignUserId] = React.useState('');
  const [reassignUserName, setReassignUserName] = React.useState('');
  const [completingTaskId, setCompletingTaskId] = React.useState<number | null>(null);

  // Helper: parse selected criteria and instructions from tasks assigned to current user (set by Head)
  const getSelectedCriteriaSet = React.useCallback((): Set<string> => {
    try {
      const meId = user?.id || '';
      const meUserName = (user as any)?.username || (user as any)?.userName || '';
      const my = (tasks || []).filter((t: any) => {
        const aid = (t?.assigneeId || t?.AssigneeId || t?.assignedTo || '').toString();
        const aname = (t?.assignee || '').toString();
        return (meId && aid === meId) || (meUserName && (aid === meUserName || aname === meUserName));
      });
      for (const t of my) {
        const desc = ((t as any).taskDescription || (t as any).TaskDescription || (t as any).description || '').toString();
        if (!desc) continue;
        const critMatch = desc.match(/Criteria:\s*([^\n\r]+)/i);
        if (critMatch && critMatch[1]) {
          const raw = critMatch[1]
            .split(',')
            .map((s) => s.trim().toLowerCase())
            .filter(Boolean);
          const map: Record<string, string> = {
            strategic: 'strategicAlignmentScore',
            feasibility: 'feasibilityScore',
            business: 'businessValueScore',
            technical: 'technicalComplexityScore',
          };
          const out = new Set<string>();
          raw.forEach((k) => { if (map[k]) out.add(map[k]); });
          if (out.size > 0) return out;
        }
      }
    } catch {}
    return new Set<string>();
  }, [tasks, user]);

  const getHeadInstructions = React.useCallback((): string | null => {
    try {
      const meId = user?.id || '';
      const meUserName = (user as any)?.username || (user as any)?.userName || '';
      const my = (tasks || []).filter((t: any) => {
        const aid = (t?.assigneeId || t?.AssigneeId || t?.assignedTo || '').toString();
        const aname = (t?.assignee || '').toString();
        return (meId && aid === meId) || (meUserName && (aid === meUserName || aname === meUserName));
      });
      for (const t of my) {
        const desc = ((t as any).taskDescription || (t as any).TaskDescription || (t as any).description || '').toString();
        if (!desc) continue;
        const instrMatch = desc.match(/Instructions:\s*([^\n\r]*)/i);
        if (instrMatch) return instrMatch[1]?.trim() || null;
      }
    } catch {}
    return null;
  }, [tasks, user]);

  const handleApproveAndProceed = async () => {
    if (!summary) return;
    setProgressing(true);
    setProgressError(null);
    try {
      const remarks = approvalRemarks.trim() || 'Evaluation phase approved. Ready for next workflow stage.';
      const res = await projectRequestService.approveRequest(summary.id, remarks);
      if (!res.success) throw new Error(res.message || 'Failed to progress workflow.');
      setShowApprovalModal(false);
      setApprovalRemarks('');
      // Refresh data instead of full page reload
      const [evals, updatedSummary] = await Promise.all([
        evaluationService.getEvaluationsByRequest(requestId),
        projectRequestService.getById(requestId)
      ]);
      setEvaluations(Array.isArray(evals) ? evals : []);
      setSummary(updatedSummary);
      try { window.dispatchEvent(new Event('requests-updated')); } catch {}
      toast.success('Evaluations approved successfully. Workflow stage updated.');
    } catch (e: any) {
      setProgressError(e.message || 'Failed to progress workflow.');
      toast.error(e.message || 'Failed to approve evaluations.');
    } finally {
      setProgressing(false);
    }
  };

  const handleRejectEvaluations = async () => {
    if (!summary || !rejectionReason.trim()) {
      toast.error('Please provide a rejection reason.');
      return;
    }
    setProgressing(true);
    setProgressError(null);
    try {
      const remarks = rejectionRemarks.trim() || 'Initial evaluations rejected by Head Reviewer.';
      const res = await projectRequestService.rejectRequest(summary.id, remarks, rejectionReason);
      if (!res.success) throw new Error(res.message || 'Failed to reject evaluations.');
      setShowRejectionModal(false);
      setRejectionReason('');
      setRejectionRemarks('');
      // Refresh data
      const [evals, updatedSummary] = await Promise.all([
        evaluationService.getEvaluationsByRequest(requestId),
        projectRequestService.getById(requestId)
      ]);
      setEvaluations(Array.isArray(evals) ? evals : []);
      setSummary(updatedSummary);
      try { window.dispatchEvent(new Event('requests-updated')); } catch {}
      toast.success('Evaluations rejected. Request status updated.');
    } catch (e: any) {
      setProgressError(e.message || 'Failed to reject evaluations.');
      toast.error(e.message || 'Failed to reject evaluations.');
    } finally {
      setProgressing(false);
    }
  };

  const tabConfig: { key: TabKey; label: string }[] = [
    { key: "summary", label: "Summary" },
    { key: "evaluations", label: "Evaluations" },
    { key: "tasks", label: "Reviewer Responses" },
    { key: "comments", label: "Comments" },
    { key: "feedback", label: "Feedback" },
    { key: "workflow", label: "Workflow History" },
    { key: "owner", label: "Ownership" },
    { key: "audit", label: "Audit Trail" },
  ];

  if (loading) {
    return (
      <div className="border border-gray-200 rounded-xl p-6 text-center text-gray-600">
        Loading detailed activity...
      </div>
    );
  }

  if (error) {
    return (
      <div className="border border-red-200 rounded-xl p-6 text-center text-red-600 bg-red-50">
        {error}
      </div>
    );
  }

  const renderSummaryTab = () => (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white border border-[#CDA352]/20 rounded-xl p-4 shadow-sm">
          <p className="text-sm text-gray-500">Evaluations Submitted</p>
          <p className="text-2xl font-semibold text-[#273238]">
            {evaluationStats.submitted}/{evaluationStats.total}
          </p>
          <p className="text-xs text-gray-400">
            {/* ✅ FIXED: Show calculated average from request (which is now average of all submitted evaluations) */}
            {summary?.totalScore != null
              ? `Average Total Score: ${summary.totalScore.toFixed(2)}`
              : evaluationStats.average !== null
                ? `Calculated Average: ${evaluationStats.average.toFixed(2)}`
                : "No submitted scores yet"}
          </p>
        </div>
        <div className="bg-white border border-[#CDA352]/20 rounded-xl p-4 shadow-sm">
          <p className="text-sm text-gray-500">Comments</p>
          <p className="text-2xl font-semibold text-[#273238]">{comments.length}</p>
          <p className="text-xs text-gray-400">Collaboration touchpoints</p>
        </div>
        <div className="bg-white border border-[#CDA352]/20 rounded-xl p-4 shadow-sm">
          <p className="text-sm text-gray-500">Feedback Rating</p>
          <p className="text-2xl font-semibold text-[#273238]">
            {feedbackStats.average !== null ? feedbackStats.average.toFixed(1) : "—"}
          </p>
          <p className="text-xs text-gray-400">{feedbackStats.total} response(s)</p>
        </div>
      </div>

      {summary && (
        <div className="bg-white border border-[#CDA352]/20 rounded-xl p-6 shadow-sm">
          <h3 className="text-base font-semibold text-[#273238] mb-4">Request Snapshot</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
            <div>
              <p className="text-gray-500">Current Status</p>
              <p className="font-medium text-gray-900">{renderConfigValue(summary.status)}</p>
            </div>
            <div>
              <p className="text-gray-500">Workflow Stage</p>
              <p className="font-medium text-gray-900">{renderConfigValue((summary as any).workflowStage)}</p>
            </div>
            <div>
              <p className="text-gray-500">Head Reviewer</p>
              <p className="font-medium text-gray-900">{summary.headReviewerName || summary.assignedTo || "—"}</p>
            </div>
            <div>
              <p className="text-gray-500">Priority</p>
              <p className="font-medium text-gray-900">{renderConfigValue(summary.priority)}</p>
            </div>
            <div>
              <p className="text-gray-500">Requestor</p>
              <p className="font-medium text-gray-900">{summary.requestedByName || "—"}</p>
            </div>
            <div>
              <p className="text-gray-500">Submitted</p>
              <p className="font-medium text-gray-900">{formatDate(summary.createdDate)}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );

  const renderEvaluations = () => {
    if (!evaluations.length) {
      return (
        <EmptyState
          icon={<ClipboardList className="w-6 h-6" />}
          title="No evaluations logged yet"
          description="As reviewers submit their scores, they will appear here."
        />
      );
    }

    return (
      <div className="space-y-4">
        {evaluations.map((evaluation) => {
          const isMine = user?.id && evaluation.reviewerId === user.id;
          const isDraft = evaluation.evaluationStatus === 'Draft';
          if (isMine && isDraft && editingEvalId === evaluation.id) {
            return (
              <div key={evaluation.id} className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="font-semibold text-gray-900">{evaluation.reviewerName || evaluation.reviewerId}</p>
                    <p className="text-xs text-gray-500">{evaluation.reviewerRole}</p>
                  </div>
                  <span className="px-3 py-1 rounded-full text-xs font-medium bg-yellow-50 text-yellow-700 border border-yellow-200">Draft (Editing)</span>
                </div>
                {(() => {
                  const allowed = getSelectedCriteriaSet();
                  const defs = [
                    { key: 'strategicAlignmentScore', label: 'Strategic (1-10)' },
                    { key: 'feasibilityScore', label: 'Feasibility (1-10)' },
                    { key: 'businessValueScore', label: 'Business Value (1-10)' },
                    { key: 'technicalComplexityScore', label: 'Technical (1-10)' },
                  ] as const;
                  const fields = allowed.size > 0 ? defs.filter(d => allowed.has(d.key)) : defs;
                  const headInstr = getHeadInstructions();
                  return (
                    <>
                      {headInstr && (
                        <div className="mt-3 p-3 rounded-lg border border-[#CDA352]/30 bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 text-sm">
                          <div className="font-semibold text-[#273238] mb-1">Head Instructions</div>
                          <div className="text-gray-700">{headInstr}</div>
                          <div className="text-xs text-gray-500 mt-1">Scoring scale: 1-10</div>
                        </div>
                      )}
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm mt-4">
                        {fields.map((f) => (
                          <div key={f.key}>
                            <p className="text-gray-500">{f.label}</p>
                            <Input
                              type="number"
                              min="1"
                              max="10"
                              value={(localForm as any)[f.key] ?? ''}
                              onChange={e => handleEvalField(f.key, Number(e.target.value))}
                              required
                            />
                          </div>
                        ))}
                      </div>
                    </>
                  );
                })()}
                <div className="mt-3">
                  <p className="text-gray-500">Remarks</p>
                  <Input type="text" value={localForm.evaluationRemarks ?? ''} onChange={e => handleEvalField('evaluationRemarks', e.target.value)} />
                </div>
                <div className="flex gap-3 mt-4">
                  <Button onClick={() => handleEvalSubmit(evaluation.id, false)} disabled={submittingEval}>Submit Evaluation</Button>
                  <Button variant="outline" onClick={() => handleEvalSubmit(evaluation.id, true)} disabled={submittingEval}>Save as Draft</Button>
                  <Button variant="ghost" onClick={() => setEditingEvalId(null)} disabled={submittingEval}>Cancel</Button>
                </div>
                {evalError && <p className="text-red-500 text-sm mt-2">{evalError}</p>}
              </div>
            );
          }
          // Show edit button for your own draft evaluations
          return (
            <div key={evaluation.id} className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="font-semibold text-gray-900">{evaluation.reviewerName || evaluation.reviewerId}</p>
                  <p className="text-xs text-gray-500">{evaluation.reviewerRole}</p>
                </div>
                <span className={`px-3 py-1 rounded-full text-xs font-medium ${evaluation.evaluationStatus === "Submitted" ? "bg-green-50 text-green-700 border border-green-200" : "bg-yellow-50 text-yellow-700 border border-yellow-200"}`}>{evaluation.evaluationStatus}</span>
              </div>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm mt-4">
                <div>
                  <p className="text-gray-500">Strategic</p>
                  <p className="font-medium text-gray-900">{evaluation.strategicAlignmentScore ?? "—"}</p>
                </div>
                <div>
                  <p className="text-gray-500">Feasibility</p>
                  <p className="font-medium text-gray-900">{evaluation.feasibilityScore ?? "—"}</p>
                </div>
                <div>
                  <p className="text-gray-500">Business Value</p>
                  <p className="font-medium text-gray-900">{evaluation.businessValueScore ?? "—"}</p>
                </div>
                <div>
                  <p className="text-gray-500">Technical</p>
                  <p className="font-medium text-gray-900">{evaluation.technicalComplexityScore ?? "—"}</p>
                </div>
              </div>
              {evaluation.evaluationRemarks && (
                <p className="text-sm text-gray-700 mt-4 border-t border-gray-100 pt-3">{evaluation.evaluationRemarks}</p>
              )}
              {isMine && isDraft && editingEvalId !== evaluation.id && (
                <Button className="mt-2" onClick={() => handleEvalEdit(evaluation)} disabled={submittingEval}>Edit / Submit</Button>
              )}
            </div>
          );
        })}
        {isHeadReviewer && (
          <div className="mt-6 p-4 bg-gray-50 rounded-lg border border-gray-200">
            <h4 className="font-semibold text-gray-900 mb-3">Head Reviewer Actions</h4>
            <div className="flex gap-3 flex-wrap">
              <Button
                onClick={() => setShowApprovalModal(true)}
                disabled={!canProceed || progressing}
                className="bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] text-white"
              >
                {progressing ? "Processing..." : "Approve & Proceed"}
              </Button>
              <Button
                onClick={() => setShowRejectionModal(true)}
                disabled={!canProceed || progressing}
                variant="destructive"
                className="bg-gradient-to-r from-red-600 to-red-700 hover:from-red-700 hover:to-red-600 text-white"
              >
                Reject Evaluations
              </Button>
            </div>
            {progressError && (
              <p className="text-red-500 mt-2 text-sm">{progressError}</p>
            )}
            {!canProceed && (
              <p className="text-gray-500 mt-2 text-xs">All assigned evaluations must be submitted before you can approve or reject.</p>
            )}
          </div>
        )}

        {/* Approval Modal */}
        {showApprovalModal && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Approve Evaluations & Proceed</h3>
              <p className="text-sm text-gray-600 mb-4">
                This will approve all submitted evaluations and transition the request to the next workflow stage.
              </p>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Approval Remarks (Optional)</label>
                <textarea
                  value={approvalRemarks}
                  onChange={(e) => setApprovalRemarks(e.target.value)}
                  placeholder="Add any remarks about this approval..."
                  className="w-full p-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
                  rows={3}
                />
              </div>
              <div className="flex gap-3 justify-end">
                <Button
                  onClick={() => {
                    setShowApprovalModal(false);
                    setApprovalRemarks('');
                  }}
                  variant="outline"
                  disabled={progressing}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleApproveAndProceed}
                  disabled={progressing}
                  className="bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] text-white"
                >
                  {progressing ? "Processing..." : "Approve & Proceed"}
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Rejection Modal */}
        {showRejectionModal && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Reject Evaluations</h3>
              <p className="text-sm text-gray-600 mb-4">
                This will reject the initial evaluations. Please provide a reason.
              </p>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Rejection Reason <span className="text-red-500">*</span></label>
                <select
                  value={rejectionReason}
                  onChange={(e) => setRejectionReason(e.target.value)}
                  className="w-full p-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
                  required
                >
                  <option value="">Select a reason...</option>
                  <option value="InsufficientInformation">Insufficient Information</option>
                  <option value="LowScores">Low Evaluation Scores</option>
                  <option value="MissingEvaluations">Missing Required Evaluations</option>
                  <option value="QualityConcerns">Quality Concerns</option>
                  <option value="Other">Other</option>
                </select>
              </div>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Rejection Remarks</label>
                <textarea
                  value={rejectionRemarks}
                  onChange={(e) => setRejectionRemarks(e.target.value)}
                  placeholder="Provide detailed remarks about the rejection..."
                  className="w-full p-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
                  rows={3}
                />
              </div>
              <div className="flex gap-3 justify-end">
                <Button
                  onClick={() => {
                    setShowRejectionModal(false);
                    setRejectionReason('');
                    setRejectionRemarks('');
                  }}
                  variant="outline"
                  disabled={progressing}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleRejectEvaluations}
                  disabled={progressing || !rejectionReason.trim()}
                  className="bg-red-600 hover:bg-red-700 text-white"
                >
                  {progressing ? "Processing..." : "Reject"}
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    );
  };

  const handleCompleteTask = async (taskId: number) => {
    setCompletingTaskId(taskId);
    try {
      await reviewTaskService.completeTask(taskId, taskCompletionRemarks.trim() || undefined);
      toast.success('Task completed successfully');
      setShowCompleteTaskModal(null);
      setTaskCompletionRemarks('');
      // Refresh tasks
      const updatedTasks = await projectRequestService.getReviewTasks(requestId);
      setTasks(Array.isArray(updatedTasks) ? updatedTasks : []);
    } catch (e: any) {
      toast.error(e.message || 'Failed to complete task');
    } finally {
      setCompletingTaskId(null);
    }
  };

  const handleReassignTask = async (taskId: number) => {
    if (!reassignUserId.trim() || !reassignUserName.trim()) {
      toast.error('Please provide both user ID and name');
      return;
    }
    try {
      await reviewTaskService.reassignTask(taskId, reassignUserId.trim(), reassignUserName.trim());
      toast.success('Task reassigned successfully');
      setShowReassignTaskModal(null);
      setReassignUserId('');
      setReassignUserName('');
      // Refresh tasks
      const updatedTasks = await projectRequestService.getReviewTasks(requestId);
      setTasks(Array.isArray(updatedTasks) ? updatedTasks : []);
    } catch (e: any) {
      toast.error(e.message || 'Failed to reassign task');
    }
  };

  const renderTasks = () => {
    if (!tasks.length) {
      return (
        <EmptyState
          icon={<Layers className="w-6 h-6" />}
          title="No reviewer assignments yet"
          description="Head Reviewer can assign reviewers from the Head Review Console or Assign Reviewers page."
        />
      );
    }

    const isHeadReviewer = summary && (user?.id === summary.headReviewerId || user?.username === summary.headReviewerName);

    return (
      <div className="space-y-3">
        {tasks.map((task) => {
          const isMyTask = user?.id === (task as any).assigneeId || user?.username === (task as any).assigneeId || user?.id === (task as any).assignee || user?.username === (task as any).assignee;
          const isCompleted = task.status?.toLowerCase() === 'completed';

          return (
            <div key={task.id} className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="font-semibold text-gray-900">{task.title}</p>
                  <p className="text-xs text-gray-500">{task.assigneeRole}</p>
                </div>
                <span className={`px-3 py-1 rounded-full text-xs font-medium ${isCompleted
                  ? 'bg-green-50 text-green-700 border border-green-200'
                  : 'bg-yellow-50 text-yellow-700 border border-yellow-200'
                  }`}>
                  {task.status}
                </span>
              </div>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-3 text-sm mt-3">
                <div>
                  <p className="text-gray-500">Assignee</p>
                  <p className="font-medium text-gray-900">{task.assignee || "—"}</p>
                </div>
                <div>
                  <p className="text-gray-500">Due Date</p>
                  <p className="font-medium text-gray-900">{formatDate(task.dueDate)}</p>
                </div>
                <div>
                  <p className="text-gray-500">Completed</p>
                  <p className="font-medium text-gray-900">{task.submittedAt ? formatDate(task.submittedAt) : "—"}</p>
                </div>
              </div>
              <div className="mt-4 flex gap-2 flex-wrap">
                {isMyTask && !isCompleted && (
                  <Button
                    onClick={() => setShowCompleteTaskModal(task.id)}
                    className="bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] text-white text-sm"
                    size="sm"
                  >
                    Submit Review Response
                  </Button>
                )}
                {isHeadReviewer && !isCompleted && (
                  <Button
                    onClick={() => setShowReassignTaskModal(task.id)}
                    variant="outline"
                    className="text-sm"
                    size="sm"
                  >
                    Reassign
                  </Button>
                )}
              </div>
            </div>
          );
        })}

        {/* Review Response Modal */}
        {showCompleteTaskModal && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Submit Review Response</h3>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">Review Response</label>
                <textarea
                  value={taskCompletionRemarks}
                  onChange={(e) => setTaskCompletionRemarks(e.target.value)}
                  placeholder="Write your review response..."
                  className="w-full p-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
                  rows={3}
                />
              </div>
              <div className="flex gap-3 justify-end">
                <Button
                  onClick={() => {
                    setShowCompleteTaskModal(null);
                    setTaskCompletionRemarks('');
                  }}
                  variant="outline"
                  disabled={completingTaskId === showCompleteTaskModal}
                >
                  Cancel
                </Button>
                <Button
                  onClick={() => handleCompleteTask(showCompleteTaskModal)}
                  disabled={completingTaskId === showCompleteTaskModal}
                  className="bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] text-white"
                >
                  {completingTaskId === showCompleteTaskModal ? "Submitting..." : "Submit Response"}
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Reassign Task Modal */}
        {showReassignTaskModal && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Reassign Task</h3>
              <div className="mb-4">
                <label className="block text-sm font-medium text-gray-700 mb-2">New Assignee <span className="text-red-500">*</span></label>
                <UserSelect
                  value={reassignUserId}
                  onChange={(ids, opts) => {
                    setReassignUserId(ids[0] || '');
                    setReassignUserName((opts[0]?.name) || '');
                  }}
                  placeholder="Select user"
                />
              </div>
              <div className="flex gap-3 justify-end">
                <Button
                  onClick={() => {
                    setShowReassignTaskModal(null);
                    setReassignUserId('');
                    setReassignUserName('');
                  }}
                  variant="outline"
                >
                  Cancel
                </Button>
                <Button
                  onClick={() => handleReassignTask(showReassignTaskModal)}
                  disabled={!reassignUserId.trim() || !reassignUserName.trim()}
                  className="bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] text-white"
                >
                  Reassign
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    );
  };

  const handleSubmitComment = async () => {
    if (!newComment.trim()) return;
    setSubmittingComment(true);
    try {
      await commentService.addComment({ projectRequestId: requestId, commentText: newComment.trim() });
      const updated = await commentService.getCommentsByRequest(requestId);
      setComments(Array.isArray(updated) ? updated : []);
      setNewComment("");
      setShowCommentsList(true);
      toast.success("Comment posted");
    } catch (e: any) {
      toast.error(e.message || "Failed to post comment");
    } finally {
      setSubmittingComment(false);
    }
  };

  const startEditComment = (id: number, text: string) => {
    setEditingCommentId(id);
    setEditCommentText(text);
  };

  const updateComment = async () => {
    if (!editingCommentId || !editCommentText.trim()) return;
    try {
      await commentService.updateComment(editingCommentId, { commentText: editCommentText.trim() });
      const updated = await commentService.getCommentsByRequest(requestId);
      setComments(Array.isArray(updated) ? updated : []);
      setEditingCommentId(null);
      setEditCommentText("");
      toast.success("Comment updated");
    } catch (e: any) {
      toast.error(e.message || "Failed to update comment");
    }
  };

  const deleteComment = async (id: number) => {
    try {
      await commentService.deleteComment(id);
      const updated = await commentService.getCommentsByRequest(requestId);
      setComments(Array.isArray(updated) ? updated : []);
      if (editingCommentId === id) {
        setEditingCommentId(null);
        setEditCommentText("");
      }
      toast.success("Comment deleted");
    } catch (e: any) {
      toast.error(e.message || "Failed to delete comment");
    }
  };

  const renderComments = () => (
    <div className="space-y-4">
      <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
        <p className="text-sm font-semibold text-gray-900 mb-2">Add a comment</p>
        <textarea
          value={newComment}
          onChange={(e) => setNewComment(e.target.value)}
          rows={3}
          placeholder="Write your comment..."
          className="w-full p-3 rounded-lg border border-gray-300 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 transition-all text-sm outline-none"
        />
        <div className="mt-3 flex items-center gap-2">
          <Button onClick={handleSubmitComment} disabled={!newComment.trim() || submittingComment}>
            {submittingComment ? "Posting..." : "Post Comment"}
          </Button>
          <Button variant="outline" onClick={() => setShowCommentsList((v) => !v)}>
            {showCommentsList ? "Hide All Comments" : `Show All Comments (${comments.length})`}
          </Button>
        </div>
      </div>
      {showCommentsList ? (
        comments.length ? (
          <div className="space-y-4">
            {comments.map((comment) => {
              const isMine = (user?.id && comment.authorID === user.id) || (user?.username && comment.authorName === user.username);
              const isEditing = editingCommentId === comment.id;
              return (
                <div key={comment.id} className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-semibold text-gray-900">{comment.authorName || comment.authorID}</p>
                      <p className="text-xs text-gray-500">{formatDateTime(comment.timestamp)}</p>
                    </div>
                    {isMine && (
                      <div className="flex items-center gap-2">
                        {isEditing ? (
                          <>
                            <Button size="sm" onClick={updateComment}>Save</Button>
                            <Button size="sm" variant="outline" onClick={() => { setEditingCommentId(null); setEditCommentText(""); }}>Cancel</Button>
                          </>
                        ) : (
                          <>
                            <Button size="sm" variant="outline" onClick={() => startEditComment(comment.id, comment.commentText)}>Edit</Button>
                            <Button size="sm" variant="destructive" onClick={() => deleteComment(comment.id)}>Delete</Button>
                          </>
                        )}
                      </div>
                    )}
                  </div>
                  {isEditing ? (
                    <textarea
                      value={editCommentText}
                      onChange={(e) => setEditCommentText(e.target.value)}
                      rows={3}
                      className="w-full p-3 mt-3 rounded-lg border border-gray-300 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 text-sm outline-none"
                    />
                  ) : (
                    <p className="text-sm text-gray-800 mt-3 whitespace-pre-line">{comment.commentText}</p>
                  )}
                </div>
              );
            })}
          </div>
        ) : (
          <EmptyState
            icon={<MessageSquare className="w-6 h-6" />}
            title="No comments yet"
            description="Reviewers and stakeholders can leave clarifications and decisions here."
          />
        )
      ) : null}
    </div>
  );

  const handleSubmitFeedback = async () => {
    if (!rating) {
      toast.error("Please select a rating");
      return;
    }
    if (hasGivenFeedback) {
      toast.error("You have already submitted feedback");
      return;
    }
    setSubmittingFeedback(true);
    try {
      await feedbackService.submitFeedback({ projectRequestId: requestId, rating, feedbackText: feedbackRemark.trim() });
      const updated = await feedbackService.getFeedbackByRequest(requestId);
      setFeedback(Array.isArray(updated) ? updated : []);
      setRating(null);
      setFeedbackRemark("");
      setShowFeedbackList(true);
      setHasGivenFeedback(true);
      toast.success("Feedback submitted");
    } catch (e: any) {
      toast.error(e.message || "Failed to submit feedback");
    } finally {
      setSubmittingFeedback(false);
    }
  };

  const startEditFeedback = (item: any) => {
    setEditingFeedbackId(item.id);
    setEditRating(item.rating || 0);
    setEditRemark(item.feedbackText || "");
  };

  const updateFeedback = async () => {
    if (!editingFeedbackId || !editRating) return;
    try {
      await feedbackService.updateFeedback(editingFeedbackId, { rating: editRating, feedbackText: editRemark });
      const [updated, hasGiven] = await Promise.all([
        feedbackService.getFeedbackByRequest(requestId),
        feedbackService.hasUserGivenFeedback(requestId),
      ]);
      setFeedback(Array.isArray(updated) ? updated : []);
      setEditingFeedbackId(null);
      setEditRating(null);
      setEditRemark("");
      setHasGivenFeedback(Boolean(hasGiven));
      toast.success("Feedback updated");
    } catch (e: any) {
      toast.error(e.message || "Failed to update feedback");
    }
  };

  const deleteFeedback = async (id: number, isMine: boolean) => {
    try {
      await feedbackService.deleteFeedback(id);
      const [updated, hasGiven] = await Promise.all([
        feedbackService.getFeedbackByRequest(requestId),
        feedbackService.hasUserGivenFeedback(requestId),
      ]);
      setFeedback(Array.isArray(updated) ? updated : []);
      if (editingFeedbackId === id) {
        setEditingFeedbackId(null);
        setEditRating(null);
        setEditRemark("");
      }
      setHasGivenFeedback(isMine ? false : Boolean(hasGiven));
      toast.success("Feedback deleted");
    } catch (e: any) {
      toast.error(e.message || "Failed to delete feedback");
    }
  };

  const renderFeedback = () => (
    <div className="space-y-4">
      <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
        <p className="text-sm font-semibold text-gray-900 mb-2">Give feedback</p>
        {hasGivenFeedback && !editingFeedbackId ? (
          <div className="flex items-center gap-2">
            <span className="text-sm text-gray-600">You have already submitted feedback.</span>
            {(() => {
              const mine = feedback.find((f: any) => (user?.username && f.authorName === user.username) || (user?.id && f.authorId === user.id));
              return mine ? (
                <Button size="sm" onClick={() => startEditFeedback(mine)}>Edit My Feedback</Button>
              ) : null;
            })()}
          </div>
        ) : (
          <>
            <div className="flex items-center gap-2 mb-3">
              {Array.from({ length: 5 }).map((_, idx) => (
                <button
                  key={idx}
                  aria-label={`Rate ${idx + 1}`}
                  onClick={() => setRating(idx + 1)}
                  className="p-1"
                >
                  <Star className={`w-6 h-6 ${rating && idx < rating ? "text-amber-500 fill-current" : "text-gray-300"}`} />
                </button>
              ))}
            </div>
            <Input
              type="text"
              placeholder="Remark (optional)"
              value={feedbackRemark}
              onChange={(e) => setFeedbackRemark(e.target.value)}
            />
            <div className="mt-3 flex items-center gap-2">
              <Button onClick={handleSubmitFeedback} disabled={!rating || submittingFeedback}>
                {submittingFeedback ? "Submitting..." : "Submit Feedback"}
              </Button>
              <Button variant="outline" onClick={() => setShowFeedbackList((v) => !v)}>
                {showFeedbackList ? "Hide All Feedback" : `Show All Feedback (${feedback.length})`}
              </Button>
            </div>
          </>
        )}
        {editingFeedbackId && (
          <div className="mt-4 border-t pt-3">
            <p className="text-sm font-semibold text-gray-900 mb-2">Edit my feedback</p>
            <div className="flex items-center gap-2 mb-3">
              {Array.from({ length: 5 }).map((_, idx) => (
                <button key={idx} onClick={() => setEditRating(idx + 1)} className="p-1" aria-label={`Edit rate ${idx + 1}`}>
                  <Star className={`w-6 h-6 ${editRating && idx < editRating ? "text-amber-500 fill-current" : "text-gray-300"}`} />
                </button>
              ))}
            </div>
            <Input type="text" placeholder="Remark (optional)" value={editRemark} onChange={(e) => setEditRemark(e.target.value)} />
            <div className="mt-3 flex items-center gap-2">
              <Button onClick={updateFeedback} disabled={!editRating}>{"Save"}</Button>
              <Button variant="outline" onClick={() => { setEditingFeedbackId(null); setEditRating(null); setEditRemark(""); }}>Cancel</Button>
            </div>
          </div>
        )}
      </div>
      {showFeedbackList ? (
        feedback.length ? (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {feedback.map((item) => {
              const isMine = (user?.username && item.authorName === user.username) || (user?.id && (item as any).authorId === user.id);
              return (
                <div key={item.id} className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
                  <div className="flex items-center justify-between">
                    <p className="font-semibold text-gray-900">{item.authorName}</p>
                    <div className="flex items-center gap-1 text-amber-500">
                      {Array.from({ length: 5 }).map((_, idx) => (
                        <Star key={idx} className={`w-4 h-4 ${idx < (item.rating || 0) ? "fill-current" : "text-gray-300"}`} />
                      ))}
                    </div>
                  </div>
                  <div className="flex items-center justify-between">
                    <p className="text-xs text-gray-500">{formatDateTime(item.timestamp)}</p>
                    {isMine && (
                      <div className="flex items-center gap-2">
                        <Button size="sm" variant="outline" onClick={() => startEditFeedback(item)}>Edit</Button>
                        <Button size="sm" variant="destructive" onClick={() => deleteFeedback(item.id, true)}>Delete</Button>
                      </div>
                    )}
                  </div>
                  <p className="text-sm text-gray-800 mt-3 whitespace-pre-line">{item.feedbackText}</p>
                </div>
              );
            })}
          </div>
        ) : (
          <EmptyState
            icon={<Star className="w-6 h-6" />}
            title="No stakeholder feedback"
            description="Stakeholders can record experience feedback once evaluation completes."
          />
        )
      ) : null}
    </div>
  );

  const renderWorkflow = () => (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
        <h4 className="font-semibold text-[#273238] mb-4 flex items-center gap-2">
          <GitPullRequest className="w-5 h-5 text-[#B351A9]" /> Workflow Stages
        </h4>
        {workflowHistory.length ? (
          <ul className="space-y-3 text-sm">
            {workflowHistory.map((item) => (
              <li key={item.id} className="border border-gray-100 rounded-lg p-3">
                <p className="font-semibold text-gray-900">{item.workflowStage}</p>
                <p className="text-xs text-gray-500">{item.changedBy}</p>
                <p className="text-xs text-gray-500">
                  {formatDate(item.startDate)} → {formatDate(item.endDate)}
                </p>
                <p className="text-xs text-gray-400">Duration: {formatDuration(item.durationDays)}</p>
              </li>
            ))}
          </ul>
        ) : (
          <EmptyState
            icon={<Layers className="w-6 h-6" />}
            title="No workflow stages recorded"
            description="Stage history will be tracked automatically when workflow events occur."
          />
        )}
      </div>

      <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
        <h4 className="font-semibold text-[#273238] mb-4 flex items-center gap-2">
          <History className="w-5 h-5 text-[#B351A9]" /> Status Transitions
        </h4>
        {statusHistory.length ? (
          <ul className="space-y-3 text-sm">
            {statusHistory.map((item) => (
              <li key={item.id} className="border border-gray-100 rounded-lg p-3">
                <p className="font-semibold text-gray-900">
                  {item.fromStatus} → {item.toStatus}
                </p>
                <p className="text-xs text-gray-500">{item.changedBy}</p>
                <p className="text-xs text-gray-500">{formatDateTime(item.changedAt)}</p>
                <p className="text-xs text-gray-400">Duration: {formatDuration(item.durationInPreviousStatus)}</p>
              </li>
            ))}
          </ul>
        ) : (
          <EmptyState
            icon={<History className="w-6 h-6" />}
            title="No status transitions yet"
            description="As the request progresses between states, they will show up here."
          />
        )}
      </div>
    </div>
  );

  const renderOwnerHistory = () => {
    if (!ownerHistory.length) {
      return (
        <EmptyState
          icon={<UserCircle2 className="w-6 h-6" />}
          title="No owner history recorded"
          description="Assign a head reviewer to start tracking ownership."
        />
      );
    }
    return (
      <div className="space-y-3">
        {ownerHistory.map((entry) => (
          <div key={entry.id} className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-semibold text-gray-900">{entry.ownerName || entry.ownerId}</p>
                <p className="text-xs text-gray-500">{entry.ownerRole}</p>
              </div>
              <span
                className={`text-xs px-2 py-0.5 rounded-full border ${entry.status === "Active"
                  ? "bg-green-50 text-green-700 border-green-200"
                  : "bg-gray-50 text-gray-600 border-gray-200"
                  }`}
              >
                {entry.status}
              </span>
            </div>
            <p className="text-xs text-gray-500 mt-2">
              {formatDate(entry.startDate)} → {formatDate(entry.endDate)}
            </p>
          </div>
        ))}
      </div>
    );
  };

  const renderAudit = () => {
    if (!auditTrail.length && !auditSummary) {
      return (
        <EmptyState
          icon={<Shield className="w-6 h-6" />}
          title="No audit events recorded"
          description="As users update this request, the compliance trail will appear here."
        />
      );
    }

    return (
      <div className="space-y-6">
        {auditSummary && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
              <p className="text-xs text-gray-500">Total Entries</p>
              <p className="text-2xl font-semibold text-gray-900">{auditSummary.totalAuditEntries}</p>
            </div>
            <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
              <p className="text-xs text-gray-500">Status Changes</p>
              <p className="text-2xl font-semibold text-gray-900">{auditSummary.statusChangeCount}</p>
            </div>
            <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
              <p className="text-xs text-gray-500">Owner Changes</p>
              <p className="text-2xl font-semibold text-gray-900">{auditSummary.ownerChangeCount}</p>
            </div>
            <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm">
              <p className="text-xs text-gray-500">Last Activity</p>
              <p className="text-sm font-semibold text-gray-900">{formatDateTime(auditSummary.lastActivity)}</p>
            </div>
          </div>
        )}

        {recentAudit.length > 0 && (
          <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
            <h4 className="font-semibold text-gray-900 mb-3">Recent Activity</h4>
            <ul className="space-y-3 text-sm">
              {recentAudit.map((entry) => (
                <li key={entry.id} className="border border-gray-100 rounded-lg p-3">
                  <p className="font-medium text-gray-900">{entry.description}</p>
                  <p className="text-xs text-gray-500">
                    {entry.changedBy} • {formatDateTime(entry.changedOn)}
                  </p>
                </li>
              ))}
            </ul>
          </div>
        )}

        {auditTrail.length > 0 && (
          <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
            <h4 className="font-semibold text-gray-900 mb-3">Full Audit Trail</h4>
            <ul className="divide-y divide-gray-100 text-sm">
              {auditTrail.slice(0, 25).map((entry) => (
                <li key={entry.id} className="py-3">
                  <p className="font-medium text-gray-900">{entry.action}</p>
                  <p className="text-xs text-gray-500">
                    {entry.changedBy} • {formatDateTime(entry.changedOn)}
                  </p>
                  <p className="text-xs text-gray-600">{entry.description}</p>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    );
  };

  const tabBody: Record<TabKey, React.ReactNode> = {
    summary: renderSummaryTab(),
    evaluations: renderEvaluations(),
    tasks: renderTasks(),
    comments: renderComments(),
    feedback: renderFeedback(),
    workflow: renderWorkflow(),
    owner: renderOwnerHistory(),
    audit: renderAudit(),
  };

  return (
    <div className="bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 border border-[#CDA352]/20 rounded-2xl p-4">
      <div className="flex flex-wrap gap-2 mb-4">
        {tabConfig.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`px-4 py-2 rounded-full text-sm font-medium border transition ${activeTab === tab.key
              ? "bg-gradient-to-r from-[#B351A9] to-[#85257C] text-white border-[#B351A9]"
              : "bg-white text-[#273238] border-[#CDA352]/30 hover:border-[#CDA352]"
              }`}
          >
            {tab.label}
          </button>
        ))}
      </div>
      <div>{tabBody[activeTab]}</div>
    </div>
  );
};

export default RequestDetailsTabs;

