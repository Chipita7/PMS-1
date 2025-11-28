import React from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ChevronLeft, Users, ClipboardList, CheckCircle, XCircle, UserPlus, AlertCircle } from "lucide-react";
import { toast } from "react-hot-toast";
import { projectRequestService } from "@/services/projectRequestService";
import { evaluationService, EvaluationDto } from "@/services/evaluationService";
import { useAuth } from "@/context/AuthContext";
import UserSelect from "@/components/UserSelect";
import { reviewTaskService } from "@/services/reviewTaskService";

const initialEvaluationRoles = [
  "Business Analyst",
  "Innovation Chapter",
  "Subject Matter Expert",
  "Stakeholder",
];

const refinementRoles = [
  "Head of Engineering",
  "Head of Data & AI",
  "Product Owner",
  "Information Security",
  "DevSecOps Team",
  "Subject Matter Expert",
];

const HeadReviewConsole: React.FC = () => {
  const navigate = useNavigate();
  const { role, id } = useParams<{ role: string; id: string }>();
  const { user } = useAuth();
  const requestId = Number(id);

  const [loading, setLoading] = React.useState(false);
  const [tasks, setTasks] = React.useState<any[]>([]);
  const [assignInputsInitial, setAssignInputsInitial] = React.useState<Record<string, string>>({});
  const [assignInputsRefine, setAssignInputsRefine] = React.useState<Record<string, string>>({});
  const [assignNamesInitial, setAssignNamesInitial] = React.useState<Record<string, string>>({});
  const [assignNamesRefine, setAssignNamesRefine] = React.useState<Record<string, string>>({});
  const [isAuthorized, setIsAuthorized] = React.useState<boolean | null>(null);
  const [requestSummary, setRequestSummary] = React.useState<any>(null);
  const [autoCreateTasks, setAutoCreateTasks] = React.useState<boolean>(true);
  const [criteria, setCriteria] = React.useState({ strategic: true, feasibility: true, business: true, technical: true });
  const [instructions, setInstructions] = React.useState<string>("");
  const [evaluations, setEvaluations] = React.useState<EvaluationDto[]>([]);
  const [viewEval, setViewEval] = React.useState<EvaluationDto | null>(null);

  const currentRole = role || "member";

  React.useEffect(() => {
    const checkAuthorization = async () => {
      if (!requestId || !user) {
        setIsAuthorized(false);
        return;
      }
      try {
        const myHeadIds = await projectRequestService.getHeadAssignmentsForCurrentUser();
        const isHead = Array.isArray(myHeadIds) && myHeadIds.includes(requestId);
        setIsAuthorized(isHead);

        if (isHead) {
          const summary = await projectRequestService.getById(requestId);
          setRequestSummary(summary);
        }
      } catch (e) {
        console.error("Failed to check head reviewer authorization:", e);
        setIsAuthorized(false);
      }
    };
    checkAuthorization();
  }, [requestId, user]);

  const loadTasks = React.useCallback(async () => {
    if (!requestId) return;
    try {
      const t = await projectRequestService.getReviewTasks(requestId);
      setTasks(Array.isArray(t) ? t : []);
    } catch (e: any) {
      console.warn("Failed to load review tasks:", e?.message || e);
      setTasks([]);
    }
  }, [requestId]);

  const loadEvaluations = React.useCallback(async () => {
    if (!requestId) return;
    try {
      const ev = await evaluationService.getEvaluationsByRequest(requestId);
      setEvaluations(Array.isArray(ev) ? ev : []);
    } catch (e: any) {
      console.warn("Failed to load evaluations:", e?.message || e);
      setEvaluations([]);
    }
  }, [requestId]);

  React.useEffect(() => {
    loadTasks();
    loadEvaluations();
  }, [loadTasks, loadEvaluations]);

  const isTaskComplete = (t: any) => {
    const s = (t?.status || t?.Status || "").toString().toLowerCase();
    return t?.completed === true || s === "completed" || s === "done" || s === "approved";
  };

  const allResponsesReceived = tasks.length > 0 && tasks.every(isTaskComplete);
  const initialTasks = React.useMemo(
    () => tasks.filter((t) => {
      const role = (t?.assigneeRole || t?.AssigneeRole || '').toString().toLowerCase();
      return initialEvaluationRoles.some((r) => r.toLowerCase() === role);
    }),
    [tasks]
  );
  const initialAllDone = initialTasks.length > 0 && initialTasks.every(isTaskComplete);

  const findEvaluationForTask = React.useCallback((t: any): EvaluationDto | undefined => {
    try {
      const taskAssigneeId = (t?.assigneeId || t?.AssigneeId || t?.assignedTo || "").toString();
      const taskAssigneeName = (t?.assignee || t?.AssignedToName || "").toString();
      const matches = (evaluations || []).filter((e) => {
        const idMatch = taskAssigneeId && e.reviewerId && e.reviewerId.toString() === taskAssigneeId;
        const nameMatch = taskAssigneeName && e.reviewerName && e.reviewerName.toString() === taskAssigneeName;
        const reqMatch = e.projectRequestId === requestId;
        return reqMatch && (idMatch || nameMatch);
      });
      return matches[0];
    } catch {
      return undefined;
    }
  }, [evaluations, requestId]);

  const allResponsesOrEvaluations = React.useMemo(() => {
    if (tasks.length === 0) return false;
    return tasks.every((t) => {
      if (isTaskComplete(t)) return true;
      const ev = findEvaluationForTask(t);
      return !!ev && ev.evaluationStatus === 'Submitted';
    });
  }, [tasks, findEvaluationForTask]);

  const handleAssign = async (phase: "initial" | "refinement", roleName: string, assigneeId: string) => {
    if (!requestId || !assigneeId) return;
    setLoading(true);
    try {
      await projectRequestService.assignReviewers(requestId, {
        assignedTeam: roleName,
        assignedTo: assigneeId,
        assigneeRole: roleName,
        reviewerType: phase,
        autoCreateTasks: autoCreateTasks,
      });
      if (autoCreateTasks) {
        try {
          await projectRequestService.generateTasksForReviewerType(requestId, roleName, assigneeId);
          const selectedKeys = Object.entries(criteria).filter(([, v]) => v).map(([k]) => k);
          const desc = `Instructions: ${instructions?.trim() || "N/A"}\nCriteria: ${selectedKeys.join(",")}\nScale: 1-10`;
          const fresh = await projectRequestService.getReviewTasks(requestId);
          const mine = (fresh || []).filter((t: any) => {
            const role = (t?.assigneeRole || t?.AssigneeRole || '').toString();
            const aid = (t?.assigneeId || t?.AssigneeId || t?.assignedTo || '').toString();
            const status = (t?.status || t?.Status || '').toString().toLowerCase();
            return role === roleName && aid === assigneeId && status !== 'completed' && status !== 'done' && status !== 'approved';
          });
          for (const t of mine) {
            const due = (t?.dueDate || t?.DueDate) ? new Date(t?.dueDate || t?.DueDate).toISOString() : new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
            try { await reviewTaskService.updateTask(Number(t.id || t.Id), desc, due); } catch {}
          }
        } catch (genErr: any) {
          console.warn("Task generation failed:", genErr?.message || genErr);
        }
      }
      const displayName = phase === "initial" ? (assignNamesInitial[roleName] || assigneeId) : (assignNamesRefine[roleName] || assigneeId);
      toast.success(`Assigned ${displayName} for ${phase === "initial" ? "Initial Evaluation" : "Refinement"} (${roleName})${autoCreateTasks ? " and generated tasks" : ""}`);
      await loadTasks();
      if (phase === "initial") {
        setAssignInputsInitial((p) => ({ ...p, [roleName]: "" }));
        setAssignNamesInitial((p) => ({ ...p, [roleName]: "" }));
      } else {
        setAssignInputsRefine((p) => ({ ...p, [roleName]: "" }));
        setAssignNamesRefine((p) => ({ ...p, [roleName]: "" }));
      }
    } catch (e: any) {
      toast.error(e?.message || "Failed to assign reviewer");
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async () => {
    if (!requestId) return;
    setLoading(true);
    try {
      try {
        const check = await projectRequestService.getCanApprove(requestId);
        if (!check.canApprove) {
          toast.error(check.message || "Cannot approve yet. Ensure all reviewers submitted and minimum score met.");
          setLoading(false);
          return;
        }
      } catch {
        // proceed; server will enforce anyway
      }
      await projectRequestService.approveRequest(requestId, "Approved by Head");
      await projectRequestService.startExecution(requestId);
      toast.success("Request approved and moved to project execution");
      try { window.dispatchEvent(new Event("requests-updated")); } catch {}
      navigate(`/dashboard/${currentRole}/requests`, { state: { statusFilter: 'approved' } });
    } catch (e: any) {
      toast.error(e?.message || "Failed to approve request");
    } finally {
      setLoading(false);
    }
  };

  const handleReject = async () => {
    if (!requestId) return;
    setLoading(true);
    try {
      await projectRequestService.rejectRequest(requestId, "Rejected by Head", "Insufficient evaluation");
      toast.success("Request rejected");
      try { window.dispatchEvent(new Event("requests-updated")); } catch {}
      navigate(`/dashboard/${currentRole}/requests`, { state: { statusFilter: 'rejected' } });
    } catch (e: any) {
      toast.error(e?.message || "Failed to reject request");
    } finally {
      setLoading(false);
    }
  };

  if (isAuthorized === null) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 flex items-center justify-center font-['Times_New_Roman',_Times,_serif]">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-[#B351A9] mb-4"></div>
          <p className="text-[#85257C] font-medium">Checking authorization...</p>
        </div>
      </div>
    );
  }

  if (isAuthorized === false) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
        <div className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30 shadow-sm">
          <div className="max-w-6xl mx-auto px-6 py-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <button onClick={() => navigate(-1)} className="p-2 hover:bg-[#B351A9]/10 rounded-lg transition-colors">
                  <ChevronLeft className="w-5 h-5 text-[#B351A9]" />
                </button>
                <div>
                  <h1 className="text-2xl font-bold text-[#B351A9]">Head of Evaluator Console</h1>
                </div>
              </div>
              <div className="text-right">
                <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
                <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
              </div>
            </div>
          </div>
        </div>
        <div className="max-w-6xl mx-auto px-6 py-8">
          <div className="bg-red-50 border-2 border-red-300 rounded-xl p-6 shadow-md">
            <div className="flex items-center">
              <AlertCircle className="w-6 h-6 text-red-600 mr-3 flex-shrink-0" />
              <div>
                <h3 className="text-lg font-semibold text-red-800">Access Denied</h3>
                <p className="text-sm text-red-600 mt-1">
                  You are not assigned as the Head of Evaluator for this request. Only the assigned Head of Evaluator can access this console.
                </p>
                <button
                  onClick={() => navigate(`/dashboard/${currentRole}/requests/${requestId}`)}
                  className="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors font-medium"
                >
                  Back to Request Details
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
      {/* Header */}
      <header className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30 shadow-sm">
        <div className="max-w-8xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <button onClick={() => navigate(-1)} className="p-2 hover:bg-[#B351A9]/10 rounded-lg transition-colors">
                <ChevronLeft className="w-5 h-5 text-[#B351A9]" />
              </button>
              <div>
                <h1 className="text-2xl font-bold text-[#B351A9]">Head of Evaluator Console</h1>
                <p className="text-sm text-[#85257C]">
                  {requestSummary ? `Request: ${requestSummary.requestID || requestSummary.requestTitle || `#${requestId}`}` : "Assign Initial Evaluation & Refinement reviewers, monitor progress, and approve/reject"}
                </p>
              </div>
            </div>
            <div className="text-right">
              <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
              <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
            </div>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-8 space-y-6">
        {/* Auto-Create Tasks Toggle */}
        <div className="bg-white rounded-xl shadow-md border border-[#CDA352]/30 p-5">
          <div className="flex items-center justify-between">
            <div>
              <label className="text-sm font-semibold text-[#273238]">Auto-Create Review Tasks</label>
              <p className="text-xs text-gray-600 mt-1">When enabled, review tasks are automatically created when assigning reviewers</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={autoCreateTasks}
                onChange={(e) => setAutoCreateTasks(e.target.checked)}
                className="sr-only peer"
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-[#B351A9]/30 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-[#B351A9]"></div>
            </label>
          </div>
        </div>

        {/* Evaluation Parameters */}
        <div className="bg-white rounded-xl shadow-md border border-[#CDA352]/30 overflow-hidden">
          <div className="px-6 py-4 bg-gradient-to-r from-[#B351A9]/10 to-[#E4CA86]/10 border-b border-[#CDA352]/20">
            <h2 className="text-lg font-bold text-[#273238]">Evaluation Parameters</h2>
          </div>
          <div className="p-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-semibold text-[#273238] mb-2">Criteria</label>
                <div className="space-y-2">
                  {[
                    { key: 'strategic', label: 'Strategic Alignment' },
                    { key: 'feasibility', label: 'Feasibility' },
                    { key: 'business', label: 'Business Value' },
                    { key: 'technical', label: 'Technical Complexity' }
                  ].map(({ key, label }) => (
                    <label key={key} className="flex items-center gap-2 text-sm font-semibold text-gray-700">
                      <input
                        type="checkbox"
                        checked={(criteria as any)[key]}
                        onChange={(e) => setCriteria((c) => ({ ...c, [key]: e.target.checked }))}
                        className="rounded border-[#CDA352]/30 text-[#B351A9] focus:ring-[#B351A9]"
                      />
                      {label}
                    </label>
                  ))}
                </div>
              </div>
              <div>
                <label className="block text-sm font-semibold text-[#273238] mb-2">Instructions</label>
                <textarea
                  value={instructions}
                  onChange={(e) => setInstructions(e.target.value)}
                  rows={4}
                  placeholder="Provide any guidance for reviewers..."
                  className="w-full p-3 rounded-lg border border-[#CDA352]/30 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 transition-all resize-none text-sm outline-none"
                />
              </div>
            </div>
          </div>
        </div>

        {/* Initial Evaluation */}
        <div className="bg-white rounded-xl shadow-md border border-[#CDA352]/30 overflow-hidden">
          <div className="px-6 py-4 bg-gradient-to-r from-[#B351A9]/10 to-[#E4CA86]/10 border-b border-[#CDA352]/20">
            <Users className="w-5 h-5 text-[#B351A9] mr-2 inline" />
            <h2 className="text-lg font-bold text-[#273238] inline">Initial Evaluation</h2>
            <p className="text-xs text-gray-600 mt-1.5">Assign evaluators to assess strategic alignment, feasibility, and business value</p>
          </div>
          <div className="p-6 grid grid-cols-1 md:grid-cols-2 gap-6">
            {initialEvaluationRoles.map((rn) => (
              <div key={rn} className="p-4 rounded-lg border border-[#CDA352]/30 bg-gradient-to-br from-white to-[#E4CA86]/5">
                <div className="text-sm font-semibold text-[#273238] mb-3">{rn}</div>
                <div className="flex items-center space-x-3">
                  <div className="flex-1">
                    <UserSelect
                      value={assignInputsInitial[rn] || ""}
                      onChange={(ids, opts) => {
                        const id = ids[0] || "";
                        const name = (opts[0]?.name) || "";
                        setAssignInputsInitial((p) => ({ ...p, [rn]: id }));
                        setAssignNamesInitial((p) => ({ ...p, [rn]: name }));
                      }}
                      placeholder="Select user"
                    />
                  </div>
                  <button
                    disabled={!assignInputsInitial[rn] || loading}
                    onClick={() => handleAssign("initial", rn, assignInputsInitial[rn])}
                    className="inline-flex items-center px-4 py-2 rounded-lg bg-gradient-to-r from-[#B351A9] to-[#85257C] text-white text-xs font-semibold shadow-md hover:from-[#85257C] hover:to-[#B351A9] disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                  >
                    <UserPlus className="w-4 h-4 mr-1" /> Assign
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Refinement */}
        <div className="bg-white rounded-xl shadow-md border border-[#CDA352]/30 overflow-hidden">
          <div className="px-6 py-4 bg-gradient-to-r from-[#B351A9]/10 to-[#E4CA86]/10 border-b border-[#CDA352]/20">
            <ClipboardList className="w-5 h-5 text-[#B351A9] mr-2 inline" />
            <h2 className="text-lg font-bold text-[#273238] inline">Idea Refinement & Prioritisation</h2>
            <p className="text-xs text-gray-600 mt-1.5">Assign technical refiners after initial evaluation is complete</p>
          </div>
          <div className="p-6">
            {!initialAllDone && (
              <div className="mb-4 px-4 py-3 rounded-lg border-2 border-amber-300 bg-amber-50 text-amber-800 text-sm font-medium">
                Complete all Initial Evaluation tasks before assigning refinement reviewers.
              </div>
            )}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {refinementRoles.map((rn) => (
                <div key={rn} className="p-4 rounded-lg border border-[#CDA352]/30 bg-gradient-to-br from-white to-[#E4CA86]/5">
                  <div className="text-sm font-semibold text-[#273238] mb-3">{rn}</div>
                  <div className="flex items-center space-x-3">
                    <div className="flex-1">
                      <UserSelect
                        value={assignInputsRefine[rn] || ""}
                        onChange={(ids, opts) => {
                          const id = ids[0] || "";
                          const name = (opts[0]?.name) || "";
                          setAssignInputsRefine((p) => ({ ...p, [rn]: id }));
                          setAssignNamesRefine((p) => ({ ...p, [rn]: name }));
                        }}
                        placeholder="Select user"
                        disabled={!initialAllDone}
                      />
                    </div>
                    <button
                      disabled={!assignInputsRefine[rn] || loading || !initialAllDone}
                      onClick={() => handleAssign("refinement", rn, assignInputsRefine[rn])}
                      className="inline-flex items-center px-4 py-2 rounded-lg bg-gradient-to-r from-[#B351A9] to-[#85257C] text-white text-xs font-semibold shadow-md hover:from-[#85257C] hover:to-[#B351A9] disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                    >
                      <UserPlus className="w-4 h-4 mr-1" /> Assign
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Status and Actions */}
        <div className="bg-white rounded-xl shadow-md border border-[#CDA352]/30 overflow-hidden">
          <div className="px-6 py-4 bg-gradient-to-r from-[#B351A9]/10 to-[#E4CA86]/10 border-b border-[#CDA352]/20">
            <h2 className="text-lg font-bold text-[#273238]">Reviewer Responses</h2>
          </div>
          <div className="p-6">
            {tasks.length === 0 ? (
              <p className="text-sm text-gray-600">No tasks yet. Assign reviewers above to generate tasks.</p>
            ) : (
              <ul className="divide-y divide-gray-200">
                {tasks.map((t, idx) => {
                  const ev = findEvaluationForTask(t);
                  const complete = isTaskComplete(t) || (ev && ev.evaluationStatus === 'Submitted');
                  return (
                    <li key={idx} className="py-3 flex items-center justify-between">
                      <div>
                        <div className="text-sm font-semibold text-[#273238]">{t?.title || t?.name || t?.assigneeRole || "Task"}</div>
                        <div className="text-xs text-gray-500">{t?.assignee || t?.assignedTo || ""}</div>
                      </div>
                      <div className="flex items-center text-sm gap-2">
                        {complete ? (
                          <>
                            <span className="inline-flex items-center text-green-700 font-medium">
                              <CheckCircle className="w-4 h-4 mr-1" /> Completed
                            </span>
                            {ev && (
                              <button
                                onClick={() => setViewEval(ev)}
                                className="px-3 py-1 rounded-md border border-[#CDA352]/40 text-[#273238] hover:bg-[#B351A9]/5 text-xs font-semibold"
                              >
                                View Response
                              </button>
                            )}
                          </>
                        ) : (
                          <span className="inline-flex items-center text-amber-700 font-medium">
                            <XCircle className="w-4 h-4 mr-1" /> Pending
                          </span>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}

            <div className="mt-6 flex items-center justify-end space-x-3">
              <button
                onClick={handleReject}
                disabled={loading}
                className="px-6 py-2.5 rounded-lg border-2 border-red-300 text-red-700 font-semibold hover:bg-red-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Reject
              </button>
              <button
                onClick={handleApprove}
                disabled={!allResponsesOrEvaluations || loading}
                className={`px-6 py-2.5 rounded-lg text-white font-semibold shadow-md transition-all ${allResponsesReceived
                  ? "bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352]"
                  : "bg-gray-300 cursor-not-allowed"
                  }`}
              >
                Approve & Move to Project
              </button>
            </div>
          </div>
        </div>
      </main>
      {viewEval && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl border border-[#CDA352]/30 w-full max-w-lg mx-4">
            <div className="px-6 py-4 border-b border-[#CDA352]/20 flex items-center justify-between">
              <h3 className="text-lg font-bold text-[#273238]">Evaluator Response</h3>
              <button onClick={() => setViewEval(null)} className="text-[#B351A9] hover:underline text-sm">Close</button>
            </div>
            <div className="p-6 space-y-3 text-sm">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <p className="text-gray-500">Reviewer</p>
                  <p className="font-medium text-gray-900">{viewEval.reviewerName || viewEval.reviewerId}</p>
                </div>
                <div>
                  <p className="text-gray-500">Status</p>
                  <p className="font-medium text-gray-900">{viewEval.evaluationStatus}</p>
                </div>
                <div>
                  <p className="text-gray-500">Strategic</p>
                  <p className="font-medium text-gray-900">{viewEval.strategicAlignmentScore ?? "—"}</p>
                </div>
                <div>
                  <p className="text-gray-500">Feasibility</p>
                  <p className="font-medium text-gray-900">{viewEval.feasibilityScore ?? "—"}</p>
                </div>
                <div>
                  <p className="text-gray-500">Business Value</p>
                  <p className="font-medium text-gray-900">{viewEval.businessValueScore ?? "—"}</p>
                </div>
                <div>
                  <p className="text-gray-500">Technical</p>
                  <p className="font-medium text-gray-900">{viewEval.technicalComplexityScore ?? "—"}</p>
                </div>
                <div className="col-span-2">
                  <p className="text-gray-500">Remarks</p>
                  <p className="font-medium text-gray-900">{viewEval.evaluationRemarks || "—"}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default HeadReviewConsole;
