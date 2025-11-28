# Task Filtering Logic - Authored vs Delegated

## 📋 **Two Different Pages, Two Different Filters**

---

## 📝 **MyTasks.tsx - AUTHORED TASKS**

**Purpose:** Show tasks YOU created (as the author)

### **Filtering Logic:**

```typescript
// Check if task was created by current user
if (hasCreatedBy) {
  // Task has CreatedByUserId field populated
  shouldShow = isCreatedByUser;  // ✅ Show if YOU created it
} else {
  // Task doesn't have CreatedByUserId (old task)
  // Backward compatibility: Show if NOT assigned to you
  shouldShow = !isAssignedToCurrentUser || !task.assignee;
}
```

### **What It Shows:**

✅ Tasks where `CreatedByUserId` = Your ID  
✅ Tasks without `CreatedByUserId` that are NOT assigned to you (likely you created them)  
❌ Tasks assigned TO you (these go in TasksAssignedToMe)  
❌ Personal todo tasks

### **Console Output:**

```javascript
📝 MyTasks - AUTHORED TASKS FILTERING
✅ AUTHORED tasks (created by me): 5
📊 Tasks assigned to me: 3  (won't show in this view)
```

---

## 👤 **TasksAssignedToMe.tsx - DELEGATED TASKS**

**Purpose:** Show tasks DELEGATED to you (assigned to you)

### **Filtering Logic:**

```typescript
// Check if task is assigned to current user
const assigneeId = (task.assignee || '').toLowerCase();
const isAssignedToMe = assigneeId === currentUserId;

return isAssignedToMe;  // ✅ Show if assigned TO you
```

### **What It Shows:**

✅ Tasks where `assignee` = Your ID  
❌ Tasks you created but assigned to others  
❌ Personal todo tasks

### **Console Output:**

```javascript
🔍 TasksAssignedToMe - FILTERING SUMMARY
🔍 Assigned to me: 8
```

---

## 📊 **Comparison Table:**

| Scenario | MyTasks (Authored) | TasksAssignedToMe (Delegated) |
|----------|-------------------|-------------------------------|
| You created task, assigned to yourself | ✅ Shows | ✅ Shows |
| You created task, assigned to John | ✅ Shows | ❌ Hidden |
| John created task, assigned to you | ❌ Hidden | ✅ Shows |
| Old task (no creator), assigned to you | ❌ Hidden | ✅ Shows |
| Old task (no creator), NOT assigned to you | ✅ Shows | ❌ Hidden |

---

## 🎯 **Use Cases:**

### **Scenario 1: Manager Creates Task for Team Member**

```
Manager (You):
  - Creates "Review Report" task
  - Assigns to: Alice

Result:
  - MyTasks (Manager): ✅ Shows "Review Report"
  - TasksAssignedToMe (Alice): ✅ Shows "Review Report"
```

---

### **Scenario 2: You Create Task for Yourself**

```
You:
  - Creates "Finish Documentation" task
  - Assigns to: Yourself

Result:
  - MyTasks (You): ✅ Shows "Finish Documentation"
  - TasksAssignedToMe (You): ✅ Shows "Finish Documentation"
```

---

### **Scenario 3: Someone Assigns Task to You**

```
Bob:
  - Creates "Fix Bug" task
  - Assigns to: You

Result:
  - MyTasks (You): ❌ Hidden (you didn't create it)
  - TasksAssignedToMe (You): ✅ Shows "Fix Bug"
  - MyTasks (Bob): ✅ Shows "Fix Bug" (he created it)
```

---

## 🔧 **Code Locations:**

### **MyTasks.tsx** (lines 153-163)
```typescript
// AUTHORED TASKS: Show ONLY tasks created by current user
if (hasCreatedBy) {
  shouldShow = isCreatedByUser;
} else {
  shouldShow = !isAssignedToCurrentUser || !task.assignee;
}
```

### **TasksAssignedToMe.tsx** (lines 78-84)
```typescript
// DELEGATED TASKS: Show tasks assigned to current user
const assigneeId = (task.assignee || '').toLowerCase();
const isAssignedToMe = assigneeId === currentUserId;
return isAssignedToMe;
```

---

## 🧪 **How to Test:**

### **Test 1: Create Task for Someone Else**

1. Go to **MyTasks**
2. Click "Create Task"
3. Assign to: Alice (not you!)
4. Create task

**Expected:**
- ✅ Task appears in YOUR MyTasks (authored)
- ❌ Task does NOT appear in YOUR TasksAssignedToMe (not assigned to you)
- ✅ Task appears in ALICE's TasksAssignedToMe (assigned to her)

---

### **Test 2: Create Task for Yourself**

1. Go to **MyTasks**
2. Click "Create Task"
3. Assign to: Yourself
4. Create task

**Expected:**
- ✅ Task appears in YOUR MyTasks (authored)
- ✅ Task appears in YOUR TasksAssignedToMe (assigned to you)

---

### **Test 3: Someone Creates Task for You**

1. Have someone else create a task
2. They assign it to you

**Expected:**
- ❌ Task does NOT appear in YOUR MyTasks (you didn't create it)
- ✅ Task appears in YOUR TasksAssignedToMe (assigned to you)
- ✅ Task appears in THEIR MyTasks (they created it)

---

## ✅ **Benefits:**

| Benefit | Description |
|---------|-------------|
| **Clear Separation** | Authored vs Assigned tasks are distinct |
| **Audit Trail** | Easy to see who created what |
| **Workload Visibility** | See what you're responsible for vs what you delegated |
| **No Duplicates** | Tasks don't appear in wrong views |

---

## 🎯 **Summary:**

- **MyTasks** = "What I CREATED" (my authored tasks)
- **TasksAssignedToMe** = "What I NEED TO DO" (delegated to me)

This matches standard task management UX patterns! ✅

