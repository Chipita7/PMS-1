# Role-Based Head Assignment Filtering

## Overview
Implemented role-based filtering for the "My Head Assignments" feature in the Requests Dashboard. The filtering now applies different access levels based on user roles.

## Implementation Details

### User Roles Hierarchy
The system recognizes the following roles (from AuthContext normalization):
- **Director** (`director`)
- **Vice President** (`vice_president`)
- **Manager** (`manager`)
- **Supervisor** (`supervisor`)
- **Member** (`member`)
- **Admin** (`admin`)

### Filtering Rules

#### Directors and Vice Presidents
- **Access Level**: Full access to all pending requests
- **"My Head Assignments" View**: Shows ALL pending requests (not just their assigned ones)
- **Rationale**: These senior roles have oversight responsibilities and need visibility into all pending work
- **Badge Count**: Displays total count of all pending requests

#### Other Roles (Managers, Supervisors, Members, etc.)
- **Access Level**: Restricted to assigned requests only
- **"My Head Assignments" View**: Shows ONLY requests where they are specifically assigned as head
- **Rationale**: Standard roles should only manage requests explicitly assigned to them
- **Badge Count**: Displays count of requests specifically assigned to them as head

## Technical Changes

### File: `RequestsDashboard.tsx`

1. **Added Import**:
   ```tsx
   import { useAuth } from "@/context/AuthContext";
   ```

2. **Added State**:
   ```tsx
   const { user } = useAuth();
   const [currentUserRole, setCurrentUserRole] = React.useState<string>("");
   ```

3. **Role Tracking Effect**:
   ```tsx
   React.useEffect(() => {
     if (user?.role) {
       setCurrentUserRole(user.role.toLowerCase());
       console.log("Current user role:", user.role.toLowerCase());
     }
   }, [user]);
   ```

4. **Updated Display Logic**:
   ```tsx
   const displayed = React.useMemo(() => {
     if (viewFilter === "mine") {
       const isDirectorOrVP = currentUserRole === "director" || currentUserRole === "vice_president";
       
       if (isDirectorOrVP) {
         // Directors and Vice Presidents see all pending requests
         return filtered;
       } else {
         // Other roles only see requests where they are specifically assigned as head
         return requests.filter((r) => myHeadIds.includes(r.id));
       }
     }
     return filtered;
   }, [requests, filtered, myHeadIds, viewFilter, currentUserRole]);
   ```

5. **Updated Badge Count**:
   ```tsx
   {currentUserRole === "director" || currentUserRole === "vice_president" 
     ? filtered.length 
     : myHeadIds.length}
   ```

## User Experience

### For Directors and Vice Presidents
1. Click "My Head Assignments" button
2. See all pending requests in the system
3. Badge shows total count of all pending requests
4. Can manage and assign any pending request

### For Other Roles
1. Click "My Head Assignments" button
2. See only requests they are assigned as head
3. Badge shows count of their assigned requests only
4. Can only manage requests explicitly assigned to them

## Benefits

- **Improved Security**: Prevents unauthorized access to requests
- **Clear Hierarchy**: Reflects organizational structure in the application
- **Better UX**: Users see only relevant data based on their role
- **Scalability**: Easy to add or modify role-based rules
- **Debugging**: Console logs added for troubleshooting

## Testing Recommendations

1. **Test as Director**:
   - Log in as a user with "Director" role
   - Click "My Head Assignments"
   - Verify all pending requests are visible

2. **Test as Vice President**:
   - Log in as a user with "Vice President" role
   - Click "My Head Assignments"
   - Verify all pending requests are visible

3. **Test as Manager/Other Role**:
   - Log in as a user with "Manager" or similar role
   - Click "My Head Assignments"
   - Verify only assigned requests are visible

4. **Test Badge Counts**:
   - Compare badge count with actual displayed requests
   - Verify count matches for both role types

## Notes

- The role normalization happens in `AuthContext.tsx`
- Roles are case-insensitive (converted to lowercase)
- The `myHeadIds` array is fetched from the backend via `getHeadAssignmentsForCurrentUser()`
- Console logs are included for debugging purposes
