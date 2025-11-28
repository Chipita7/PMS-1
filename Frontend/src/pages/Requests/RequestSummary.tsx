import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft } from 'lucide-react';
import RequestDetailsTabs from '@/components/RequestDetailsTabs';

const RequestSummary: React.FC<{ darkMode?: boolean }> = ({ darkMode = false }) => {
  const navigate = useNavigate();
  const { role, id } = useParams<{ role: string; id: string }>();

  const handleBack = () => {
    navigate(`/dashboard/${role}/requests/${id}`);
  };

  const requestId = id ? parseInt(id, 10) : NaN;

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
              <h1 className="text-2xl font-bold text-[#B351A9] flex">Request Summary</h1>
              <p className="text-sm text-[#85257C]">Detailed evaluations, comments, workflow and audit trail</p>
            </div>
          </div>
          <div className="text-right">
            <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
            <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
          </div>
        </div>
      </header>

      <main className="max-w-8xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <section className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 p-6">
          {!Number.isNaN(requestId) && <RequestDetailsTabs requestId={requestId} />}
        </section>
      </main>
    </div>
  );
};

export default RequestSummary;
