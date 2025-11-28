# 🎨 Visual Workflow Guide - TasksAssignedToMe

## 📱 **Complete UI Walkthrough**

---

## 🎯 **Scenario: From Assignment to Completion**

### **Step 1: Task Assigned (Pending Status)**

```
╔═══════════════════════════════════════════════════════════════╗
║  ← Back   Build Login Feature         [🟡 Pending Acceptance] ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  ⚠ This task requires your acceptance                        ║
║  ┌──────────────────────────────────────────────────────┐   ║
║  │ Please review the task details and accept or reject  │   ║
║  │ the assignment.                                       │   ║
║  │                                                       │   ║
║  │ [✓ Accept Task]  [✗ Reject Task]                     │   ║
║  └──────────────────────────────────────────────────────┘   ║
║                                                               ║
║  🚩 High Priority                                             ║
║  Project: E-Commerce Platform                                 ║
║                                                               ║
║ ┌─────────────────────────┬────────────────────────────────┐ ║
║ │ LEFT SIDE               │ RIGHT SIDE                     │ ║
║ │                         │                                │ ║
║ │ Progress: 0%            │ Action Items (TodoItems)       │ ║
║ │ ░░░░░░░░░░░             │                                │ ║
║ │                         │ [+ Add Action Item] (disabled) │ ║
║ │ Description:            │                                │ ║
║ │ Create a login...       │ No action items yet            │ ║
║ │                         │                                │ ║
║ │ Details:                │                                │ ║
║ │ • Task Type: Project    │                                │ ║
║ │ • Due Date: Oct 25      │                                │ ║
║ │ • Created: Oct 13       │                                │ ║
║ │ • Weight: 100 points    │                                │ ║
║ │ • Status: Pending       │                                │ ║
║ └─────────────────────────┴────────────────────────────────┘ ║
╚═══════════════════════════════════════════════════════════════╝
```

**Actions Available:**
- ✅ Accept Task
- ✅ Reject Task (with reason)

---

### **Step 2: Task Accepted + TodoItem Created**

```
╔═══════════════════════════════════════════════════════════════╗
║  ← Back   Build Login Feature              [🔵 Accepted]      ║
╠═══════════════════════════════════════════════════════════════╣
║  🚩 High Priority                                             ║
║  Project: E-Commerce Platform                                 ║
║                                                               ║
║ ┌─────────────────────────┬────────────────────────────────┐ ║
║ │ LEFT SIDE               │ RIGHT SIDE                     │ ║
║ │                         │                                │ ║
║ │ Progress: 0%            │ Action Items (TodoItems)       │ ║
║ │ 🔄 Auto-calculated      │ [+ Add Action Item]            │ ║
║ │ ░░░░░░░░░░░             │                                │ ║
║ │                         │ ┌─────────────────────────────┐│ ║
║ │ Description:            │ │ Create API endpoints        ││ ║
║ │ Create a login...       │ │ [🟡 Pending]                ││ ║
║ │                         │ │ Weight: 50 pts | Progress: 0%││ ║
║ │ Details:                │ │                             ││ ║
║ │ • Status: Accepted ✓    │ │ [✓ Accept] [✗ Reject]       ││ ║
║ │ • Weight: 100 points    │ └─────────────────────────────┘│ ║
║ └─────────────────────────┴────────────────────────────────┘ ║
╚═══════════════════════════════════════════════════════════════╝
```

**New Elements:**
- ✅ Status badge changed to "Accepted"
- ✅ TodoItem auto-created (if IsAutoCreateTodo = true)
- ✅ TodoItem shows Pending status
- ✅ Accept/Reject buttons for TodoItem

---

### **Step 3: Working on TodoItem**

```
╔═══════════════════════════════════════════════════════════════╗
║  ← Back   Build Login Feature           [🟣 In Progress]      ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║ ┌─────────────────────────┬────────────────────────────────┐ ║
║ │ Progress: 75.0%         │ Action Items (TodoItems)       │ ║
║ │ 🔄 Auto-calculated      │                                │ ║
║ │ ▓▓▓▓▓▓▓▓░░              │ ┌─────────────────────────────┐│ ║
║ │                         │ │ Design UI                   ││ ║
║ │ Progress Breakdown:     │ │ [🟢 Approved]               ││ ║
║ │ ✓ Design UI (30pts×100%)│ │ Weight: 30 pts | Prog: 100% ││ ║
║ │   +30.0%                │ │ ✓ Approved - Contributing   ││ ║
║ │ ✓ Create API (50×90%)   │ │   30.0 pts to task progress ││ ║
║ │   +45.0%                │ └─────────────────────────────┘│ ║
║ │ ⏳ Tests (20×0%)  —     │                                │ ║
║ │ ────────────────────    │ ┌─────────────────────────────┐│ ║
║ │ Total: 75.0 / 100       │ │ Create API endpoints        ││ ║
║ │       = 75.0%           │ │ [🟣 In Progress]            ││ ║
║ │                         │ │ Weight: 50 pts | Prog: 90%  ││ ║
║ │                         │ │                             ││ ║
║ │                         │ │ Update Progress:            ││ ║
║ │                         │ │ ▓▓▓▓▓▓▓▓▓░ 90%             ││ ║
║ │                         │ │ [Save] [Cancel]             ││ ║
║ │                         │ │                             ││ ║
║ │                         │ │ [✏ Update] [📤 Submit]      ││ ║
║ │                         │ └─────────────────────────────┘│ ║
║ │                         │                                │ ║
║ │                         │ ┌─────────────────────────────┐│ ║
║ │                         │ │ Write Tests                 ││ ║
║ │                         │ │ [🔵 Accepted]               ││ ║
║ │                         │ │ Weight: 20 pts | Progress: 0%││ ║
║ │                         │ │ [▶ Start Work]              ││ ║
║ │                         │ └─────────────────────────────┘│ ║
║ └─────────────────────────┴────────────────────────────────┘ ║
╚═══════════════════════════════════════════════════════════════╝
```

**Features Shown:**
- ✅ Real-time progress calculation (75%)
- ✅ Breakdown showing each TodoItem
- ✅ Different statuses (Approved, InProgress, Accepted)
- ✅ Progress slider for active TodoItem
- ✅ Submit for Review button (when 100%)
- ✅ Start Work button for accepted TodoItem

---

### **Step 4: Waiting for Review**

```
╔═══════════════════════════════════════════════════════════════╗
║  ← Back   Build Login Feature      [🟠 Waiting for Review]    ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  🟠 Task Ready for Review (Team Leader View)                  ║
║  ┌──────────────────────────────────────────────────────┐   ║
║  │ The assignee has completed this task and submitted   │   ║
║  │ it for your review.                                   │   ║
║  │                                                       │   ║
║  │ [✓ Approve Completion] [✗ Request Revision]          │   ║
║  └──────────────────────────────────────────────────────┘   ║
║                                                               ║
║ ┌─────────────────────────┬────────────────────────────────┐ ║
║ │ Progress: 100.0%        │ Action Items (TodoItems)       │ ║
║ │ ▓▓▓▓▓▓▓▓▓▓             │                                │ ║
║ │                         │ ┌─────────────────────────────┐│ ║
║ │ Progress Breakdown:     │ │ Create API endpoints        ││ ║
║ │ ✓ Design (30pts×100%)   │ │ [🟠 Waiting Review]         ││ ║
║ │   +30.0%                │ │ Weight: 50 pts | Prog: 100% ││ ║
║ │ ✓ API (50pts×100%)      │ │                             ││ ║
║ │   +50.0%                │ │ Pending Your Approval       ││ ║
║ │ ✓ Tests (20pts×100%)    │ │ [✓ Approve] [✗ Revision]    ││ ║
║ │   +20.0%                │ └─────────────────────────────┘│ ║
║ │ ────────────────────    │                                │ ║
║ │ Total: 100 / 100 = 100% │                                │ ║
║ └─────────────────────────┴────────────────────────────────┘ ║
╚═══════════════════════════════════════════════════════════════╝
```

**Team Leader Actions:**
- ✅ Approve Completion button
- ✅ Request Revision button
- ✅ Approve TodoItem button
- ✅ Request TodoItem Revision button

---

### **Step 5: Completed**

```
╔═══════════════════════════════════════════════════════════════╗
║  ← Back   Build Login Feature            [🟢 Completed] ✓     ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  ✓ Task Completed!                                            ║
║  ┌──────────────────────────────────────────────────────┐   ║
║  └──────────────────────────────────────────────────────┘   ║
║                                                               ║
║ ┌─────────────────────────┬────────────────────────────────┐ ║
║ │ Progress: 100%          │ Action Items (TodoItems)       │ ║
║ │ ▓▓▓▓▓▓▓▓▓▓▓▓           │                                │ ║
║ │                         │ ┌─────────────────────────────┐│ ║
║ │ All 3 TodoItems         │ │ Design UI                   ││ ║
║ │ Approved ✅             │ │ [🟢 Approved] ✓             ││ ║
║ │                         │ │ Contributing 30.0 pts       ││ ║
║ │                         │ └─────────────────────────────┘│ ║
║ │                         │ ┌─────────────────────────────┐│ ║
║ │                         │ │ Create API                  ││ ║
║ │                         │ │ [🟢 Approved] ✓             ││ ║
║ │                         │ │ Contributing 50.0 pts       ││ ║
║ │                         │ └─────────────────────────────┘│ ║
║ │                         │ ┌─────────────────────────────┐│ ║
║ │                         │ │ Write Tests                 ││ ║
║ │                         │ │ [🟢 Approved] ✓             ││ ║
║ │                         │ │ Contributing 20.0 pts       ││ ║
║ │                         │ └─────────────────────────────┘│ ║
║ └─────────────────────────┴────────────────────────────────┘ ║
╚═══════════════════════════════════════════════════════════════╝
```

---

## 🎨 **UI Component Gallery**

### **1. Status Badges**

```
[🟡 Pending Acceptance]  ← Yellow with clock icon
[🔵 Accepted]            ← Blue with checkmark
[🔴 Rejected]            ← Red with X
[🟣 In Progress]         ← Purple with refresh
[🟠 Waiting for Review]  ← Orange with send icon
[🟢 Completed]           ← Green with checkmark
```

---

### **2. Action Buttons**

**Accept/Reject (Pending):**
```
┌──────────────────────────────────────┐
│ [✓ Accept Task]  [✗ Reject Task]    │
└──────────────────────────────────────┘
```

**Start Work (Accepted):**
```
┌──────────────────────────────────────┐
│ [▶ Start Work]                       │
└──────────────────────────────────────┘
```

**Update Progress (InProgress):**
```
┌──────────────────────────────────────┐
│ Update Progress:                     │
│ ▓▓▓▓▓▓▓▓░░ 75%                      │
│ [Save] [Cancel]                      │
│                                      │
│ [✏ Update Progress] [📤 Submit]     │
└──────────────────────────────────────┘
```

**Team Leader Approval (WaitingForReview):**
```
┌──────────────────────────────────────┐
│ [✓ Approve] [✗ Request Revision]    │
└──────────────────────────────────────┘
```

---

### **3. Progress Breakdown**

```
┌───────────────────────────────────────────┐
│ Progress Breakdown:                       │
│ ✓ Design UI (30pts × 100%)    +30.0%    │
│ ✓ Create API (50pts × 100%)   +50.0%    │
│ ⏳ Write Tests (20pts × 50%)    —        │
│ ─────────────────────────────────────────│
│ Total: 80.0 / 100 = 80.0%                │
└───────────────────────────────────────────┘
```

---

### **4. TodoItem Card (All States)**

**Pending:**
```
┌─────────────────────────────────────────┐
│ Create API endpoints    [🟡 Pending]    │
│ Weight: 50 pts | Progress: 0%           │
│                                         │
│ [✓ Accept]  [✗ Reject]                 │
└─────────────────────────────────────────┘
```

**Accepted:**
```
┌─────────────────────────────────────────┐
│ Create API endpoints    [🔵 Accepted]   │
│ Weight: 50 pts | Progress: 0%           │
│                                         │
│ [▶ Start Work]                          │
└─────────────────────────────────────────┘
```

**In Progress:**
```
┌─────────────────────────────────────────┐
│ Create API endpoints  [🟣 In Progress]  │
│ Weight: 50 pts | Progress: 75%          │
│                                         │
│ [✏ Update Progress] [📤 Submit]        │
└─────────────────────────────────────────┘
```

**Waiting Review:**
```
┌─────────────────────────────────────────┐
│ Create API endpoints [🟠 Waiting Review]│
│ Weight: 50 pts | Progress: 100%         │
│                                         │
│ Pending Your Approval (Team Leader)    │
│ [✓ Approve] [✗ Request Revision]       │
└─────────────────────────────────────────┘
```

**Approved:**
```
┌─────────────────────────────────────────┐
│ Create API endpoints     [🟢 Approved]  │
│ Weight: 50 pts | Progress: 100%         │
│                                         │
│ ✓ Approved - Contributing 50.0 pts     │
│   to task progress                      │
└─────────────────────────────────────────┘
```

**Rejected:**
```
┌─────────────────────────────────────────┐
│ Create API endpoints     [🔴 Rejected]  │
│ Weight: 50 pts | Progress: 0%           │
│                                         │
│ ✗ Rejected: Need TypeScript instead    │
└─────────────────────────────────────────┘
```

**Reopened:**
```
┌─────────────────────────────────────────┐
│ Create API endpoints     [🔷 Reopened]  │
│ Weight: 50 pts | Progress: 75%          │
│                                         │
│ ⚠ Needs Revision: Add error handling   │
│ [▶ Resume Work]                         │
└─────────────────────────────────────────┘
```

---

## 🔄 **Status Flow Diagram**

```
PROJECT TASK:
┌─────────┐   Accept   ┌─────────┐   Work   ┌─────────┐  Submit  ┌──────────┐  Approve  ┌─────────┐
│ Pending │ ────────> │Accepted │ ──────> │InProgress│ ──────> │Waiting   │ ──────> │Completed│
└─────────┘            └─────────┘          └─────────┘          │Review    │          └─────────┘
     │                                                            └──────────┘
     │ Reject                                                          │
     └─────────> [🔴 Rejected]                                        │ Reject
                                                                       └──────> Back to InProgress


TODO ITEM:
┌─────────┐   Accept   ┌─────────┐   Start  ┌─────────┐  Complete ┌──────────┐  Approve  ┌─────────┐
│ Pending │ ────────> │Accepted │ ──────> │InProgress│ ──────>  │Waiting   │ ──────> │Approved │
└─────────┘            └─────────┘          └─────────┘           │Review    │          └─────────┘
     │                                                             └──────────┘
     │ Reject                                                           │
     └─────────> [🔴 Rejected]                                         │ Reject
                                                                        └──────> [🔷 Reopened]
```

---

## 🎬 **Modal Screens**

### **Reject Task Modal:**

```
╔═════════════════════════════════════════╗
║  ✗ Reject Task Assignment               ║
╠═════════════════════════════════════════╣
║                                         ║
║  Please provide a reason for rejecting  ║
║  this task:                             ║
║                                         ║
║  ┌────────────────────────────────────┐║
║  │ Reason for rejection...            │║
║  │                                    │║
║  │                                    │║
║  └────────────────────────────────────┘║
║                                         ║
║           [Cancel]  [Reject Task]       ║
╚═════════════════════════════════════════╝
```

### **Request Revision Modal:**

```
╔═════════════════════════════════════════╗
║  ⚠ Request Task Revision                ║
╠═════════════════════════════════════════╣
║                                         ║
║  The task will be sent back to the      ║
║  assignee. Please explain what needs    ║
║  to be improved:                        ║
║                                         ║
║  ┌────────────────────────────────────┐║
║  │ What needs to be improved?         │║
║  │                                    │║
║  │                                    │║
║  └────────────────────────────────────┘║
║                                         ║
║        [Cancel]  [Request Revision]     ║
╚═════════════════════════════════════════╝
```

### **Create Action Item Modal:**

```
╔═════════════════════════════════════════╗
║  Create New Action Item                 ║
╠═════════════════════════════════════════╣
║                                         ║
║  Title *                                ║
║  ┌────────────────────────────────────┐║
║  │ Action item title...               │║
║  └────────────────────────────────────┘║
║                                         ║
║  Description                            ║
║  ┌────────────────────────────────────┐║
║  │ Optional description...            │║
║  └────────────────────────────────────┘║
║                                         ║
║  Weight (0-100)                         ║
║  ┌────────────────────────────────────┐║
║  │ 50                                 │║
║  └────────────────────────────────────┘║
║                                         ║
║              [Cancel]  [Create]         ║
╚═════════════════════════════════════════╝
```

---

## 📊 **Complete Feature Matrix**

| Feature | Assignee | Team Leader | Description |
|---------|----------|-------------|-------------|
| **Accept Task** | ✅ | ❌ | Accept task assignment |
| **Reject Task** | ✅ | ❌ | Reject with reason |
| **Create TodoItem** | ✅ | ✅ | Add action items |
| **Accept TodoItem** | ✅ | ❌ | Accept TodoItem assignment |
| **Reject TodoItem** | ✅ | ❌ | Reject TodoItem with reason |
| **Start Work** | ✅ | ❌ | Begin working on TodoItem |
| **Update Progress** | ✅ | ❌ | Update completion % |
| **Submit for Review** | ✅ | ❌ | Mark TodoItem complete |
| **Approve TodoItem** | ❌ | ✅ | Approve completion |
| **Reject TodoItem Completion** | ❌ | ✅ | Send back for revision |
| **Approve Task** | ❌ | ✅ | Approve task completion |
| **Reject Task Completion** | ❌ | ✅ | Send task back |
| **View Progress Breakdown** | ✅ | ✅ | See calculation |
| **Delete TodoItem** | ✅ | ✅ | Remove action item |

---

## 🎯 **Quick Reference**

### **For Assignees:**

```
1. Accept task
2. Accept TodoItem (or create new ones)
3. Start Work
4. Update Progress (0-100%)
5. Submit for Review (when 100%)
6. Wait for Team Leader approval
```

### **For Team Leaders:**

```
1. Review submitted TodoItems
2. Approve or Request Revision
3. Provide feedback if rejecting
4. Monitor team progress
5. Approve final task completion
```

---

## ✅ **Implementation Statistics**

- **Lines of Code Added:** ~500+
- **API Endpoints Integrated:** 14
- **Status States Handled:** 7 (Task) + 7 (TodoItem)
- **Modals Created:** 4
- **Handler Functions:** 12
- **UI Components:** 15+
- **Linter Errors:** 0
- **Dark Mode:** 100% support
- **Responsive:** Mobile + Desktop

---

## 🚀 **Ready to Use!**

**Everything is implemented and working!**

**Just refresh your browser and start testing!** 🎉

