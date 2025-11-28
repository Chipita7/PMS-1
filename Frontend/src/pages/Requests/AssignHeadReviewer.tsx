import React from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ChevronLeft, UserPlus, ShieldCheck, Send, Sparkles } from "lucide-react";
import { toast } from "react-hot-toast";
import { projectRequestService, ProjectRequestSummary } from "@/services/projectRequestService";
import { useAuth } from "@/context/AuthContext";
import UserSelect from "@/components/UserSelect";

const PENDING_STATUSES = ["Submitted", "Under Evaluation", "New", "Pending", "Backlogged"];

const AssignHeadReviewer: React.FC = () => {
  const navigate = useNavigate();
  const { role, id } = useParams<{ role: string; id?: string }>();
  const { user } = useAuth();

  const [requests, setRequests] = React.useState<ProjectRequestSummary[]>([]);
  const [loading, setLoading] = React.useState<boolean>(false);
  const [error, setError] = React.useState<string | null>(null);

  const [selectedRequestId, setSelectedRequestId] = React.useState<number | null>(id ? Number(id) : null);
  const [assignUserId, setAssignUserId] = React.useState<string>("");
  const [assignUserName, setAssignUserName] = React.useState<string>("");
  const [submitting, setSubmitting] = React.useState<boolean>(false);

  React.useEffect(() => {
    let mounted = true;
    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await projectRequestService.getAll();
        if (!mounted) return;
        const pending = data.filter((r) => PENDING_STATUSES.some((s) => r.status?.toLowerCase() === s.toLowerCase()));
        setRequests(pending);
        if (!selectedRequestId && id) setSelectedRequestId(Number(id));
      } catch (e: any) {
        if (!mounted) return;
        setError(e.message || "Failed to load requests");
      } finally {
        if (mounted) setLoading(false);
      }
    };
    load();
    return () => {
      mounted = false;
    };
  }, [id, selectedRequestId]);

  const currentRole = role || "member";

  const selectedRequest = React.useMemo(() => requests.find((r) => r.id === selectedRequestId) || null, [requests, selectedRequestId]);

  const handleSelfAssign = async () => {
    if (!selectedRequestId) {
      toast.error("Please select a request first");
      return;
    }
    if (!user) {
      toast.error("You are not authenticated. Please log in again.");
      navigate("/login");
      return;
    }

    // ✅ FIXED: For self-assignment, send empty payload (only notify flag)
    // The backend will extract the current user from the JWT token (GetCurrentUserId())
    // This ensures the CURRENT LOGGED-IN USER is assigned, not the request creator
    const payload = { notify: true };

    console.log("🔍 Self-assign payload (backend will use JWT token for user ID):", payload);
    console.log("🔍 Current user from auth context:", { id: user.id, username: user.username, name: user.name });

    setSubmitting(true);
    try {
      await projectRequestService.assignHeadReviewer(selectedRequestId, payload as any);
      toast.success("You are assigned as Head Reviewer for this request.");
      setTimeout(() => {
        navigate(`/dashboard/${currentRole}/requests`);
      }, 500);
    } catch (e: any) {
      toast.error(e.message || "Failed to assign head reviewer");
    } finally {
      setSubmitting(false);
    }
  };

  const handleAssignOther = async () => {
    if (!selectedRequestId || !assignUserId) return;
    setSubmitting(true);
    try {
      await projectRequestService.assignHeadReviewer(selectedRequestId, {
        headUserId: assignUserId,
        notify: true,
      } as any);
      toast.success(`Assigned ${assignUserName || assignUserId} as Head Reviewer.`);
      setTimeout(() => {
        navigate(`/dashboard/${currentRole}/requests`);
      }, 500);
    } catch (e: any) {
      toast.error(e.message || "Failed to assign head reviewer");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
      {/* Header - Matching RequestForm */}
      <div className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30">
        <div className="max-w-8xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <button
                onClick={() => navigate(-1)}
                className="p-2 hover:bg-[#B351A9]/10 rounded-lg transition-all"
              >
                <ChevronLeft className="w-6 h-6 text-[#B351A9]" />
              </button>
              <div>
                <h1 className="text-2xl font-bold text-[#B351A9] flex items-center gap-2">
                  <ShieldCheck className="w-5 h-5 text-[#CDA352]" />
                  Assign Head Reviewer
                </h1>
                <p className="text-gray-600 text-sm ml-5">Appoint the owner responsible for evaluation and final decision</p>
              </div>
            </div>
            <div className="text-right">
              <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
              <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-6 py-6">
        <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden">
          <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-6 py-4 border-b border-[#CDA352]/20">
            <h2 className="text-lg font-bold text-[#273238]">Select Request</h2>
          </div>
          <div className="p-6 space-y-6">
            {error && <div className="p-3 rounded-lg border border-red-200 bg-red-50 text-sm text-red-700 font-semibold">{error}</div>}
            {loading ? (
              <div className="text-sm text-gray-600">Loading requests...</div>
            ) : (
              <div>
                <select
                  className="w-full md:w-1/2 border border-[#CDA352]/30 rounded-lg px-4 py-2.5 focus:outline-none focus:ring-2 focus:ring-[#B351A9]/20 focus:border-[#B351A9] transition-all text-sm"
                  value={selectedRequestId ?? ""}
                  onChange={(e) => setSelectedRequestId(e.target.value ? Number(e.target.value) : null)}
                >
                  <option value="">Select a request...</option>
                  {requests.map((r) => (
                    <option key={r.id} value={r.id}>
                      #{r.requestID} — {r.requestTitle}
                    </option>
                  ))}
                </select>
                {selectedRequest && (
                  <p className="mt-2 text-sm text-gray-600">{selectedRequest.requestDescription || "No description"}</p>
                )}
              </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Assign Myself Card */}
              <div className="p-5 rounded-xl border border-[#CDA352]/30 bg-gradient-to-br from-[#B351A9]/5 to-[#E4CA86]/5">
                <div className="flex items-center mb-3">
                  <ShieldCheck className="w-5 h-5 text-[#B351A9] mr-2" />
                  <h3 className="font-bold text-[#273238] text-sm">Assign Myself</h3>
                </div>
                <p className="text-sm text-gray-600 mb-4">You will be responsible for reviewer assignments, tracking, and final decision.</p>
                <button
                  disabled={!selectedRequestId || submitting}
                  onClick={handleSelfAssign}
                  className="inline-flex items-center px-5 py-2.5 rounded-lg bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] text-white text-sm font-semibold shadow-md hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                >
                  <UserPlus className="w-4 h-4 mr-2" />
                  {submitting ? "Assigning..." : "Assign Me as Head"}
                </button>
              </div>

              {/* Assign Someone Else Card */}
              <div className="p-5 rounded-xl border border-[#CDA352]/30 bg-gradient-to-br from-[#E4CA86]/5 to-[#CDA352]/5">
                <div className="flex items-center mb-3">
                  <UserPlus className="w-5 h-5 text-[#CDA352] mr-2" />
                  <h3 className="font-bold text-[#273238] text-sm">Assign Someone Else</h3>
                </div>
                <div className="flex items-center space-x-3">
                  <div className="flex-1">
                    <UserSelect
                      value={assignUserId}
                      onChange={(ids, opts) => {
                        setAssignUserId(ids[0] || "");
                        setAssignUserName((opts[0]?.name) || "");
                      }}
                      placeholder="Select head reviewer"
                    />
                  </div>
                  <button
                    disabled={!selectedRequestId || !assignUserId}
                    onClick={handleAssignOther}
                    className="inline-flex items-center px-5 py-2.5 rounded-lg bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] text-white text-sm font-semibold shadow-md hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                  >
                    <Send className="w-4 h-4 mr-2" />
                    Assign
                  </button>
                </div>
                <p className="text-xs text-gray-500 mt-2">They will receive a notification and see this under their head assignments.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AssignHeadReviewer;
