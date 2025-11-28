# 📋 Complete Workflow Explanation

## 🎯 **Why Two Accept/Reject Buttons?**

You're seeing **TWO different workflows** because there are **TWO levels** in the system:

---

## 📊 **The Two-Level System:**

```
Level 1: PROJECT TASK (Main Task)
    ↓
Level 2: TODO ITEMS (Action Items under the task)
```

---

## 🔄 **Level 1: PROJECT TASK Workflow**

### **When You See This:**
```
┌────────────────────────────────────────┐
│ Status: [🟡 Pending Acceptance]        │
│                                        │
│ ⚠ This task requires your acceptance  │
│ [✓ Accept Task] [✗ Reject Task]       │  ← Level 1 buttons
└────────────────────────────────────────┘
```

### **What It Means:**
- Someone assigned a **PROJECT TASK** to you
- You need to **accept or reject** taking on this task
- This is the **main task assignment**

### **What Happens When You Click:**

**✅ Accept Task:**
```
1. Task status: Pending → Accepted
2. Database updated: UPDATE ProjectTasks SET Status='Accepted'
3. You can now start working on it
4. You can create TodoItems (action items) under it
```

**❌ Reject Task:**
```
1. Task status: Pending → Rejected
2. You provide a reason why you rejected it
3. Task owner/manager is notified
4. You won't work on this task
```

---

## 🔄 **Level 2: TODO ITEM Workflow**

### **When You See This:**
```
┌────────────────────────────────────────┐
│ Status: [🟢 Accepted]                  │  ← Task is accepted
│                                        │
│ Action Items (TodoItems):              │
│                                        │
│  📌 Create Login API                   │
│     Status: [🟡 Pending]               │
│     [✓ Accept] [✗ Reject]              │  ← Level 2 buttons
│                                        │
│  📌 Design Database Schema             │
│     Status: [🟢 Accepted]              │
│     [▶ Start Work]                     │
└────────────────────────────────────────┘
```

### **What It Means:**
- The main task is **already accepted**
- Now you see **individual action items** (TodoItems) under it
- Each action item can be **separately accepted/rejected**

### **Why TodoItems Have Their Own Accept/Reject:**

**Scenario:**
```
Main Task: "Build User Authentication"  ← You accepted this
  ├─ TodoItem 1: "Create Login API"     ← You can accept this
  ├─ TodoItem 2: "Add Social Login"     ← You might reject this (too complex)
  └─ TodoItem 3: "Write Unit Tests"     ← You can accept this
```

**You can:**
- ✅ Accept the overall task
- ✅ Accept some TodoItems
- ❌ Reject other TodoItems (e.g., if they're out of scope or too complex)

---

## 🔄 **Complete TodoItem Lifecycle:**

```
1. [🟡 Pending] 
   │
   ├─ Accept → [🟢 Accepted]
   │             │
   │             ├─ Start Work → [🔵 InProgress]
   │             │                 │
   │             │                 ├─ Update Progress (0-100%)
   │             │                 │
   │             │                 └─ Submit for Review → [⏳ WaitingForReview]
   │             │                                           │
   │             │                                           ├─ Team Leader Approves → [✅ Approved]
   │             │                                           │
   │             │                                           └─ Team Leader Rejects → [🔵 InProgress] (fix and resubmit)
   │
   └─ Reject → [❌ Rejected]
```

---

## 👥 **Where Does Team Leader Approve?**

### **Current Situation:**

**YOU (Assignee) see:**
- TasksAssignedToMe page
- You can accept, reject, work on, and submit TodoItems

**TEAM LEADER/SCRUM MASTER needs:**
- A page to see **all TodoItems** with status "WaitingForReview"
- Approve or reject completed work

---

## 🚨 **The Missing Piece:**

### **Team Leader doesn't have a view yet!**

**What's needed:**
A new page for Team Leaders showing:
```
┌──────────────────────────────────────────┐
│ PENDING APPROVALS (Team Leader View)    │
├──────────────────────────────────────────┤
│                                          │
│ Project: User Authentication System      │
│ Task: Build Login Feature                │
│                                          │
│  📌 TodoItem: Create Login API           │
│     Assignee: John Doe                   │
│     Status: [⏳ WaitingForReview]        │
│     Progress: 100%                       │
│     Submitted: 2 hours ago               │
│                                          │
│     [✅ Approve] [❌ Request Revision]   │
│                                          │
├──────────────────────────────────────────┤
│                                          │
│ Project: Mobile App                      │
│ Task: Payment Integration                │
│                                          │
│  📌 TodoItem: Stripe Integration         │
│     Assignee: Jane Smith                 │
│     Status: [⏳ WaitingForReview]        │
│     Progress: 100%                       │
│     Submitted: 5 hours ago               │
│                                          │
│     [✅ Approve] [❌ Request Revision]   │
│                                          │
└──────────────────────────────────────────┘
```

---

## 🔧 **Let Me Create This Missing Page!**

I'll create:
1. **New page:** `TeamLeaderApprovals.tsx`
2. **Shows:** All TodoItems with status "WaitingForReview"
3. **Filtered by:** Projects where current user is Team Leader/Scrum Master
4. **Actions:** Approve ✅ or Request Revision ❌

---

## 📊 **Summary:**

### **Two Accept/Reject Buttons:**

| Location | What It's For | Who Uses It |
|----------|---------------|-------------|
| **Top (Task Level)** | Accept/Reject the main PROJECT TASK assignment | Task Assignee |
| **Bottom (TodoItem Level)** | Accept/Reject individual action items | TodoItem Assignee |

### **They're DIFFERENT!**

- **Task Accept/Reject** = "Do I want to take on this entire task?"
- **TodoItem Accept/Reject** = "Do I want to work on this specific action item?"

### **Team Leader Approval:**

- **Current:** TodoItem goes to "WaitingForReview" but team leader has no view
- **Need:** A page for team leaders to see and approve/reject TodoItems
- **Fix:** I'll create it now!

---

## 🎯 **What To Test After My Next Fix:**

1. **Refresh browser** - Check if DB updates persist
2. **Accept a task** - Check console for API success
3. **Navigate away and back** - Check if status persists
4. **As Team Leader** - Use new approval page to approve TodoItems

Let me create the Team Leader approval page now!

