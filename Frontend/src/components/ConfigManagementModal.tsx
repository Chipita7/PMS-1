import React, { useState, useEffect } from 'react';
import { X, Plus, Trash2, Save, AlertCircle, Check, Settings, Sparkles } from 'lucide-react';
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogDescription,
    DialogFooter
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue
} from '@/components/ui/select';
import {
    addConfigOption,
    deleteConfigOption,
    getRequestConfigurations,
    type RequestConfigurations,
    type ConfigOption
} from '../services/requestService';

interface ConfigManagementModalProps {
    isOpen: boolean;
    onClose: () => void;
    onUpdate: () => void;
}

const CONFIG_TYPES = [
    { id: 'requesttypes', label: 'Request Types', icon: '📋' },
    { id: 'requestcategories', label: 'Request Categories', icon: '📁' },
    { id: 'servicecategories', label: 'Service Categories', icon: '🔧' },
    { id: 'productcategories', label: 'Product Categories', icon: '📦' },
    { id: 'priorities', label: 'Priorities', icon: '⚡' },
    { id: 'impacturgencies', label: 'Impact/Urgency Levels', icon: '🎯' },
    { id: 'strategicalignments', label: 'Strategic Alignments', icon: '🎪' },
];

export const ConfigManagementModal: React.FC<ConfigManagementModalProps> = ({
    isOpen,
    onClose,
    onUpdate
}) => {
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
        if (isOpen) {
            fetchConfigs();
        }
    }, [isOpen]);

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
            setSuccessMsg(null);

            await addConfigOption(activeTab, {
                name: newItemName,
                code: newItemCode.toUpperCase(),
                description: newItemDesc,
                color: newItemColor,
                sortOrder: getCurrentList().length + 1
            });

            setSuccessMsg('Option added successfully');
            setNewItemName('');
            setNewItemCode('');
            setNewItemDesc('');

            await fetchConfigs();
            onUpdate();
        } catch (err: any) {
            setError(err.message || 'Failed to add option');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleDelete = async (id: number) => {
        if (!window.confirm('Are you sure you want to delete this option?')) return;

        try {
            setLoading(true);
            setError(null);
            await deleteConfigOption(activeTab, id);
            setSuccessMsg('Option deleted successfully');
            await fetchConfigs();
            onUpdate();
        } catch (err: any) {
            setError(err.message || 'Failed to delete option');
        } finally {
            setLoading(false);
        }
    };

    const handleNameChange = (val: string) => {
        setNewItemName(val);
        if (!newItemCode) {
            setNewItemCode(val.toUpperCase().replace(/[^A-Z0-9]/g, '_').substring(0, 20));
        }
    };

    return (
        <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
            <DialogContent className="max-w-5xl bg-white border-4 border-[#B351A9] shadow-2xl rounded-3xl">
                {/* Header */}
                <DialogHeader className="bg-white text-[#B351A9] p-6 -m-6 mb-6 rounded-t-3xl border-b-4 border-[#CDA352]">
                    <div className="flex justify-between items-center">
                        <div>
                            <DialogTitle className="text-3xl font-bold text-[#B351A9] flex items-center gap-3">
                                <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-[#B351A9] to-[#85257C] flex items-center justify-center">
                                    <Settings className="w-6 h-6 text-white" />
                                </div>
                                Manage Dropdown Options
                            </DialogTitle>
                            <DialogDescription className="text-gray-600 mt-2 text-sm">
                                Add or remove options for request forms. Changes are saved immediately to the database.
                            </DialogDescription>
                        </div>
                    </div>
                </DialogHeader>

                <div className="flex gap-6 h-[600px]">
                    {/* Sidebar Tabs */}
                    <div className="w-1/3 border-r-2 border-[#E4CA86]/30 pr-6">
                        <div className="space-y-2">
                            {CONFIG_TYPES.map((type) => (
                                <button
                                    key={type.id}
                                    onClick={() => {
                                        setActiveTab(type.id);
                                        setError(null);
                                        setSuccessMsg(null);
                                    }}
                                    className={`w-full text-left px-4 py-3.5 rounded-xl transition-all duration-300 font-semibold flex items-center gap-3 ${activeTab === type.id
                                        ? 'bg-gradient-to-r from-[#B351A9] to-[#85257C] text-white shadow-lg border-2 border-[#CDA352] scale-105'
                                        : 'text-[#273238] hover:bg-gradient-to-r hover:from-[#E4CA86]/20 hover:to-[#CDA352]/20 border-2 border-transparent hover:border-[#E4CA86]'
                                        }`}
                                >
                                    <span className="text-xl">{type.icon}</span>
                                    <span className="flex-1">{type.label}</span>
                                    <span className={`text-xs px-2 py-1 rounded-full ${activeTab === type.id
                                        ? 'bg-white/20 text-white'
                                        : 'bg-gray-100 text-gray-600'
                                        }`}>
                                        {getCurrentList().length}
                                    </span>
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Main Content */}
                    <div className="flex-1 flex flex-col">
                        {/* Add New Section */}
                        <div className="bg-gradient-to-br from-[#B351A9]/10 to-[#E4CA86]/10 p-5 rounded-2xl border-2 border-[#CDA352]/30 mb-6">
                            <h4 className="text-[#273238] font-bold mb-4 flex items-center gap-2 text-lg">
                                <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-[#B351A9] to-[#85257C] flex items-center justify-center">
                                    <Plus className="w-5 h-5 text-white" />
                                </div>
                                Add New {CONFIG_TYPES.find(t => t.id === activeTab)?.label.slice(0, -1)}
                            </h4>
                            <div className="grid grid-cols-2 gap-4 mb-4">
                                <div>
                                    <Label className="text-xs font-bold text-[#273238] mb-1.5 block">Name <span className="text-[#85257C]">*</span></Label>
                                    <Input
                                        value={newItemName}
                                        onChange={(e) => handleNameChange(e.target.value)}
                                        placeholder="e.g. New Option"
                                        className="border-2 border-gray-200 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 rounded-xl"
                                    />
                                </div>
                                <div>
                                    <Label className="text-xs font-bold text-[#273238] mb-1.5 block">Code <span className="text-[#85257C]">*</span></Label>
                                    <Input
                                        value={newItemCode}
                                        onChange={(e) => setNewItemCode(e.target.value.toUpperCase())}
                                        placeholder="e.g. NEW_OPTION"
                                        className="border-2 border-gray-200 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 rounded-xl font-mono text-xs"
                                    />
                                </div>
                                <div className="col-span-2">
                                    <Label className="text-xs font-bold text-[#273238] mb-1.5 block">Description</Label>
                                    <Input
                                        value={newItemDesc}
                                        onChange={(e) => setNewItemDesc(e.target.value)}
                                        placeholder="Optional description"
                                        className="border-2 border-gray-200 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 rounded-xl"
                                    />
                                </div>
                                {(activeTab === 'priorities' || activeTab === 'impacturgencies') && (
                                    <div>
                                        <Label className="text-xs font-bold text-[#273238] mb-1.5 block">Color</Label>
                                        <div className="flex gap-2">
                                            <Input
                                                type="color"
                                                value={newItemColor}
                                                onChange={(e) => setNewItemColor(e.target.value)}
                                                className="w-14 h-11 p-1 cursor-pointer rounded-xl border-2 border-gray-200"
                                            />
                                            <Input
                                                value={newItemColor}
                                                onChange={(e) => setNewItemColor(e.target.value)}
                                                className="flex-1 font-mono border-2 border-gray-200 focus:border-[#B351A9] rounded-xl"
                                            />
                                        </div>
                                    </div>
                                )}
                            </div>
                            <div className="flex justify-end">
                                <Button
                                    onClick={handleAdd}
                                    disabled={isSubmitting || !newItemName}
                                    className="bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] text-white font-bold px-6 py-2.5 rounded-xl shadow-lg border-2 border-[#B351A9] transition-all"
                                >
                                    <Plus className="w-4 h-4 mr-2" />
                                    {isSubmitting ? 'Adding...' : 'Add Option'}
                                </Button>
                            </div>
                        </div>

                        {/* Messages */}
                        {error && (
                            <div className="mb-4 p-4 bg-red-50 text-red-700 rounded-xl flex items-center gap-3 text-sm border-2 border-red-200 font-semibold">
                                <AlertCircle className="w-5 h-5" /> {error}
                            </div>
                        )}
                        {successMsg && (
                            <div className="mb-4 p-4 bg-green-50 text-green-700 rounded-xl flex items-center gap-3 text-sm border-2 border-green-200 font-semibold">
                                <Check className="w-5 h-5" /> {successMsg}
                            </div>
                        )}

                        {/* List */}
                        <div className="flex-1 overflow-y-auto pr-2">
                            <table className="w-full">
                                <thead className="sticky top-0 bg-gradient-to-r from-[#273238] to-[#0F1516] z-10 rounded-t-xl">
                                    <tr>
                                        <th className="text-left py-3 px-4 text-xs font-bold text-[#E4CA86] uppercase tracking-wider rounded-tl-xl">Name</th>
                                        <th className="text-left py-3 px-4 text-xs font-bold text-[#E4CA86] uppercase tracking-wider">Code</th>
                                        <th className="text-right py-3 px-4 text-xs font-bold text-[#E4CA86] uppercase tracking-wider rounded-tr-xl">Action</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y-2 divide-[#E4CA86]/20">
                                    {loading && configs[activeTab as keyof RequestConfigurations] === undefined ? (
                                        <tr><td colSpan={3} className="text-center py-12 text-gray-500">
                                            <div className="flex flex-col items-center gap-3">
                                                <div className="w-8 h-8 border-4 border-[#B351A9] border-t-transparent rounded-full animate-spin"></div>
                                                <span>Loading options...</span>
                                            </div>
                                        </td></tr>
                                    ) : getCurrentList().length === 0 ? (
                                        <tr><td colSpan={3} className="text-center py-12 text-gray-500">
                                            <div className="flex flex-col items-center gap-2">
                                                <Sparkles className="w-12 h-12 text-gray-300" />
                                                <span className="font-semibold">No options found</span>
                                                <span className="text-xs">Add your first option above</span>
                                            </div>
                                        </td></tr>
                                    ) : (
                                        getCurrentList().map((item) => (
                                            <tr key={item.id} className="group hover:bg-gradient-to-r hover:from-[#B351A9]/5 hover:to-[#E4CA86]/5 transition-all">
                                                <td className="py-4 px-4 text-sm font-semibold text-[#273238]">
                                                    <div className="flex items-center gap-3">
                                                        {item.color && (
                                                            <div
                                                                className="w-4 h-4 rounded-lg shadow-md border-2 border-white"
                                                                style={{ backgroundColor: item.color }}
                                                            />
                                                        )}
                                                        <div>
                                                            {item.name}
                                                            {item.description && (
                                                                <p className="text-xs text-gray-500 mt-0.5">{item.description}</p>
                                                            )}
                                                        </div>
                                                    </div>
                                                </td>
                                                <td className="py-4 px-4 text-xs font-mono text-gray-600 bg-gray-50 group-hover:bg-white transition-colors">
                                                    {item.code}
                                                </td>
                                                <td className="py-4 px-4 text-right">
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        onClick={() => handleDelete(item.id)}
                                                        className="text-gray-400 hover:text-white hover:bg-red-500 opacity-0 group-hover:opacity-100 transition-all rounded-lg"
                                                    >
                                                        <Trash2 className="w-4 h-4" />
                                                    </Button>
                                                </td>
                                            </tr>
                                        ))
                                    )}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </DialogContent>
        </Dialog>
    );
};
