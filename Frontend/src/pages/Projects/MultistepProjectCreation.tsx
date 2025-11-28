import { useEffect, useState, useCallback } from "react";
import { ArrowLeft, Crown, Plus, Flag, Calendar, Users, Target, CheckCircle, AlertCircle } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
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

import { Project, Employee, Milestone, Task } from "@/types/types";
import CreateProject from "./CreateProject";
import { useAuth } from '@/context/AuthContext';
import { projectService } from '@/services';

const getTodayDate = (): string => {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};

interface MultistepProjectCreationProps {
  darkMode: boolean;
  onProjectCreated: (project: Project) => void;
}

const MultistepProjectCreation = ({ darkMode, onProjectCreated }: MultistepProjectCreationProps) => {
  const { step } = useParams<{ step: string }>();
  const navigate = useNavigate();
  const { user: currentUser } = useAuth();
  const [formStep, setFormStep] = useState(1);
  const [newProjectTitle, setNewProjectTitle] = useState("");
  const [newProjectDescription, setNewProjectDescription] = useState("");
  const [newProjectDueDate, setNewProjectDueDate] = useState("");
  const [newProjectPriority, setNewProjectPriority] = useState<'High' | 'Medium' | 'Low'>("Medium");
  const [newProjectFiles] = useState<File[]>([]);
  const [selectedTeamMembers, setSelectedTeamMembers] = useState<Employee[]>([]);
  const [selectedScrumMasters, setSelectedScrumMasters] = useState<Employee[]>([]);
  const [selectedTeamLeaders, setSelectedTeamLeaders] = useState<Employee[]>([]);
  const [newMilestoneTitle, setNewMilestoneTitle] = useState("");
  const [newMilestoneDescription, setNewMilestoneDescription] = useState("");
  const [newMilestoneDueDate, setNewMilestoneDueDate] = useState("");
  const [newMilestonePriority, setNewMilestonePriority] = useState<'High' | 'Medium' | 'Low'>("Medium");
  const [newMilestoneWeight, setNewMilestoneWeight] = useState<number>(50);
  const [newMilestoneAssignee, setNewMilestoneAssignee] = useState("");
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [newTaskDescription, setNewTaskDescription] = useState("");
  const [newTaskDueDate, setNewTaskDueDate] = useState("");
  const [newTaskPriority, setNewTaskPriority] = useState<'Low' | 'Medium' | 'High'>("Medium");
  const [newTaskAssignee, setNewTaskAssignee] = useState("");
  const [newTaskMilestone, setNewTaskMilestone] = useState<string>("");
  const [newTaskWeight, setNewTaskWeight] = useState<number>(50);
  const [newTaskAutoCreateTodo, setNewTaskAutoCreateTodo] = useState<boolean>(true);
  const [tempProject, setTempProject] = useState<Project | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const minDate = getTodayDate();

  // Clean up object URLs when component unmounts
  useEffect(() => {
    return () => {
      if (tempProject?.files) {
        tempProject.files.forEach(file => {
          if (file.url && file.url.startsWith('blob:')) {
            URL.revokeObjectURL(file.url);
          }
        });
      }
    };
  }, [tempProject]);

  // Initialize tempProject when component mounts or when form data changes
  useEffect(() => {
    if (newProjectTitle && (selectedTeamMembers.length > 0 || selectedScrumMasters.length > 0 || selectedTeamLeaders.length > 0)) {
      setTempProject(prev => {
        if (!prev) {
          return {
            id: 0,
            title: newProjectTitle,
            description: newProjectDescription,
            dueDate: newProjectDueDate,
            createdBy: currentUser?.name || currentUser?.username || 'Unknown User',
            teamMembers: selectedTeamMembers,
            scrumMaster: selectedScrumMasters[0] || null,
            teamLeader: selectedTeamLeaders[0] || null,
            priority: newProjectPriority,
            status: "Active" as const,
            progress: 0,
            files: newProjectFiles.map((file, index) => ({
              id: index,
              name: file.name,
              size: file.size,
              type: file.type,
              url: URL.createObjectURL(file)
            })),
            milestones: [],
            tasks: []
          };
        }
        return prev;
      });
    }
  }, [
    newProjectTitle, 
    selectedTeamMembers, 
    selectedScrumMasters, 
    selectedTeamLeaders, 
    newProjectDescription, 
    newProjectDueDate, 
    newProjectPriority, 
    newProjectFiles, 
    currentUser
  ]);

  const handleProjectCreated = (project: Project) => {
    setTempProject(project);
    navigateToStep(2);
  };

  const handleCreateMilestone = () => {
    if (!newMilestoneTitle.trim() || !newMilestoneAssignee) return;

    // Search for assignee in all team members
    let assignee = selectedTeamMembers.find(member => member.id === newMilestoneAssignee);
    if (!assignee) {
      assignee = selectedScrumMasters.find(sm => sm.id === newMilestoneAssignee);
    }
    if (!assignee) {
      assignee = selectedTeamLeaders.find(tl => tl.id === newMilestoneAssignee);
    }

    const newMilestone: Milestone = {
      id: `milestone${Date.now()}`,
      title: newMilestoneTitle,
      description: newMilestoneDescription,
      assignee: assignee?.name || "",
      assigneeId: newMilestoneAssignee,
      status: "Pending",
      priority: newMilestonePriority,
      dueDate: newMilestoneDueDate,
      weight: newMilestoneWeight,
      tasks: []
    };

    setTempProject(prev => {
      if (!prev) {
        const newProject: Project = {
          id: 0,
          title: newProjectTitle,
          description: newProjectDescription,
          dueDate: newProjectDueDate,
          createdBy: currentUser?.name || currentUser?.username || 'Unknown User',
          teamMembers: selectedTeamMembers,
          scrumMaster: selectedScrumMasters[0] || null,
          teamLeader: selectedTeamLeaders[0] || null,
          priority: newProjectPriority,
          status: "Active" as const,
          progress: 0,
          files: newProjectFiles.map((file, index) => ({
            id: index,
            name: file.name,
            size: file.size,
            type: file.type,
            url: URL.createObjectURL(file)
          })),
          milestones: [newMilestone],
          tasks: []
        };
        return newProject;
      } else {
        return {
          ...prev,
          milestones: [...(prev.milestones || []), newMilestone]
        };
      }
    });

    // Reset form
    setNewMilestoneTitle("");
    setNewMilestoneDescription("");
    setNewMilestoneDueDate("");
    setNewMilestonePriority("Medium");
    setNewMilestoneWeight(50);
    setNewMilestoneAssignee("");
  };

  const handleCreateTask = () => {
    console.log('🔄 handleCreateTask called');
    
    if (!newTaskTitle.trim() || !newTaskAssignee) {
      console.log('❌ Task creation cancelled - missing title or assignee');
      return;
    }

    const assignee = selectedTeamMembers.find(member => member.id === newTaskAssignee) ||
                    selectedScrumMasters.find(sm => sm.id === newTaskAssignee) ||
                    selectedTeamLeaders.find(tl => tl.id === newTaskAssignee);
    
    console.log('✅ Found assignee:', assignee?.name);

    const newTask: Task = {
      id: `task${Date.now()}`,
      title: newTaskTitle,
      description: newTaskDescription,
      assignee: assignee?.name || "",
      assigneeId: newTaskAssignee,
      status: "Pending",
      priority: newTaskPriority,
      dueDate: newTaskDueDate,
      weight: newTaskWeight,
      ...(newTaskMilestone && { milestoneId: newTaskMilestone }),
    };
    
    console.log('📋 Created task object:', newTask);

    setTempProject(prev => {
      if (!prev) {
        const newProject: Project = {
          id: 0,
          title: newProjectTitle,
          description: newProjectDescription,
          dueDate: newProjectDueDate,
          createdBy: currentUser?.name || currentUser?.username || 'Unknown User',
          teamMembers: selectedTeamMembers,
          scrumMaster: selectedScrumMasters[0] || null,
          teamLeader: selectedTeamLeaders[0] || null,
          priority: newProjectPriority,
          status: "Active" as const,
          progress: 0,
          files: newProjectFiles.map((file, index) => ({
            id: index,
            name: file.name,
            size: file.size,
            type: file.type,
            url: URL.createObjectURL(file)
          })),
          milestones: [],
          tasks: [newTask]
        };
        return newProject;
      } else {
        if (newTaskMilestone) {
          const updatedMilestones = prev.milestones?.map(milestone =>
            milestone.id === newTaskMilestone
              ? { ...milestone, tasks: [...(milestone.tasks || []), newTask] }
              : milestone
          ) || [];

          return {
            ...prev,
            milestones: updatedMilestones
          };
        } else {
          return {
            ...prev,
            tasks: [...(prev.tasks || []), newTask]
          };
        }
      }
    });

    console.log('✅ Task added to tempProject');

    // Reset form
    setNewTaskTitle("");
    setNewTaskDescription("");
    setNewTaskDueDate("");
    setNewTaskPriority("Medium");
    setNewTaskAssignee("");
    setNewTaskMilestone("");
    setNewTaskWeight(50);
    setNewTaskAutoCreateTodo(true);
  };

  const handleCreateProject = async () => {
    if (!newProjectTitle.trim()) return;

    if (!currentUser) {
      console.error('❌ No authenticated user found');
      setError('You must be logged in to create a project');
      return;
    }

    setIsLoading(true);
    setError(null);

    const newProject: Project = {
      id: 0,
      title: newProjectTitle,
      description: newProjectDescription,
      dueDate: newProjectDueDate,
      createdBy: currentUser?.name || currentUser?.username || 'Unknown User',
      teamMembers: selectedTeamMembers,
      scrumMaster: selectedScrumMasters[0] || null,
      teamLeader: selectedTeamLeaders[0] || null,
      priority: newProjectPriority,
      status: "Active" as const,
      progress: 0,
      files: newProjectFiles.map((file, index) => ({
        id: index,
        name: file.name,
        size: file.size,
        type: file.type,
        url: URL.createObjectURL(file)
      })),
      milestones: tempProject?.milestones || [],
      tasks: tempProject?.tasks || []
    };

    const createProjectRequest = {
      title: newProject.title,
      description: newProject.description,
      priority: newProject.priority as 'Low' | 'Medium' | 'High' | 'Critical',
      dueDate: new Date(newProject.dueDate).toISOString(),
      projectOwner: selectedScrumMasters[0]?.name || currentUser?.fullName || currentUser?.name || currentUser?.username || 'Unknown',
      projectOwnerEmail: selectedScrumMasters[0]?.email || currentUser?.email || '',
      projectOwnerPhone: selectedScrumMasters[0]?.phone || currentUser?.phone || '+1234567890',
      department: selectedScrumMasters[0]?.department || currentUser?.department || 'General',
      status: 'Active' as const
    };

    if (selectedScrumMasters.length > 0) {
      (createProjectRequest as any).assignedEmployeeId = selectedScrumMasters[0].employeeId || selectedScrumMasters[0].id;
      (createProjectRequest as any).assignedRole = 'ScrumMaster';
    }

    try {
      const response = await projectService.createProject(createProjectRequest);

      if (!response || !response.success || !response.data) {
        console.error('❌ Project creation API returned error:', response);
        const serverMessage = (response && (response.message || response.raw?.message)) || 'Project creation failed';
        setError(serverMessage);
        throw response;
      }

      const backendApiResponse = response.data as any;
      const createdProject = backendApiResponse?.data || response.data;
      const projectId = createdProject?.Id || createdProject?.id || createdProject?.projectId;
      
      console.log('🔍 Extracted project ID:', projectId);
      
      if (!projectId) {
        console.error('❌ CRITICAL ERROR: No project ID found in response');
        throw new Error('Project was created but no ID was returned from the server');
      }
      
      const backendData = createdProject && typeof createdProject === 'object' ? createdProject : {};
      const integratedProject: Project = {
        ...newProject,
        ...backendData,
        id: Number(projectId)
      };
      
      console.log('✅ Project created successfully with ID:', projectId);
      
      // Import and use project assignment service
      const { projectAssignmentService } = await import('@/services/projectAssignmentService');
      
      // Assign Scrum Masters
      if (selectedScrumMasters.length > 0) {
        console.log('🚀 Assigning Scrum Masters...');
        for (const scrumMaster of selectedScrumMasters) {
          try {
            const scrumMasterData = {
              projectId: Number(projectId),
              employeeId: scrumMaster.employeeId || scrumMaster.id,
              memberRole: 'Scrum Master',
              role: scrumMaster.role || 'member'
            };
            await projectAssignmentService.addMembers(scrumMasterData);
            console.log('✅ Scrum Master assigned:', scrumMaster.name);
          } catch (error) {
            console.error('❌ Failed to assign Scrum Master:', scrumMaster.name, error);
          }
        }
      }
      
      // Assign Team Leaders
      for (const teamLeader of selectedTeamLeaders) {
        try {
          const teamLeaderData = {
            projectId: Number(projectId),
            employeeId: teamLeader.employeeId || teamLeader.id,
            memberRole: 'Team Leader',
            role: teamLeader.role || 'member'
          };
          await projectAssignmentService.addMembers(teamLeaderData);
          console.log('✅ Team Leader assigned:', teamLeader.name);
        } catch (error) {
          console.error('❌ Error assigning Team Leader:', teamLeader.name, error);
        }
      }
      
      // Assign Team Members
      for (const member of selectedTeamMembers) {
        try {
          const memberData = {
            projectId: Number(projectId),
            employeeId: member.employeeId || member.id,
            memberRole: 'Member',
            role: member.role || 'member'
          };
          await projectAssignmentService.addMembers(memberData);
          console.log('✅ Team Member assigned:', member.name);
        } catch (error) {
          console.error('❌ Error assigning Team Member:', member.name, error);
        }
      }
      
      // Get project assignments for task creation
      const memberAssignmentMapping = new Map<string, number>();
      
      try {
        const assignmentsResponse = await projectAssignmentService.getProjectMembers(Number(projectId));
        
        if (assignmentsResponse.success && assignmentsResponse.data) {
          const assignments = Array.isArray(assignmentsResponse.data) ? assignmentsResponse.data : [assignmentsResponse.data];
          
          assignments.forEach((assignment: any) => {
            const employeeId = assignment.memberId || assignment.employeeId;
            const assignmentId = assignment.id;
            memberAssignmentMapping.set(employeeId, assignmentId);
          });
        }
      } catch (error) {
        console.error('❌ Failed to fetch project assignments:', error);
      }
      
      // Upload files
      if (newProjectFiles.length > 0) {
        for (const file of newProjectFiles) {
          const formData = new FormData();
          formData.append("file", file);
          formData.append("entityType", "Project");
          formData.append("entityId", projectId.toString());
          formData.append("description", `Attachment for project: ${newProject.title}`);
          formData.append("tags", "[]");
          
          try {
            const { attachmentsService } = await import('@/services/attachmentsService');
            await attachmentsService.upload(formData);
            console.log('✅ File uploaded successfully:', file.name);
          } catch (error) {
            console.error('❌ File upload error:', file.name, error);
          }
        }
      }
      
      // Create milestones
      const milestoneIdMapping = new Map<string, number>();
      
      if (tempProject?.milestones && tempProject.milestones.length > 0) {
        const { milestoneService } = await import('@/services/milestoneService');
        
        for (const milestone of tempProject.milestones) {
          try {
            const createMilestoneData = {
              milestoneName: milestone.title,
              description: milestone.description || '',
              assignedMemberId: milestone.assigneeId || '',
              startDate: milestone.startDate || new Date().toISOString(),
              dueDate: milestone.dueDate || new Date().toISOString(),
              weight: milestone.weight || 50,
              projectId: Number(projectId),
              status: 'Pending' as const
            };
            
            const milestoneResponse = await milestoneService.createMilestone(createMilestoneData);
            
            if (milestoneResponse.success && milestoneResponse.data) {
              const realMilestoneId = milestoneResponse.data.milestoneId;
              milestoneIdMapping.set(milestone.id, realMilestoneId);
            }
          } catch (error) {
            console.error('❌ Error creating milestone:', milestone.title, error);
          }
        }
      }
      
      // Create tasks
      const allTasks: Task[] = [];
      
      if (tempProject?.tasks && tempProject.tasks.length > 0) {
        allTasks.push(...tempProject.tasks);
      }
      
      if (tempProject?.milestones && tempProject.milestones.length > 0) {
        tempProject.milestones.forEach(milestone => {
          if (milestone.tasks && milestone.tasks.length > 0) {
            allTasks.push(...milestone.tasks);
          }
        });
      }
      
      if (allTasks.length > 0) {
        const { projectTaskService } = await import('@/services/projectTaskService');
        
        for (const task of allTasks) {
          try {
            let realMilestoneId: number | null = null;
            if (task.milestoneId) {
              realMilestoneId = milestoneIdMapping.get(task.milestoneId) || null;
            }
            
            const taskAssigneeId = task.assigneeId || '';
            const projectAssignmentId = memberAssignmentMapping.get(taskAssigneeId);
            
            if (!projectAssignmentId) {
              console.error('❌ No ProjectAssignmentId found for assignee:', taskAssigneeId);
              continue;
            }
            
            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 1);
            
            const oneWeekFromNow = new Date();
            oneWeekFromNow.setDate(oneWeekFromNow.getDate() + 7);
            
            const taskDueDate = task.dueDate ? new Date(task.dueDate) : oneWeekFromNow;
            
            const createTaskData = {
              projectAssignmentId: projectAssignmentId,
              assignedMemberId: taskAssigneeId,
              title: task.title,
              description: task.description || '',
              startDate: tomorrow.toISOString(),
              dueDate: taskDueDate.toISOString(),
              priority: task.priority || 'Medium',
              weight: task.weight || 50,
              status: 'Pending' as const,
              milestoneId: realMilestoneId,
              isAutoCreateTodoItem: newTaskAutoCreateTodo
            };
            
            await projectTaskService.createTask(createTaskData);
            console.log('✅ Task created successfully:', task.title);
          } catch (error: any) {
            console.error('❌ Error creating task:', task.title, error);
            alert(`Failed to create task "${task.title}":\n\n${error?.message || 'Unknown error'}`);
          }
        }
      }
      
      onProjectCreated(integratedProject);
      alert(`Project "${integratedProject.title}" created successfully!`);
      
      const userRole = currentUser?.role?.toLowerCase() || 'member';
      const roleRoute = userRole === 'vice_president' ? 'vice-president' : userRole;
      navigate(`/dashboard/${roleRoute}/projects/mine`);
    } catch (err: any) {
      console.error('❌ Error creating project:', err);
      
      let errorMessage = 'An error occurred while creating the project';
      
      if (err && typeof err === 'object') {
        if (err.message) {
          errorMessage = err.message;
        }
        
        if (err.errors && Array.isArray(err.errors) && err.errors.length > 0) {
          errorMessage += '\n\nValidation Errors:\n' + err.errors.join('\n');
        } else if (err.raw && err.raw.errors && typeof err.raw.errors === 'object') {
          errorMessage += '\n\nValidation Errors:';
          Object.entries(err.raw.errors).forEach(([field, messages]: [string, any]) => {
            if (Array.isArray(messages)) {
              messages.forEach(msg => {
                errorMessage += `\n• ${field}: ${msg}`;
              });
            }
          });
        }
        
        if (err.status) {
          errorMessage += `\n\nStatus Code: ${err.status}`;
        }
      }
      
      setError(errorMessage);
      alert(`Project creation failed:\n\n${errorMessage}`);
    } finally {
      setIsLoading(false);
    }
  };

  const navigateToStep = (stepNumber: number) => {
    console.log('🔍 Navigating to step:', stepNumber);
    const userRole = currentUser?.role?.toLowerCase() || 'member';
    const roleRoute = userRole === 'vice_president' ? 'vice-president' : userRole;
    navigate(`/dashboard/${roleRoute}/projects/new/${stepNumber}`);
  };

  const handleTeamSelectionChange = useCallback((selections: any) => {
    console.log('🔍 Team selections received:', selections);
    
    if (!currentUser) {
      console.error('❌ No authenticated user found');
      return;
    }
    
    if (selections.selectedManagerObjects && Array.isArray(selections.selectedManagerObjects)) {
      const scrumMasters = selections.selectedManagerObjects.map((manager: any) => ({
        id: manager.id,
        employeeId: manager.employeeId,
        name: manager.name,
        email: manager.email,
        department: manager.department,
        position: manager.role,
        role: manager.role,
        avatar: ''
      }));
      
      setSelectedScrumMasters(scrumMasters);
    } else if (selections.selectedManagerObject) {
      const manager = selections.selectedManagerObject;
      setSelectedScrumMasters([{
        id: manager.id,
        employeeId: manager.employeeId,
        name: manager.name,
        email: manager.email,
        department: manager.department,
        position: manager.role,
        role: manager.role,
        avatar: ''
      }]);
    }
    
    if (selections.selectedTeamLeaderObjects && Array.isArray(selections.selectedTeamLeaderObjects)) {
      const teamLeaders = selections.selectedTeamLeaderObjects.map((leader: any) => ({
        id: leader.id,
        employeeId: leader.employeeId,
        name: leader.name,
        email: leader.email,
        department: leader.department,
        position: leader.role,
        role: leader.role,
        avatar: ''
      }));
      
      setSelectedTeamLeaders(teamLeaders);
    } else if (selections.selectedTeamLeaderObject) {
      const leader = selections.selectedTeamLeaderObject;
      setSelectedTeamLeaders([{
        id: leader.id,
        employeeId: leader.employeeId,
        name: leader.name,
        email: leader.email,
        department: leader.department,
        position: leader.role,
        role: leader.role,
        avatar: ''
      }]);
    }
    
    if (selections.teamMembers && selections.teamMembers.length > 0) {
      const employees = selections.teamMembers.map((tm: any) => ({
        id: tm.id,
        employeeId: tm.employeeId,
        name: tm.name,
        email: tm.email || '',
        department: tm.department || 'Engineering',
        position: tm.role || 'team_member',
        role: tm.role,
        avatar: ''
      }));
      setSelectedTeamMembers(employees);
    }
  }, [currentUser]);

  const handleProjectDataChange = useCallback((projectData: any) => {
    console.log('🔍 Project data received:', projectData);
    setNewProjectTitle(projectData.title);
    setNewProjectDescription(projectData.description);
    setNewProjectDueDate(projectData.dueDate);
    setNewProjectPriority(projectData.priority as 'High' | 'Medium' | 'Low');
  }, []);

  const handleButtonClick = useCallback(() => {
    console.log('🔍 CreateProject button clicked - navigating to next step');
    navigateToStep(formStep + 1);
  }, [formStep]);

  // Update form step when URL params change
  useEffect(() => {
    const stepNumber = step ? parseInt(step) : 1;

    if (stepNumber < 1 || stepNumber > 3) {
      navigate('new/1', { replace: true });
    } else {
      setFormStep(stepNumber);
    }
  }, [step, navigate]);

  return (
    <div className={`min-h-screen p-6 ${
      darkMode 
        ? "bg-gradient-to-br from-slate-900 via-purple-900 to-slate-900" 
        : "bg-white"
    }`}>
      {isLoading && (
        <div className="flex items-center justify-center min-h-screen">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-purple-600"></div>
        </div>
      )}
      {error && (
        <div className="mb-6 p-4 bg-red-100 dark:bg-red-900/30 border border-red-200 dark:border-red-800 rounded-lg shadow-lg">
          <div className="flex items-center gap-3">
            <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400" />
            <p className="text-red-800 dark:text-red-200">{error}</p>
          </div>
        </div>
      )}
      
      <div className="max-w-full mx-auto">
        {/* Header */}
        <div className="flex justify-between items-start mb-4">
        {/* Back Button */}
        <div className="mb-6">
          <button
            onClick={() => {
              if (formStep > 1) {
                navigateToStep(formStep - 1);
              } else {
                navigate(-1);
              }
            }}
            className={`inline-flex items-center px-4 py-2 rounded-lg transition-all duration-200 hover:scale-105 ${
              darkMode
                ? "bg-slate-700 border-slate-600 text-gray-300 hover:bg-slate-600 border"
                : "bg-white "
            }`}
          >
            <ArrowLeft className="w-6 h-6 mr-2" />
          
          </button>
        </div>
        
        {/* Header Text */}
        <div>
          <h1 className={`text-2xl font-bold mb-1 ${
            darkMode ? "text-white" : "text-gray-900"
          }`}>
            Create New Project
          </h1>
          <p className={`text-md ${
            darkMode ? "text-gray-300" : "text-gray-600"
          }`}>
            Build your project step by step with milestones and tasks
          </p>
        </div>

        {/* Scrum Master Info - Right side */}
        <div className="ml-auto">
          {selectedScrumMasters.length > 0 ? (
            <div className={`p-3 rounded-lg ${
              selectedScrumMasters.some(sm => sm.id === currentUser?.id)
                ? 'bg-green-100 text-green-800 border border-green-200' 
                : 'bg-yellow-100 text-yellow-800 border border-yellow-200'
            }`}>
              <div className="flex items-center space-x-2">
                <Crown className="w-4 h-4" />
                <span className="font-medium">
                  {selectedScrumMasters.some(sm => sm.id === currentUser?.id)
                    ? `You are ${selectedScrumMasters.length > 1 ? 'one of the' : 'the'} Scrum Master${selectedScrumMasters.length > 1 ? 's' : ''} - Full control over project` 
                    : `${selectedScrumMasters.map(sm => sm.name).join(', ')} ${selectedScrumMasters.length > 1 ? 'are' : 'is'} the Scrum Master${selectedScrumMasters.length > 1 ? 's' : ''} - You can create but they will manage`}
                </span>
              </div>
            </div>
          ) : (
            <div className="p-3 rounded-lg bg-blue-100 text-blue-800 border border-blue-200">
              <div className="flex items-center space-x-2">
                <Crown className="w-4 h-4" />
                <span className="font-medium">
                  ℹ️ No Scrum Master selected - You will be automatically assigned as the Scrum Master
                </span>
              </div>
            </div>
          )}
        </div>
        </div>

        
        {/* Step Content */}
        <div className={`rounded-lg p-4 ${darkMode ? 'bg-zinc-700' : 'bg-white'} mb-6`}>
          {formStep === 1 && (
            <div>
              <CreateProject
                darkMode={darkMode}
                onProjectCreated={handleProjectCreated}
                buttonLabel="Next"
                isMultistep={true}
                onTeamSelectionChange={handleTeamSelectionChange}
                onProjectDataChange={handleProjectDataChange}
                onButtonClick={handleButtonClick}
                initialData={{
                  title: newProjectTitle,
                  description: newProjectDescription,
                  dueDate: newProjectDueDate,
                  priority: newProjectPriority,
                  department: currentUser?.department || '',
                  projectOwner: currentUser?.name || currentUser?.username || '',
                  projectOwnerEmail: currentUser?.email || '',
                  projectOwnerPhone: currentUser?.phone || '',
                  files: newProjectFiles
                }}
              />
            </div>
          )}

          {formStep === 2 && (
            <Card className={`shadow-xl border-0 ${
              darkMode 
                ? "bg-slate-800/90 backdrop-blur-sm border-slate-700" 
                : "bg-white/90 backdrop-blur-sm"
            }`}>
              <CardHeader>
                <CardTitle className="flex items-center gap-2 text-xl">
                  <div className="p-2 bg-purple-100 dark:bg-purple-900 rounded-lg">
                    <Target className="w-5 h-5 text-purple-600 dark:text-purple-300" />
                  </div>
                  Add Initial Milestones
                </CardTitle>
                <CardDescription>
                  Define key milestones for your project progress tracking
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="space-y-2">
                  <Label htmlFor="milestoneTitle" className={`flex items-center gap-2 font-medium ${
                    darkMode ? "text-gray-200" : "text-gray-700"
                  }`}>
                    <Target className="w-4 h-4" />
                    Milestone Title
                  </Label>
                  <Input
                    id="milestoneTitle"
                    value={newMilestoneTitle}
                    onChange={(e) => setNewMilestoneTitle(e.target.value)}
                    placeholder="Enter milestone title"
                    className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-purple-500 ${
                      darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                    }`}
                  />
                </div>

                <div className="space-y-2">
                  <Label
                    htmlFor="milestoneDescription"
                    className={`flex items-center gap-2 font-medium ${
                      darkMode ? "text-gray-200" : "text-gray-700"
                    }`}
                  >
                    Description
                  </Label>
                  <Textarea
                    id="milestoneDescription"
                    value={newMilestoneDescription}
                    onChange={(e) => setNewMilestoneDescription(e.target.value)}
                    placeholder="Describe this milestone and its deliverables..."
                    className={`min-h-[100px] transition-all duration-200 focus:ring-2 focus:ring-purple-500 resize-none ${
                      darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                    }`}
                    rows={3}
                  />
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="space-y-2">
                    <Label htmlFor="milestoneDueDate" className={`flex items-center gap-2 font-medium ${
                      darkMode ? "text-gray-200" : "text-gray-700"
                    }`}>
                      <Calendar className="w-4 h-4" />
                      Due Date
                    </Label>
                    <Input
                      id="milestoneDueDate"
                      type="date"
                      value={newMilestoneDueDate}
                      onChange={(e) => setNewMilestoneDueDate(e.target.value)}
                      min={minDate}
                      className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-purple-500 ${
                        darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                      }`}
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="milestonePriority" className={`flex items-center gap-2 font-medium ${
                      darkMode ? "text-gray-200" : "text-gray-700"
                    }`}>
                      <Flag className="w-4 h-4" />
                      Priority
                    </Label>
                    <Select
                      value={newMilestonePriority}
                      onValueChange={(value: "High" | "Medium" | "Low") =>
                        setNewMilestonePriority(value)
                      }
                    >
                      <SelectTrigger className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-purple-500 ${
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
                            <div className="w-2 h-2 bg-red-500 rounded-full"></div>
                            High
                          </div>
                        </SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                {/* Weight Input Field */}
                <div className="space-y-2">
                  <Label className={`flex items-center gap-2 font-medium ${
                    darkMode ? "text-gray-200" : "text-gray-700"
                  }`}>
                    <Target className="w-4 h-4" />
                    Weight (1-100) - Importance/Progress Impact
                  </Label>
                  <div className="space-y-3">
                    <input
                      type="range"
                      min="1"
                      max="100"
                      value={newMilestoneWeight}
                      onChange={(e) => setNewMilestoneWeight(Number(e.target.value))}
                      className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-purple-600"
                    />
                    <div className="flex justify-between items-center">
                      <Input
                        type="number"
                        min="1"
                        max="100"
                        value={newMilestoneWeight}
                        onChange={(e) => {
                          const value = Math.min(100, Math.max(1, Number(e.target.value)));
                          setNewMilestoneWeight(value);
                        }}
                        className={`w-20 h-11 text-center transition-all duration-200 focus:ring-2 focus:ring-purple-500 ${
                          darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                        }`}
                      />
                      <span className={`text-sm font-medium px-3 py-1 rounded-full ${
                        newMilestoneWeight < 33 
                          ? "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300"
                          : newMilestoneWeight < 67 
                          ? "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300"
                          : "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300"
                      }`}>
                        {newMilestoneWeight < 33 ? '🟢 Low Impact' : newMilestoneWeight < 67 ? '🟡 Medium Impact' : '🔴 High Impact'}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="milestoneAssignee" className={`flex items-center gap-2 font-medium ${
                    darkMode ? "text-gray-200" : "text-gray-700"
                  }`}>
                    <Users className="w-4 h-4" />
                    Assign To
                  </Label>
                  <Select
                    value={newMilestoneAssignee}
                    onValueChange={(value: string) => setNewMilestoneAssignee(value)}
                  >
                    <SelectTrigger className={`h-11 transition-all duration-200 focus:ring-2 focus:ring-purple-500 ${
                      darkMode ? "bg-slate-700 border-slate-600 text-gray-100" : "border-gray-300"
                    }`}>
                      <SelectValue placeholder="Select team member" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">Select team member</SelectItem>
                      {selectedScrumMasters.map(sm => (
                        <SelectItem key={sm.id} value={sm.id}>
                          <div className="flex items-center gap-2">
                            <Crown className="w-4 h-4 text-yellow-500" />
                            {sm.name} (Scrum Master)
                          </div>
                        </SelectItem>
                      ))}
                      {selectedTeamLeaders.map(tl => (
                        <SelectItem key={tl.id} value={tl.id}>
                          <div className="flex items-center gap-2">
                            <div className="w-4 h-4 bg-blue-500 rounded-full"></div>
                            {tl.name} (Team Leader)
                          </div>
                        </SelectItem>
                      ))}
                      {selectedTeamMembers.map(member => (
                        <SelectItem key={member.id} value={member.id}>
                          <div className="flex items-center gap-2">
                            <div className="w-4 h-4 bg-green-500 rounded-full"></div>
                            {member.name} (Team Member)
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <Button
                  onClick={handleCreateMilestone}
                  disabled={!newMilestoneTitle.trim() || !newMilestoneAssignee}
                  className={`h-11 px-6 transition-all duration-200 hover:scale-105 shadow-lg hover:shadow-xl ${
                    !newMilestoneTitle.trim() || !newMilestoneAssignee
                      ? "bg-gray-400 cursor-not-allowed"
                      : "bg-gradient-to-r from-purple-500 to-purple-600 hover:from-purple-600 hover:to-purple-700 text-white"
                  }`}
                >
                  <div className="flex items-center gap-2">
                    <Plus className="w-4 h-4" />
                    Add Milestone
                  </div>
                </Button>
              </CardContent>

              {/* Show added milestones */}
              {tempProject?.milestones && tempProject.milestones.length > 0 && (
                <div className="mt-6">
                  <div className="flex items-center gap-2 mb-4">
                    <Badge variant="secondary" className="text-xs">
                      {tempProject.milestones.length} milestone{tempProject.milestones.length !== 1 ? 's' : ''} added
                    </Badge>
                  </div>
                  <div className="space-y-3">
                    {tempProject.milestones.map((milestone) => (
                      <div
                        key={milestone.id}
                        className={`p-4 rounded-lg border transition-all duration-200 hover:shadow-md ${
                          darkMode 
                            ? "bg-slate-700 border-slate-600 hover:border-slate-500" 
                            : "bg-white border-gray-200 hover:border-gray-300"
                        }`}
                      >
                        <div className="flex justify-between items-start">
                          <div className="flex-1">
                            <h4 className={`font-medium text-base mb-2 ${
                              darkMode ? "text-gray-200" : "text-gray-800"
                            }`}>
                              {milestone.title}
                            </h4>
                            <p className={`text-sm mb-3 ${
                              darkMode ? "text-gray-400" : "text-gray-600"
                            }`}>
                              {milestone.description}
                            </p>
                            <div className="flex items-center gap-4 text-sm">
                              <div className="flex items-center gap-2">
                                <Users className="w-4 h-4 text-purple-500" />
                                <span className={darkMode ? "text-gray-300" : "text-gray-700"}>
                                  {milestone.assignee}
                                </span>
                              </div>
                              <div className="flex items-center gap-2">
                                <Calendar className="w-4 h-4 text-blue-500" />
                                <span className={darkMode ? "text-gray-300" : "text-gray-700"}>
                                  {new Date(milestone.dueDate).toLocaleDateString()}
                                </span>
                              </div>
                            </div>
                          </div>
                          <div className="flex flex-col items-end gap-2">
                            <Badge className={`px-3 py-1 text-xs font-medium ${
                              milestone.priority === "High"
                                ? "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300"
                                : milestone.priority === "Medium"
                                ? "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300"
                                : "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300"
                            }`}>
                              {milestone.priority}
                            </Badge>
                            <div className={`text-xs px-2 py-1 rounded-full ${
                              milestone.weight && milestone.weight < 33
                                ? "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300"
                                : milestone.weight && milestone.weight < 67
                                ? "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300"
                                : "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300"
                            }`}>
                              Weight: {milestone.weight || 50}
                            </div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </Card>
          )}

          {formStep === 3 && (
            <div>
              <h2 className="text-xl font-semibold mb-4">Add Initial Tasks</h2>
              <p className="text-sm text-gray-500 mb-4">Add the initial tasks for your project (optional)</p>

              <div className="space-y-4">
                <div className="mb-4">
                  <label className="block text-sm font-medium mb-1">Task Title</label>
                  <input
                    type="text"
                    value={newTaskTitle}
                    onChange={(e) => setNewTaskTitle(e.target.value)}
                    className={`w-full p-2 rounded-lg border ${darkMode ? 'bg-zinc-600 border-zinc-500 text-white' : 'bg-white border-gray-300 text-gray-800'}`}
                    placeholder="Task title"
                  />
                </div>

                <div className="mb-4">
                  <label className="block text-sm font-medium mb-1">Description</label>
                  <textarea
                    value={newTaskDescription}
                    onChange={(e) => setNewTaskDescription(e.target.value)}
                    className={`w-full p-2 rounded-lg border ${darkMode ? 'bg-zinc-600 border-zinc-500 text-white' : 'bg-white border-gray-300 text-gray-800'}`}
                    placeholder="Task description"
                    rows={3}
                  />
                </div>

                <div className="grid grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="block text-sm font-medium mb-1">Due Date</label>
                    <input
                      type="date"
                      value={newTaskDueDate}
                      onChange={(e) => setNewTaskDueDate(e.target.value)}
                      min={minDate}
                      className={`w-full p-2 rounded-lg border ${darkMode ? 'bg-zinc-600 border-zinc-500 text-white' : 'bg-white border-gray-300 text-gray-800'}`}
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium mb-1">Priority</label>
                    <select
                      value={newTaskPriority}
                      onChange={(e) => setNewTaskPriority(e.target.value as 'Low' | 'Medium' | 'High')}
                      className={`w-full p-2 rounded-lg border ${darkMode ? 'bg-zinc-600 border-zinc-500 text-white' : 'bg-white border-gray-300 text-gray-800'}`}
                    >
                      <option value="Low">Low</option>
                      <option value="Medium">Medium</option>
                      <option value="High">High</option>
                    </select>
                  </div>
                </div>

                <div className="mb-4">
                  <label className="block text-sm font-medium mb-1">
                    Assign to Milestone (optional)
                  </label>
                  <select
                    value={newTaskMilestone}
                    onChange={(e) => setNewTaskMilestone(e.target.value)}
                    className={`w-full p-2 rounded-lg border ${darkMode ? 'bg-zinc-600 border-zinc-500 text-white' : 'bg-white border-gray-300 text-gray-800'}`}
                  >
                    <option value="">No Milestone (Direct to Project)</option>
                    {tempProject?.milestones && tempProject.milestones.length > 0 ? (
                      tempProject.milestones.map(milestone => (
                        <option key={milestone.id} value={milestone.id}>
                          {milestone.title} (Due: {new Date(milestone.dueDate).toLocaleDateString()})
                        </option>
                      ))
                    ) : (
                      <option value="" disabled>No milestones created yet</option>
                    )}
                  </select>
                  {(!tempProject?.milestones || tempProject.milestones.length === 0) && (
                    <p className="text-xs text-gray-500 mt-1">
                      Create milestones first to assign tasks to them
                    </p>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-4 mb-4">
                  <div>
                    <label className="block text-sm font-medium mb-1">Assign To</label>
                    <select
                      value={newTaskAssignee}
                      onChange={(e) => setNewTaskAssignee(e.target.value)}
                      className={`w-full p-2 rounded-lg border ${darkMode ? 'bg-zinc-600 border-zinc-500 text-white' : 'bg-white border-gray-300 text-gray-800'}`}
                    >
                      <option value="">Select team member</option>
                      {selectedScrumMasters.map(sm => (
                        <option key={sm.id} value={sm.id}>
                          {sm.name} (Scrum Master)
                        </option>
                      ))}
                      {selectedTeamLeaders.map(tl => 
                        !selectedScrumMasters.some(sm => sm.id === tl.id) && (
                        <option key={tl.id} value={tl.id}>
                          {tl.name} (Team Leader)
                        </option>
                      ))}
                      {selectedTeamMembers.map(member => (
                        <option key={member.id} value={member.id}>
                          {member.name} (Team Member)
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium mb-1">
                      Task Weight (1-100) *
                    </label>
                    <input
                      type="number"
                      value={newTaskWeight}
                      onChange={(e) => setNewTaskWeight(parseInt(e.target.value) || 50)}
                      min={1}
                      max={100}
                      className={`w-full p-2 rounded-lg border ${darkMode ? 'bg-zinc-600 border-zinc-500 text-white' : 'bg-white border-gray-300 text-gray-800'}`}
                    />
                    <p className="text-xs text-gray-500 mt-1">
                      Importance/effort level (50 = medium)
                    </p>
                  </div>
                </div>

                <div className={`mb-4 p-3 rounded-lg border ${
                  darkMode ? 'bg-zinc-700 border-zinc-600' : 'bg-blue-50 border-blue-200'
                }`}>
                  <label className="flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      checked={newTaskAutoCreateTodo}
                      onChange={(e) => setNewTaskAutoCreateTodo(e.target.checked)}
                      className="mr-3 h-4 w-4 text-purple-600 border-gray-300 rounded focus:ring-2 focus:ring-purple-500"
                    />
                    <div className="flex-1">
                      <span className="font-medium text-sm">Auto-create Action Item (TodoItem)</span>
                      <p className="text-xs text-gray-500 mt-1">
                        Automatically creates a checklist item for the assignee when this task is saved.
                      </p>
                    </div>
                  </label>
                </div>

                <button
                  onClick={handleCreateTask}
                  disabled={!newTaskTitle.trim() || !newTaskAssignee}
                  className={`px-4 py-2 rounded-lg text-white ${
                    !newTaskTitle.trim() || !newTaskAssignee
                      ? 'bg-gray-400'
                      : 'bg-purple-600 hover:bg-purple-500'
                  }`}
                >
                  Add Task
                </button>
              </div>

              {/* Show added tasks */}
              {tempProject?.tasks && tempProject.tasks.length > 0 && (
                <div className="mt-6">
                  <h3 className="font-semibold mb-3">Added Tasks</h3>
                  <div className="space-y-3">
                    {tempProject.tasks.map((task) => (
                      <div
                        key={task.id}
                        className={`p-3 rounded-lg ${darkMode ? 'bg-zinc-600' : 'bg-white'} border ${darkMode ? 'border-zinc-500' : 'border-gray-200'}`}
                      >
                        <div className="flex justify-between items-start">
                          <div>
                            <h4 className="font-medium">{task.title}</h4>
                            <p className="text-sm opacity-75">{task.description}</p>
                          </div>
                          <span
                            className={`px-2 py-1 text-xs rounded-full ${
                              task.priority === "High"
                                ? "bg-red-100 text-red-800"
                                : task.priority === "Medium"
                                ? "bg-yellow-100 text-yellow-800"
                                : "bg-green-100 text-green-800"
                            }`}
                          >
                            {task.priority}
                          </span>
                        </div>
                        <div className="flex justify-between items-center mt-2">
                          <span className="text-sm">Assigned to: {task.assignee}</span>
                          <span className="text-sm">Due: {new Date(task.dueDate).toLocaleDateString()}</span>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Navigation Buttons */}
        <div className="flex justify-between items-center pt-8">
          <Button
            variant="outline"
            onClick={() => {
              if (formStep > 1) {
                navigateToStep(formStep - 1);
              } else {
                const userRole = currentUser?.role?.toLowerCase() || 'member';
                const roleRoute = userRole === 'vice_president' ? 'vice-president' : userRole;
                navigate(`/dashboard/${roleRoute}/projects/mine`);
              }
            }}
            className={`h-11 px-6 transition-all duration-200 hover:scale-105 ${
              darkMode
                ? "border-slate-600 text-slate-300 hover:bg-slate-700 hover:border-slate-500"
                : "border-gray-300 text-gray-700 hover:bg-gray-50"
            }`}
          >
            {formStep === 1 ? 'Cancel' : 'Back'}
          </Button>

          <div className="flex gap-3">
            {formStep === 2 && (
              <Button
                onClick={() => navigateToStep(formStep + 1)}
                disabled={!newProjectTitle.trim()}
                className={`h-11 px-6 transition-all duration-200 hover:scale-105 shadow-lg hover:shadow-xl ${
                  !newProjectTitle.trim()
                    ? "bg-gray-400 cursor-not-allowed"
                    : "bg-gradient-to-r from-purple-500 to-purple-600 hover:from-purple-600 hover:to-purple-700 text-white"
                }`}
              >
                Next
              </Button>
            )}

            {formStep === 2 && (
              <Button
                onClick={handleCreateProject}
                disabled={!newProjectTitle.trim()}
                className={`h-11 px-6 transition-all duration-200 hover:scale-105 shadow-lg hover:shadow-xl ${
                  !newProjectTitle.trim()
                    ? "bg-gray-400 cursor-not-allowed"
                    : "bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-600 hover:to-orange-700 text-white"
                }`}
              >
                Skip Milestones
              </Button>
            )}

            <Button
              onClick={handleCreateProject}
              disabled={!newProjectTitle.trim()}
              className={`h-11 px-6 transition-all duration-200 hover:scale-105 shadow-lg hover:shadow-xl ${
                !newProjectTitle.trim()
                  ? "bg-gray-400 cursor-not-allowed"
                  : "bg-gradient-to-r from-green-500 to-green-600 hover:from-green-600 hover:to-green-700 text-white"
              }`}
            >
              <div className="flex items-center gap-2">
                <CheckCircle className="w-4 h-4" />
                Create Project
              </div>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default MultistepProjectCreation;