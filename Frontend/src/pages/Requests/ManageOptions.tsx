import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, Plus, Trash2, AlertCircle, Check } from 'lucide-react';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import {
    addConfigOption,
    deleteConfigOption,
    getRequestConfigurations,
    type RequestConfigurations,
    type ConfigOption
} from '../../services/requestService';

const CONFIG_TYPES = [
    { id: 'requesttypes', label: 'Request Types' },
    { id: 'requestcategories', label: 'Request Categories' },
    { id: 'servicecategories', label: 'Service Categories' },
    { id: 'productcategories', label: 'Product Categories' },
    { id: 'priorities', label: 'Priorities' },
    { id: 'impacturgencies', label: 'Impact/Urgency Levels' },
    { id: 'strategicalignments', label: 'Strategic Alignments' },
];

const ManageOptions: React.FC = () => {
    const navigate = useNavigate();
    const { role } = useParams<{ role: string }>();
    const [activeTab, setActiveTab] = useState('requesttypes');
    const [configs, setConfigs] = useState<RequestConfigurations>({});
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successMsg, setSuccessMsg] = useState<string | null>(null);

    // New item state
    const [newItemName, setNewItemName] = useState('');
    const [newItemCode, setNewItemCode] = useState('');
    const [newItemDesc, setNewItemDesc] = useState('');
    const [newItemColor, setNewItemColor] = useState('#B351A9');
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        fetchConfigs();
    }, []);

    const fetchConfigs = async () => {
        try {
            setLoading(true);
            const data = await getRequestConfigurations();
            setConfigs(data);
            setError(null);
        } catch (err: any) {
            setError('Failed to load configurations');
        } finally {
            setLoading(false);
        }
    };

    const getCurrentList = (): ConfigOption[] => {
        switch (activeTab) {
            case 'requesttypes': return configs.RequestTypes || [];
            case 'requestcategories': return configs.RequestCategories || [];
            case 'servicecategories': return configs.ServiceCategories || [];
            case 'productcategories': return configs.ProductCategories || [];
            case 'priorities': return configs.Priorities || [];
            case 'impacturgencies': return configs.ImpactUrgencies || [];
            case 'strategicalignments': return configs.StrategicAlignments || [];
            default: return [];
        }
    };

    const handleAdd = async () => {
        if (!newItemName || !newItemCode) {
            setError('Name and Code are required');
            return;
        }

        try {
            setIsSubmitting(true);
            setError(null);

            // Get current list to determine sort order
            const currentList = getCurrentList();
            const maxSortOrder = currentList.length > 0
                ? Math.max(...currentList.map(item => item.sortOrder || 0))
                : 0;

            await addConfigOption(activeTab, {
                name: newItemName,
                code: newItemCode,
                description: newItemDesc || undefined,
                color: newItemColor || undefined,
                sortOrder: maxSortOrder + 1
            });

            setSuccessMsg(`Successfully added "${newItemName}"`);
            setNewItemName('');
            setNewItemCode('');
            setNewItemDesc('');
            setNewItemColor('#B351A9');

            await fetchConfigs();

            setTimeout(() => setSuccessMsg(null), 3000);
        } catch (err: any) {
            setError(err.message || 'Failed to add option');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleDelete = async (id: number, name: string) => {
        if (!confirm(`Are you sure you want to delete "${name}"?`)) {
            return;
        }

        try {
            setError(null);
            await deleteConfigOption(activeTab, id);
            setSuccessMsg(`Successfully deleted "${name}"`);
            await fetchConfigs();
            setTimeout(() => setSuccessMsg(null), 3000);
        } catch (err: any) {
            setError(err.message || 'Failed to delete option');
        }
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
            {/* Header */}
            <div className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30">
                <div className="max-w-8xl mx-auto px-6 py-4 flex justify-between items-center">
                    <div className="flex items-center gap-3">
                        <button
                            onClick={() => navigate(`/dashboard/${role}/requests/new`)}
                            className="p-2 hover:bg-[#B351A9]/10 rounded-lg transition-all"
                        >
                            <ChevronLeft className="w-6 h-6 text-[#B351A9]" />
                        </button>
                        <div>
                            <h1 className="text-2xl font-bold text-[#B351A9]">
                                Manage Dropdown Options
                            </h1>
                            <p className="text-gray-600 text-sm ml-3">Configure request form options</p>
                        </div>
                    </div>
                    <div className="text-right">
                        <div className="text-xl font-bold text-[#85257C]">Commercial Bank of Ethiopia</div>
                        <div className="text-base font-medium text-[#CDA352]">Digital Factory</div>
                    </div>
                </div>
            </div>

            <div className="max-w-7xl mx-auto px-6 py-6">
                {/* Success/Error Messages */}
                {successMsg && (
                    <div className="mb-4 p-4 bg-green-50 border border-green-200 rounded-lg flex items-center gap-2 text-green-700">
                        <Check className="w-5 h-5" />
                        <span className="text-sm font-semibold">{successMsg}</span>
                    </div>
                )}
                {error && (
                    <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg flex items-center gap-2 text-red-700">
                        <AlertCircle className="w-5 h-5" />
                        <span className="text-sm font-semibold">{error}</span>
                    </div>
                )}

                <div className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden">
                    {/* Tabs Header */}
                    <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 border-b border-[#CDA352]/20 p-6">
                        <h2 className="text-xl font-bold text-[#273238] mb-4 text-center">Configuration Categories</h2>
                        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
                            <TabsList className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-2 bg-transparent h-auto p-0">
                                {CONFIG_TYPES.map(type => (
                                    <TabsTrigger
                                        key={type.id}
                                        value={type.id}
                                        className="data-[state=active]:bg-gradient-to-r data-[state=active]:from-[#B351A9] data-[state=active]:to-[#85257C] data-[state=active]:text-white bg-white border border-gray-200 hover:border-[#B351A9] rounded-lg px-4 py-2.5 text-sm font-semibold transition-all"
                                    >
                                        {type.label}
                                    </TabsTrigger>
                                ))}
                            </TabsList>
                        </Tabs>
                    </div>

                    {/* Content */}
                    <div className="p-6">
                        {loading ? (
                            <div className="text-center py-8">
                                <div className="inline-block animate-spin rounded-full h-8 w-8 border-4 border-[#B351A9] border-t-transparent"></div>
                                <p className="text-sm text-gray-600 mt-3">Loading...</p>
                            </div>
                        ) : (
                            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                                {/* Add New Option */}
                                <div className="bg-gradient-to-br from-[#B351A9]/5 to-[#E4CA86]/5 rounded-xl p-5 border border-[#CDA352]/30">
                                    <h3 className="text-lg font-bold text-[#273238] mb-4 text-center">Add New Option</h3>
                                    <div className="space-y-4">
                                        <div>
                                            <label className="block text-sm font-semibold text-[#273238] mb-2">
                                                Name <span className="text-[#85257C]">*</span>
                                            </label>
                                            <input
                                                value={newItemName}
                                                onChange={(e) => setNewItemName(e.target.value)}
                                                className="w-full p-2.5 rounded-lg border border-gray-200 focus:border-[#B351A9] focus:ring-1 focus:ring-[#B351A9]/20 transition-all outline-none text-sm"
                                                placeholder="Enter option name"
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-semibold text-[#273238] mb-2">
                                                Code <span className="text-[#85257C]">*</span>
                                            </label>
                                            <input
                                                value={newItemCode}
                                                onChange={(e) => setNewItemCode(e.target.value)}
                                                className="w-full p-2.5 rounded-lg border border-gray-200 focus:border-[#B351A9] focus:ring-1 focus:ring-[#B351A9]/20 transition-all outline-none text-sm"
                                                placeholder="Enter option code"
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-semibold text-[#273238] mb-2">
                                                Description
                                            </label>
                                            <textarea
                                                value={newItemDesc}
                                                onChange={(e) => setNewItemDesc(e.target.value)}
                                                rows={2}
                                                className="w-full p-2.5 rounded-lg border border-gray-200 focus:border-[#B351A9] focus:ring-1 focus:ring-[#B351A9]/20 transition-all outline-none resize-none text-sm"
                                                placeholder="Optional description"
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-semibold text-[#273238] mb-2">
                                                Color
                                            </label>
                                            <div className="flex items-center gap-2">
                                                <input
                                                    type="color"
                                                    value={newItemColor}
                                                    onChange={(e) => setNewItemColor(e.target.value)}
                                                    className="h-10 w-20 rounded-lg border border-gray-200 focus:border-[#B351A9] cursor-pointer"
                                                />
                                                <span className="text-xs text-gray-500">{newItemColor}</span>
                                            </div>
                                        </div>
                                        <button
                                            onClick={handleAdd}
                                            disabled={isSubmitting || !newItemName || !newItemCode}
                                            className="w-full px-4 py-2 rounded-lg font-semibold text-sm text-white bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2"
                                        >
                                            <Plus className="w-4 h-4" />
                                            {isSubmitting ? 'Adding...' : 'Add Option'}
                                        </button>
                                    </div>
                                </div>

                                {/* Existing Options */}
                                <div className="bg-white rounded-xl border border-gray-200 p-5">
                                    <h3 className="text-lg font-bold text-[#273238] mb-4 text-center">
                                        Current Options ({getCurrentList().length})
                                    </h3>
                                    <div className="space-y-2 max-h-[500px] overflow-y-auto">
                                        {getCurrentList().length === 0 ? (
                                            <p className="text-center text-gray-500 py-8 text-sm">No options available</p>
                                        ) : (
                                            getCurrentList().map(item => (
                                                <div
                                                    key={item.id}
                                                    className="flex items-center justify-between p-3 bg-gray-50 hover:bg-gray-100 rounded-lg border border-gray-200 transition-all group"
                                                >
                                                    <div className="flex-1 min-w-0">
                                                        <div className="flex items-center gap-2">
                                                            {item.color && (
                                                                <div
                                                                    className="w-4 h-4 rounded border border-gray-300"
                                                                    style={{ backgroundColor: item.color }}
                                                                />
                                                            )}
                                                            <span className="font-semibold text-sm text-[#273238]">{item.name}</span>
                                                        </div>
                                                        <div className="flex items-center gap-2 mt-1">
                                                            <span className="text-xs text-gray-500 font-mono">{item.code}</span>
                                                            {item.description && (
                                                                <span className="text-xs text-gray-400">• {item.description}</span>
                                                            )}
                                                        </div>
                                                    </div>
                                                    <button
                                                        onClick={() => handleDelete(item.id, item.name)}
                                                        className="ml-3 p-2 text-red-600 hover:bg-red-50 rounded-lg opacity-0 group-hover:opacity-100 transition-all"
                                                        title="Delete option"
                                                    >
                                                        <Trash2 className="w-4 h-4" />
                                                    </button>
                                                </div>
                                            ))
                                        )}
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

export default ManageOptions;
