# Quick Testing Guide - Project Member Filtering

## ✅ What Was Implemented

### Before
- When creating any task (Project or Independent), the "Assign To" dropdown showed **all members** in the system
- No distinction between project members and non-project members

### After
- **For Project Tasks**: Only shows members assigned to the selected project
- **For Independent Tasks**: Shows all members (unchanged behavior)

## 🎯 How to Test

### Test 1: Create a Project Task
1. Click "Create New Task" button
2. Select a project from the dropdown (e.g., "Project Alpha")
3. **Expected Result**: 
   - The "Assign To" dropdown now shows only members of Project Alpha
   - You'll see "Select Project Member" as the placeholder
   - If loading, you'll see "Loading members..."

### Test 2: Create an Independent Task
1. Click "Create New Task" button
2. Leave project selection as "No Project (Independent Task)"
3. **Expected Result**: 
   - The "Assign To" dropdown shows all members in the system
   - You'll see "Select Assignee" as the placeholder
   - This is the same behavior as before (unchanged)

### Test 3: Switch Between Projects
1. Click "Create New Task"
2. Select "Project Alpha"
3. Select a member from Project Alpha
4. Now switch to "Project Beta"
5. **Expected Result**:
   - The previously selected assignee is cleared
   - Dropdown now shows only Project Beta members
   - Cannot assign Project Alpha members to Project Beta

### Test 4: Switch Between Project and Independent
1. Select a project and an assignee
2. Change to "No Project (Independent Task)"
3. **Expected Result**:
   - Assignee selection is cleared
   - All members are now available
   - Can proceed to create independent task

### Test 5: Project with No Members
1. Select a project that has no members assigned yet
2. **Expected Result**:
   - Warning message appears: "No members found for this project. Please add members to the project first."
   - Cannot proceed without selecting an assignee

## 🔍 What to Look For

### Visual Indicators
✅ Loading spinner in dropdown while fetching members  
✅ Different placeholder text for project vs independent tasks  
✅ Warning message if no members found  
✅ Smooth clearing of assignee when switching projects  

### Console Logs (for debugging)
Open browser DevTools → Console tab to see:
- `🔍 Fetching members for project: {projectId}`
- `📦 Project members response: [...]`
- `✅ Mapped project members: [...]`

### API Call
In DevTools → Network tab, you should see:
```
GET /api/ProjectAssignment/All-members?projectId={id}
```

## 🐛 Common Issues & Solutions

### Issue: "Failed to load project members" error
**Possible Causes:**
- Backend not running
- Project doesn't exist
- Network connection issue

**Solution:**
- Check backend is running on `http://localhost:8080`
- Verify the project ID exists in database
- Check browser console for detailed error

### Issue: Dropdown shows no members but members exist
**Possible Causes:**
- Members not assigned to the project
- Department mismatch (if restrictions apply)

**Solution:**
- Go to Project Management
- Add members to the project first
- Verify members are approved

### Issue: Can select wrong members
**Possible Causes:**
- State not updating correctly
- Old data cached

**Solution:**
- Refresh the page
- Close and reopen the modal
- Check console for state updates

## 📝 Technical Details

### Files Modified
- `Frontend/src/pages/Tasks/CreateTaskModal.tsx`
  - Added `projectMembers` state
  - Added `loadingMembers` state
  - Added useEffect to fetch project members
  - Updated assignee dropdown logic
  - Updated form submission handler

### New Dependencies
- None! Uses existing `projectAssignmentService`

### Backend Endpoint Used
```
GET /api/ProjectAssignment/All-members?projectId={id}
```

### Response Format
```typescript
[
  {
    MemberId: "uuid-string",
    MemberFullName: "John Doe",
    MemberRole: "Developer",
    EmployeeId: "EMP123",
    // ... other fields
  }
]
```

## ✨ Additional Features

### Auto-Clear on Project Change
When you switch projects, the selected assignee is automatically cleared to prevent assigning the wrong person.

### Employee ID Search
The Employee ID search field also respects the project filter:
- For project tasks: searches only project members
- For independent tasks: searches all members

### Loading States
Smooth loading experience with:
- Disabled dropdown while loading
- "Loading members..." placeholder text
- Visual opacity change

## 🎨 User Experience Benefits

1. **Prevents Errors**: Can't accidentally assign non-project members to project tasks
2. **Faster Selection**: Shorter list = quicker to find the right person
3. **Clear Context**: Different labels make it obvious what you're doing
4. **Smooth Transitions**: Auto-clear prevents confusion when switching
5. **Helpful Warnings**: Know immediately if a project has no members

## 🔄 Rollback Instructions

If you need to revert these changes:

1. Remove lines with `projectMembers` state
2. Remove lines with `loadingMembers` state
3. Remove the project members fetch useEffect (around line 62-118)
4. In assignee dropdown, change back to always use `allMembers`
5. In form submit, use `allMembers` instead of `memberList`

The independent task functionality is completely separate and unaffected.


