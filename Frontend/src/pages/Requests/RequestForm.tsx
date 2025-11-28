import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ChevronLeft, Settings, Sparkles, CheckCircle2 } from 'lucide-react';
import { motion } from 'framer-motion';
import AttachmentUploader, { AttachmentItem } from '../../components/AttachmentUploader';
import {
  getRequestConfigurations,
  createProjectRequest,
  findConfigIdByName,
  calculatePriorityFromUrgencyAndImpact,
  type RequestConfigurations,
  type ConfigOption,
  type CreateProjectRequestDto
} from '../../services/requestService';
import { userService } from '../../services/userService';
import { useAuth } from '../../context/AuthContext';

type RequestModel = {
  requestId: string;
  referenceNo?: string;
  title: string;
  description?: string;
  requestedBy: string;
  requestedByName?: string;
  businessSector?: string;
  businessDivision?: string;
  businessDepartment?: string;
  organizationName?: string;
  primaryContactEmail?: string;
  secondaryContactEmail?: string;
  primaryContactPhone?: string;
  secondaryContactPhone?: string;
  requestType?: string;
  requestCategory?: string;
  serviceCategory?: string;
  productCategory?: string;
  priorityLevel?: string;
  businessImpact?: string;
  requestUrgency?: string;
  strategicAlignment?: string;
  estimatedCost?: number;
  estimatedBenefit?: number;
  benefitCaptureDuration?: number;
  requestedDeliveryDate?: string;
  riskLevel?: string;
  complexityLevel?: string;
  status?: string;
  workflowStage?: string;
  assignedTeam?: string;
  assignedTo?: string;
  createdDate?: string;
  lastUpdatedDate?: string;
  lastUpdatedBy?: string;
  approvalDate?: string;
  requestDurationDays?: number;
  attachmentList?: AttachmentItem[];
};

const defaultOrg = 'Commercial Bank of Ethiopia';
const pad2 = (n: number) => n.toString().padStart(2, '0');
const generateRequestId = () => {
  const now = new Date();
  const YYYY = now.getFullYear();
  const MM = pad2(now.getMonth() + 1);
  const DD = pad2(now.getDate());
  const seq = pad2(Math.floor(Math.random() * 99) + 1);
  return `PR${YYYY}${MM}${DD}${seq}`;
};

const SCREENS = {
  SUMMARY: 'summary',
  CLASSIFICATION: 'classification',
  METADATA: 'metadata',
  REQUESTOR_DETAILS: 'requestor_details',
  ATTACHMENTS: 'attachments',
  REVIEW: 'review'
};

const RequestForm: React.FC<{ darkMode?: boolean }> = ({ darkMode = false }) => {
  const navigate = useNavigate();
  const { role } = useParams<{ role: string }>();
  const { user } = useAuth();

  const [currentScreen, setCurrentScreen] = React.useState(SCREENS.SUMMARY);
  const [form, setForm] = React.useState<RequestModel>({
    requestId: generateRequestId(),
    title: '',
    requestedBy: '',
    requestedByName: '',
    organizationName: defaultOrg,
    status: 'Submitted',
    createdDate: new Date().toISOString()
  });
  const [attachmentList, setAttachmentList] = React.useState<AttachmentItem[]>([]);
  const [submitted, setSubmitted] = React.useState<RequestModel | null>(null);
  const [errors, setErrors] = React.useState<Record<string, string>>({});
  const [completedSteps, setCompletedSteps] = React.useState<Set<string>>(new Set());
  const [alignmentSelectOption, setAlignmentSelectOption] = React.useState<string>('');
  const [configurations, setConfigurations] = React.useState<RequestConfigurations>({});
  const [loadingConfigs, setLoadingConfigs] = React.useState(true);
  const [submitting, setSubmitting] = React.useState(false);
  const [submitError, setSubmitError] = React.useState<string | null>(null);
  const [requestedByDisplay, setRequestedByDisplay] = React.useState<string>('');

  React.useEffect(() => {
    const fetchConfigurations = async () => {
      try {
        setLoadingConfigs(true);
        const configs = await getRequestConfigurations();
        console.log('📋 Loaded configurations:', {
          ImpactLevels: configs.ImpactLevels?.length || 0,
          UrgencyLevels: configs.UrgencyLevels?.length || 0,
          Priorities: configs.Priorities?.length || 0,
          RiskLevels: configs.RiskLevels?.length || 0,
          ComplexityLevels: configs.ComplexityLevels?.length || 0,
          ImpactUrgencies: configs.ImpactUrgencies?.length || 0
        });
        setConfigurations(configs);
      } catch (error: any) {
        console.error('Failed to fetch configurations:', error);
        setErrors((prev) => ({ ...prev, _general: error.message || 'Failed to load form options. Please refresh the page.' }));
      } finally {
        setLoadingConfigs(false);
      }
    };
    fetchConfigurations();
  }, []);

  React.useEffect(() => {
    const applyUserDefaults = (u: any, isFromDatabase = false) => {
      const id = (u?.id || u?.Id || '').toString();
      const employeeId = (u?.employeeId || u?.EmployeeId || '').toString();
      const fullName = (u?.fullName || u?.name || `${u?.firstName || ''} ${u?.lastName || ''}`.trim()).trim();
      const username = u?.userName || u?.username || '';
      const incomingPhone = (u?.phoneNumber || u?.PhoneNumber || '').toString();
      
      // For display, prioritize database employee ID above all else
      if (isFromDatabase && employeeId) {
        setRequestedByDisplay(employeeId);
      } else if (!requestedByDisplay) {
        // Fallback to other identifiers only if we don't have employee ID yet
        const display = employeeId || username || fullName || id;
        setRequestedByDisplay(display || '');
      }
      
      setForm((prev) => ({
        ...prev,
        // Backend still gets the GUID (id), but frontend shows employee ID in display
        requestedBy: prev.requestedBy || id || '',
        requestedByName: prev.requestedByName || fullName || username || '',
        primaryContactEmail: prev.primaryContactEmail || u?.email || u?.Email || '',
        primaryContactPhone: (() => {
          const current = (prev.primaryContactPhone || '').toString();
          if (!current && incomingPhone) return incomingPhone;
          if (incomingPhone && incomingPhone.length > current.length) return incomingPhone;
          return current;
        })(),
        businessDepartment: prev.businessDepartment || u?.department || u?.Department || '',
        organizationName: prev.organizationName || defaultOrg,
      }));
    };

    // First apply JWT data (for immediate display)
    if (user) {
      applyUserDefaults(user, false);
    }
    
    // Then fetch and apply database data (this will override display with employee ID)
    (async () => {
      try {
        const res = await userService.getCurrentUser();
        const me = (res as any)?.data || res;
        if (me) applyUserDefaults(me, true); // Mark as from database
      } catch (error) {
        console.error('Failed to fetch user details:', error);
      }
    })();
  }, [user]);

  const onChange = (k: keyof RequestModel, v: any) => {
    setForm((s) => {
      const updated = { ...s, [k]: v };
      if ((k === 'requestUrgency' || k === 'businessImpact') &&
        configurations.Priorities &&
        configurations.Priorities.length > 0) {
        const calculatedPriority = calculatePriorityFromUrgencyAndImpact(
          updated.requestUrgency,
          updated.businessImpact,
          configurations.Priorities
        );
        if (calculatedPriority) {
          updated.priorityLevel = calculatedPriority;
        } else {
          updated.priorityLevel = '';
        }
      }
      return updated;
    });
    setErrors((e) => ({ ...e, [k]: '' }));
  };

  const screensArray = React.useMemo(() => Object.values(SCREENS), []);

  const numberSequentiallyCompleted = React.useMemo(() => {
    for (let i = 0; i < screensArray.length; i++) {
      if (!completedSteps.has(screensArray[i])) return i;
    }
    return screensArray.length;
  }, [completedSteps, screensArray]);

  const firstIncompleteIndex = Math.min(screensArray.length - 1, Math.max(0, numberSequentiallyCompleted));

  const validateScreen = (screen: string): boolean => {
    const errs: Record<string, string> = {};
    switch (screen) {
      case SCREENS.SUMMARY:
        if (!form.title) errs.title = 'Request title is required';
        if (!form.description) errs.description = 'Request description is required';
        break;
      case SCREENS.CLASSIFICATION:
        if (!form.requestType) errs.requestType = 'Request type is required';
        if (!form.requestCategory) errs.requestCategory = 'Request category is required';
        if (!form.serviceCategory) errs.serviceCategory = 'Service category is required';
        if (!form.productCategory) errs.productCategory = 'Product category is required';
        if (!form.priorityLevel) errs.priorityLevel = 'Priority level is required';
        if (!form.businessImpact) errs.businessImpact = 'Business impact is required';
        if (!form.requestUrgency) errs.requestUrgency = 'Request urgency is required';
        break;
      case SCREENS.METADATA:
        if (!form.strategicAlignment) errs.strategicAlignment = 'Strategic alignment is required';
        break;
      case SCREENS.REQUESTOR_DETAILS:
        if (!form.requestedBy) errs.requestedBy = 'Requested By (Employee ID) is required';
        if (!form.requestedByName) errs.requestedByName = 'Requested By Name is required';
        if (!form.businessSector) errs.businessSector = 'Business Sector is required';
        if (!form.businessDivision) errs.businessDivision = 'Business Division is required';
        if (!form.businessDepartment) errs.businessDepartment = 'Business Department is required';
        if (!form.primaryContactEmail) errs.primaryContactEmail = 'Primary contact email is required';
        if (!form.primaryContactPhone) errs.primaryContactPhone = 'Primary contact phone is required';
        break;
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const nextScreen = () => {
    if (!validateScreen(currentScreen)) return;
    setCompletedSteps((prev) => new Set(prev).add(currentScreen));
    const screens = screensArray;
    const currentIndex = screens.indexOf(currentScreen);
    if (currentIndex < screens.length - 1) {
      setCurrentScreen(screens[currentIndex + 1]);
    }
  };

  const prevScreen = () => {
    const screens = screensArray;
    const currentIndex = screens.indexOf(currentScreen);
    if (currentIndex > 0) {
      setCurrentScreen(screens[currentIndex - 1]);
    }
  };

  const handleStepClick = (screen: string) => {
    const screens = screensArray;
    const targetIndex = screens.indexOf(screen);
    const currentIndex = screens.indexOf(currentScreen);
    if (targetIndex <= currentIndex) {
      setCurrentScreen(screen);
      return;
    }
    if (targetIndex > firstIncompleteIndex) {
      setCurrentScreen(screens[firstIncompleteIndex]);
      return;
    }
    setCurrentScreen(screen);
  };

  const onSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!validateScreen(currentScreen)) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      const requestTypeConfigId = findConfigIdByName(configurations.RequestTypes, form.requestType || '');
      const requestCategoryConfigId = findConfigIdByName(configurations.RequestCategories, form.requestCategory || '');
      const serviceCategoryConfigId = findConfigIdByName(configurations.ServiceCategories, form.serviceCategory || '');
      const productCategoryConfigId = findConfigIdByName(configurations.ProductCategories, form.productCategory || '');
      const priorityConfigId = findConfigIdByName(configurations.Priorities, form.priorityLevel || '');
      const businessImpactConfigId = findConfigIdByName(
        configurations.ImpactLevels || configurations.ImpactUrgencies,
        form.businessImpact || ''
      );
      const requestUrgencyConfigId = findConfigIdByName(
        configurations.UrgencyLevels || configurations.ImpactUrgencies,
        form.requestUrgency || ''
      );
      const riskLevelConfigId = form.riskLevel ? findConfigIdByName(
        configurations.RiskLevels || configurations.ImpactUrgencies,
        form.riskLevel
      ) : undefined;
      const complexityLevelConfigId = form.complexityLevel ? findConfigIdByName(
        configurations.ComplexityLevels || configurations.ImpactUrgencies,
        form.complexityLevel
      ) : undefined;

      if (!requestTypeConfigId || !requestCategoryConfigId || !serviceCategoryConfigId ||
        !productCategoryConfigId || !priorityConfigId || !businessImpactConfigId || !requestUrgencyConfigId) {
        throw new Error('Please ensure all classification fields are properly selected.');
      }

      const createDto: CreateProjectRequestDto = {
        requestTitle: form.title,
        requestDescription: form.description || '',
        referenceNo: form.referenceNo,
        requestTypeConfigId,
        requestCategoryConfigId,
        serviceCategoryConfigId,
        productCategoryConfigId,
        priorityConfigId,
        businessImpactConfigId,
        requestUrgencyConfigId,
        strategicAlignment: form.strategicAlignment || '',
        estimatedCost: form.estimatedCost,
        estimatedBenefit: form.estimatedBenefit,
        benefitCaptureDuration: form.benefitCaptureDuration,
        requestedDeliveryDate: form.requestedDeliveryDate,
        riskLevelConfigId,
        complexityLevelConfigId,
        primaryContactEmail: form.primaryContactEmail || '',
        secondaryContactEmail: form.secondaryContactEmail,
        primaryContactPhone: form.primaryContactPhone || '',
        secondaryContactPhone: form.secondaryContactPhone,
        requestedByName: form.requestedByName,
        businessSector: form.businessSector,
        businessDivision: form.businessDivision,
        businessDepartment: form.businessDepartment,
        organizationName: form.organizationName
      };

      await createProjectRequest(createDto);
      const payload: RequestModel = {
        ...form,
        attachmentList,
        lastUpdatedDate: new Date().toISOString()
      };
      setSubmitted(payload);
    } catch (error: any) {
      console.error('Failed to submit request:', error);
      setSubmitError(error.message || 'Failed to submit request. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const renderScreen = () => {
    const inputClass = "w-full p-3 rounded-lg border border-gray-300 focus:border-[#B351A9] focus:ring-2 focus:ring-[#B351A9]/20 transition-all duration-200 font-['Times_New_Roman',_Times,_serif]";
    const labelClass = "block text-sm font-semibold text-[#273238] mb-2 font-['Times_New_Roman',_Times,_serif]";

    switch (currentScreen) {
      case SCREENS.SUMMARY:
        return (
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} className="space-y-6">
            <div className="text-center mb-8">
              <h3 className="text-2xl font-bold text-[#B351A9]">Request Summary</h3>
              <p className="text-gray-600 mt-2">Provide the basic information about your request</p>
            </div>
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <div className="space-y-6">
                <div>
                  <label className={labelClass}>Request ID *</label>
                  <div className="p-3 rounded-lg border border-[#CDA352]/30 bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5">
                    <span className="font-mono text-[#85257C] font-bold">{form.requestId}</span>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">Automatically generated request identifier</p>
                </div>
                <div>
                  <label className={labelClass}>Reference No</label>
                  <input
                    value={form.referenceNo || ''}
                    onChange={(e) => onChange('referenceNo', e.target.value)}
                    className={inputClass}
                    placeholder="e.g., BR-DVS-012345"
                  />
                </div>
              </div>
              <div className="space-y-6">
                <div>
                  <label className={labelClass}>Request Title *</label>
                  <input
                    value={form.title}
                    onChange={(e) => onChange('title', e.target.value)}
                    className={inputClass}
                    placeholder="Brief, descriptive title of the request"
                  />
                  {errors.title && <div className="text-xs text-red-500 mt-2 flex items-center">⚠ {errors.title}</div>}
                </div>
              </div>
            </div>
            <div>
              <label className={labelClass}>Request Description *</label>
              <textarea
                value={form.description || ''}
                onChange={(e) => onChange('description', e.target.value)}
                rows={4}
                className={`${inputClass} resize-none`}
                placeholder="Detailed description of the idea or request..."
              />
              {errors.description && <div className="text-xs text-red-500 mt-2 flex items-center">⚠ {errors.description}</div>}
            </div>
          </motion.div>
        );

      case SCREENS.CLASSIFICATION:
        return (
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} className="space-y-6">
            <div className="text-center mb-8">
              <h3 className="text-2xl font-bold text-[#B351A9]">Classification & Categorization</h3>
              <p className="text-gray-600 mt-2">Categorize your request for proper processing</p>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {loadingConfigs ? (
                <div className="md:col-span-2 text-center py-8">
                  <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-[#B351A9]"></div>
                  <p className="mt-2 text-gray-600">Loading form options...</p>
                </div>
              ) : (
                <>
                  {[
                    { label: 'Request Type *', key: 'requestType', options: configurations.RequestTypes, error: errors.requestType },
                    { label: 'Request Category *', key: 'requestCategory', options: configurations.RequestCategories, error: errors.requestCategory },
                    { label: 'Service Category *', key: 'serviceCategory', options: configurations.ServiceCategories, error: errors.serviceCategory },
                    { label: 'Product Category *', key: 'productCategory', options: configurations.ProductCategories, error: errors.productCategory },
                    { label: 'Business Impact *', key: 'businessImpact', options: configurations.ImpactLevels || configurations.ImpactUrgencies, error: errors.businessImpact },
                    { label: 'Request Urgency *', key: 'requestUrgency', options: configurations.UrgencyLevels || configurations.ImpactUrgencies, error: errors.requestUrgency }
                  ].map(({ label, key, options, error }) => (
                    <div key={key}>
                      <label className={labelClass}>{label}</label>
                      <select
                        value={(form as any)[key] || ''}
                        onChange={(e) => onChange(key as keyof RequestModel, e.target.value)}
                        className={`${inputClass} appearance-none bg-white`}
                      >
                        <option value="">Select {label.replace(' *', '')}</option>
                        {options?.map(option => (
                          <option key={option.id} value={option.name}>{option.name}</option>
                        ))}
                      </select>
                      {error && <div className="text-xs text-red-500 mt-2 flex items-center">⚠ {error}</div>}
                    </div>
                  ))}
                  <div>
                    <label className={labelClass}>
                      Priority Level * <span className="ml-2 text-xs font-normal text-[#CDA352]">(Auto-calculated)</span>
                    </label>
                    <div className="relative">
                      <div className={`p-3 rounded-lg border ${form.priorityLevel && form.requestUrgency && form.businessImpact
                        ? 'bg-gradient-to-r from-[#B351A9]/10 to-[#E4CA86]/10 border-[#B351A9]/30 text-[#85257C] font-semibold'
                        : 'bg-gray-100 border-gray-300 text-gray-500'
                        }`}>
                        {form.priorityLevel && form.requestUrgency && form.businessImpact ? (
                          <div className="flex items-center justify-between">
                            <span>{form.priorityLevel}</span>
                            <CheckCircle2 className="w-5 h-5 text-[#CDA352]" />
                          </div>
                        ) : (
                          <span>Select Urgency & Impact first</span>
                        )}
                      </div>
                    </div>
                    {form.priorityLevel && form.requestUrgency && form.businessImpact && (
                      <p className="text-xs text-gray-600 mt-1">
                        Priority automatically set to <strong className="text-[#B351A9]">{form.priorityLevel}</strong> based on {form.requestUrgency} Urgency and {form.businessImpact} Impact
                      </p>
                    )}
                    {errors.priorityLevel && <div className="text-xs text-red-500 mt-2">⚠ {errors.priorityLevel}</div>}
                  </div>
                </>
              )}
            </div>
          </motion.div>
        );

        case SCREENS.METADATA:
  return (
    <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} className="space-y-6">
      <div className="text-center mb-8">
        <h3 className="text-2xl font-bold text-[#B351A9]">Additional Metadata</h3>
        <p className="text-gray-600 mt-2">Provide strategic and financial details</p>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {[
          { label: 'Estimated Cost (ETB)', key: 'estimatedCost', type: 'number', placeholder: 'Rough cost estimate in ETB' },
          { label: 'Estimated Benefit (ETB)', key: 'estimatedBenefit', type: 'number', placeholder: 'Business value or ROI' },
          { label: 'Benefit Capture Duration (months)', key: 'benefitCaptureDuration', type: 'number', placeholder: 'Duration in months' },
          { label: 'Requested Delivery Date', key: 'requestedDeliveryDate', type: 'date' },
          { label: 'Risk Level', key: 'riskLevel', type: 'select', options: configurations.RiskLevels || configurations.ImpactUrgencies },
          { label: 'Complexity Level', key: 'complexityLevel', type: 'select', options: configurations.ComplexityLevels || configurations.ImpactUrgencies },
        ].map(({ label, key, type, placeholder, options }) => (
          <div key={key}>
            <label className={labelClass}>{label}</label>
            {type === 'select' ? (
              <select
                value={(form as any)[key] || ''}
                onChange={(e) => onChange(key as keyof RequestModel, e.target.value)}
                className={`${inputClass} appearance-none bg-white`}
              >
                <option value="">Select {label}</option>
                {options?.map(option => (
                  <option key={option.id} value={option.name}>{option.name}</option>
                ))}
              </select>
            ) : (
              <input
                type={type}
                value={(form as any)[key] || ''}
                onChange={(e) => onChange(key as keyof RequestModel, type === 'number' ? parseFloat(e.target.value) || undefined : e.target.value)}
                className={inputClass}
                placeholder={placeholder}
              />
            )}
          </div>
        ))}
        
        {/* Strategic Alignment - Span full width when "Other" is selected */}
        <div className={alignmentSelectOption === 'Other' ? 'md:col-span-2' : ''}>
          <label className={labelClass}>Strategic Alignment *</label>
          <div className="flex gap-3 items-start">
            <select
              value={alignmentSelectOption}
              onChange={(e) => {
                const v = e.target.value;
                setAlignmentSelectOption(v);
                if (v === 'Other') {
                  onChange('strategicAlignment', '');
                } else {
                  onChange('strategicAlignment', v);
                }
              }}
              className={`${inputClass} appearance-none bg-white ${alignmentSelectOption === 'Other' ? 'md:w-1/2' : 'w-full'}`}
            >
              <option value="">Select alignment</option>
              <option value="Strategic Pillars">Strategic Pillars</option>
              <option value="Corporate Objective">Corporate Objective</option>
              <option value="Sector">Sector</option>
              <option value="Division">Division</option>
              <option value="Other">Other</option>
            </select>
            {(alignmentSelectOption === 'Other') && (
              <input
                placeholder="Specify alignment"
                value={form.strategicAlignment || ''}
                onChange={(e) => onChange('strategicAlignment', e.target.value)}
                className={`${inputClass} ${alignmentSelectOption === 'Other' ? 'md:w-1/2' : ''}`}
              />
            )}
          </div>
          {errors.strategicAlignment && <div className="text-xs text-red-500 mt-2">⚠ {errors.strategicAlignment}</div>}
        </div>
      </div>
    </motion.div>
  );

  

    case SCREENS.REQUESTOR_DETAILS:
        return (
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} className="space-y-6">
            <div className="text-center mb-8">
              <h3 className="text-2xl font-bold text-[#B351A9]">Requestor Details</h3>
              <p className="text-gray-600 mt-2">Provide your contact and organizational information</p>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {(() => {
                const lockedKeys = new Set<string>([
                  'requestedBy',
                  'requestedByName',
                  'primaryContactEmail',
                  'primaryContactPhone',
                  'businessDepartment',
                  'organizationName',
                ]);
                return (
                  [
                    { 
                      label: 'Requested By (Employee ID) *', 
                      key: 'requestedBy', 
                      placeholder: 'e.g., EMP00123', 
                      error: errors.requestedBy 
                    },
                    { label: 'Requested By Name *', key: 'requestedByName', placeholder: 'e.g., Anwar Indris', error: errors.requestedByName },
                    { label: 'Business Sector *', key: 'businessSector', error: errors.businessSector },
                    { label: 'Business Division *', key: 'businessDivision', error: errors.businessDivision },
                    { label: 'Business Department *', key: 'businessDepartment', error: errors.businessDepartment },
                    { label: 'Organization Name *', key: 'organizationName' },
                    { label: 'Primary Contact Email *', key: 'primaryContactEmail', type: 'email', placeholder: 'e.g., anwarindris@cbe.com.et', error: errors.primaryContactEmail },
                    { label: 'Secondary Contact Email', key: 'secondaryContactEmail', type: 'email', error: errors.secondaryContactEmail },
                    { label: 'Primary Contact Phone *', key: 'primaryContactPhone', type: 'tel', placeholder: 'e.g., +251-911-098765', error: errors.primaryContactPhone },
                    { label: 'Secondary Contact Phone', key: 'secondaryContactPhone', type: 'tel', error: errors.secondaryContactPhone },
                  ].map(({ label, key, type, placeholder, error }) => {
                    const locked = lockedKeys.has(key as string) && Boolean((form as any)[key]);
                    return (
                      <div key={key}>
                        <label className={labelClass}>{label}</label>
                        <input
                          type={type || 'text'}
                          value={
                            (key === 'requestedBy' && locked)
                              ? (requestedByDisplay || (form as any)[key] || '')
                              : ((form as any)[key] || '')
                          }
                          onChange={(e) => onChange(key as keyof RequestModel, e.target.value)}
                          className={`${inputClass} ${locked ? 'bg-gray-100 text-gray-500 cursor-not-allowed' : ''}`}
                          placeholder={placeholder}
                          disabled={locked}
                          readOnly={locked}
                        />
                        {error && <div className="text-xs text-red-500 mt-2">⚠ {error}</div>}
                      </div>
                    );
                  })
                );
              })()}
            </div>
          </motion.div>
        );

      case SCREENS.ATTACHMENTS:
        return (
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} className="space-y-6">
            <div className="text-center mb-8">
              <h3 className="text-2xl font-bold text-[#B351A9]">Attachments & Documents</h3>
              <p className="text-gray-600 mt-2">Upload supporting documents for your request</p>
            </div>
            <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 rounded-lg p-6 border border-[#CDA352]/20">
              <p className="text-sm text-gray-700 mb-4">
                Upload supporting documents such as Business Case, Cost Estimates, Business Requirement Document,
                Business Feasibility Report, Technical Feasibility Report, UAT Test Report, INSA Security Certificate,
                Security Clearance, Request Memo, Resource Assignment Memo.
              </p>
              <AttachmentUploader
                darkMode={darkMode}
                attachmentList={attachmentList}
                setAttachmentList={setAttachmentList}
                uploadedBy={form.requestedBy || 'system'}
              />
            </div>
          </motion.div>
        );

      case SCREENS.REVIEW:
        return (
          <motion.div initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} className="space-y-6">
            <div className="text-center mb-8">
              <h3 className="text-2xl font-bold text-[#B351A9]">Review & Submit</h3>
              <p className="text-gray-600 mt-2">Please verify all information before submission</p>
            </div>
            <div className="space-y-6">
              <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 rounded-lg p-6 border border-[#CDA352]/20">
                <h4 className="font-semibold text-[#85257C] mb-4 pb-3 border-b border-[#CDA352]/30">Core Information</h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
                  <div className="py-2">
                    <span className="text-sm font-medium text-gray-600 block mb-1">Request ID</span>
                    <span className="text-gray-900 font-mono text-base">{form.requestId}</span>
                  </div>
                  <div className="py-2">
                    <span className="text-sm font-medium text-gray-600 block mb-1">Reference No</span>
                    <span className="text-gray-900">{form.referenceNo || 'Not provided'}</span>
                  </div>
                  <div className="md:col-span-2 py-2">
                    <span className="text-sm font-medium text-gray-600 block mb-1">Request Title</span>
                    <span className="text-gray-900 text-base">{form.title}</span>
                  </div>
                  <div className="md:col-span-2 py-2">
                    <span className="text-sm font-medium text-gray-600 block mb-1">Description</span>
                    <p className="text-gray-900 text-base whitespace-pre-wrap">{form.description || 'Not provided'}</p>
                  </div>
                </div>
              </div>

              <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 rounded-lg p-6 border border-[#CDA352]/20">
                <h4 className="font-semibold text-[#85257C] mb-4 pb-3 border-b border-[#CDA352]/30">Classification & Categorization</h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
                  {[
                    { label: 'Request Type', value: form.requestType },
                    { label: 'Request Category', value: form.requestCategory },
                    { label: 'Service Category', value: form.serviceCategory },
                    { label: 'Product Category', value: form.productCategory },
                    { label: 'Business Impact', value: form.businessImpact },
                    { label: 'Request Urgency', value: form.requestUrgency },
                  ].map(({ label, value }) => (
                    <div key={label} className="py-2">
                      <span className="text-sm font-medium text-gray-600 block mb-1">{label}</span>
                      <span className="text-gray-900">{value || 'Not provided'}</span>
                    </div>
                  ))}
                  <div className="py-2">
                    <span className="text-sm font-medium text-gray-600 block mb-1">Priority Level</span>
                    <span className="text-[#B351A9] font-semibold">{form.priorityLevel || 'Not provided'}</span>
                    {form.priorityLevel && form.requestUrgency && form.businessImpact && (
                      <span className="text-xs text-gray-500 block mt-1">
                        (Auto-calculated from {form.requestUrgency} Urgency and {form.businessImpact} Impact)
                      </span>
                    )}
                  </div>
                </div>
              </div>

              <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 rounded-lg p-6 border border-[#CDA352]/20">
                <h4 className="font-semibold text-[#85257C] mb-4 pb-3 border-b border-[#CDA352]/30">Additional Metadata</h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
                  {[
                    { label: 'Requested Delivery Date', value: form.requestedDeliveryDate ? new Date(form.requestedDeliveryDate).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' }) : null },
                    { label: 'Estimated Cost (ETB)', value: form.estimatedCost ? new Intl.NumberFormat('en-ET', { style: 'currency', currency: 'ETB' }).format(form.estimatedCost) : null },
                    { label: 'Estimated Benefit (ETB)', value: form.estimatedBenefit ? new Intl.NumberFormat('en-ET', { style: 'currency', currency: 'ETB' }).format(form.estimatedBenefit) : null },
                    { label: 'Benefit Capture Duration', value: form.benefitCaptureDuration ? `${form.benefitCaptureDuration} month${form.benefitCaptureDuration !== 1 ? 's' : ''}` : null },
                    { label: 'Risk Level', value: form.riskLevel },
                    { label: 'Complexity Level', value: form.complexityLevel },
                    { label: 'Strategic Alignment', value: form.strategicAlignment },
                  ].map(({ label, value }) => (
                    <div key={label} className="py-2">
                      <span className="text-sm font-medium text-gray-600 block mb-1">{label}</span>
                      <span className="text-gray-900">{value || 'Not provided'}</span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 rounded-lg p-6 border border-[#CDA352]/20">
                <h4 className="font-semibold text-[#85257C] mb-4 pb-3 border-b border-[#CDA352]/30">Requestor Details</h4>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
                  {[
                    { label: 'Requested By (Employee ID)', value: requestedByDisplay || form.requestedBy },
                    { label: 'Requested By Name', value: form.requestedByName },
                    { label: 'Business Sector', value: form.businessSector },
                    { label: 'Business Division', value: form.businessDivision },
                    { label: 'Business Department', value: form.businessDepartment },
                    { label: 'Organization Name', value: form.organizationName },
                    { label: 'Primary Contact Email', value: form.primaryContactEmail },
                    { label: 'Secondary Contact Email', value: form.secondaryContactEmail },
                    { label: 'Primary Contact Phone', value: form.primaryContactPhone },
                    { label: 'Secondary Contact Phone', value: form.secondaryContactPhone },
                  ].map(({ label, value }) => (
                    <div key={label} className="py-2">
                      <span className="text-sm font-medium text-gray-600 block mb-1">{label}</span>
                      <span className="text-gray-900">{value || 'Not provided'}</span>
                    </div>
                  ))}
                </div>
              </div>

              <div className="bg-gradient-to-r from-[#B351A9]/5 to-[#E4CA86]/5 rounded-lg p-6 border border-[#CDA352]/20">
                <h4 className="font-semibold text-[#85257C] mb-4 pb-3 border-b border-[#CDA352]/30">Attachments & Documents</h4>
                <div className="mt-4">
                  {attachmentList.length > 0 ? (
                    <div className="space-y-2">
                      <p className="text-sm text-gray-600 mb-3">
                        <strong>{attachmentList.length}</strong> file{attachmentList.length !== 1 ? 's' : ''} attached
                      </p>
                      <div className="space-y-2">
                        {attachmentList.map((attachment, index) => (
                          <div key={index} className="flex items-center justify-between p-3 bg-gray-50 rounded border border-gray-200">
                            <div className="flex items-center flex-1 min-w-0">
                              <div className="flex-1 min-w-0">
                                <p className="text-sm font-medium text-gray-900 truncate">{attachment.fileName}</p>
                                <p className="text-xs text-gray-500">
                                  {attachment.fileType ? `Type: ${attachment.fileType}` : 'Type: Unknown'}
                                  {attachment.description ? ` • ${attachment.description}` : ''}
                                </p>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  ) : (
                    <p className="text-sm text-gray-500">No attachments provided</p>
                  )}
                </div>
              </div>

              <div className="bg-gradient-to-r from-[#B351A9]/10 to-[#E4CA86]/10 rounded-lg p-6 border border-[#CDA352]/30">
                <div className="flex items-start">
                  <Sparkles className="w-5 h-5 text-[#B351A9] mr-3 mt-0.5 flex-shrink-0" />
                  <div>
                    <h5 className="text-sm font-semibold text-[#85257C] mb-1">Ready to Submit</h5>
                    <p className="text-sm text-gray-700">
                      Please review all information above before submitting. Once submitted, your request will be processed
                      and you will receive a confirmation email. You can track the status of your request using the Request ID:
                      <strong className="font-mono ml-1 text-[#B351A9]">{form.requestId}</strong>
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </motion.div>
        );

      default:
        return null;
    }
  };

  const getScreenTitle = (screen: string) => {
    const titles = {
      [SCREENS.SUMMARY]: 'Summary',
      [SCREENS.CLASSIFICATION]: 'Classification',
      [SCREENS.METADATA]: 'Metadata',
      [SCREENS.REQUESTOR_DETAILS]: 'Requestor',
      [SCREENS.ATTACHMENTS]: 'Attachments',
      [SCREENS.REVIEW]: 'Review'
    };
    return titles[screen] || 'Request Form';
  };

  const getScreenIcon = (screen: string) => {
    const icons = {
      [SCREENS.SUMMARY]: '1',
      [SCREENS.CLASSIFICATION]: '2',
      [SCREENS.METADATA]: '3',
      [SCREENS.REQUESTOR_DETAILS]: '4',
      [SCREENS.ATTACHMENTS]: '5',
      [SCREENS.REVIEW]: '6'
    };
    return icons[screen] || '●';
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-white via-[#E4CA86]/5 to-[#B351A9]/5 font-['Times_New_Roman',_Times,_serif]">
      <div className="bg-white border-b-4 border-[#B351A9] sticky top-0 z-30">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <button
                onClick={() => navigate(`/dashboard/${role}/requests`)}
                className="mr-4 p-2 hover:bg-[#B351A9]/10 rounded-lg transition-colors"
              >
                <ChevronLeft className="w-5 h-5 text-[#B351A9]" />
              </button>
              <div>
                <h1 className="text-2xl font-bold text-[#B351A9] flex items-center gap-2">
                  <Sparkles className="w-5 h-5 text-[#CDA352]" />
                  Idea Intake
                </h1>
                <p className="text-gray-600 text-sm ml-7">Submit a new project or idea request</p>
              </div>
            </div>
            <div className="text-right">
              <div className="text-xl font-semibold text-[#85257C]">Commercial Bank of Ethiopia</div>
              <div className="text-sm font-medium text-[#CDA352]">Digital Factory</div>
            </div>
          </div>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-4 py-2 flex justify-end">
        <button
          onClick={() => navigate(`/dashboard/${role}/requests/manage-options`)}
          className="text-sm font-semibold text-white hover:scale-105 flex items-center gap-2 bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] px-4 py-2.5 rounded-lg border border-[#CDA352] transition-all shadow-md hover:shadow-lg"
        >
          <Settings className="w-4 h-4" />
          Manage Options
        </button>
      </div>

      <div className="max-w-6xl mx-auto px-6 py-8">
        <motion.div
          key={currentScreen}
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="bg-white rounded-2xl shadow-xl border border-[#CDA352]/20 overflow-hidden"
        >
          <div className="px-8 py-8 bg-white">
            <div className="relative">
              <div className="absolute top-4 left-10 right-10 h-1 bg-gray-200 -translate-y-1/2 -z-10 rounded-full">
                <motion.div
                  className="absolute top-0 left-0 h-full bg-gradient-to-r from-[#B351A9] to-[#CDA352] rounded-full"
                  initial={{ width: '0%' }}
                  animate={{ width: `${(numberSequentiallyCompleted / (screensArray.length - 1)) * 100}%` }}
                  transition={{ duration: 0.5, ease: "easeOut" }}
                />
              </div>

              <div className="flex justify-between relative z-10">
                {screensArray.map((screen, index) => {
                  const isActive = currentScreen === screen;
                  const isCompleted = completedSteps.has(screen);
                  const screenIndex = index;
                  const isClickable = screenIndex <= firstIncompleteIndex || screenIndex <= screensArray.indexOf(currentScreen);
                  const isFutureStep = index > numberSequentiallyCompleted;

                  return (
                    <div key={screen} className="flex flex-col items-center flex-1 relative mt-3">
                      <button
                        onClick={() => isClickable && handleStepClick(screen)}
                        disabled={!isClickable}
                        className={`w-10 h-10 rounded-full flex items-center justify-center text-sm font-semibold transition-all duration-200 relative ${isClickable ? 'hover:scale-105 cursor-pointer' : 'cursor-not-allowed'
                          } ${isCompleted
                            ? 'bg-gradient-to-r from-[#CDA352] to-[#E4CA86] text-white shadow-md'
                            : isActive
                              ? 'bg-[#B351A9] text-white shadow-md ring-2 ring-[#B351A9]/30'
                              : 'bg-white border-2 border-gray-300 text-gray-400'
                          } ${isFutureStep ? 'opacity-60' : 'opacity-100'}`}
                        style={{ zIndex: 20 }}
                      >
                        {isCompleted ? (
                          <CheckCircle2 className="w-5 h-5" />
                        ) : (
                          <span className={`text-sm font-bold ${isActive ? 'text-white' : 'text-gray-500'}`}>
                            {getScreenIcon(screen)}
                          </span>
                        )}
                        {isActive && !isCompleted && (
                          <motion.div
                            initial={{ scale: 0 }}
                            animate={{ scale: 1 }}
                            className="absolute -bottom-1 w-2 h-2 bg-[#B351A9] rounded-full ring-2 ring-white"
                          />
                        )}
                      </button>
                      <button
                        onClick={() => isClickable && handleStepClick(screen)}
                        disabled={!isClickable}
                        className={`mt-3 text-xs font-medium transition-colors duration-200 px-2 py-1 rounded ${isActive ? 'text-[#B351A9] font-semibold' :
                          isCompleted ? 'text-[#CDA352]' : 'text-gray-500'
                          } ${isClickable ? 'cursor-pointer hover:text-[#B351A9]' : 'cursor-not-allowed opacity-60'}`}
                      >
                        {getScreenTitle(screen)}
                      </button>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          <div className="px-8 pb-8">
            <div className="max-w-4xl mx-auto px-4 lg:px-8">
              <form onSubmit={onSubmit}>
                {renderScreen()}

                <div className="mt-12 flex justify-between items-center pt-8 border-t border-[#CDA352]/20">
                  <button
                    type="button"
                    onClick={prevScreen}
                    disabled={currentScreen === SCREENS.SUMMARY}
                    className={`px-6 py-3 rounded-lg font-medium transition-all duration-200 flex items-center space-x-2 ${currentScreen === SCREENS.SUMMARY
                      ? 'opacity-50 cursor-not-allowed text-gray-400'
                      : 'text-[#B351A9] hover:text-[#85257C] hover:bg-[#B351A9]/10'
                      }`}
                  >
                    <ChevronLeft className="w-5 h-5" />
                    <span>Previous</span>
                  </button>

                  <div className="flex items-center space-x-4">
                    {currentScreen !== SCREENS.REVIEW ? (
                      <button
                        type="button"
                        onClick={nextScreen}
                        className="px-6 py-3 rounded-lg font-medium text-white bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] transition-all duration-200 shadow-sm hover:shadow-md flex items-center space-x-2"
                      >
                        <span>Continue</span>
                        <ChevronLeft className="w-5 h-5 rotate-180" />
                      </button>
                    ) : (
                      <button
                        type="submit"
                        disabled={submitting}
                        className={`px-6 py-3 rounded-lg font-medium text-white bg-gradient-to-r from-[#CDA352] to-[#E4CA86] hover:from-[#E4CA86] hover:to-[#CDA352] transition-all duration-200 shadow-sm hover:shadow-md flex items-center space-x-2 ${submitting ? 'opacity-50 cursor-not-allowed' : ''}`}
                      >
                        {submitting ? (
                          <>
                            <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white"></div>
                            <span>Submitting...</span>
                          </>
                        ) : (
                          <>
                            <CheckCircle2 className="w-5 h-5" />
                            <span>Submit Request</span>
                          </>
                        )}
                      </button>
                    )}
                  </div>
                </div>
              </form>

              {submitError && (
                <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg">
                  <div className="flex items-start">
                    <div className="flex-1">
                      <h3 className="text-sm font-semibold text-red-800">Submission Error</h3>
                      <p className="text-sm text-red-700 mt-1">{submitError}</p>
                    </div>
                    <button
                      onClick={() => setSubmitError(null)}
                      className="text-red-600 hover:text-red-800"
                    >
                      ✕
                    </button>
                  </div>
                </div>
              )}

              {errors._general && (
                <div className="mt-4 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                  <div className="flex items-start">
                    <div className="flex-1">
                      <p className="text-sm text-yellow-800">{errors._general}</p>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </motion.div>
      </div>

      {submitted && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#273238]/80 backdrop-blur-sm">
          <motion.div
            initial={{ scale: 0.9, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            className="w-full max-w-2xl mx-4"
          >
            <div className="bg-white rounded-2xl shadow-2xl border-2 border-[#CDA352] overflow-hidden">
              <div className="p-8">
                <div className="text-center mb-6">
                  <div className="w-16 h-16 bg-gradient-to-br from-[#CDA352] to-[#E4CA86] rounded-full flex items-center justify-center mx-auto mb-4 shadow-lg">
                    <CheckCircle2 className="w-8 h-8 text-white" />
                  </div>
                  <h2 className="text-3xl font-bold text-[#273238] mb-2">Request Submitted Successfully</h2>
                  <motion.div
                    initial={{ y: 10, opacity: 0 }}
                    animate={{ y: 0, opacity: 1 }}
                    transition={{ delay: 0.2 }}
                    className="text-gray-600"
                  >
                    <p>Thank you for your submission. Your request has been recorded and will be processed shortly.</p>
                  </motion.div>
                  <div className="mt-4 px-4 py-2 bg-gradient-to-r from-[#B351A9]/10 to-[#E4CA86]/10 rounded-lg inline-block border border-[#CDA352]">
                    <span className="text-sm text-[#85257C] font-mono font-semibold">{submitted.requestId}</span>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6 text-sm mb-8">
                  <div className="space-y-3">
                    <div><strong className="text-gray-700">Title:</strong> {submitted.title}</div>
                    <div><strong className="text-gray-700">Request Type:</strong> {submitted.requestType || '—'}</div>
                    <div><strong className="text-gray-700">Priority:</strong> {submitted.priorityLevel || '—'}</div>
                  </div>
                  <div className="space-y-3">
                    <div><strong className="text-gray-700">Requested By:</strong> {requestedByDisplay || submitted.requestedBy}</div>
                    <div><strong className="text-gray-700">Organization:</strong> {submitted.organizationName}</div>
                    <div><strong className="text-gray-700">Attachments:</strong> {(submitted.attachmentList || []).length} files</div>
                  </div>
                </div>

                <div className="flex justify-center space-x-3">
                  <button
                    className="px-6 py-3 rounded-lg font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 transition-all duration-200 border border-gray-300"
                    onClick={() => {
                      setSubmitted(null);
                      navigate(`/dashboard/${role}/requests`);
                    }}
                  >
                    View All Requests
                  </button>
                  <button
                    className="px-6 py-3 rounded-lg font-medium text-white bg-gradient-to-r from-[#B351A9] to-[#85257C] hover:from-[#85257C] hover:to-[#B351A9] transition-all duration-200 shadow-sm hover:shadow-md"
                    onClick={() => {
                      setSubmitted(null);
                      setForm({
                        requestId: generateRequestId(),
                        title: '',
                        description: '',
                        requestedBy: '',
                        requestedByName: '',
                        organizationName: defaultOrg,
                        status: 'Submitted',
                        createdDate: new Date().toISOString()
                      });
                      setAttachmentList([]);
                      setCompletedSteps(new Set());
                      setCurrentScreen(SCREENS.SUMMARY);
                    }}
                  >
                    Create Another
                  </button>
                </div>
              </div>
            </div>
          </motion.div>
        </div>
      )}
    </div>
  );
};

export default RequestForm;