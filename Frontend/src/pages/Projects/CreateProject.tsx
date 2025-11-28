import React, { useState, useEffect, useRef } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Paperclip, X, Users, Calendar, Flag, Building2, User, Mail, Phone, Upload, Plus, Crown, Shield } from "lucide-react";
import { useNotifications } from "@/context/NotificationContext";
import { useAuth } from "@/context/AuthContext";
import { Project } from "@/types/types";
import { projectService } from "@/services";

interface DepartmentManager {
  id: string;
  employeeId?: string;
  name: string;
  role: string;
  department: string;
  email: string;
  phone: string;
}

interface TeamMember {
  id: string;
  employeeId?: string;
  name: string;
  role: string;
  department: string;
  email: string;
}

interface CreateProjectProps {
  darkMode: boolean;
  open?: boolean;
  onClose?: () => void;
  onProjectCreated: (project: Project) => void;
  buttonLabel?: string;
  onTeamSelectionChange?: (selections: {
    managerId: string;
    teamLeaderId: string;
    teamMembers: TeamMember[];
    selectedManagerObject?: DepartmentManager;
    selectedTeamLeaderObject?: TeamMember;
    selectedManagerObjects?: DepartmentManager[];
    selectedTeamLeaderObjects?: TeamMember[];
  }) => void;
  onProjectDataChange?: (projectData: {
    title: string;
    description: string;
    dueDate: string;
    priority: string;
  }) => void;
  onButtonClick?: () => void;
  isMultistep?: boolean;
  initialData?: {
    title: string;
    description: string;
    dueDate: string;
    priority: string;
    department: string;
    projectOwner: string;
    projectOwnerEmail: string;
    projectOwnerPhone: string;
    files?: File[];
  };
}

const CreateProject = ({
  darkMode,
  onClose,
  onProjectCreated,
  buttonLabel = "Create Project",
  onTeamSelectionChange,
  onProjectDataChange,
  onButtonClick,
  isMultistep = false,
  initialData,
}: CreateProjectProps) => {
  const { addNotification } = useNotifications();
  const { user } = useAuth();
  const [newProject, setNewProject] = useState<{
    title: string;
    description: string;
    dueDate: string;
    priority: "High" | "Medium" | "Low" | "Critical";
    department: string;
    files: File[];
    teamMembers: TeamMember[];
    projectOwner: string;
    projectOwnerEmail: string;
    projectOwnerPhone: string;
  }>({
    title: initialData?.title || "",
    description: initialData?.description || "",
    dueDate: initialData?.dueDate || "",
    priority: (initialData?.priority as "High" | "Medium" | "Low" | "Critical") || "Medium",
    department: initialData?.department || "",
    files: initialData?.files || [],
    teamMembers: [],
    projectOwner: initialData?.projectOwner || "",
    projectOwnerEmail: initialData?.projectOwnerEmail || "",
    projectOwnerPhone: initialData?.projectOwnerPhone || "",
  });
  const [managerIds, setManagerIds] = useState<string[]>([]);
  const [teamLeaderIds, setTeamLeaderIds] = useState<string[]>([]);
  const [teamMembersAvailable, setTeamMembersAvailable] = useState<TeamMember[]>([]);
  const [departmentManagers, setDepartmentManagers] = useState<DepartmentManager[]>([]);
  const [isAddingMember, setIsAddingMember] = useState(false);
  const [newMemberId, setNewMemberId] = useState("");
  const [showSuccessMessage, setShowSuccessMessage] = useState(false);
  const [showErrorMessage, setShowErrorMessage] = useState(false);
  const [isLoadingManagers, setIsLoadingManagers] = useState<boolean>(true);
  const [managerLoadError, setManagerLoadError] = useState<string | null>(null);
  const [isLoadingTeamMembers, setIsLoadingTeamMembers] = useState<boolean>(true);
  const [teamMembersLoadError, setTeamMembersLoadError] = useState<string | null>(null);

  // Refs to prevent infinite loops in multistep mode
  const lastTeamSelectionRef = useRef<string>("");
  const lastProjectDataRef = useRef<string>("");

  // Search functionality
  const [managerSearch, setManagerSearch] = useState("");
  const [teamMemberSearch, setTeamMemberSearch] = useState("");
  const [showManagerDropdown, setShowManagerDropdown] = useState(false);
  const [showTeamMemberDropdown, setShowTeamMemberDropdown] = useState(false);

  // Filtered results
  const filteredManagers = departmentManagers.filter(
    (manager) =>
      (manager.name || "")
        .toLowerCase()
        .includes((managerSearch || "").toLowerCase()) ||
      (manager.email || "")
        .toLowerCase()
        .includes((managerSearch || "").toLowerCase()) ||
      (manager.department || "")
        .toLowerCase()
        .includes((managerSearch || "").toLowerCase())
  );

  const filteredTeamMembers = teamMembersAvailable.filter(
    (member) =>
      (member.name || "")
        .toLowerCase()
        .includes((teamMemberSearch || "").toLowerCase()) ||
      (member.role || "")
        .toLowerCase()
        .includes((teamMemberSearch || "").toLowerCase()) ||
      (member.department || "")
        .toLowerCase()
        .includes((teamMemberSearch || "").toLowerCase())
  );

  // Close dropdowns when clicking outside
  useEffect(() => {
    const handleClickOutside = () => {
      setShowManagerDropdown(false);
      setShowTeamMemberDropdown(false);
    };

    document.addEventListener("click", handleClickOutside);
    return () => document.removeEventListener("click", handleClickOutside);
  }, []);

  // Auto-populate project owner details from current user
  useEffect(() => {
    if (user) {
      const displayName = user.name || user.fullName || user.username || "";

      setNewProject((prev) => ({
        ...prev,
        projectOwner: displayName,
        projectOwnerEmail: user.email || "",
        projectOwnerPhone: user.phone || "",
      }));

      if (managerIds.length === 0 && user.id) {
        setManagerIds([user.id]);
      }
    }
  }, [user]);

  // Communicate team selections to parent component
  useEffect(() => {
    if (isMultistep && onTeamSelectionChange) {
      const selectedManagers = managerIds
        .map((id) => departmentManagers.find((m) => m.id === id))
        .filter(Boolean) as DepartmentManager[];

      const selectedTeamLeaders = teamLeaderIds
        .map(
          (id) =>
            teamMembersAvailable.find((m) => m.id === id) ||
            departmentManagers.find((m) => m.id === id)
        )
        .filter(Boolean) as TeamMember[];

      const currentSelection = JSON.stringify({
        managerIds,
        teamLeaderIds,
        teamMembers: newProject.teamMembers,
      });

      if (currentSelection !== lastTeamSelectionRef.current) {
        lastTeamSelectionRef.current = currentSelection;
        onTeamSelectionChange({
          managerId: managerIds[0] || "",
          teamLeaderId: teamLeaderIds[0] || "",
          teamMembers: newProject.teamMembers,
          selectedManagerObjects: selectedManagers,
          selectedTeamLeaderObjects: selectedTeamLeaders,
          selectedManagerObject: selectedManagers[0],
          selectedTeamLeaderObject: selectedTeamLeaders[0],
        });
      }
    }
  }, [
    managerIds,
    teamLeaderIds,
    newProject.teamMembers,
    isMultistep,
    onTeamSelectionChange,
    departmentManagers,
    teamMembersAvailable,
  ]);

  // Communicate project data changes to parent component
  useEffect(() => {
    if (isMultistep && onProjectDataChange) {
      const currentProjectData = JSON.stringify({
        title: newProject.title,
        description: newProject.description,
        dueDate: newProject.dueDate,
        priority: newProject.priority,
      });

      if (currentProjectData !== lastProjectDataRef.current) {
        lastProjectDataRef.current = currentProjectData;
        onProjectDataChange({
          title: newProject.title,
          description: newProject.description,
          dueDate: newProject.dueDate,
          priority: newProject.priority,
        });
      }
    }
  }, [
    newProject.title,
    newProject.description,
    newProject.dueDate,
    newProject.priority,
    isMultistep,
    onProjectDataChange,
  ]);

  // Load ALL users for manager selection
  useEffect(() => {
    const loadManagers = async () => {
      setIsLoadingManagers(true);
      setManagerLoadError(null);
      try {
        const { userService } = await import("@/services/userService");
        const resp = await userService.getAllUsers();

        if (resp.success && Array.isArray(resp.data)) {
          const allUsersAsManagers = resp.data.map((u: any) => ({
            id: u.id,
            employeeId: u.employeeId,
            name: `${u.firstName ?? ""} ${u.lastName ?? ""}`.trim() ||
                  u.fullName ||
                  u.name ||
                  u.username ||
                  u.userName ||
                  "Unknown",
            role: u.role || "User",
            department: u.department || "Unknown",
            email: u.email || "",
            phone: u.phoneNumber || "",
          }));

          if (user && !allUsersAsManagers.some((manager) => manager.id === user.id)) {
            allUsersAsManagers.push({
              id: user.id,
              employeeId: user.employeeId,
              name: user.name || user.username || "Current User",
              role: user.role || "User",
              department: user.department || "Unknown",
              email: user.email || "",
              phone: user.phone || "",
            });
          }

          let updatedManagers = allUsersAsManagers;
          if (!updatedManagers.length && user) {
            updatedManagers = [
              {
                id: user.id,
                employeeId: user.employeeId,
                name: user.name || user.username || "Current User",
                role: user.role || "User",
                department: user.department || "Unknown",
                email: user.email || "",
                phone: user.phone || "",
              },
            ];
          }

          setDepartmentManagers(updatedManagers);
          if (!updatedManagers.length) {
            setManagerLoadError("No users were returned from the server.");
          }
        } else {
          setDepartmentManagers([]);
          setManagerLoadError(
            resp.message ||
            "No users were returned from the server. Please ensure the backend /api/User endpoint returns data."
          );
        }
      } catch (error) {
        console.error("❌ Failed to load managers:", error);
        setDepartmentManagers([]);
        setManagerLoadError(
          (error as any)?.message ||
          "Failed to load users. Please verify the backend is running."
        );
      } finally {
        setIsLoadingManagers(false);
      }
    };

    loadManagers();
  }, [newProject.department, user]);

  // Load all users for team members dropdown
  useEffect(() => {
    const loadAllUsersForTeamMembers = async () => {
      setIsLoadingTeamMembers(true);
      setTeamMembersLoadError(null);
      try {
        const { userService } = await import("@/services/userService");
        const resp = await userService.getAllUsers();

        if (resp.success && Array.isArray(resp.data)) {
          const allUsers = resp.data.map((u: any) => ({
            id: u.id,
            employeeId: u.employeeId,
            name: `${u.firstName ?? ""} ${u.lastName ?? ""}`.trim() ||
                  u.fullName ||
                  u.name ||
                  "Unknown",
            role: u.role || "Member",
            department: u.department || "Unknown",
            email: u.email || "",
          }));

          setTeamMembersAvailable(allUsers);
          if (!allUsers.length) {
            setTeamMembersLoadError("No users were returned for team members.");
          }
        } else {
          setTeamMembersAvailable([]);
          setTeamMembersLoadError(
            resp.message ||
            "Unable to fetch users for team members. Please verify the backend endpoint."
          );
        }
      } catch (error) {
        console.error("❌ Failed to load all users for team members:", error);
        setTeamMembersAvailable([]);
        setTeamMembersLoadError(
          (error as any)?.message ||
          "Failed to load team members. Please verify the backend is running."
        );
      } finally {
        setIsLoadingTeamMembers(false);
      }
    };

    loadAllUsersForTeamMembers();
  }, []);

  const handleAddMember = () => {
    if (!newMemberId) return;

    const member = teamMembersAvailable.find((m) => m.id === newMemberId);
    if (member && !newProject.teamMembers.some((m) => m.id === member.id)) {
      setNewProject((prev) => ({
        ...prev,
        teamMembers: [...prev.teamMembers, member],
      }));
    }
    setNewMemberId("");
    setIsAddingMember(false);
  };

  const handleRemoveMember = (memberId: string) => {
    setNewProject((prev) => ({
      ...prev,
      teamMembers: prev.teamMembers.filter((member) => member.id !== memberId),
    }));
  };

  const handleNewProjectSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const projectData = {
        title: newProject.title,
        description: newProject.description,
        priority: newProject.priority as "Low" | "Medium" | "High" | "Critical",
        dueDate: new Date(newProject.dueDate).toISOString(),
        projectOwner: newProject.projectOwner || user?.name || user?.username || "Unknown",
        projectOwnerEmail: newProject.projectOwnerEmail || user?.email || "",
        projectOwnerPhone: newProject.projectOwnerPhone || user?.phone || "+1234567890",
        department: newProject.department || user?.department || "General",
        status: "Active" as const,
      };

      if (managerIds && managerIds.length > 0) {
        const primaryManager = departmentManagers.find((m) => m.id === managerIds[0]);
        (projectData as any).assignedEmployeeId = primaryManager?.employeeId || managerIds[0];
        (projectData as any).assignedRole = "ScrumMaster";
      }

      // Validation check
      const validationErrors = [];
      if (!projectData.title || projectData.title.trim() === "") {
        validationErrors.push("Title is required");
      }
      if (!projectData.dueDate || projectData.dueDate.trim() === "") {
        validationErrors.push("Due date is required");
      }

      if (validationErrors.length > 0) {
        console.error("❌ VALIDATION ERRORS FOUND:", validationErrors);
        setShowErrorMessage(true);
        setTimeout(() => setShowErrorMessage(false), 5000);
        return;
      }

      const response = await projectService.createProject(projectData);

      if (response && response.success && response.data) {
        console.log("✅ Project created successfully with ID:", response.data.id);

        // Upload files if any
        if (newProject.files.length > 0) {
          for (const file of newProject.files) {
            const formData = new FormData();
            formData.append("file", file);
            formData.append("entityType", "Project");
            formData.append("entityId", response.data.id.toString());
            formData.append("description", `Attachment for project: ${newProject.title}`);
            formData.append("tags", "[]");

            try {
              const { attachmentsService } = await import("@/services/attachmentsService");
              await attachmentsService.upload(formData);
              console.log("✅ File uploaded successfully:", file.name);
            } catch (error) {
              console.error("❌ File upload error:", file.name, error);
            }
          }
        }

        // Assign team members to the project
        if (newProject.teamMembers.length > 0 || managerIds.length > 0 || teamLeaderIds.length > 0) {
          const { projectAssignmentService } = await import("@/services/projectAssignmentService");

          // Assign Scrum Masters
          for (const managerId of managerIds) {
            try {
              const manager = departmentManagers.find((m) => m.id === managerId);
              const managerData = {
                projectId: Number(response.data.id),
                employeeId: manager?.employeeId || managerId,
                memberRole: "Scrum Master",
                role: manager?.role,
              };
              await projectAssignmentService.addMembers(managerData);
              console.log("✅ Scrum Master assigned:", manager?.name || managerId);
            } catch (error) {
              console.error("❌ Error assigning Scrum Master:", error);
            }
          }

          // Assign Team Leaders
          for (const teamLeaderId of teamLeaderIds) {
            try {
              const leader = teamMembersAvailable.find((m) => m.id === teamLeaderId) ||
                            departmentManagers.find((m) => m.id === teamLeaderId);
              const teamLeaderData = {
                projectId: Number(response.data.id),
                employeeId: leader?.employeeId || teamLeaderId,
                memberRole: "Team Leader",
                role: leader?.role,
              };
              await projectAssignmentService.addMembers(teamLeaderData);
              console.log("✅ Team Leader assigned:", leader?.name || teamLeaderId);
            } catch (error) {
              console.error("❌ Error assigning Team Leader:", error);
            }
          }

          // Assign Team Members
          for (const member of newProject.teamMembers) {
            try {
              const memberData = {
                projectId: Number(response.data.id),
                employeeId: (member as any).employeeId || member.id,
                memberRole: "Member",
              };
              await projectAssignmentService.addMembers(memberData);
              console.log("✅ Team Member assigned:", member.name);
            } catch (error) {
              console.error("❌ Error assigning Team Member:", member.name, error);
            }
          }
        }

        // Show success message
        setShowSuccessMessage(true);
        setTimeout(() => setShowSuccessMessage(false), 3000);

        // Call the onProjectCreated callback if provided
        if (onProjectCreated && response.data) {
          const convertedProject: Project = {
            id: parseInt(response.data.id) || 0,
            title: response.data.title || newProject.title,
            description: response.data.description || newProject.description,
            dueDate: response.data.dueDate || newProject.dueDate,
            createdBy: response.data.createdBy || user?.name || user?.username || "Unknown",
            teamMembers: [],
            scrumMaster: null,
            teamLeader: null,
            priority: (() => {
              const priority = response.data.priority?.toLowerCase();
              if (priority === "high" || priority === "critical") return "High";
              if (priority === "medium") return "Medium";
              if (priority === "low") return "Low";
              return "Medium";
            })(),
            status: (() => {
              const status = (response.data.status as string)?.toLowerCase();
              if (status === "active") return "Active";
              if (status === "on hold") return "On Hold";
              if (status === "completed") return "Completed";
              if (status === "archived") return "Archived";
              return "Active";
            })() as "Active" | "On Hold" | "Completed" | "Archived",
            progress: 0,
            files: [],
            tasks: [],
          };
          onProjectCreated(convertedProject);
        }

        // Reset form
        setNewProject({
          title: "",
          description: "",
          dueDate: "",
          priority: "Medium",
          department: "",
          files: [],
          teamMembers: [],
          projectOwner: "",
          projectOwnerEmail: "",
          projectOwnerPhone: "",
        });
        setManagerIds([]);
        setTeamLeaderIds([]);

        if (onClose) {
          onClose();
        }
      } else {
        console.error("❌ Project creation failed:", response);
        const serverMessage = (response && (response.message || response.raw?.message)) || "Project creation failed";
        
        addNotification({
          type: "error" as any,
          title: "Project Creation Failed",
          message: serverMessage,
          category: "PROJECT_UPDATE" as any,
          userId: user?.id || "",
        });

        setShowErrorMessage(true);
        setTimeout(() => setShowErrorMessage(false), 8000);
        throw response;
      }
    } catch (error: any) {
      console.error("❌ Catch block error:", error);
      
      let errorMessage = "Failed to create project";
      let errorDetails: string[] = [];

      if (error && typeof error === "object") {
        if (error.message) {
          errorMessage = error.message;
        }

        if (error.errors && Array.isArray(error.errors) && error.errors.length > 0) {
          errorDetails = error.errors;
        } else if (error.raw && error.raw.errors && typeof error.raw.errors === "object") {
          Object.entries(error.raw.errors).forEach(([field, messages]: [string, any]) => {
            if (Array.isArray(messages)) {
              messages.forEach((msg) => {
                errorDetails.push(`${field}: ${msg}`);
              });
            }
          });
        }

        if (errorDetails.length > 0) {
          errorMessage += "\n\nValidation Errors:\n• " + errorDetails.join("\n• ");
        }

        if (error.status) {
          errorMessage += `\n\nStatus Code: ${error.status}`;
        }
      }

      addNotification({
        type: "error" as any,
        title: "Project Creation Failed",
        message: errorMessage,
        category: "PROJECT_UPDATE" as any,
        userId: user?.id || "",
      });
    }
  };

  return (
    <div className={`min-h-screen p-6 ${
      darkMode 
        ? "bg-gradient-to-br from-slate-900 via-purple-900 to-slate-900" 
        : "bg-white"
    }`}>

      <div className="max-w-full mx-auto">
        <form onSubmit={handleNewProjectSubmit} className="space-y-6">
          {/* Top row with project info and project owner side by side */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Project Information */}
            <div className="lg:col-span-2">
              <Card className={`shadow-xl border-0 h-full ${
                darkMode 
                  ? "bg-slate-800/90 backdrop-blur-sm border-slate-700" 
                  : "bg-white/90 backdrop-blur-sm"
              }`}>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-xl">
                    <div className="p-2 bg-blue-100 dark:bg-blue-900 rounded-lg">
                      <Flag className="w-5 h-5 text-blue-600 dark:text-blue-300" />
                    </div>
                    Project Information
                  </CardTitle>
                  <CardDescription>
                    Basic details about your project
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div className="space-y-2">
                      <Label htmlFor="title" className={`flex items-center gap-2 font-medium ${
                        darkMode ? "text-gray-200" : "text-gray-700"
                      }`}>
                        Project Title <span className="text-red-500">*</span>
                      </Label>
                      <Input
                        id="title"
                        value={newProject.title}
                        onChange={(e) =>
                          setNewProject((prev) => ({ ...prev, title: e.target.value }))
                        }
                        placeholder="Enter project title"
                        className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-blue-500 ${
                          darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                        }`}
                      />
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="department" className={`flex items-center gap-2 font-medium ${
                        darkMode ? "text-gray-200" : "text-gray-700"
                      }`}>
                        <Building2 className="w-4 h-4" />
                        Department
                      </Label>
                      <Input
                        id="department"
                        value={newProject.department}
                        onChange={(e) =>
                          setNewProject((prev) => ({ ...prev, department: e.target.value }))
                        }
                        placeholder="Enter department"
                        className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-blue-500 ${
                          darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                        }`}
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label
                      htmlFor="description"
                      className={`flex items-center gap-2 font-medium ${
                        darkMode ? "text-gray-200" : "text-gray-700"
                      }`}
                    >
                      Description
                    </Label>
                    <Textarea
                      id="description"
                      value={newProject.description}
                      onChange={(e) =>
                        setNewProject((prev) => ({
                          ...prev,
                          description: e.target.value,
                        }))
                      }
                      placeholder="Describe your project goals, objectives, and key deliverables..."
                      className={`min-h-[100px] transition-all duration-200 focus:ring-2 focus:ring-blue-500 resize-none ${
                        darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                      }`}
                      rows={4}
                    />
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div className="space-y-2">
                      <Label htmlFor="dueDate" className={`flex items-center gap-2 font-medium ${
                        darkMode ? "text-gray-200" : "text-gray-700"
                      }`}>
                        <Calendar className="w-4 h-4" />
                        Due Date <span className="text-red-500">*</span>
                      </Label>
                      <Input
                        id="dueDate"
                        type="date"
                        value={newProject.dueDate}
                        onChange={(e) =>
                          setNewProject((prev) => ({ ...prev, dueDate: e.target.value }))
                        }
                        className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-blue-500 ${
                          darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                        }`}
                      />
                    </div>

                    <div className="space-y-2">
                      <Label htmlFor="priority" className={`flex items-center gap-2 font-medium ${
                        darkMode ? "text-gray-200" : "text-gray-700"
                      }`}>
                        <Flag className="w-4 h-4" />
                        Priority
                      </Label>
                      <Select
                        value={newProject.priority}
                        onValueChange={(value: "High" | "Medium" | "Low" | "Critical") =>
                          setNewProject((prev) => ({ ...prev, priority: value }))
                        }
                      >
                        <SelectTrigger className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-blue-500 ${
                          darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                        }`}>
                          <SelectValue placeholder="Select priority" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Low">
                            <div className="flex items-center gap-2">
                              <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                              Low
                            </div>
                          </SelectItem>
                          <SelectItem value="Medium">
                            <div className="flex items-center gap-2">
                              <div className="w-2 h-2 bg-yellow-500 rounded-full"></div>
                              Medium
                            </div>
                          </SelectItem>
                          <SelectItem value="High">
                            <div className="flex items-center gap-2">
                              <div className="w-2 h-2 bg-orange-500 rounded-full"></div>
                              High
                            </div>
                          </SelectItem>
                          <SelectItem value="Critical">
                            <div className="flex items-center gap-2">
                              <div className="w-2 h-2 bg-red-500 rounded-full"></div>
                              Critical
                            </div>
                          </SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Project Owner */}
            <div className="lg:col-span-1">
              <Card className={`shadow-xl border-0 h-full ${
                darkMode 
                  ? "bg-slate-800/90 backdrop-blur-sm border-slate-700" 
                  : "bg-white/90 backdrop-blur-sm"
              }`}>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-xl">
                    <div className="p-2 bg-green-100 dark:bg-green-900 rounded-lg">
                      <User className="w-5 h-5 text-green-600 dark:text-green-300" />
                    </div>
                    Project Owner
                  </CardTitle>
                  <CardDescription>
                    Contact information for the project owner
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-6">
                  <div className="space-y-2">
                    <Label
                      htmlFor="projectOwner"
                      className={`flex items-center gap-2 font-medium ${
                        darkMode ? "text-gray-200" : "text-gray-700"
                      }`}
                    >
                      <User className="w-4 h-4" />
                      Owner Name
                    </Label>
                    <Input
                      id="projectOwner"
                      value={newProject.projectOwner}
                      onChange={(e) =>
                        setNewProject((prev) => ({
                          ...prev,
                          projectOwner: e.target.value,
                        }))
                      }
                      placeholder="Enter project owner name"
                      className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-green-500 ${
                        darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                      }`}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label
                      htmlFor="projectOwnerEmail"
                      className={`flex items-center gap-2 font-medium ${
                        darkMode ? "text-gray-200" : "text-gray-700"
                      }`}
                    >
                      <Mail className="w-4 h-4" />
                      Email Address
                    </Label>
                    <Input
                      id="projectOwnerEmail"
                      type="email"
                      value={newProject.projectOwnerEmail}
                      onChange={(e) =>
                        setNewProject((prev) => ({
                          ...prev,
                          projectOwnerEmail: e.target.value,
                        }))
                      }
                      placeholder="owner@company.com"
                      className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-green-500 ${
                        darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                      }`}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label
                      htmlFor="projectOwnerPhone"
                      className={`flex items-center gap-2 font-medium ${
                        darkMode ? "text-gray-200" : "text-gray-700"
                      }`}
                    >
                      <Phone className="w-4 h-4" />
                      Phone Number
                    </Label>
                    <Input
                      id="projectOwnerPhone"
                      type="tel"
                      value={newProject.projectOwnerPhone}
                      onChange={(e) =>
                        setNewProject((prev) => ({
                          ...prev,
                          projectOwnerPhone: e.target.value,
                        }))
                      }
                      placeholder="+1 (555) 123-4567"
                      className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-green-500 ${
                        darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                      }`}
                    />
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>

          {/* Attachments and Team Assignment Cards - Side by side */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* File Attachments Card - Takes 1/3 */}
            <div className="lg:col-span-1">
              <Card className={`shadow-xl border-0 h-full ${
                darkMode 
                  ? "bg-slate-800/90 backdrop-blur-sm border-slate-700" 
                  : "bg-white/90 backdrop-blur-sm"
              }`}>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-xl">
                    <div className="p-2 bg-purple-100 dark:bg-purple-900 rounded-lg">
                      <Paperclip className="w-5 h-5 text-purple-600 dark:text-purple-300" />
                    </div>
                    Attachments
                  </CardTitle>
                  <CardDescription>
                    Upload relevant files and documents
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    <div className="border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg p-4 text-center">
                      <div className="flex flex-col items-center">
                        <Paperclip className="w-8 h-8 text-gray-400 mb-2" />
                        <p className={`text-sm ${darkMode ? "text-gray-300" : "text-gray-600"}`}>
                          Click to select files
                        </p>
                        <Input
                          type="file"
                          multiple
                          onChange={handleFileChange}
                          className="hidden"
                          id="file-upload"
                        />
                        <label
                          htmlFor="file-upload"
                          className={`mt-3 px-4 py-2 rounded-lg cursor-pointer transition-colors ${
                            darkMode
                              ? "bg-purple-600 hover:bg-purple-700 text-white"
                              : "bg-purple-100 hover:bg-purple-200 text-purple-700"
                          }`}
                        >
                          <Upload className="w-4 h-4 mr-2" />
                          Browse Files
                        </label>
                      </div>
                    </div>
                    
                    {newProject.files.length > 0 && (
                      <div className="space-y-2">
                        <h4 className={`text-sm font-medium mb-2 ${
                          darkMode ? "text-gray-200" : "text-gray-700"
                        }`}>
                          Selected Files ({newProject.files.length})
                        </h4>
                        <div className="space-y-2 max-h-40 overflow-y-auto">
                          {newProject.files.map((file, index) => (
                            <div
                              key={index}
                              className={`flex items-center justify-between p-2 rounded-lg border ${
                                darkMode ? "bg-gray-700 border-gray-600" : "bg-gray-50 border-gray-200"
                              }`}
                            >
                              <div className="flex items-center gap-2">
                                <Paperclip className="w-4 h-4 text-purple-500" />
                                <span className={`text-sm truncate ${
                                  darkMode ? "text-gray-300" : "text-gray-700"
                                }`}>
                                  {file.name}
                                </span>
                              </div>
                              <button
                                type="button"
                                onClick={() => {
                                  setNewProject((prev) => ({
                                    ...prev,
                                    files: prev.files.filter((_, i) => i !== index),
                                  }));
                                }}
                                className="p-1 rounded text-red-500 hover:bg-red-50"
                              >
                                <X className="w-3 h-3" />
                              </button>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Team Assignment Card - Takes 2/3 */}
            <div className="lg:col-span-2">
          <Card className={`shadow-xl border-0 ${
            darkMode 
              ? "bg-slate-800/90 backdrop-blur-sm border-slate-700" 
              : "bg-white/90 backdrop-blur-sm"
          }`}>
            <CardHeader>
              <CardTitle className="flex items-center gap-2 text-xl">
                <div className="p-2 bg-orange-100 dark:bg-orange-900 rounded-lg">
                  <Users className="w-5 h-5 text-orange-600 dark:text-orange-300" />
                </div>
                Team Assignment
              </CardTitle>
              <CardDescription>
                Assign Scrum Masters, Team Leaders, and Team Members
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-8">
              {/* Scrum Master Selection */}
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <Label className={`font-medium flex items-center gap-2 ${
                    darkMode ? "text-gray-200" : "text-gray-700"
                  }`}>
                    <Crown className="w-4 h-4 text-yellow-500" />
                    Scrum Master(s)
                  </Label>
                  {managerIds.length > 0 && (
                    <Badge variant="secondary" className="text-xs">
                      {managerIds.length} selected
                    </Badge>
                  )}
                </div>

                {/* Display Selected Scrum Masters */}
                {managerIds.length > 0 && (
                  <div className="space-y-2">
                    {managerIds.map((id) => {
                      let manager = departmentManagers.find((m) => m.id === id);
                      if (!manager && user && user.id === id) {
                        manager = {
                          id: user.id,
                          employeeId: user.employeeId,
                          name: user.name || user.fullName || user.username || "You",
                          role: user.role || "User",
                          department: user.department || "",
                          email: user.email || "",
                          phone: user.phone || "",
                        } as any;
                      }

                      if (!manager) return null;

                      return (
                        <div
                          key={id}
                          className={`flex items-center justify-between p-3 rounded-lg border transition-all duration-200 hover:shadow-md ${
                            darkMode 
                              ? "bg-slate-700 border-slate-600 hover:border-slate-500" 
                              : "bg-yellow-50 border-yellow-200 hover:border-yellow-300"
                          }`}
                        >
                          <div className="flex items-center gap-3">
                            <div className="w-8 h-8 bg-gradient-to-r from-yellow-400 to-orange-500 rounded-full flex items-center justify-center text-white text-sm font-medium">
                              {(manager.name || "U").charAt(0).toUpperCase()}
                            </div>
                            <div>
                              <p className={`text-sm font-medium ${
                                darkMode ? "text-gray-200" : "text-gray-700"
                              }`}>
                                {manager.name}
                              </p>
                              <p className={`text-xs ${
                                darkMode ? "text-gray-400" : "text-gray-500"
                              }`}>
                                {manager.department} • {manager.email}
                              </p>
                            </div>
                          </div>
                          <button
                            type="button"
                            onClick={() =>
                              setManagerIds((prev) => prev.filter((i) => i !== id))
                            }
                            className="p-2 rounded-lg text-red-500 hover:bg-red-50 hover:text-red-700 transition-all duration-200"
                          >
                            <X className="w-4 h-4" />
                          </button>
                        </div>
                      );
                    })}
                  </div>
                )}

                {/* Add Scrum Master */}
                <div className="relative">
                  <Input
                    value={managerSearch}
                    onChange={(e) => {
                      setManagerSearch(e.target.value);
                      setShowManagerDropdown(true);
                    }}
                    onFocus={() => setShowManagerDropdown(true)}
                    placeholder={
                      managerIds.length > 0
                        ? "Add another Scrum Master..."
                        : "Search for a Scrum Master..."
                    }
                    className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-yellow-500 ${
                      darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                    }`}
                  />
                  {showManagerDropdown && (
                    <div
                      className={`absolute z-10 w-full mt-1 max-h-60 overflow-auto rounded-lg shadow-xl border ${
                        darkMode
                          ? "bg-slate-700 border-slate-600"
                          : "bg-white border-gray-200"
                      }`}
                    >
                      {filteredManagers.filter((m) => !managerIds.includes(m.id))
                        .length > 0 ? (
                        filteredManagers
                          .filter((m) => !managerIds.includes(m.id))
                          .map((manager) => (
                            <div
                              key={manager.id}
                              onClick={() => {
                                setManagerIds((prev) => [...prev, manager.id]);
                                setManagerSearch("");
                                setShowManagerDropdown(false);
                              }}
                              className={`p-3 cursor-pointer transition-all duration-200 hover:bg-opacity-80 border-b last:border-b-0 ${
                                darkMode 
                                  ? "hover:bg-slate-600 border-slate-600" 
                                  : "hover:bg-gray-50 border-gray-100"
                              }`}
                            >
                              <div className="flex items-center space-x-3">
                                <div className="flex-shrink-0">
                                  <div className="w-8 h-8 bg-gradient-to-r from-yellow-400 to-orange-500 rounded-full flex items-center justify-center text-white text-sm font-medium">
                                    {(manager.name || "U").charAt(0).toUpperCase()}
                                  </div>
                                </div>
                                <div className="flex-1 min-w-0">
                                  <p className={`text-sm font-medium truncate ${
                                    darkMode ? "text-white" : "text-gray-900"
                                  }`}>
                                    {manager.name}
                                  </p>
                                  <p className={`text-sm truncate ${
                                    darkMode ? "text-gray-400" : "text-gray-500"
                                  }`}>
                                    {manager.email}
                                  </p>
                                </div>
                                <div className={`text-sm ${
                                  darkMode ? "text-gray-400" : "text-gray-500"
                                }`}>
                                  🏢 {manager.department}
                                </div>
                              </div>
                            </div>
                          ))
                      ) : (
                        <div className={`p-3 text-sm ${
                          darkMode ? "text-gray-400" : "text-gray-500"
                        }`}>
                          {managerSearch
                            ? "No managers found"
                            : "All managers already selected"}
                        </div>
                      )}
                    </div>
                  )}
                </div>
                {isLoadingManagers && (
                  <div className={`text-sm ${
                    darkMode ? "text-gray-400" : "text-gray-500"
                  }`}>
                    Loading available users…
                  </div>
                )}
                {!isLoadingManagers && departmentManagers.length === 0 && (
                  <div className="text-sm text-yellow-600">
                    {managerLoadError ??
                      "No users available. Please ensure the backend returns user data."}
                  </div>
                )}
              </div>

              {/* Team Leader Selection */}
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <Label className={`font-medium flex items-center gap-2 ${
                    darkMode ? "text-gray-200" : "text-gray-700"
                  }`}>
                    <Shield className="w-4 h-4 text-blue-500" />
                    Team Leader(s)
                  </Label>
                  {teamLeaderIds.length > 0 && (
                    <Badge variant="secondary" className="text-xs">
                      {teamLeaderIds.length} selected
                    </Badge>
                  )}
                </div>

                {/* Display Selected Team Leaders */}
                {teamLeaderIds.length > 0 && (
                  <div className="space-y-2">
                    {teamLeaderIds.map((id) => {
                      let leader =
                        teamMembersAvailable.find((m) => m.id === id) ||
                        departmentManagers.find((m) => m.id === id);
                      if (!leader && user && user.id === id) {
                        leader = {
                          id: user.id,
                          employeeId: user.employeeId,
                          name: user.name || user.fullName || user.username || "You",
                          role: user.role || "User",
                          department: user.department || "",
                          email: user.email || "",
                        } as any;
                      }
                      if (!leader) return null;
                      return (
                        <div
                          key={id}
                          className={`flex items-center justify-between p-3 rounded-lg border transition-all duration-200 hover:shadow-md ${
                            darkMode 
                              ? "bg-slate-700 border-slate-600 hover:border-slate-500" 
                              : "bg-blue-50 border-blue-200 hover:border-blue-300"
                          }`}
                        >
                          <div className="flex items-center gap-3">
                            <div className="w-8 h-8 bg-gradient-to-r from-blue-400 to-cyan-500 rounded-full flex items-center justify-center text-white text-sm font-medium">
                              {(leader.name || "U").charAt(0).toUpperCase()}
                            </div>
                            <div>
                              <p className={`text-sm font-medium ${
                                darkMode ? "text-gray-200" : "text-gray-700"
                              }`}>
                                {leader.name}
                              </p>
                              <p className={`text-xs ${
                                darkMode ? "text-gray-400" : "text-gray-500"
                              }`}>
                                {leader.department} • {leader.email || leader.role}
                              </p>
                            </div>
                          </div>
                          <button
                            type="button"
                            onClick={() =>
                              setTeamLeaderIds((prev) => prev.filter((i) => i !== id))
                            }
                            className="p-2 rounded-lg text-red-500 hover:bg-red-50 hover:text-red-700 transition-all duration-200"
                          >
                            <X className="w-4 h-4" />
                          </button>
                        </div>
                      );
                    })}
                  </div>
                )}

                {/* Add Team Leader */}
                <div className="relative">
                  <Input
                    value={teamMemberSearch}
                    onChange={(e) => {
                      setTeamMemberSearch(e.target.value);
                      setShowTeamMemberDropdown(true);
                    }}
                    onFocus={() => setShowTeamMemberDropdown(true)}
                    placeholder={
                      teamLeaderIds.length > 0
                        ? "Add another Team Leader..."
                        : "Search for a Team Leader..."
                    }
                    className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-blue-500 ${
                      darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                    }`}
                  />
                  {showTeamMemberDropdown && (
                    <div
                      className={`absolute z-10 w-full mt-1 max-h-60 overflow-auto rounded-lg shadow-xl border ${
                        darkMode
                          ? "bg-slate-700 border-slate-600"
                          : "bg-white border-gray-200"
                      }`}
                    >
                      {filteredTeamMembers.filter(
                        (m) => !teamLeaderIds.includes(m.id)
                      ).length > 0 ? (
                        filteredTeamMembers
                          .filter((m) => !teamLeaderIds.includes(m.id))
                          .map((member) => (
                            <div
                              key={member.id}
                              onClick={() => {
                                setTeamLeaderIds((prev) => [...prev, member.id]);
                                setTeamMemberSearch("");
                                setShowTeamMemberDropdown(false);
                              }}
                              className={`p-3 cursor-pointer transition-all duration-200 hover:bg-opacity-80 border-b last:border-b-0 ${
                                darkMode 
                                  ? "hover:bg-slate-600 border-slate-600" 
                                  : "hover:bg-gray-50 border-gray-100"
                              }`}
                            >
                              <div className="flex items-center space-x-3">
                                <div className="flex-shrink-0">
                                  <div className="w-8 h-8 bg-gradient-to-r from-blue-400 to-cyan-500 rounded-full flex items-center justify-center text-white text-sm font-medium">
                                    {(member.name || "U").charAt(0).toUpperCase()}
                                  </div>
                                </div>
                                <div className="flex-1 min-w-0">
                                  <p className={`text-sm font-medium truncate ${
                                    darkMode ? "text-white" : "text-gray-900"
                                  }`}>
                                    {member.name}
                                  </p>
                                  <p className={`text-sm truncate ${
                                    darkMode ? "text-gray-400" : "text-gray-500"
                                  }`}>
                                    {member.email || member.role}
                                  </p>
                                </div>
                                <div className={`text-sm ${
                                  darkMode ? "text-gray-400" : "text-gray-500"
                                }`}>
                                  🏢 {member.department}
                                </div>
                              </div>
                            </div>
                          ))
                      ) : (
                        <div className={`p-3 text-sm ${
                          darkMode ? "text-gray-400" : "text-gray-500"
                        }`}>
                          {teamMemberSearch
                            ? "No team members found"
                            : "All members already selected"}
                        </div>
                      )}
                    </div>
                  )}
                </div>
                {isLoadingTeamMembers && (
                  <div className={`text-sm ${
                    darkMode ? "text-gray-400" : "text-gray-500"
                  }`}>
                    Loading team members…
                  </div>
                )}
                {!isLoadingTeamMembers && teamMembersAvailable.length === 0 && (
                  <div className="text-sm text-yellow-600">
                    {teamMembersLoadError ??
                      "No users available to add as team members. Please verify the backend users endpoint."}
                  </div>
                )}
              </div>

              {/* Team Members Section */}
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <Label className={`font-medium flex items-center gap-2 ${
                    darkMode ? "text-gray-200" : "text-gray-700"
                  }`}>
                    <Users className="w-4 h-4 text-green-500" />
                    Team Members
                  </Label>
                  {newProject.teamMembers.length > 0 && (
                    <Badge variant="secondary" className="text-xs">
                      {newProject.teamMembers.length} selected
                    </Badge>
                  )}
                </div>

                <div className="space-y-2">
                  {newProject.teamMembers.map((member) => (
                    <div
                      key={member.id}
                      className={`flex items-center justify-between p-3 rounded-lg border transition-all duration-200 hover:shadow-md ${
                        darkMode 
                          ? "bg-slate-700 border-slate-600 hover:border-slate-500" 
                          : "bg-green-50 border-green-200 hover:border-green-300"
                      }`}
                    >
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 bg-gradient-to-r from-green-400 to-emerald-500 rounded-full flex items-center justify-center text-white text-sm font-medium">
                          {(member.name || "U").charAt(0).toUpperCase()}
                        </div>
                        <div>
                          <p className={`text-sm font-medium ${
                            darkMode ? "text-gray-200" : "text-gray-700"
                          }`}>
                            {member.name}
                          </p>
                          <p className={`text-xs ${
                            darkMode ? "text-gray-400" : "text-gray-500"
                          }`}>
                            {member.role} • {member.department}
                          </p>
                        </div>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleRemoveMember(member.id)}
                        className="p-2 rounded-lg text-red-500 hover:bg-red-50 hover:text-red-700 transition-all duration-200"
                      >
                        <X className="w-4 h-4" />
                      </Button>
                    </div>
                  ))}
                  
                  {!isAddingMember ? (
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => setIsAddingMember(true)}
                      className={`w-full h-11 transition-all duration-200 hover:scale-105 border-dashed ${
                        darkMode 
                          ? "border-slate-600 text-slate-300 hover:bg-slate-700 hover:border-slate-500" 
                          : "border-green-300 text-green-700 hover:bg-green-50 hover:border-green-400"
                      }`}
                    >
                      <Plus className="w-4 h-4 mr-2" />
                      Add Team Member
                    </Button>
                  ) : (
                    <div className="flex space-x-2">
                      <Select value={newMemberId} onValueChange={setNewMemberId}>
                        <SelectTrigger className={`flex-1 h-11 transition-all duration-200 focus:ring-2 focus:ring-green-500 ${
                          darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                        }`}>
                          <SelectValue placeholder="Select team member" />
                        </SelectTrigger>
                        <SelectContent>
                          {teamMembersAvailable.map((m) => (
                            <SelectItem
                              key={m.id}
                              value={m.id}
                              className={darkMode ? "hover:bg-slate-600" : ""}
                            >
                              <div className="flex items-center gap-2">
                                <div className="w-6 h-6 bg-gradient-to-r from-green-400 to-emerald-500 rounded-full flex items-center justify-center text-white text-xs font-medium">
                                  {(m.name || "U").charAt(0).toUpperCase()}
                                </div>
                                <div>
                                  <div className="font-medium">{m.name}</div>
                                  <div className="text-xs opacity-70">{m.role} • {m.department}</div>
                                </div>
                              </div>
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <Button
                        type="button"
                        onClick={handleAddMember}
                        disabled={!newMemberId}
                        className="bg-green-600 hover:bg-green-700 text-white transition-all duration-200 hover:scale-105"
                      >
                        <Plus className="w-4 h-4 mr-1" />
                        Add
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => {
                          setIsAddingMember(false);
                          setNewMemberId("");
                        }}
                        className={`transition-all duration-200 ${
                          darkMode 
                            ? "border-slate-600 text-slate-300 hover:bg-slate-700" 
                            : "border-gray-300 hover:bg-gray-50"
                        }`}
                      >
                        Cancel
                      </Button>
                    </div>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
          </div>
          </div>

          {/* Action Buttons */}
          <div className="flex justify-end gap-3 pt-6">
            {onClose && (
              <Button
                variant="outline"
                onClick={onClose}
                className={`h-11 px-6 transition-all duration-200 hover:scale-105 ${
                  darkMode
                    ? "border-slate-600 text-slate-300 hover:bg-slate-700 hover:border-slate-500"
                    : "border-gray-300 hover:bg-gray-50"
                }`}
              >
                Cancel
              </Button>
            )}
            <Button
              type={isMultistep ? "button" : "submit"}
              className={`h-11 px-6 transition-all duration-200 hover:scale-105 shadow-lg hover:shadow-xl ${
                darkMode
                  ? "bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white"
                  : "bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white"
              }`}
              disabled={!newProject.title || !newProject.dueDate}
              onClick={() => {
                if (isMultistep && onButtonClick) {
                  onButtonClick();
                  return;
                }
              }}
            >
              <div className="flex items-center gap-2">
                <Plus className="w-4 h-4" />
                {buttonLabel}
              </div>
            </Button>
          </div>
        </form>

        {/* Success Message */}
        {showSuccessMessage && (
          <div className="mt-6 p-4 bg-green-100 dark:bg-green-900/30 border border-green-200 dark:border-green-800 rounded-lg shadow-lg animate-in slide-in-from-bottom-2">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 bg-green-500 rounded-full flex items-center justify-center">
                <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <div>
                <p className="font-medium text-green-800 dark:text-green-200">
                  Project created successfully!
                </p>
                <p className="text-sm text-green-600 dark:text-green-400">
                  Your project is now ready for team collaboration.
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Error Message */}
        {showErrorMessage && (
          <div className="mt-6 p-4 bg-red-100 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg shadow-lg animate-in slide-in-from-bottom-2">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 bg-red-500 rounded-full flex items-center justify-center">
                <X className="w-5 h-5 text-white" />
              </div>
              <div>
                <p className="font-medium text-red-800 dark:text-red-200">
                  Failed to create project
                </p>
                <p className="text-sm text-red-600 dark:text-red-400">
                  Please check your information and try again.
                </p>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default CreateProject;