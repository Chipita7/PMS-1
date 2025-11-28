import React from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Calendar, ChevronLeft, ClipboardList, Loader2, Send, Users, Sparkles } from "lucide-react";
import { toast } from "react-hot-toast";
import { projectRequestService, ProjectRequestSummary, AssignmentRole } from "@/services/projectRequestService";
import { reviewTaskService } from "@/services/reviewTaskService";
import { useAuth } from "@/context/AuthContext";
import UserSelect from "@/components/UserSelect";

const PENDING_STATUSES = ["Submitted", "Under Evaluation", "New", "Pending", "Backlogged"];

// AssignReviewers page now handles only Approvals & Governance
// Initial Evaluation and Refinement are assigned by Head Reviewer in HeadReviewConsole
const ROLE_GROUPS: { key: string; title: string; description: string; codes: string[] }[] = [
  {
    key: "approvals",
    title: "Approvals & Governance",
    description: "Executive roles responsible for gating and compliance.",
    codes: ["VP_DIRECTOR", "COMPLIANCE_OFFICER", "PMO"],
  },
];

const reviewerTypeMap: Record<string, string> = {
  HEAD_PRODUCT_MGMT: "Head Reviewer",
  BUSINESS_ANALYST: "Business Analysis",
  INNOVATION_CHAPTER: "Innovation",
  SUBJECT_MATTER_EXPERT: "SME",
  STAKEHOLDER: "Stakeholder",
  HEAD_ENGINEERING: "Engineering Review",
  HEAD_DATA_AI: "Data & AI",
  PRODUCT_OWNER: "Product Owner",
  INFO_SECURITY: "Security",
  DEVSECOPS_TEAM: "DevSecOps",
  VP_DIRECTOR: "Executive Approval",
  COMPLIANCE_OFFICER: "Compliance",
  PMO: "PMO",
};

// Only governance/approval roles are enabled by default in this page
const defaultEnabledRoles = new Set([
  "VP_DIRECTOR",
  "PMO",
]);
const defaultPrimaryRoles = new Set(["VP_DIRECTOR"]);

type RoleAssignmentState = {
  enabled: boolean;
  assignees: string;
  setAsPrimary: boolean;
  assigneeIds?: string[];
};

const AssignReviewers: React.FC<{ darkMode?: boolean }> = ({ darkMode = false }) => {
  const navigate = useNavigate();
  const { role, id } = useParams<{ role: string; id?: string }>();
  const { user } = useAuth();

  const [requests, setRequests] = React.useState<ProjectRequestSummary[]>([]);
  const [roles, setRoles] = React.useState<AssignmentRole[]>([]);
  const [roleAssignments, setRoleAssignments] = React.useState<Record<string, RoleAssignmentState>>({});
  const [roleErrors, setRoleErrors] = React.useState<Record<string, string>>({});
  const [rolesLoading, setRolesLoading] = React.useState(true);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [selectedRequestId, setSelectedRequestId] = React.useState<number | null>(id ? parseInt(id) : null);
  const [dueDate, setDueDate] = React.useState<string>("");

  const [submitting, setSubmitting] = React.useState(false);

  const resolvedRole = (user?.role || role || "").toLowerCase();

  const [headRequestIds, setHeadRequestIds] = React.useState<number[] | null>(null);
  const [authError, setAuthError] = React.useState<string | null>(null);
  const [checkingAuth, setCheckingAuth] = React.useState<boolean>(true);

  const [reviewTasks, setReviewTasks] = React.useState<any[]>([]);

  // Generic Reviewer inputs (separate from business analyst, SME, etc.)
  type ReviewerInput = { id: number; userId: string; userName: string; dueDate?: string };
  const [reviewers, setReviewers] = React.useState<ReviewerInput[]>([
    { id: 1, userId: "", userName: "", dueDate: "" },
    { id: 2, userId: "", userName: "", dueDate: "" },
    { id: 3, userId: "", userName: "", dueDate: "" },
  ]);

  const addReviewer = () => {
    setReviewers((prev) => {
      const nextId = prev.length ? Math.max(...prev.map(r => r.id)) + 1 : 1;
      return [...prev, { id: nextId, userId: "", userName: "", dueDate: "" }];
    });
  };

  const handleAssignGenericReviewers = async () => {
    if (!selectedRequestId) {
      toast.error("Please select a request to assign reviewers.");
      return;
    }
    const entries = reviewers.filter(r => r.userId.trim().length > 0);
    if (!entries.length) {
      toast.error("Provide at least one reviewer (user ID).");
      return;
    }
    setSubmitting(true);
    try {
      for (const r of entries) {
        const desc = `Reviewer Response`;
        const due = (r.dueDate || dueDate || "").trim() || undefined;
        await reviewTaskService.createManualTask(
          selectedRequestId,
          r.userId.trim(),
          r.userName?.trim() || r.userId.trim(),
          "REVIEWER",
          desc,
          due
        );
      }
      toast.success(`Assigned ${entries.length} reviewer${entries.length > 1 ? 's' : ''}.`);
      // Refresh reviewer tasks view
      try {
        const tasks = await projectRequestService.getReviewTasks(selectedRequestId);
        setReviewTasks(Array.isArray(tasks) ? tasks : []);
      } catch {}
    } catch (e: any) {
      toast.error(e?.message || "Failed to create reviewer tasks");
    } finally {
      setSubmitting(false);
    }
  };
  const removeReviewer = (id: number) => {
    setReviewers((prev) => prev.filter(r => r.id !== id));
  };
  const updateReviewer = (id: number, field: keyof ReviewerInput, value: string) => {
    setReviewers((prev) => prev.map(r => r.id === id ? { ...r, [field]: value } : r));
  };

  React.useEffect(() => {
    let mounted = true;
    const checkHeadAssignments = async () => {
      try {
        const ids = await projectRequestService.getHeadAssignmentsForCurrentUser();
        if (!mounted) return;
        const safeIds = Array.isArray(ids) ? ids : [];
        setHeadRequestIds(safeIds);

        const routeId = id ? parseInt(id, 10) : null;

        if (routeId !== null && Number.isFinite(routeId)) {
          if (!safeIds.includes(routeId)) {
            setAuthError("You are not assigned as Head Reviewer for this request.");
          }
        } else if (safeIds.length === 0) {
          setAuthError("You are not assigned as Head Reviewer for any requests.");
        }
      } catch (e: any) {
        if (!mounted) return;
        setAuthError(e?.message || "Failed to verify head reviewer assignments.");
      } finally {
        if (mounted) setCheckingAuth(false);
      }
    };

    checkHeadAssignments();
    return () => {
      mounted = false;
    };
  }, [id]);

  React.useEffect(() => {
    let mounted = true;
    const loadRequests = async () => {
      if (headRequestIds === null || authError) return;
      try {
        setLoading(true);
        setError(null);
        const data = await projectRequestService.getAll();
        if (!mounted) return;
        const pending = data.filter((r) => PENDING_STATUSES.some((s) => r.status?.toLowerCase() === s.toLowerCase()));
        const allowed = pending.filter((r) => headRequestIds.includes(r.id));
        setRequests(allowed);
        if (!selectedRequestId && allowed.length > 0 && !id) {
          setSelectedRequestId(allowed[0].id);
        }
      } catch (e: any) {
        if (!mounted) return;
        setError(e.message || "Failed to load requests");
      } finally {
        if (mounted) setLoading(false);
      }
    };
    loadRequests();
    return () => {
      mounted = false;
    };
  }, [id, selectedRequestId, headRequestIds, authError]);

  // Load reviewer tasks for the selected request so Head Reviewer can monitor responses
  React.useEffect(() => {
    let mounted = true;
    const loadTasks = async () => {
      if (!selectedRequestId) {
        setReviewTasks([]);
        return;
      }
      try {
        const tasks = await projectRequestService.getReviewTasks(selectedRequestId);
        if (!mounted) return;
        setReviewTasks(Array.isArray(tasks) ? tasks : []);
      } catch (e) {
        if (!mounted) return;
        setReviewTasks([]);
      }
    };
    loadTasks();
    return () => {
      mounted = false;
    };
  }, [selectedRequestId]);

  React.useEffect(() => {
    let mounted = true;
    const loadRoles = async () => {
      try {
        setRolesLoading(true);
        const data = await projectRequestService.getAssignmentRoles();
        if (!mounted) return;
        setRoles(data);
        const initialAssignments: Record<string, RoleAssignmentState> = {};
        data.forEach((roleConfig) => {
          initialAssignments[roleConfig.code] = {
            enabled: defaultEnabledRoles.has(roleConfig.code),
            assignees: "",
            setAsPrimary: defaultPrimaryRoles.has(roleConfig.code),
          };
        });
        setRoleAssignments(initialAssignments);
        setRoleErrors({});
      } catch (e: any) {
        if (!mounted) return;
        toast.error(e.message || "Failed to load role configurations");
        setRoles([]);
        setRoleAssignments({});
      } finally {
        if (mounted) setRolesLoading(false);
      }
    };
    loadRoles();
    return () => {
      mounted = false;
    };
  }, []);

  type RoleGroupWithItems = { key: string; title: string; description: string; items: AssignmentRole[] };

  const groupedRoles = React.useMemo(() => {
    if (!roles.length) return [];
    return ROLE_GROUPS.map((group) => ({
      key: group.key,
      title: group.title,
      description: group.description,
      items: roles.filter((roleConfig) => group.codes.includes(roleConfig.code)),
    })).filter((group) => group.items.length > 0);
  }, [roles]);

  const handleRoleToggle = (code: string, enabled: boolean) => {
    setRoleAssignments((prev) => {
      const existing = prev[code] || { assignees: "", setAsPrimary: defaultPrimaryRoles.has(code) };
      return {
        ...prev,
        [code]: { ...existing, enabled },
      };
    });
    setRoleErrors((prev) => ({ ...prev, [code]: "" }));
  };

  const handleAssigneeChange = (code: string, value: string) => {
    setRoleAssignments((prev) => {
      const existing = prev[code] || { enabled: defaultEnabledRoles.has(code), setAsPrimary: defaultPrimaryRoles.has(code) };
      return {
        ...prev,
        [code]: { ...existing, assignees: value },
      };
    });
    setRoleErrors((prev) => ({ ...prev, [code]: "" }));
  };

  const handlePrimaryToggle = (code: string, checked: boolean) => {
    setRoleAssignments((prev) => {
      const existing = prev[code] || { enabled: defaultEnabledRoles.has(code), assignees: "" };
      return {
        ...prev,
        [code]: { ...existing, setAsPrimary: checked },
      };
    });
  };

  const renderRoleCard = (roleConfig: AssignmentRole) => {
    const assignment = roleAssignments[roleConfig.code] || {
      enabled: defaultEnabledRoles.has(roleConfig.code),
      assignees: "",
      setAsPrimary: defaultPrimaryRoles.has(roleConfig.code),
    };
    return (
      <div key={roleConfig.code} className="border border-[#CDA352]/30 rounded-xl p-4 shadow-sm bg-white hover:border-[#B351A9]/40 transition-all">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h4 className="text-sm font-bold text-[#273238]">{roleConfig.name}</h4>
            {roleConfig.description && <p className="text-xs text-gray-500 mt-1">{roleConfig.description}</p>}
          </div>
          <label className="inline-flex items-center gap-2 text-xs text-gray-600 font-semibold">
            <input
              type="checkbox"
              checked={assignment.enabled}
              onChange={(e) => handleRoleToggle(roleConfig.code, e.target.checked)}
              className="rounded border-[#CDA352]/30 text-[#B351A9] focus:ring-[#B351A9]"
            />
            Enable
          </label>
        </div>

        {assignment.enabled && (
          <div className="mt-4 space-y-3">
            <div>
              <label className="text-xs font-semibold text-[#273238] block mb-1">
                {roleConfig.canBeMultiple ? "Assignees" : "Assignee"}
              </label>
              {roleConfig.canBeMultiple ? (
                <UserSelect
                  multiple
                  value={assignment.assigneeIds || []}
                  onChange={(ids, opts) => {
                    setRoleAssignments((prev) => ({
                      ...prev,
                      [roleConfig.code]: {
                        ...(prev[roleConfig.code] || { enabled: true, setAsPrimary: defaultPrimaryRoles.has(roleConfig.code), assignees: '' }),
                        assigneeIds: ids,
                        assignees: opts.map(o => o.name).join(', '),
                      },
                    }));
                    setRoleErrors((prev) => ({ ...prev, [roleConfig.code]: "" }));
                  }}
                  placeholder="Select assignees"
                />
              ) : (
                <UserSelect
                  value={(assignment.assigneeIds && assignment.assigneeIds[0]) || ''}
                  onChange={(ids, opts) => {
                    const id = ids[0] || '';
                    const name = (opts[0]?.name) || '';
                    setRoleAssignments((prev) => ({
                      ...prev,
                      [roleConfig.code]: {
                        ...(prev[roleConfig.code] || { enabled: true, setAsPrimary: defaultPrimaryRoles.has(roleConfig.code), assignees: '' }),
                        assigneeIds: id ? [id] : [],
                        assignees: name,
                      },
                    }));
                    setRoleErrors((prev) => ({ ...prev, [roleConfig.code]: "" }));
                  }}
                  placeholder="Select assignee"
                />
              )}
              {roleErrors[roleConfig.code] && <p className="text-xs text-red-600 mt-1 font-semibold">{roleErrors[roleConfig.code]}</p>}
            </div>

            <label className="text-xs text-gray-600 inline-flex items-center gap-2 font-semibold">
              <input
                type="checkbox"
                checked={assignment.setAsPrimary}
                onChange={(e) => handlePrimaryToggle(roleConfig.code, e.target.checked)}
                className="rounded border-[#CDA352]/30 text-[#B351A9] focus:ring-[#B351A9]"
              />
              Mark first assignee as primary for this role
            </label>
          </div>
        )}
      </div>
    );
  };

  const handleAssign = async () => {
    if (!selectedRequestId) {
      toast.error("Please select a request to assign.");
      return;
    }

    const activeRoles = Object.entries(roleAssignments).filter(
      ([, config]) => config.enabled && ((config.assigneeIds && config.assigneeIds.length > 0) || config.assignees.trim().length > 0),
    );

    if (!activeRoles.length) {
      toast.error("Enable at least one role and provide assignee usernames.");
      return;
    }

    setSubmitting(true);
    const newErrors: Record<string, string> = {};
    const summaryMessages: string[] = [];

    try {
      for (const [code, assignment] of activeRoles) {
        const roleConfig = roles.find((r) => r.code === code);
        if (!roleConfig) continue;
        const assignees = (assignment.assigneeIds && assignment.assigneeIds.length > 0)
          ? assignment.assigneeIds
          : assignment.assignees
            .split(/[\n,]/)
            .map((v) => v.trim())
            .filter(Boolean);

        if (!assignees.length) {
          newErrors[code] = "Provide at least one assignee.";
          continue;
        }
        if (!roleConfig.canBeMultiple && assignees.length > 1) {
          newErrors[code] = "This role accepts only one assignee.";
          continue;
        }

        for (let idx = 0; idx < assignees.length; idx++) {
          const assigneeId = assignees[idx];
          await projectRequestService.assignReviewers(selectedRequestId, {
            assignedTeam: roleConfig.name,
            assignedTo: assigneeId,
            assigneeRole: roleConfig.code,
            setAsPrimary: assignment.setAsPrimary && idx === 0,
            reviewerType: reviewerTypeMap[roleConfig.code] || "Member",
          });
        }
        summaryMessages.push(`${roleConfig.name}: ${assignees.join(", ")}`);
      }

      setRoleErrors(newErrors);

      if (summaryMessages.length) {
        toast.success(`Assigned ${summaryMessages.length} role${summaryMessages.length > 1 ? "s" : ""} successfully.`);
        navigate(`/dashboard/${resolvedRole}/requests/${selectedRequestId}`);
      } else if (Object.keys(newErrors).length) {
        toast.error("Resolve the highlighted role issues and try again.");
      }
    } catch (e: any) {
      toast.error(e.message || "Failed to assign reviewers");
    } finally {
      setSubmitting(false);
    }
  };

  const goBack = () => {
    navigate(`/dashboard/${resolvedRole || "member"}/requests`);
  };

  if (checkingAuth) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 flex items-center justify-center font-['Times_New_Roman',_Times,_serif]">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-[#B351A9] mb-4" />
          <p className="text-[#85257C] font-medium">Checking head reviewer permissions...</p>
        </div>
      </div>
    );
  }

  if (authError) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 flex items-center justify-center font-['Times_New_Roman',_Times,_serif]">
        <div className="bg-white border-2 border-[#CDA352] rounded-2xl p-8 text-center shadow-xl max-w-md">
          <div className="w-16 h-16 bg-gradient-to-br from-[#B351A9] to-[#85257C] rounded-full flex items-center justify-center mx-auto mb-4">
            <Users className="w-8 h-8 text-white" />
          </div>
          <div className="text-[#B351A9] font-bold text-lg mb-2">Access Restricted</div>
          <p className="text-gray-600 text-sm mb-4">
            {authError || "This page is only available for users assigned as Head Reviewer (Head of Product Management) for a request."}
          </p>
          <button
            onClick={goBack}
            className="inline-flex items-center px-5 py-2.5 rounded-lg bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] text-white font-semibold text-sm shadow-md hover:shadow-lg transition-all"
          >
            <ChevronLeft className="w-4 h-4 mr-2" /> Back to Requests
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
      {/* Header - Matching RequestForm */}
      <div className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30">
        <div className="max-w-8xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <button
                onClick={goBack}
                className="p-2 hover:bg-[#B351A9]/10 rounded-lg transition-all"
              >
                <ChevronLeft className="w-6 h-6 text-[#B351A9]" />
              </button>
              <div>
                <h1 className="text-2xl font-bold text-[#B351A9] flex items-center gap-2">
                  <Users className="w-5 h-5 text-[#CDA352]" />
                  Assign Approvals & Governance
                </h1>
                <p className="text-gray-600 text-sm ml-3">Configure executive approvers and governance reviewers (PMO, Compliance, VP/Director)</p>
              </div>
            </div>
            <div className="text-right">
              <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
              <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6 space-y-6">
        {/* Request Selection */}
        <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden">
          <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-6 py-3 border-b border-[#CDA352]/20">
            <h2 className="text-lg font-bold text-[#273238] flex items-center">
              <ClipboardList className="w-5 h-5 mr-2 text-[#B351A9]" />
              Request Selection
            </h2>
          </div>
          <div className="p-6">
            {loading ? (
              <div className="flex items-center text-gray-600">
                <Loader2 className="w-5 h-5 mr-2 animate-spin text-[#B351A9]" />
                <span className="text-sm">Loading requests...</span>
              </div>
            ) : error ? (
              <div className="text-red-600 text-sm font-semibold">{error}</div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-semibold text-[#273238] mb-2">Request</label>
                  <select
                    value={selectedRequestId ?? ""}
                    onChange={(e) => setSelectedRequestId(e.target.value ? parseInt(e.target.value) : null)}
                    className="w-full p-3 rounded-lg border border-[#CDA352]/30 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 transition-all text-sm outline-none"
                  >
                    <option value="">Select a request</option>
                    {requests.map((r) => (
                      <option key={r.id} value={r.id}>
                        #{r.requestID || r.id} — {r.requestTitle}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-semibold text-[#273238] mb-2">Due Date (optional)</label>
                  <div className="relative">
                    <input
                      type="date"
                      value={dueDate}
                      onChange={(e) => setDueDate(e.target.value)}
                      className="w-full p-3 rounded-lg border border-[#CDA352]/30 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 transition-all text-sm outline-none"
                    />
                    <Calendar className="w-4 h-4 text-gray-500 absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                  </div>
                  <p className="text-xs text-gray-500 mt-1">Used in assignment notifications and reminders.</p>
                </div>
              </div>
            )}
          </div>
        </div>



        {/* Reviewer Assignments (Generic, separate from BA/SME/etc.) */}
        <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden">
          <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-6 py-3 border-b border-[#CDA352]/20">
            <h2 className="text-lg font-bold text-[#273238]">Reviewer Assignments</h2>
            <p className="text-xs text-gray-600 mt-1">Add reviewers independent of evaluation/refinement roles. Defaults to 3; you can add more. No auto-generated tasks.</p>
          </div>
          <div className="p-6 space-y-4">
            {!selectedRequestId && (
              <div className="text-sm text-gray-600">Select a request first to assign reviewers.</div>
            )}
            {selectedRequestId && (
              <>
                <div className="space-y-3">
                  {reviewers.map((r, idx) => (
                    <div key={r.id} className="rounded-lg border border-[#CDA352]/30 p-4 bg-gradient-to-br from-white to-[#E4CA86]/5">
                      <div className="flex items-center justify-between mb-3">
                        <div className="text-sm font-bold text-[#273238]">Reviewer {idx + 1}</div>
                        {reviewers.length > 1 && (
                          <button
                            type="button"
                            onClick={() => removeReviewer(r.id)}
                            className="text-xs font-semibold text-red-600 hover:text-red-700"
                          >
                            Remove
                          </button>
                        )}
                      </div>
                      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                        <div>
                          <label className="block text-xs font-semibold text-[#273238] mb-1">Reviewer <span className="text-red-500">*</span></label>
                          <UserSelect
                            value={r.userId}
                            onChange={(ids, opts) => {
                              const id = ids[0] || '';
                              const name = (opts[0]?.name) || '';
                              updateReviewer(r.id, 'userId', id);
                              updateReviewer(r.id, 'userName', name);
                            }}
                            placeholder="Select reviewer"
                          />
                        </div>
                        <div>
                          <label className="block text-xs font-semibold text-[#273238] mb-1">Due Date (optional)</label>
                          <div className="relative">
                            <input
                              type="date"
                              value={r.dueDate || ''}
                              onChange={(e) => updateReviewer(r.id, 'dueDate', e.target.value)}
                              className="w-full p-2.5 rounded-lg border border-[#CDA352]/30 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 transition-all text-sm outline-none"
                            />
                            <Calendar className="w-4 h-4 text-gray-500 absolute right-3 top-1/2 -translate-y-1/2 pointer-events-none" />
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
                <div className="flex items-center justify-between mt-2">
                  <button
                    type="button"
                    onClick={addReviewer}
                    className="inline-flex items-center px-4 py-2 rounded-lg border-2 border-[#CDA352]/30 bg-white text-[#273238] hover:bg-gray-50 text-sm font-semibold"
                  >
                    + Add Reviewer
                  </button>
                  <button
                    type="button"
                    onClick={handleAssignGenericReviewers}
                    disabled={submitting}
                    className={`inline-flex items-center px-6 py-2.5 rounded-lg text-white bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] font-semibold text-sm shadow-md hover:shadow-lg transition-all ${submitting ? 'opacity-60 cursor-not-allowed' : ''}`}
                  >
                    {submitting ? (
                      <span className="flex items-center">
                        <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                        Assigning...
                      </span>
                    ) : (
                      <>
                        <Send className="w-4 h-4 mr-2" /> Assign Reviewers
                      </>
                    )}
                  </button>
                </div>
              </>
            )}
          </div>
        </div>

        {/* Reviewer & Approver Roles */}
        <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden">
          <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-6 py-3 border-b border-[#CDA352]/20">
            <h2 className="text-lg font-bold text-[#273238]">Approvals & Governance Roles</h2>
            <p className="text-xs text-gray-600 mt-1">
              Assign executive approvers and compliance reviewers. Note: Initial Evaluation and Refinement are assigned by the Head Reviewer.
            </p>
          </div>
          <div className="p-6">
            {rolesLoading ? (
              <div className="flex items-center text-gray-600">
                <Loader2 className="w-5 h-5 mr-2 animate-spin text-[#B351A9]" />
                <span className="text-sm">Loading role configurations...</span>
              </div>
            ) : !roles.length ? (
              <div className="text-gray-600 text-sm">
                No assignment roles are active in the backend. Please run the role regeneration endpoint first.
              </div>
            ) : (
              <div className="space-y-8">
                {groupedRoles.map((group) => (
                  <div key={group.key} className="space-y-4">
                    <div>
                      <h3 className="text-sm font-bold text-[#273238]">{group.title}</h3>
                      {group.description && <p className="text-xs text-gray-500">{group.description}</p>}
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">{group.items.map(renderRoleCard)}</div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Reviewer Responses (read-only) */}
        <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden">
          <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-6 py-3 border-b border-[#CDA352]/20">
            <h2 className="text-lg font-bold text-[#273238]">Reviewer Responses</h2>
            <p className="text-xs text-gray-600 mt-1">Live status of reviewer submissions for the selected request.</p>
          </div>
          <div className="p-6">
            {!selectedRequestId ? (
              <div className="text-sm text-gray-600">Select a request to view reviewer responses.</div>
            ) : reviewTasks.length === 0 ? (
              <div className="text-sm text-gray-600">No reviewer tasks found for this request.</div>
            ) : (
              <ul className="divide-y divide-gray-200">
                {reviewTasks.map((t, idx) => {
                  const status = (t?.status || t?.Status || '').toString();
                  const assignee = t?.assignee || t?.assignedTo || '—';
                  const submitted = t?.submittedAt ? new Date(t.submittedAt).toLocaleString() : '—';
                  return (
                    <li key={idx} className="py-3 flex items-center justify-between">
                      <div>
                        <div className="text-sm font-semibold text-[#273238]">{t?.title || t?.name || 'Task'}</div>
                        <div className="text-xs text-gray-500">{assignee}</div>
                      </div>
                      <div className="text-right">
                        <div className={`inline-flex items-center text-xs font-medium px-2.5 py-1 rounded-full border ${status.toLowerCase() === 'completed' ? 'text-green-700 border-green-300 bg-green-50' : 'text-amber-700 border-amber-300 bg-amber-50'}`}>
                          {status}
                        </div>
                        <div className="text-[11px] text-gray-500 mt-1">Submitted: {submitted}</div>
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        </div>

        {/* Action Buttons */}
        <div className="flex items-center justify-between">
          <button
            onClick={goBack}
            className="inline-flex items-center px-5 py-2.5 rounded-lg border-2 border-[#CDA352]/30 bg-white text-[#273238] hover:bg-gray-50 font-semibold text-sm transition-all"
          >
            <ChevronLeft className="w-4 h-4 mr-2" /> Back
          </button>
          <button
            onClick={handleAssign}
            disabled={submitting}
            className={`inline-flex items-center px-6 py-2.5 rounded-lg text-white bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] font-semibold text-sm shadow-md hover:shadow-lg transition-all ${submitting ? "opacity-60 cursor-not-allowed" : ""
              }`}
          >
            {submitting ? (
              <span className="flex items-center">
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Assigning...
              </span>
            ) : (
              <>
                <Send className="w-4 h-4 mr-2" /> Assign & Notify
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};

export default AssignReviewers;
