// RequestList.tsx - refreshed implementation with purple/gold theme and description removed from columns
import React, { useState, useEffect } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import {  RefreshCw, ChevronLeft } from 'lucide-react';
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

  // Columns with equal width distribution
  const requestColumns = (darkMode: boolean) => [
    {
      name: 'Request ID',
      selector: (row: ProjectRequestDto) => row.requestID || `PR-${row.id}`,
      sortable: true,
      width: '12%',
      cell: (row: ProjectRequestDto) => (
        <div className="font-mono text-sm font-bold text-[#B351A9]">
          {row.requestID || `PR-${row.id}`}
        </div>
      ),
    },
    {
      name: 'Title',
      selector: (row: ProjectRequestDto) => row.requestTitle,
      sortable: true,
      width: '18%',
      cell: (row: ProjectRequestDto) => (
        <div className="font-semibold text-[#273238]">{row.requestTitle}</div>
      ),
    },
    {
      name: 'Request Type',
      selector: (row: ProjectRequestDto) => row.requestType,
      sortable: true,
      width: '12%',
      cell: (row: ProjectRequestDto) => (
        <span className="text-sm text-[#273238]">{row.requestType}</span>
      ),
    },
    {
      name: 'Priority',
      selector: (row: ProjectRequestDto) => row.priority,
      sortable: true,
      width: '10%',
      cell: (row: ProjectRequestDto) => (
        <div className="flex items-center">
          <span
            className="px-3 py-1.5 rounded-lg text-xs font-bold"
            style={(() => {
              const priority = (row.priority || '').toLowerCase();
              let color = '#273238';
              let borderColor = 'rgba(39, 50, 56, 0.15)';

              if (priority.includes('high')) {
                color = '#85257c';
                borderColor = 'rgba(133, 37, 124, 0.15)';
              } else if (priority.includes('medium') || priority.includes('med')) {
                color = '#CDA352';
                borderColor = 'rgba(205, 163, 82, 0.15)';
              } else if (priority.includes('low')) {
                color = '#273238';
                borderColor = 'rgba(39, 50, 56, 0.15)';
              }

              return {
                backgroundColor: `${color}20`,
                color,
                border: `1px solid ${borderColor}`,
              };
            })()}
          >
            {row.priority || '—'}
          </span>
        </div>
      ),
    },
    {
      name: 'Status',
      selector: (row: ProjectRequestDto) => row.status,
      sortable: true,
      width: '13%',
      cell: (row: ProjectRequestDto) => {
        const raw = (row.status || '').toLowerCase();
        let color = '#273238';
        let borderColor = 'rgba(39, 50, 56, 0.15)';

        if (raw.includes('submit') || raw.includes('new') || raw.includes('pending')) {
          color = '#CDA352';
          borderColor = 'rgba(205, 163, 82, 0.35)';
        } else if (raw.includes('approve') || raw.includes('complete') || raw.includes('deliver')) {
          color = '#10B981';
          borderColor = 'rgba(16, 185, 129, 0.35)';
        } else if (raw.includes('reject') || raw.includes('backlog') || raw.includes('close') || raw.includes('progress')) {
          color = '#273238';
          borderColor = 'rgba(39, 50, 56, 0.35)';
        }
        return (
          <span
            className="px-3 py-1.5 rounded-lg text-xs font-bold border-2"
            style={{
              backgroundColor: `${color}20`,
              color,
              borderColor,
            }}
          >
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
        <span className="text-sm text-[#273238]">{row.requestedByName}</span>
      ),
    },
    {
      name: 'Department',
      selector: (row: ProjectRequestDto) => row.businessDepartment,
      sortable: true,
      width: '12%',
      cell: (row: ProjectRequestDto) => (
        <span className="text-sm text-gray-600">{row.businessDepartment}</span>
      ),
    },
    {
      name: 'Created Date',
      selector: (row: ProjectRequestDto) => row.createdDate,
      sortable: true,
      width: '10%',
      cell: (row: ProjectRequestDto) => {
        if (!row.createdDate) return '—';
        const date = new Date(row.createdDate);
        return (
          <div className="text-sm">
            <div className="font-semibold text-[#273238]">
              {date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
            </div>
            <div className="text-gray-500 text-xs">
              {date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}
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
      {/* Header */}
      <header className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30 shadow-sm">
        <div className="max-w-8xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button onClick={handleBack} className="p-2 hover:bg-[#B351A9]/10 rounded-xl transition-colors">
              <ChevronLeft className="w-5 h-5 text-[#B351A9]" />
            </button>
            <div>
            <h1 className="text-2xl font-bold text-[#B351A9] flex">Project Requests</h1>
            <p className="text-sm text-[#85257C]">View and manage all project requests</p>
            </div>
          </div>
          <div className="text-right">
            <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
            <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
          </div>
        </div>
      </header>

      <main className="max-w-8xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Error Message */}
        {error && (
          <div className="mb-6 p-4 bg-red-50 border-2 border-red-300 rounded-xl">
            <div className="flex items-center gap-2">
              <span className="text-red-600 font-bold">⚠</span>
              <div>
                <p className="text-sm font-bold text-red-800">Error loading requests</p>
                <p className="text-sm text-red-600">{error}</p>
              </div>
            </div>
          </div>
        )}

        {/* Data Table */}
        <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-visible">
          <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 px-6 py-5 border-b border-[#CDA352]/20">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <h2 className="text-xl font-bold text-[#273238]">All Project Requests</h2>
              <div className="flex flex-col sm:flex-row sm:items-center gap-3">
                <div className="flex items-center gap-2">
                  <label className="text-sm text-[#273238] font-semibold">Status</label>
                  <select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    className="px-3 py-2 rounded-lg border border-[#CDA352]/30 text-sm focus:outline-none focus:ring-2 focus:ring-[#B351A9]/20 bg-white"
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
                    className={`px-4 py-2 rounded-lg font-semibold transition-all flex items-center gap-2 bg-white text-[#273238] hover:bg-gray-50 border-2 border-[#273238]/20 ${loading ? 'opacity-50 cursor-not-allowed' : ''}`}
                  >
                    <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
                  </button>
                  <button
                    onClick={handleNewRequest}
                    className="px-4 py-2 rounded-lg font-semibold text-sm text-white bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] transition-all flex items-center gap-2 shadow-md hover:shadow-lg"
                  >
                    <span>New Request</span>
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