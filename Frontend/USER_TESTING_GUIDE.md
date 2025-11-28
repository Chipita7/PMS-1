# 🧪 **User Testing Guide: Project Filtering & Accept/Reject**

## 🎯 **What to Test:**

### **1. Authored Projects Page (MyProjects.tsx)**
**Location:** `/dashboard/user/projects/mine` or similar

**Expected Behavior:**
- ✅ Shows ONLY projects **you created**
- ✅ Hides projects created by other users
- ✅ Console shows filtering logs

---

### **2. Delegated Projects Page (DelegatedAssignments.tsx)**
**Location:** `/dashboard/user/milestone/delegated` or similar

**Expected Behavior:**
- ✅ Shows projects **assigned to you**
- ✅ Each project has **Accept** and **Reject** buttons
- ✅ Accept shows confirmation dialog
- ✅ Reject requires you to enter a reason

---

## 📋 **Test Scenarios:**

### **Scenario 1: Filter Authored Projects**

**Steps:**
1. Create a new project as User A (e.g., john@example.com)
2. Login as User A
3. Navigate to "My Projects" (Authored Projects)
4. **✅ Expected:** See the project you just created
5. Open browser console (F12)
6. **✅ Expected:** See logs like:
   ```
   📊 Converted projects: 15 projects
   🔍 Checking project: "Your Project" createdBy: "john@example.com"
   🔍 Logged-in user: "john@example.com" or "308527a8..." or "EMP12345"
   ✅ Filtered projects (created by me): 3 out of 15
   ```

**What to Check:**
- [ ] Only YOUR projects appear
- [ ] Other users' projects are hidden
- [ ] Console shows correct filtering

---

### **Scenario 2: Accept a Project Assignment**

**Setup:**
1. As admin/manager, assign a project to User B
2. Login as User B
3. Navigate to "Delegated to Me"

**Steps:**
1. Find the assigned project
2. Click the green **"✓ Accept"** button
3. **✅ Expected:** Confirmation dialog appears showing:
   - Project name
   - Your role (e.g., "Scrum Master", "Team Leader")
   - Department
4. Click **"✓ Accept Assignment"**
5. **✅ Expected:**
   - Success message appears
   - Page refreshes
   - Assignment status updates

**What to Check:**
- [ ] Accept button is visible
- [ ] Confirmation dialog shows correct details
- [ ] Success message appears
- [ ] Backend receives approve API call

---

### **Scenario 3: Reject a Project Assignment**

**Setup:**
1. As admin/manager, assign another project to User B
2. Login as User B
3. Navigate to "Delegated to Me"

**Steps:**
1. Find the assigned project
2. Click the red **"✗ Reject"** button
3. **✅ Expected:** Rejection dialog appears with:
   - Project name
   - Your role
   - Text area for reason
4. Type a rejection reason:
   ```
   "I don't have capacity for this project right now."
   ```
5. **✅ Expected:** Reject button becomes enabled
6. Click **"✗ Reject Assignment"**
7. **✅ Expected:**
   - Success message appears
   - Page refreshes
   - Assignment status updates to rejected

**What to Check:**
- [ ] Reject button is visible
- [ ] Reason field is required (reject button disabled until filled)
- [ ] Success message appears
- [ ] Backend receives reject API call with reason

---

## 🔍 **Console Debugging:**

### **Open Browser Console (F12 → Console tab)**

**For Authored Projects:**
```javascript
// You should see:
📊 Converted projects: 15 projects
🔍 Checking project: "Website Redesign" createdBy: "john@example.com"
🔍 Logged-in user: "john@example.com" or "308527a8-9d38..." or "EMP12345"
🔍 Checking project: "Mobile App" createdBy: "jane@example.com"
🔍 Logged-in user: "john@example.com" or "308527a8-9d38..." or "EMP12345"
✅ Filtered projects (created by me): 3 out of 15
```

**For Accept Assignment:**
```javascript
// You should see:
✅ Approving assignment: 123
✅ Assignment approved successfully
📡 Loading assignments for user ID: john@example.com
✅ Project assignments found: 5
```

**For Reject Assignment:**
```javascript
// You should see:
❌ Rejecting assignment: 456 Reason: "I don't have capacity..."
✅ Assignment rejected successfully
📡 Loading assignments for user ID: john@example.com
✅ Project assignments found: 4
```

---

## 🌐 **Network Tab Debugging:**

### **Open Network Tab (F12 → Network tab)**

**Filter:** XHR/Fetch

**For Accept:**
```
Request:
PUT /api/ProjectAssignment/123/approve

Response:
{
  "success": true,
  "message": "Assignment approved successfully",
  "data": { ... }
}
```

**For Reject:**
```
Request:
PUT /api/ProjectAssignment/456/reject
Body: "I don't have capacity for this project right now."

Response:
{
  "success": true,
  "message": "Assignment rejected successfully",
  "data": { ... }
}
```

---

## ❌ **Troubleshooting:**

### **Problem 1: No Projects Show in "My Projects"**

**Possible Causes:**
1. Backend `createdBy` field is empty
2. Backend uses different field name
3. User object is missing

**How to Debug:**
```javascript
// Open console, check:
1. User object:
   console.log('Current user:', user);
   
2. Project data:
   console.log('All projects:', convertedProjects);
   
3. Which field has creator info:
   console.log('First project:', convertedProjects[0]);
   // Look for: createdBy, createUser, projectOwner, etc.
```

---

### **Problem 2: Accept/Reject Buttons Don't Work**

**Possible Causes:**
1. Backend endpoints not implemented
2. API returns error
3. Assignment ID is missing

**How to Debug:**
```javascript
// Open Network tab (F12)
// Click Accept/Reject
// Check the API response:

// ✅ Good response:
{
  "success": true,
  "message": "Assignment approved"
}

// ❌ Bad response:
{
  "success": false,
  "message": "Endpoint not found"
}
// → Backend needs to implement endpoints
```

---

### **Problem 3: All Projects Still Show (Not Filtered)**

**Possible Causes:**
1. User object is null/undefined
2. Backend field name doesn't match

**How to Debug:**
```javascript
// Check console logs:
🔍 Logged-in user: null or undefined or undefined
// → User not loaded properly

// OR:
🔍 Checking project: "..." createdBy: undefined
// → Backend doesn't send createdBy field
```

**Solution:**
```typescript
// Check backend DTO includes creator field:
public class ProjectDto
{
    public string CreatedBy { get; set; }  // ← Must be present
    // OR
    public string CreateUser { get; set; }
    // OR
    public string ProjectOwner { get; set; }
}
```

---

## 📞 **Quick Checklist:**

### **Before Testing:**
- [ ] Backend has `createdBy` or `createUser` field in project DTO
- [ ] Backend has `/ProjectAssignment/{id}/approve` endpoint
- [ ] Backend has `/ProjectAssignment/{id}/reject` endpoint
- [ ] User is logged in and `user` object is available

### **During Testing:**
- [ ] Console shows filtering logs
- [ ] Network tab shows API calls
- [ ] Success messages appear
- [ ] Page refreshes after accept/reject

### **After Testing:**
- [ ] Database shows updated assignment status
- [ ] Rejected assignments have rejection reason
- [ ] Other users can't see your authored projects

---

## 🎯 **Expected UI:**

### **Authored Projects Page:**
```
┌─────────────────────────────────────┐
│ My Projects                         │
│                                     │
│ ┌─────────────────────────────────┐│
│ │ Website Redesign                ││
│ │ Created by: john@example.com    ││
│ │ Status: Active | Progress: 45%  ││
│ └─────────────────────────────────┘│
│                                     │
│ ┌─────────────────────────────────┐│
│ │ Mobile App                      ││
│ │ Created by: john@example.com    ││
│ │ Status: Active | Progress: 20%  ││
│ └─────────────────────────────────┘│
│                                     │
│ (Only shows projects YOU created)  │
└─────────────────────────────────────┘
```

### **Delegated Projects Page:**
```
┌─────────────────────────────────────┐
│ Delegated to Me                     │
│                                     │
│ ┌─────────────────────────────────┐│
│ │ E-commerce Platform             ││
│ │ You are assigned as: Team Leader││
│ │ Department: Engineering         ││
│ │                                 ││
│ │ [ ✓ Accept ]  [ ✗ Reject ]     ││
│ │ [ View Project Details ]        ││
│ └─────────────────────────────────┘│
│                                     │
│ ┌─────────────────────────────────┐│
│ │ Data Analytics Dashboard        ││
│ │ You are assigned as: Scrum Master││
│ │ Department: Data Science        ││
│ │                                 ││
│ │ [ ✓ Accept ]  [ ✗ Reject ]     ││
│ │ [ View Project Details ]        ││
│ └─────────────────────────────────┘│
└─────────────────────────────────────┘
```

### **Accept Dialog:**
```
┌─────────────────────────────────────┐
│ Accept Project Assignment           │
├─────────────────────────────────────┤
│ Are you sure you want to accept the │
│ assignment to "E-commerce Platform"?│
│                                     │
│ ┌─────────────────────────────────┐│
│ │ Project: E-commerce Platform    ││
│ │ Your Role: Team Leader          ││
│ │ Department: Engineering         ││
│ └─────────────────────────────────┘│
│                                     │
│         [ Cancel ]  [ ✓ Accept ]   │
└─────────────────────────────────────┘
```

### **Reject Dialog:**
```
┌─────────────────────────────────────┐
│ Reject Project Assignment           │
├─────────────────────────────────────┤
│ Please provide a reason for         │
│ rejecting the assignment            │
│                                     │
│ ┌─────────────────────────────────┐│
│ │ Project: Data Analytics Dashboard││
│ │ Your Role: Scrum Master         ││
│ └─────────────────────────────────┘│
│                                     │
│ Reason for Rejection:               │
│ ┌─────────────────────────────────┐│
│ │ I don't have capacity for this  ││
│ │ project right now...            ││
│ │                                 ││
│ └─────────────────────────────────┘│
│                                     │
│         [ Cancel ]  [ ✗ Reject ]   │
└─────────────────────────────────────┘
```

---

## ✅ **Success Criteria:**

### **Filtering Works:**
- [ ] Authored Projects shows only your projects
- [ ] Console logs show correct filtering
- [ ] Other users' projects are hidden

### **Accept Works:**
- [ ] Accept button appears on delegated projects
- [ ] Confirmation dialog shows correct details
- [ ] API call succeeds (check Network tab)
- [ ] Success message appears
- [ ] Assignment status updates in database

### **Reject Works:**
- [ ] Reject button appears on delegated projects
- [ ] Rejection reason is required
- [ ] API call succeeds with reason
- [ ] Success message appears
- [ ] Assignment status and reason saved in database

---

## 🚀 **Ready to Test!**

1. Open your application
2. Login as a user
3. Navigate to "My Projects" → Check filtering
4. Navigate to "Delegated to Me" → Test Accept/Reject
5. Open console (F12) → Monitor logs
6. Open Network tab → Monitor API calls

**Everything is ready! Start testing and let me know if you see any issues!** 🎉

---

