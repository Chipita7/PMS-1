import React from "react";
import { useNavigate, useParams, useLocation } from "react-router-dom";
import {
  projectRequestService,
  ProjectRequestSummary,
} from "@/services/projectRequestService";
import {
  Bell,
  FileText,
  BarChart2,
  CheckCircle,
  Clock,
  Plus,
  ArrowRight,
  Users,
  Eye,
  UserPlus,
  ShieldCheck,
  MoreVertical,
  ChevronLeft,
  Sparkles,
  Calendar,
  User,
  Tag,
} from "lucide-react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import DataTables from "@/components/DataTables";
import { useAuth } from "@/context/AuthContext";
import { getRequestConfigurations } from "@/services/requestService";

const RequestsDashboard: React.FC<{ darkMode?: boolean }> = ({ darkMode }) => {
  const { role } = useParams<{ role: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const resolvedRole = role || "member";
  const { user } = useAuth();

  const [requests, setRequests] = React.useState<ProjectRequestSummary[]>([]);
  const [filtered, setFiltered] = React.useState<ProjectRequestSummary[]>([]);
  const [loading, setLoading] = React.useState<boolean>(false);
  const [error, setError] = React.useState<string | null>(null);
  const [myHeadIds, setMyHeadIds] = React.useState<number[]>([]);
  const [viewFilter, setViewFilter] = React.useState<"all" | "mine">("all");
  const [openMenuId, setOpenMenuId] = React.useState<number | null>(null);
  const [currentUserRole, setCurrentUserRole] = React.useState<string>("");
  const [statusFilter, setStatusFilter] = React.useState<string>("active");
  const [availableStatuses, setAvailableStatuses] = React.useState<string[]>([]);

  // Load all requests
  React.useEffect(() => {
    let isMounted = true;
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await projectRequestService.getAll();
        if (isMounted) setRequests(data);
      } catch (e: any) {
        if (isMounted) setError(e.message || "Failed to load requests");
      } finally {
        if (isMounted) setLoading(false);
      }
    };
    load();
    return () => {
      isMounted = false;
    };
  }, []);

  // Set current user role from AuthContext
  React.useEffect(() => {
    if (user?.role) {
      setCurrentUserRole(user.role.toLowerCase());
      console.log("Current user role:", user.role.toLowerCase());
    }
  }, [user]);

  React.useEffect(() => {
    const normalize = (s?: string) => (s || "").toLowerCase();
    const aliases: Record<string, string[]> = {
      pending: ["Submitted", "Under Evaluation", "New", "Pending", "Backlogged"],
      newRequests: ["Submitted", "New"],
      inEvaluation: ["Under Evaluation"],
      approved: ["Approved"],
      rejected: ["Rejected"],
      active: ["Submitted", "Under Evaluation", "New", "Pending", "Backlogged", "Approved", "In Progress", "In Development"],
    };
    
    let next: ProjectRequestSummary[] = requests;
    if (statusFilter !== "all") {
      if (statusFilter === "active") {
        const set = new Set(aliases.active.map(x => x.toLowerCase()));
        next = requests.filter(r => set.has(normalize(r.status)));
      } else if (aliases[statusFilter]) {
        const set = new Set(aliases[statusFilter].map(x => x.toLowerCase()));
        next = requests.filter(r => set.has(normalize(r.status)));
      } else {
        next = requests.filter(r => normalize(r.status) === normalize(statusFilter));
      }
    }
    setFiltered(next);
  }, [requests, statusFilter]);

  React.useEffect(() => {
    const aliasExclusions = new Set([
      "submitted",
      "under evaluation",
      "new",
      "pending",
      "backlogged",
      "approved",
      "rejected",
    ]);
    const unique = Array.from(
      new Set(
        (requests || [])
          .map((r) => (r.status || "").trim())
          .filter((s): s is string => !!s)
      )
    )
      .filter((s) => !aliasExclusions.has(s.toLowerCase()))
      .sort((a, b) => a.localeCompare(b));
    setAvailableStatuses(unique);
  }, [requests]);

  React.useEffect(() => {
    const maybe = (location.state as any)?.statusFilter;
    if (typeof maybe === "string" && maybe) {
      setStatusFilter(maybe);
    }
  }, [location.state]);

  React.useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const cfg = await getRequestConfigurations();
        const aliasExclusions = new Set([
          "submitted",
          "under evaluation",
          "new",
          "pending",
          "backlogged",
          "approved",
          "rejected",
        ]);
        const names = ((cfg.Statuses || []).map((x) => x.name).filter(Boolean) as string[])
          .filter((s) => !aliasExclusions.has(s.toLowerCase()));
        if (!cancelled) {
          setAvailableStatuses((prev) => Array.from(new Set([...(prev || []), ...names])).sort((a, b) => a.localeCompare(b)));
        }
      } catch { }
    })();
    return () => { cancelled = true; };
  }, []);

  React.useEffect(() => {
    const handler = () => {
      projectRequestService.getAll().then((data) => setRequests(data)).catch(() => { });
    };
    window.addEventListener("requests-updated", handler as EventListener);
    return () => window.removeEventListener("requests-updated", handler as EventListener);
  }, []);

  // Load head assignments for current user and refresh properly
  const loadHead = async () => {
    try {
      const ids = await projectRequestService.getHeadAssignmentsForCurrentUser();
      setMyHeadIds(ids || []);
    } catch (e) {
      console.error("Failed to load head assignments:", e);
      setMyHeadIds([]);
    }
  };

  // Initial load of head assignments
  React.useEffect(() => {
    loadHead();
  }, []);

  // Refresh head assignments when returning from head-assign page or after any navigation
  React.useEffect(() => {
    const timer = setTimeout(() => {
      loadHead();
    }, 500);
    return () => clearTimeout(timer);
  }, [location.pathname]);

  // Also refresh when requests list changes
  React.useEffect(() => {
    loadHead();
  }, [requests.length]);

  // Display requests where user is head reviewer
  // Role-based filtering:
  // - Directors and Vice Presidents: see all pending requests in "My Head Assignments"
  // - Other roles (managers, supervisors, members): only see requests specifically assigned to them as head
  const displayed = React.useMemo(() => {
    const base = filtered;
    if (viewFilter === "mine") {
      // Always show only requests where the current user is explicitly assigned as head
      const myAssignedRequests = base.filter((r) => myHeadIds.includes(r.id));
      console.log("My Head Assignments - showing only assigned filtered requests:", myAssignedRequests.length);
      return myAssignedRequests;
    }
    return base;
  }, [filtered, myHeadIds, viewFilter]);

  const formatTimeAgo = (iso: string) => {
    const d = new Date(iso);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    if (diffMins < 60) return `${diffMins} min ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? "s" : ""} ago`;
    return `${diffDays} day${diffDays > 1 ? "s" : ""} ago`;
  };

  const pendingCount = React.useMemo(() => {
    const pendingStatuses = ["Submitted", "Under Evaluation", "New", "Pending", "Backlogged"];
    return requests.filter((r) => pendingStatuses.some((s) => r.status?.toLowerCase() === s.toLowerCase())).length;
  }, [requests]);

  // Metrics for dashboard cards
  const metrics = React.useMemo(() => {
    const newRequests = requests.filter((r) => ["submitted", "new"].includes(r.status?.toLowerCase() ?? ""));
    const inEvaluation = requests.filter((r) => r.status?.toLowerCase() === "under evaluation");
    const approved = requests.filter((r) => r.status?.toLowerCase() === "approved");
    const rejected = requests.filter((r) => r.status?.toLowerCase() === "rejected");
    
    // Calculate active requests (everything except rejected/closed)
    const activeRequests = requests.filter((r) => 
      !["rejected", "closed", "cancelled"].includes(r.status?.toLowerCase() ?? "")
    );
    
    return {
      new: newRequests.length,
      inEvaluation: inEvaluation.length,
      approved: approved.length,
      rejected: rejected.length,
      active: activeRequests.length, // Add active count
    };
  }, [requests]);

  // Mock notifications (last 3 recent requests)
  const notifications = React.useMemo(() => {
    const recent = requests
      .filter((r) => {
        const created = new Date(r.createdDate);
        const diffDays = (new Date().getTime() - created.getTime()) / (1000 * 60 * 60 * 24);
        return diffDays <= 1;
      })
      .slice(0, 3);
    return recent.map((r, idx) => ({
      id: r.id,
      type: idx === 0 ? "new" : idx === 1 ? "update" : "reminder",
      message:
        idx === 0
          ? `New request from ${r.businessDepartment || "Unknown Dept"}`
          : idx === 1
            ? `Evaluation completed for "${r.requestTitle}"`
            : `Reminder: ${pendingCount} pending reviews`,
      time: formatTimeAgo(r.createdDate),
      read: idx > 0,
    }));
  }, [requests, pendingCount]);

  const handleAssignReviewers = (requestId: number) => {
    navigate(`/dashboard/${resolvedRole}/requests/${requestId}/assign`);
  };

  const handleViewRequest = (requestId: number) => {
    navigate(`/dashboard/${resolvedRole}/requests/${requestId}`);
  };

  const requestColumns = React.useMemo(() => [
    {
      name: "Request ID",
      selector: (row: ProjectRequestSummary) => row.requestID || row.id,
      sortable: true,
      width: "13%",
      cell: (row: ProjectRequestSummary) => (
        <div className="px-3 py-1.5 rounded-lg bg-gradient-to-r from-[#B351A9]/8 to-[#E4CA86]/8 border border-[#CDA352]/30">
          <span className="font-mono text-sm font-bold text-[#85257C]">
            {row.requestID || `PR-${row.id}`}
          </span>
        </div>
      ),
    },
    {
      name: "Title",
      selector: (row: ProjectRequestSummary) => row.requestTitle,
      sortable: true,
      width: "22%",
      cell: (row: ProjectRequestSummary) => (
        <div className="py-1">
          <div className="font-semibold text-[#273238] text-sm leading-snug">{row.requestTitle}</div>
        </div>
      ),
    },
    {
      name: "Status",
      selector: (row: ProjectRequestSummary) => row.status || "",
      sortable: true,
      width: "13%",
      cell: (row: ProjectRequestSummary) => {
        const raw = (row.status || "").toLowerCase();
        let bgClass = "bg-gray-100 text-[#273238] border-gray-200";
        if (raw.includes("submit") || raw.includes("new") || raw.includes("pending")) {
          bgClass = "bg-[#CDA352]/10 text-[#CDA352] border-[#CDA352]/25";
        } else if (raw.includes("approve") || raw.includes("complete") || raw.includes("deliver")) {
          bgClass = "bg-emerald-50 text-emerald-600 border-emerald-200";
        } else if (raw.includes("reject")) {
          bgClass = "bg-red-50 text-red-600 border-red-200";
        } else if (raw.includes("evaluation") || raw.includes("progress")) {
          bgClass = "bg-[#B351A9]/10 text-[#B351A9] border-[#B351A9]/20";
        }
        return (
          <span className={`px-3 py-1.5 rounded-lg text-xs font-bold border ${bgClass}`}>
            {row.status || "—"}
          </span>
        );
      },
    },
    {
      name: "Requester",
      selector: (row: ProjectRequestSummary) => row.requestedByName || "—",
      sortable: true,
      width: "17%",
      cell: (row: ProjectRequestSummary) => (
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 rounded-full bg-gradient-to-br from-[#B351A9] to-[#85257C] flex items-center justify-center flex-shrink-0">
            <User className="w-3.5 h-3.5 text-white" />
          </div>
          <span className="text-sm font-medium text-[#273238] truncate">{row.requestedByName || "—"}</span>
        </div>
      ),
    },
    {
      name: "Date",
      selector: (row: ProjectRequestSummary) => row.createdDate,
      sortable: true,
      width: "12%",
      cell: (row: ProjectRequestSummary) => {
        if (!row.createdDate) return <span className="text-sm text-gray-400">—</span>;
        const date = new Date(row.createdDate);
        return (
          <div className="flex items-start gap-1.5">
            <Calendar className="w-3.5 h-3.5 text-[#B351A9] mt-0.5 flex-shrink-0" />
            <div>
              <div className="text-sm font-semibold text-[#273238] leading-tight">
                {date.toLocaleDateString("en-US", { month: "short", day: "numeric" })}
              </div>
              <div className="text-xs text-gray-400 leading-tight">
                {date.getFullYear()}
              </div>
            </div>
          </div>
        );
      },
    },
    {
      name: "Actions",
      width: "10%",
      cell: (row: ProjectRequestSummary) => {
        const [dropdownPosition, setDropdownPosition] = React.useState<'bottom' | 'top'>('bottom');
        const buttonRef = React.useRef<HTMLButtonElement>(null);

        const handleClick = (e: React.MouseEvent) => {
          e.stopPropagation();

          if (openMenuId !== row.id && buttonRef.current) {
            const rect = buttonRef.current.getBoundingClientRect();
            const viewportHeight = window.innerHeight;
            const spaceBelow = viewportHeight - rect.bottom;
            const spaceAbove = rect.top;

            if (spaceBelow < 250 && spaceAbove > spaceBelow) {
              setDropdownPosition('top');
            } else {
              setDropdownPosition('bottom');
            }
          }

          setOpenMenuId(openMenuId === row.id ? null : row.id);
        };

        return (
          <div className="relative inline-block text-left">
            <button
              ref={buttonRef}
              onClick={handleClick}
              className="inline-flex items-center justify-center p-2 rounded-full hover:bg-[#B351A9]/10 focus:outline-none transition-all"
            >
              <MoreVertical className="w-4 h-4 text-[#B351A9]" />
            </button>
            {openMenuId === row.id && (
              <div
                className={`origin-top-right absolute right-0 w-56 rounded-xl shadow-lg bg-white ring-1 ring-[#CDA352]/20 z-[9999] border border-[#CDA352]/30 ${dropdownPosition === 'top' ? 'bottom-full mb-2' : 'top-full mt-2'
                  }`}
              >
                <div className="py-1" role="menu">
                  <button
                    onClick={(e) => { e.stopPropagation(); handleViewRequest(row.id); setOpenMenuId(null); }}
                    className="block w-full text-left px-4 py-2.5 text-sm text-[#273238] hover:bg-[#B351A9]/5 transition-all"
                  >
                    <Eye className="inline w-4 h-4 mr-2 text-[#B351A9]" />
                    View
                  </button>
                  {myHeadIds.includes(row.id) && (
                    <button
                      onClick={(e) => { e.stopPropagation(); navigate(`/dashboard/${resolvedRole}/requests/${row.id}/head`); setOpenMenuId(null); }}
                      className="block w-full text-left px-4 py-2.5 text-sm text-[#273238] hover:bg-[#B351A9]/5 transition-all"
                    >
                      <BarChart2 className="inline w-4 h-4 mr-2 text-[#B351A9]" /> Head of Evaluator Console
                    </button>
                  )}
                  {!myHeadIds.includes(row.id) && (
                    <button
                      onClick={(e) => { e.stopPropagation(); navigate(`/dashboard/${resolvedRole}/requests/${row.id}/head-assign`); setOpenMenuId(null); }}
                      className="block w-full text-left px-4 py-2.5 text-sm text-[#273238] hover:bg-[#B351A9]/5 transition-all"
                    >
                      <ShieldCheck className="inline w-4 h-4 mr-2 text-[#B351A9]" /> Assign Head
                    </button>
                  )}
                  <button
                    onClick={(e) => { e.stopPropagation(); handleAssignReviewers(row.id); setOpenMenuId(null); }}
                    className="block w-full text-left px-4 py-2.5 text-sm text-[#273238] hover:bg-[#B351A9]/5 transition-all"
                  >
                    <UserPlus className="inline w-4 h-4 mr-2 text-[#B351A9]" /> Assign Reviewers
                  </button>
                </div>
              </div>
            )}
          </div>
        );
      },
    },
  ], [myHeadIds, navigate, resolvedRole, openMenuId]);

  const headingLabel = React.useMemo(() => {
    if (statusFilter === "all") return "All Requests";
    if (statusFilter === "active") return "Active Requests";
    if (statusFilter === "pending") return "Pending Requests";
    if (statusFilter === "newRequests") return "New Requests";
    if (statusFilter === "inEvaluation") return "In Evaluation Requests";
    return `${statusFilter} Requests`;
  }, [statusFilter]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
      {/* Header - Matching RequestForm */}
      <div className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30">
        <div className="max-w-8xl mx-auto px-7 py-4 flex justify-between items-center">
          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate(`/dashboard/${resolvedRole}`)}
              className="p-1 hover:bg-[#B351A9]/10 rounded-lg transition-all"
            >
              <ChevronLeft className="w-6 h-6 text-[#B351A9]" />
            </button>
            <div>
              <h1 className="text-2xl font-bold text-[#B351A9] flex items-center gap-2">
                <Sparkles className="w-5 h-5 text-[#CDA352]" />
                Requests Dashboard
              </h1>
              <p className="text-gray-600 text-sm ml-6">Manage and review project requests</p>
            </div>
          </div>
          <div className="text-right">
            <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
            <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto  py-8">
        {/* Metrics Cards - Smaller Size with 5 cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4 mb-6">
          {[
            { title: "Total Requests", value: metrics.active, icon: FileText, gradient: "from-[#B351A9] to-[#85257C]", filter: "active" },
            { title: "New Requests", value: metrics.new, icon: FileText, gradient: "from-[#CDA352] to-[#E4CA86]", filter: "newRequests" },
            { title: "In Evaluation", value: metrics.inEvaluation, icon: Clock, gradient: "from-[#85257C] to-[#B351A9]", filter: "inEvaluation" },
            { title: "Approved", value: metrics.approved, icon: CheckCircle, gradient:"from-[#CDA352] to-[#E4CA86]", filter: "approved" },
            { title: "Rejected", value: metrics.rejected, icon: FileText, gradient: "from-[#273238] to-gray-700", filter: "rejected" }
          ].map((card, idx) => (
            <div
              key={idx}
              onClick={() => setStatusFilter(card.filter)}
              className={`bg-gradient-to-r ${card.gradient} rounded-xl p-4 text-white shadow-lg border border-[#CDA352]/30 cursor-pointer hover:opacity-90 transition`}
            >
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-xs font-semibold uppercase tracking-wide">{card.title}</h3>
                <card.icon className="h-4 w-4 opacity-80" />
              </div>
              <div className="text-2xl font-bold">{card.value}</div>
              <div className="text-xs opacity-70 mt-1">Updated just now</div>
            </div>
          ))}
        </div>

        {/* Quick Actions and Notifications - Same Row */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
          {/* Quick Actions */}
          <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden">
            <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-7 py-4 border-b border-[#CDA352]/20">
              <h2 className="text-lg font-bold text-[#273238]">Quick Actions</h2>
            </div>
            <div className="p-4">
              <div className="grid grid-cols-3 gap-3">
                <button
                  onClick={() => navigate(`/dashboard/${resolvedRole}/requests/new`)}
                  className="flex flex-col items-center justify-center p-4 rounded-lg bg-gradient-to-br from-[#CDA352]/10 to-[#E4CA86]/10 hover:from-[#CDA352]/20 hover:to-[#E4CA86]/20 border border-[#CDA352]/30 transition-all group"
                >
                  <Plus className="w-6 h-6 text-[#CDA352] mb-2" />
                  <span className="text-xs font-semibold text-[#273238] text-center">New Request</span>
                </button>
                <button
                  onClick={() => navigate(`/dashboard/${resolvedRole}/requests/head-assign`)}
                  className="flex flex-col items-center justify-center p-4 rounded-lg bg-gradient-to-br from-[#B351A9]/10 to-[#85257C]/10 hover:from-[#B351A9]/20 hover:to-[#85257C]/20 border border-[#CDA352]/30 transition-all group"
                >
                  <ShieldCheck className="w-6 h-6 text-[#B351A9] mb-2" />
                  <span className="text-xs font-semibold text-[#273238] text-center">Assign Head</span>
                </button>
                <button
                  onClick={() => navigate(`/dashboard/${resolvedRole}/requests/assign`)}
                  className="flex flex-col items-center justify-center p-4 rounded-lg bg-gradient-to-br from-[#85257C]/10 to-[#B351A9]/10 hover:from-[#85257C]/20 hover:to-[#B351A9]/20 border border-[#CDA352]/30 transition-all group"
                >
                  <Users className="w-6 h-6 text-[#85257C] mb-2" />
                  <span className="text-xs font-semibold text-[#273238] text-center">Assign Reviewers</span>
                </button>
              </div>

              {/* Helpful descriptions */}
              <div className="mt-4 pt-4 border-t border-[#CDA352]/20">
                <ul className="space-y-2 text-sm text-gray-600">
                  <li className="flex items-start">
                    <span className="text-[#CDA352] mr-2">•</span>
                    <span><strong className="text-[#273238]">New Request:</strong> Submit a new project or idea request for evaluation</span>
                  </li>
                  <li className="flex items-start">
                    <span className="text-[#B351A9] mr-2">•</span>
                    <span><strong className="text-[#273238]">Assign Head:</strong> Appoint a head reviewer to oversee the evaluation process</span>
                  </li>
                  <li className="flex items-start">
                    <span className="text-[#85257C] mr-2">•</span>
                    <span><strong className="text-[#273238]">Assign Reviewers:</strong> Configure multi-role evaluation team for workflow</span>
                  </li>
                </ul>
              </div>
            </div>
          </div>

          {/* Notifications */}
          <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden">
            <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-7 py-4 border-b border-[#CDA352]/20">
              <div className="flex items-center justify-between">
                <div className="flex items-center">
                  <Bell className="w-5 h-5 text-[#B351A9] mr-2" />
                  <h2 className="text-lg font-bold text-[#273238]">Notifications</h2>
                </div>
                <span className="px-3 py-1 text-xs font-semibold bg-gradient-to-r from-[#CDA352] to-[#E4CA86] text-white rounded-full">
                  {notifications.filter((n) => !n.read).length} New
                </span>
              </div>
            </div>
            <div className="p-4">
              <div className="space-y-2 max-h-48 overflow-y-auto">
                {notifications.length > 0 ? (
                  notifications.map((notification) => (
                    <div
                      key={notification.id}
                      className={`p-3 rounded-lg border transition-all ${!notification.read ? "bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 border-[#CDA352]/30" : "bg-gray-50 border-gray-200"}`}
                    >
                      <div className="flex items-start">
                        <div
                          className={`w-2 h-2 mt-2 rounded-full mr-3 flex-shrink-0 ${notification.type === "new" ? "bg-[#B351A9]" : notification.type === "update" ? "bg-[#CDA352]" : "bg-[#E4CA86]"}`}
                        />
                        <div className="flex-1 min-w-0">
                          <p className="text-sm text-[#273238] font-medium">{notification.message}</p>
                          <p className="text-xs text-gray-500 mt-1">{notification.time}</p>
                        </div>
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="text-sm text-gray-500 text-center py-4">No new notifications</p>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Requests Table */}
        {error && !loading && (
          <div className="mb-4 px-4 py-3 rounded-xl border border-red-200 bg-red-50 text-sm text-red-700">{error}</div>
        )}
        <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-visible">
          <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-6 py-5 border-b border-[#CDA352]/20">
            {/* Title and Filters Row */}
            <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 ">
              <div className="flex flex-col gap-3">
                <div>
                  <h2 className="text-xl font-bold text-[#273238]">{headingLabel}</h2>
                  <p className="text-sm text-gray-500 mt-1">{displayed.length} request{displayed.length !== 1 ? 's' : ''} found</p>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => { setViewFilter("all"); setStatusFilter("active"); }}
                    className={`px-4 py-2 rounded-lg text-sm font-semibold border transition-all ${viewFilter === "all" ? "bg-gradient-to-r from-[#B351A9] to-[#85257C] text-white border-[#B351A9]" : "bg-white text-[#B351A9] border-[#CDA352]/30 hover:bg-[#B351A9]/5"}`}
                  >All Active</button>
                  <button
                    onClick={() => setViewFilter("mine")}
                    className={`px-4 py-2 rounded-lg text-sm font-semibold border transition-all ${viewFilter === "mine" ? "bg-gradient-to-r from-[#B351A9] to-[#85257C] text-white border-[#B351A9]" : "bg-white text-[#B351A9] border-[#CDA352]/30 hover:bg-[#B351A9]/5"}`}
                  >
                    My Head Assignments
                    <span className="ml-2 inline-flex items-center justify-center text-xs px-2 py-0.5 rounded-full bg-[#CDA352] text-white font-bold">
                      {myHeadIds.length}
                    </span>
                  </button>

                  <div className="flex items-center gap-2">
                    <label className="text-sm text-[#273238] font-semibold">Status</label>
                    <select
                      value={statusFilter}
                      onChange={(e) => setStatusFilter(e.target.value)}
                      className="px-3 py-2 rounded-lg border border-[#CDA352]/30 text-sm focus:outline-none focus:ring-2 focus:ring-[#B351A9]/20"
                    >
                      <option value="all">All</option>
                      <option value="active">Active</option>
                      <option value="pending">Pending</option>
                      <option value="newRequests">New</option>
                      <option value="inEvaluation">Under Evaluation</option>
                      <option value="approved">Approved</option>
                      <option value="rejected">Rejected</option>
                      {availableStatuses.map((s) => (
                        <option key={s} value={s}>{s}</option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>
              <button
                onClick={() => navigate(`/dashboard/${resolvedRole}/requests/new`)}
                className="inline-flex items-center px-5 py-2.5 rounded-lg bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] text-white text-sm font-semibold shadow-md hover:shadow-lg transition-all"
              >
                New Request
              </button>
            </div>
          </div>
          <div className="p-6">
            <DataTables
              data={displayed}
              columns={requestColumns as any}
              onRowClicked={(row: any) => handleViewRequest(row.id)}
              darkMode={!!darkMode}
              loading={loading}
              pagination
              paginationPerPage={10}
              paginationRowsPerPageOptions={[5, 10, 15, 20]}
              searchable
              searchPlaceholder="Search requests..."
              customizableColumns
              theme="purple-gold"
            />
          </div>
        </div>
      </div>
    </div>
  );
};

export default RequestsDashboard;