# Project Member Filtering Implementation

## Overview
Updated the `CreateTaskModal` to fetch and display only project-specific members when creating a project task, while maintaining the existing behavior for independent tasks.

## Changes Made

### 1. **Added New State Variables** (`CreateTaskModal.tsx`)
- `projectMembers`: Array to store members of the selected project
- `loadingMembers`: Boolean to track the loading state while fetching project members

### 2. **Project Members Fetch Logic**
Added a new `useEffect` hook that:
- Triggers when `selectedProjectId` changes
- Fetches project members from the backend using `projectAssignmentService.getProjectMembers()`
- Maps the backend response to the `Member` format
- Handles both success and error cases with appropriate user feedback
- Clears project members when no project is selected (for independent tasks)

### 3. **Updated Assignee Dropdown**
Modified the "Assign To" field to:
- **For Project Tasks** (when a project is selected):
  - Display only members assigned to that specific project
  - Show "Select Project Member" as placeholder
  - Display loading state while fetching members
  - Show warning message if no members are found
  
- **For Independent Tasks** (when no project is selected):
  - Display all available members (unchanged behavior)
  - Show "Select Assignee" as placeholder

### 4. **Employee ID Search Integration**
Updated the employee ID search field to:
- Use `projectMembers` when a project is selected
- Use `allMembers` for independent tasks
- Auto-sync with the main assignee dropdown

### 5. **Form Submission Handler**
Modified the form submission to:
- Use the appropriate member list (project members or all members) when finding the selected user
- Validate that the selected assignee exists in the correct member list

### 6. **Assignee Reset on Project Change**
Added logic to:
- Clear the selected assignee when switching between projects
- Clear the selected assignee when switching between project task and independent task
- Prevent invalid assignee selections

### 7. **Form Reset on Modal Close**
Updated the form reset logic to:
- Clear `projectMembers` state when modal is closed
- Reset all form fields to initial values

## API Integration

### Backend Endpoint Used
```typescript
GET /api/ProjectAssignment/All-members?projectId={projectId}
```

### Response Mapping
The backend returns `AssignmentDto` with the following structure:
```csharp
{
  Id: number,
  ProjectId: number,
  ProjectName: string,
  MemberId: string,           // UUID from Users table
  EmployeeId: string,          // Employee ID (EMP123 format)
  MemberFullName: string,      // User's full name
  MemberEmail: string,
  MemberPhone: string,
  MemberDepartment: string,
  MemberRole: string,          // Role in project (TeamLeader, Developer, etc.)
  Role: string,
  Status: double,
  // ... audit fields
}
```

These are mapped to the frontend `Member` interface:
```typescript
{
  id: string,              // MemberId (UUID)
  name: string,            // MemberFullName
  role: string,            // MemberRole
  projectId: string[]      // [selectedProjectId]
}
```

## User Experience Improvements

1. **Visual Feedback**:
   - Loading state shows "Loading members..." while fetching
   - Disabled state prevents interaction during loading
   - Warning message if no members are found for a project

2. **Clear Context**:
   - Different placeholder text for project vs independent tasks
   - Automatic assignee reset when changing projects

3. **Error Handling**:
   - Toast notifications for fetch errors
   - Graceful fallback to empty member list
   - Validation prevents invalid assignee selection

## Testing Guide

### Test Case 1: Create Project Task
1. Open the Create Task modal
2. Select a project from the dropdown
3. Verify that:
   - The "Assign To" dropdown shows "Loading members..." briefly
   - Only members assigned to that project appear in the dropdown
   - Employee ID search only shows project members

### Test Case 2: Create Independent Task
1. Open the Create Task modal
2. Leave the project field as "No Project (Independent Task)"
3. Verify that:
   - All members appear in the "Assign To" dropdown
   - Employee ID search shows all members
   - This behavior is unchanged from before

### Test Case 3: Switch Between Projects
1. Select Project A
2. Select an assignee from Project A's members
3. Switch to Project B
4. Verify that:
   - The assignee selection is cleared
   - Only Project B's members are shown
   - Cannot submit with Project A's member for Project B

### Test Case 4: Switch Between Project and Independent
1. Select a project and an assignee
2. Change to "No Project (Independent Task)"
3. Verify that:
   - Assignee selection is cleared
   - All members are now available
   - Can select any member for independent task

### Test Case 5: No Members in Project
1. Select a project that has no assigned members
2. Verify that:
   - A warning message appears: "No members found for this project..."
   - Cannot proceed without an assignee
   - Appropriate feedback is provided

### Test Case 6: Error Handling
1. Simulate a network error (disconnect or use browser DevTools)
2. Select a project
3. Verify that:
   - Error toast appears
   - Dropdown shows empty or falls back gracefully
   - Can still cancel and retry

## Code Quality

### Key Features
- ✅ TypeScript type safety maintained
- ✅ Error handling with try-catch blocks
- ✅ Loading states for better UX
- ✅ Console logging for debugging
- ✅ No linting errors
- ✅ Backward compatible with independent tasks

### Performance Considerations
- Members are fetched only when a project is selected
- Cached in state to avoid redundant fetches
- Cleared when modal closes to prevent stale data

## Future Enhancements

Potential improvements for future iterations:
1. Cache project members to avoid refetching for recently selected projects
2. Add pagination if a project has many members
3. Add search/filter functionality within project members
4. Display member avatars or additional metadata
5. Allow quick member addition from the modal if no members exist

## Rollback Plan

If issues arise, the changes can be easily rolled back by:
1. Remove the `projectMembers` and `loadingMembers` state
2. Remove the project members fetch `useEffect`
3. Restore the assignee dropdown to always use `allMembers`
4. Keep the original form submission logic

The independent task functionality is completely untouched and will continue to work as before.

