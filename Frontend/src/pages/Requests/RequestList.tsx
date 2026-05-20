// RequestList.tsx - redesigned to match the Idea Intake (RequestForm) page aesthetic
import React, { useState, useEffect } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { RefreshCw, ChevronLeft, Sparkles, Calendar, User, Building2, Tag, AlertCircle } from 'lucide-react';
import DataTables from '@/components/DataTables';
import { getAllProjectRequests, type ProjectRequestDto } from '@/services/requestService';

const RequestList: React.FC<{ darkMode?: boolean }> = ({ darkMode = false }) => {
  const navigate = useNavigate();
  const { role } = useParams<{ role: string }>();
  const [requests, setRequests] = useState<ProjectRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>('active');
  const [availableStatuses, setAvailableStatuses] = useState<string[]>([]);
  const location = useLocation();

  const handleBack = () => {
    navigate(`/dashboard/${role}`);
  };

  const fetchRequests = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await getAllProjectRequests();
      setRequests(data);
    } catch (err: any) {
      console.error('Failed to fetch requests:', err);
      const msg = err.message || 'Failed to load requests';
      setError(
        msg.includes('Cannot connect') || msg.includes('Network error')
          ? 'Backend server is not reachable. Please ensure the backend is running on http://localhost:8080'
          : msg
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRequests();
  }, []);

  // React to cross-page updates
  useEffect(() => {
    const handler = () => { fetchRequests(); };
    window.addEventListener('requests-updated', handler as EventListener);
    return () => window.removeEventListener('requests-updated', handler as EventListener);
  }, []);

  // Apply initial status filter from navigation state if provided
  useEffect(() => {
    const maybe = (location.state as any)?.statusFilter;
    if (typeof maybe === 'string' && maybe) {
      setStatusFilter(maybe);
    }
  }, [location.state]);

  useEffect(() => {
    const aliasExclusions = new Set([
      'submitted',
      'under evaluation',
      'new',
      'pending',
      'backlogged',
      'approved',
      'rejected',
    ]);

    const unique = Array.from(
      new Set(
        (requests || [])
          .map((r) => (r.status || '').trim())
          .filter((s): s is string => !!s)
      )
    )
      .filter((s) => !aliasExclusions.has(s.toLowerCase()))
      .sort((a, b) => a.localeCompare(b));

    setAvailableStatuses(unique);
  }, [requests]);

  const handleNewRequest = () => {
    navigate(`/dashboard/${role}/requests/new`);
  };

  const handleRowClick = (row: ProjectRequestDto) => {
    navigate(`/dashboard/${role}/requests/${row.id}`);
  };

  const requestColumns = (darkMode: boolean) => [
    {
      name: 'Request ID',
      selector: (row: ProjectRequestDto) => row.requestID || `PR-${row.id}`,
      sortable: true,
      width: '13%',
      cell: (row: ProjectRequestDto) => (
        <div className="px-3 py-1.5 rounded-lg bg-gradient-to-r from-[#B351A9]/8 to-[#E4CA86]/8 border border-[#CDA352]/30">
          <span className="font-mono text-sm font-bold text-[#85257C]">
            {row.requestID || `PR-${row.id}`}
          </span>
        </div>
      ),
    },
    {
      name: 'Title',
      selector: (row: ProjectRequestDto) => row.requestTitle,
      sortable: true,
      width: '20%',
      cell: (row: ProjectRequestDto) => (
        <div className="py-1">
          <div className="font-semibold text-[#273238] text-sm leading-snug">{row.requestTitle}</div>
        </div>
      ),
    },
    {
      name: 'Request Type',
      selector: (row: ProjectRequestDto) => row.requestType,
      sortable: true,
      width: '12%',
      cell: (row: ProjectRequestDto) => (
        <div className="flex items-center gap-1.5">
          <Tag className="w-3.5 h-3.5 text-[#B351A9] flex-shrink-0" />
          <span className="text-sm font-medium text-[#273238]">{row.requestType || '—'}</span>
        </div>
      ),
    },
    {
      name: 'Priority',
      selector: (row: ProjectRequestDto) => row.priority,
      sortable: true,
      width: '10%',
      cell: (row: ProjectRequestDto) => {
        const priority = (row.priority || '').toLowerCase();
        let bgClass = 'bg-gray-100 text-[#273238] border-gray-200';
        if (priority.includes('critical')) {
          bgClass = 'bg-red-50 text-red-700 border-red-200';
        } else if (priority.includes('high')) {
          bgClass = 'bg-[#85257c]/10 text-[#85257C] border-[#85257c]/20';
        } else if (priority.includes('medium') || priority.includes('med')) {
          bgClass = 'bg-[#CDA352]/10 text-[#CDA352] border-[#CDA352]/20';
        } else if (priority.includes('low')) {
          bgClass = 'bg-gray-100 text-[#273238] border-gray-200';
        }
        return (
          <span className={`px-3 py-1.5 rounded-lg text-xs font-bold border ${bgClass}`}>
            {row.priority || '—'}
          </span>
        );
      },
    },
    {
      name: 'Status',
      selector: (row: ProjectRequestDto) => row.status,
      sortable: true,
      width: '13%',
      cell: (row: ProjectRequestDto) => {
        const raw = (row.status || '').toLowerCase();
        let bgClass = 'bg-gray-100 text-[#273238] border-gray-200';
        if (raw.includes('submit') || raw.includes('new') || raw.includes('pending')) {
          bgClass = 'bg-[#CDA352]/10 text-[#CDA352] border-[#CDA352]/25';
        } else if (raw.includes('approve') || raw.includes('complete') || raw.includes('deliver')) {
          bgClass = 'bg-emerald-50 text-emerald-600 border-emerald-200';
        } else if (raw.includes('reject')) {
          bgClass = 'bg-red-50 text-red-600 border-red-200';
        } else if (raw.includes('evaluation') || raw.includes('progress')) {
          bgClass = 'bg-[#B351A9]/10 text-[#B351A9] border-[#B351A9]/20';
        }
        return (
          <span className={`px-3 py-1.5 rounded-lg text-xs font-bold border ${bgClass}`}>
            {row.status || '—'}
          </span>
        );
      },
    },
    {
      name: 'Requested By',
      selector: (row: ProjectRequestDto) => row.requestedByName,
      sortable: true,
      width: '13%',
      cell: (row: ProjectRequestDto) => (
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 rounded-full bg-gradient-to-br from-[#B351A9] to-[#85257C] flex items-center justify-center flex-shrink-0">
            <User className="w-3.5 h-3.5 text-white" />
          </div>
          <span className="text-sm font-medium text-[#273238] truncate">{row.requestedByName || '—'}</span>
        </div>
      ),
    },
    {
      name: 'Department',
      selector: (row: ProjectRequestDto) => row.businessDepartment,
      sortable: true,
      width: '11%',
      cell: (row: ProjectRequestDto) => (
        <div className="flex items-center gap-1.5">
          <Building2 className="w-3.5 h-3.5 text-[#CDA352] flex-shrink-0" />
          <span className="text-sm text-gray-600 truncate">{row.businessDepartment || '—'}</span>
        </div>
      ),
    },
    {
      name: 'Created Date',
      selector: (row: ProjectRequestDto) => row.createdDate,
      sortable: true,
      width: '8%',
      cell: (row: ProjectRequestDto) => {
        if (!row.createdDate) return <span className="text-sm text-gray-400">—</span>;
        const date = new Date(row.createdDate);
        return (
          <div className="flex items-start gap-1.5">
            <Calendar className="w-3.5 h-3.5 text-[#B351A9] mt-0.5 flex-shrink-0" />
            <div>
              <div className="text-sm font-semibold text-[#273238] leading-tight">
                {date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
              </div>
              <div className="text-xs text-gray-400 leading-tight">
                {date.getFullYear()}
              </div>
            </div>
          </div>
        );
      },
    },
  ];

  const filteredRequests = (() => {
    const normalize = (s?: string) => (s || '').toLowerCase();
    const aliases: Record<string, string[]> = {
      pending: ['Submitted', 'Under Evaluation', 'New', 'Pending', 'Backlogged'],
      newRequests: ['Submitted', 'New'],
      inEvaluation: ['Under Evaluation'],
      approved: ['Approved'],
      rejected: ['Rejected'],
      active: ['Submitted', 'Under Evaluation', 'New', 'Pending', 'Backlogged', 'Approved', 'In Progress', 'In Development'],
    };

    if (statusFilter === 'all') return requests;

    if (statusFilter === 'active') {
      const set = new Set(aliases.active.map(x => x.toLowerCase()));
      return requests.filter(r => set.has(normalize(r.status)));
    }

    if (aliases[statusFilter]) {
      const set = new Set(aliases[statusFilter].map(x => x.toLowerCase()));
      return requests.filter(r => set.has(normalize(r.status)));
    }

    return requests.filter(r => normalize(r.status) === normalize(statusFilter));
  })();

  return (
    <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
      {/* Header - Matching Idea Intake (RequestForm) */}
      <div className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <button onClick={handleBack} className="mr-4 p-2 hover:bg-[#B351A9]/10 rounded-lg transition-colors">
                <ChevronLeft className="w-5 h-5 text-[#B351A9]" />
              </button>
              <div>
                <h1 className="text-2xl font-bold text-[#B351A9] flex items-center gap-2">
                  <Sparkles className="w-5 h-5 text-[#CDA352]" />
                  Submitted Ideas
                </h1>
                <p className="text-gray-600 text-sm ml-7">View and manage all submitted project requests</p>
              </div>
            </div>
            <div className="text-right">
              <div className="text-xl font-semibold text-[#85257C]">Commercial Bank of Ethiopia</div>
              <div className="text-sm font-medium text-[#CDA352]">Digital Factory</div>
            </div>
          </div>
        </div>
      </div>

      <main className="max-w-7xl mx-auto px-6 py-8">
        {/* Error Message */}
        {error && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
            <div className="flex items-start">
              <AlertCircle className="w-5 h-5 text-red-500 mr-3 mt-0.5 flex-shrink-0" />
              <div>
                <h3 className="text-sm font-semibold text-red-800">Error loading requests</h3>
                <p className="text-sm text-red-700 mt-1">{error}</p>
              </div>
            </div>
          </div>
        )}

        {/* Data Table */}
        <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-visible">
          <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-6 py-5 border-b border-[#CDA352]/20">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-xl font-bold text-[#273238]">All Project Requests</h2>
                <p className="text-sm text-gray-500 mt-1">{filteredRequests.length} request{filteredRequests.length !== 1 ? 's' : ''} found</p>
              </div>
              <div className="flex flex-col sm:flex-row sm:items-center gap-3">
                <div className="flex items-center gap-2">
                  <label className="text-sm text-[#273238] font-semibold">Status</label>
                  <select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    className="px-3 py-2 rounded-lg border border-[#CDA352]/30 text-sm focus:outline-none focus:ring-2 focus:ring-[#B351A9]/20 bg-white font-['Times_New_Roman',_Times,_serif]"
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
                <div className="flex items-center gap-3 justify-end">
                  <button
                    onClick={fetchRequests}
                    disabled={loading}
                    className={`p-2.5 rounded-lg font-semibold transition-all flex items-center gap-2 bg-white text-[#273238] hover:bg-gray-50 border border-[#CDA352]/30 ${loading ? 'opacity-50 cursor-not-allowed' : ''}`}
                  >
                    <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
                  </button>
                  <button
                    onClick={handleNewRequest}
                    className="inline-flex items-center px-5 py-2.5 rounded-lg bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] text-white text-sm font-semibold shadow-md hover:shadow-lg transition-all"
                  >
                    New Request
                  </button>
                </div>
              </div>
            </div>
          </div>
          <div className="p-6">
            <DataTables
              columns={requestColumns(darkMode)}
              data={filteredRequests}
              loading={loading}
              onRowClicked={handleRowClick}
              darkMode={darkMode}
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
      </main>
    </div>
  );
};

export default RequestList;