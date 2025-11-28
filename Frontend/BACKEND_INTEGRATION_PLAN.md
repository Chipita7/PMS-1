# Backend Integration Plan - ProjectTask & TodoItems Workflow

## 📋 **Backend Analysis Summary**

### **1. Entity Structure**

#### **ProjectTask Entity**
```csharp
public class ProjectTask
{
    // Status Lifecycle
    TaskStatus Status { get; set; } // Pending → Accepted → InProgress → WaitingForReview → Completed
                                    // OR: Pending → Rejected
    
    // Core Properties
    int Id { get; set; }
    string Title { get; set; }
    string? AssignedMemberId { get; set; }
    string? CreatedByUserId { get; set; }
    int Weight { get; set; } // 1-100
    double Progress { get; set; } // Auto-calculated from TodoItems + SubTasks
    DateTime? AcceptedDate { get; set; }
    string? RejectionReason { get; set; }
    TaskPriority Priority { get; set; } // Low, Medium, High, Critical
    
    // Relationships
    ICollection<TodoItem> TodoItems { get; set; }
    ICollection<ProjectTask> SubTasks { get; set; }
    int? ParentTaskId { get; set; }
    int ProjectAssignmentId { get; set; }
    int? MilestoneId { get; set; }
    
    // Auto-Create TodoItem Flag
    bool IsAutoCreateTodo { get; set; } // When true, auto-creates a TodoItem
}

// Status Enum
public enum TaskStatus { 
    Pending,          // Just created, awaiting assignee acceptance
    Accepted,         // Assignee accepted the task
    Rejected,         // Assignee rejected the task
    Completed,        // Team Leader approved completion
    InProgress,       // Assignee is working on it
    WaitingForReview  // Assignee marked as complete, awaiting TL approval
}
```

#### **TodoItem Entity**
```csharp
public class TodoItem
{
    // Status Lifecycle
    TodoItemStatus Status { get; set; } // Pending → Accepted → InProgress → WaitingForReview → Approved
                                        // OR: Pending → Rejected
    
    // Core Properties
    int Id { get; set; }
    int ProjectTaskId { get; set; } // Parent task
    string Title { get; set; }
    string AssigneeId { get; set; }
    string AssignedBy { get; set; }
    int Weight { get; set; } // 0-100
    double Progress { get; set; } // 0-100
    DateTime? AcceptedDate { get; set; }
    string? RejectionReason { get; set; }
    string? CompletionDetails { get; set; }
    string? DetailsForLateCompletion { get; set; }
    
    // Relationship
    ProjectTask ProjectTask { get; set; }
}

// Status Enum
public enum TodoItemStatus { 
    Pending,          // Just created/assigned
    Accepted,         // Assignee accepted
    Rejected,         // Assignee rejected
    InProgress,       // Assignee started work
    WaitingForReview, // Assignee marked complete
    Approved,         // Team Leader approved
    Reopened          // Team Leader rejected, needs rework
}
```

---

## 🔄 **Workflow Cycles**

### **ProjectTask Lifecycle:**

```
┌─────────────────────────────────────────────────────────────┐
│                  PROJECT TASK LIFECYCLE                     │
└─────────────────────────────────────────────────────────────┘

1. CREATION (Manager/Team Leader creates task)
   ↓
   Status: Pending
   ↓
2. ASSIGNMENT DECISION (Assignee receives task)
   ├─→ ACCEPT → Status: Accepted → Auto-creates TodoItem (if IsAutoCreateTodo = true)
   │                                ↓
   │                                Status: Accepted (ready to start work)
   │
   └─→ REJECT → Status: Rejected + RejectionReason
                 (Task ends here or gets reassigned)

3. WORK PHASE (After Accepted)
   ↓
   TodoItems created/assigned → Assignee works on TodoItems
   ↓
   Progress auto-calculated from TodoItem weights
   ↓
   When all TodoItems approved → ProjectTask progress → 100%

4. COMPLETION REVIEW (Assignee marks task complete)
   ↓
   Status: WaitingForReview
   ↓
5. TEAM LEADER DECISION
   ├─→ APPROVE → Status: Completed ✅
   └─→ REJECT → Status: InProgress + RejectionReason
                 (Assignee must continue work)
```

---

### **TodoItem Lifecycle:**

```
┌─────────────────────────────────────────────────────────────┐
│                   TODO ITEM LIFECYCLE                       │
└─────────────────────────────────────────────────────────────┘

1. CREATION (Auto-created or manually created by TL)
   ↓
   Status: Pending
   ↓
2. ASSIGNMENT DECISION (Assignee receives TodoItem)
   ├─→ ACCEPT → Status: Accepted
   │            ↓
   │            Can now START work
   │
   └─→ REJECT → Status: Rejected + RejectionReason

3. START WORK (Assignee clicks "Start")
   ↓
   Status: InProgress
   ↓
   Assignee updates progress (0-100%)
   ↓
4. MARK COMPLETE (Assignee finishes)
   ↓
   Status: WaitingForReview + Progress = X%
   ↓
5. TEAM LEADER REVIEW
   ├─→ APPROVE → Status: Approved ✅
   │             Progress applies to parent ProjectTask
   │
   └─→ REJECT → Status: Reopened + RejectionReason
                 Assignee must rework
```

---

## 📊 **Progress Auto-Calculation**

### **Formula:**

```
ProjectTask.Progress = (Sum of (TodoItem.Progress × TodoItem.Weight)) / (Sum of TodoItem.Weight)
```

### **Example:**

```
ProjectTask: "Build Login Feature" (Weight: 100)
├─ TodoItem 1: "Design UI" (Weight: 30, Progress: 100%) → Approved
├─ TodoItem 2: "Create API" (Weight: 50, Progress: 100%) → Approved
└─ TodoItem 3: "Write Tests" (Weight: 20, Progress: 50%) → InProgress

Calculation:
Progress = (30×100 + 50×100 + 20×50) / (30 + 50 + 20)
         = (3000 + 5000 + 1000) / 100
         = 9000 / 100
         = 90%

Status: InProgress (because not all TodoItems are completed)
```

### **Key Rules:**

1. **Only APPROVED TodoItems** affect parent task progress
2. **Progress auto-updates** when TodoItem status changes
3. **Weights must be normalized** to sum to 100 (backend does this automatically)
4. **Parent task progress is read-only** (calculated from children)

---

## 🔌 **Available API Endpoints**

### **ProjectTask Endpoints:**

```typescript
// 1. ACCEPT TASK ASSIGNMENT (Assignee)
PUT /api/ProjectTask/{id}/accept
// Changes status: Pending → Accepted
// Sets AcceptedDate
// Auto-creates TodoItem if IsAutoCreateTodo = true

// 2. REJECT TASK ASSIGNMENT (Assignee)
PUT /api/ProjectTask/{id}/reject
Body: "Rejection reason here"
// Changes status: Pending → Rejected
// Sets RejectionReason

// 3. UPDATE PROGRESS (Assignee)
PUT /api/ProjectTask/{taskId}/progress
Body: { "progress": 75 }
// Updates progress (must have accepted TodoItem first)
// Auto-updates status based on progress

// 4. ACCEPT COMPLETION (Team Leader)
PUT /api/ProjectTask/{id}/acceptcompletion
// Changes status: WaitingForReview → Completed

// 5. REJECT COMPLETION (Team Leader)
PUT /api/ProjectTask/{id}/rejectcompletion
Body: "Needs improvement because..."
// Changes status: WaitingForReview → InProgress
// Sets RejectionReason
```

### **TodoItem Endpoints:**

```typescript
// 1. ACCEPT ASSIGNMENT (Assignee)
PUT /api/todoitems/{id}/acceptassignment
// Changes status: Pending → Accepted

// 2. REJECT ASSIGNMENT (Assignee)
PUT /api/todoitems/{id}/rejectassignment
Body: "Rejection reason"
// Changes status: Pending → Rejected

// 3. START TODO (Assignee)
PUT /api/todoitems/{id}/start
// Changes status: Accepted → InProgress

// 4. COMPLETE TODO (Assignee)
PUT /api/todoitems/{id}/complete?progress=100
Body: { 
  "detailsForLateCompletion": "Was delayed due to...",
  "completionDetails": "Completed with..."
}
// Changes status: InProgress → WaitingForReview

// 5. UPDATE PROGRESS (Assignee)
PUT /api/todoitems/{id}/progress
Body: { "progress": 50 }
// Updates progress while InProgress

// 6. APPROVE COMPLETION (Team Leader)
PUT /api/todoitems/{id}/acceptapproval
// Changes status: WaitingForReview → Approved
// **THIS IS WHEN PROGRESS AFFECTS PARENT TASK**

// 7. REJECT COMPLETION (Team Leader)
PUT /api/todoitems/{id}/rejectcompletion
Body: "Needs rework because..."
// Changes status: WaitingForReview → Reopened
```

---

## 🎨 **Missing UI Elements**

Based on backend analysis, here's what's MISSING in the current frontend:

### **1. TasksAssignedToMe - Detail View:**

#### **Missing: Accept/Reject Buttons**
```typescript
// For tasks with status: Pending
// Show two buttons:
[ Accept Task ]  [ Reject Task ]

// After rejection, show modal:
"Why are you rejecting this task?"
[Text area for rejection reason]
```

#### **Missing: Status Badge**
```typescript
// Show current task status prominently
Status: 🟡 Pending Acceptance
Status: 🟢 Accepted
Status: 🔴 Rejected
Status: 🔵 In Progress
Status: 🟠 Waiting for Review
Status: ✅ Completed
```

#### **Missing: TodoItems Accept/Reject**
```typescript
// For each TodoItem with status: Pending
TodoItem: "Create API endpoints"
Status: Pending
[ Accept ]  [ Reject ]

// For TodoItems with status: Accepted
TodoItem: "Create API endpoints"
Status: Accepted
[ Start Work ] ← Changes status to InProgress
```

#### **Missing: TodoItem Progress Update**
```typescript
// For TodoItems with status: InProgress
TodoItem: "Create API endpoints" (Weight: 50%)
Status: In Progress
Progress: [Slider 0-100%] 75%
[ Update Progress ]

// When ready:
[ Mark as Complete ] ← Changes status to WaitingForReview
Modal: "Any details about late completion?"
```

#### **Missing: Completion Actions**
```typescript
// When task progress = 100%
Your Progress: 100%
All TodoItems completed!

[ Submit for Review ] ← Changes task status to WaitingForReview

// Team Leader View (if you're TL):
Task is waiting for your review
[ Approve ] [ Reject ]
```

---

### **2. Progress Display:**

#### **Missing: Weight Display**
```typescript
// Show weight for each TodoItem
TodoItem: "Design UI"
Weight: 30 points (30% of task)
Progress: 100% ✅

// Show calculation
Total Task Progress: 90%
├─ Design UI (30 pts × 100%) = 30 pts ✅
├─ Create API (50 pts × 100%) = 50 pts ✅
└─ Write Tests (20 pts × 50%) = 10 pts ⏳
   = 90 / 100 = 90%
```

#### **Missing: Auto-Update Indicator**
```typescript
// Show that progress is auto-calculated
Progress: 90% (auto-calculated from TodoItems)
🔄 Updates automatically when TodoItems are approved
```

---

### **3. Rejection Reason Display:**

```typescript
// If task was rejected
❌ Task Rejected
Reason: "Need more clarification on requirements"
Rejected by: John Doe (Team Leader)
Rejected on: Oct 13, 2025

// For TodoItems
❌ TodoItem Rejected
Reason: "Please use TypeScript instead of JavaScript"
```

---

## 🚀 **Implementation Plan**

### **Phase 1: Status Management**

1. **Add Status Display**
   - Create status badge component with colors
   - Show current status on task detail
   - Show status on TodoItem list

2. **Add Accept/Reject UI**
   - Accept button (green)
   - Reject button (red) with reason modal
   - Show only for Pending status
   - Disable after action taken

### **Phase 2: TodoItem Workflow**

1. **TodoItem Status Actions**
   - Accept/Reject buttons
   - Start Work button
   - Mark Complete button
   - Progress slider (for InProgress status)

2. **TodoItem CRUD**
   - Already implemented ✅
   - Just need to add status management

### **Phase 3: Progress Auto-Calculation**

1. **Display Calculation**
   - Show weighted progress formula
   - Show each TodoItem contribution
   - Real-time updates

2. **Backend Integration**
   - Fetch TodoItems for task
   - Listen for TodoItem updates
   - Refresh task progress

### **Phase 4: Team Leader Actions**

1. **Approval UI**
   - Approve/Reject buttons
   - Only show for WaitingForReview status
   - Only show for Team Leaders

2. **Rejection Modal**
   - Text area for reason
   - Submit rejection with reason

---

## 📝 **Recommended UI Layout**

```
┌──────────────────────────────────────────────────────────┐
│ Task: Build Login Feature                                │
│ Status: 🔵 In Progress                                   │
│ ┌──────────────────────────────────────────────────────┐ │
│ │ [Accept Task] [Reject Task]  (if Pending)            │ │
│ │ [Submit for Review]          (if progress = 100%)    │ │
│ └──────────────────────────────────────────────────────┘ │
│                                                          │
│ Progress: 90% (auto-calculated from TodoItems) 🔄        │
│ ▓▓▓▓▓▓▓▓▓░░                                             │
│                                                          │
│ Details:                                                 │
│ • Weight: 100 points                                     │
│ • Created: Oct 13, 2025                                  │
│ • Accepted: Oct 13, 2025 ✅                              │
│                                                          │
│ ┌──────────────────────────────────────────────────────┐ │
│ │ TodoItems (3)                                         │ │
│ │                                                       │ │
│ │ ✅ Design UI (30 pts, 100%) - Approved               │ │
│ │ ✅ Create API (50 pts, 100%) - Approved              │ │
│ │ ⏳ Write Tests (20 pts, 50%) - In Progress           │ │
│ │    Progress: [Slider] 50%                            │ │
│ │    [Update Progress] [Mark Complete]                 │ │
│ └──────────────────────────────────────────────────────┘ │
│                                                          │
│ Calculation:                                             │
│ (30×100 + 50×100 + 20×50) / 100 = 90%                   │
└──────────────────────────────────────────────────────────┘
```

---

## ✅ **Next Steps**

1. **Review this plan** - Do you want to proceed?
2. **Prioritize phases** - Which phase should we implement first?
3. **Start implementation** - I'll create the UI components and integrate with backend

**Recommendation:** Start with **Phase 1 (Status Management)** and **Phase 2 (TodoItem Workflow)** as they're the foundation for everything else.

Would you like me to proceed with the implementation?

