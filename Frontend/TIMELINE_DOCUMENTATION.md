# Timeline Feature - Complete Implementation Documentation

## Overview
The Timeline feature provides a comprehensive view of project lifecycle events, including project creation, task assignments, milestones, comments, notifications, escalations, and activity logs. Users can view timelines in two different visual templates: **Vertical Timeline** (alternating left-right cards) and **Horizontal Stepper** (numbered sequential cards).

---

## Architecture

### Backend Components

#### 1. **Database Entity** (`Backend/Model/Entities/Timeline.cs`)
```csharp
public class Timeline
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CompletedDate { get; set; }
    public DateTime? EventTime { get; set; }
    public string EventType { get; set; }
    public string UserId { get; set; }
    public string Color { get; set; }
    public string RedirectUrl { get; set; }
    public string Status { get; set; }
    public string TimelineType { get; set; }
    public string Priority { get; set; }
    public int? Progress { get; set; }
    public int ProjectId { get; set; }
    
    // Time tracking fields
    public TimeSpan? LeadTime { get; set; }
    public TimeSpan? CycleTime { get; set; }
    public DateTime? RequestVerificationDate { get; set; }
    public DateTime? FeasibilityTestDate { get; set; }
    
    // Relationships
    public List<TimelinePhase> Phases { get; set; }
    public Project Project { get; set; }
    public ApplicationUser User { get; set; }
}

public class TimelinePhase
{
    public int Id { get; set; }
    public string PhaseName { get; set; }
    public TimeSpan Duration { get; set; }
    public string PhaseStatus { get; set; }
    public DateTime? PhaseStartDate { get; set; }
    public DateTime? PhaseEndDate { get; set; }
    public int TimelineId { get; set; }
    public int? Order { get; set; }
}
```

**Key Fields:**
- `TimelineType`: Categorizes events (Project, ProjectTask, Milestone, Comment, ActivityLog, Notification, Escalation, Message)
- `EventTime`: Timestamp when the event occurred
- `LeadTime`: Duration from request verification to project start
- `CycleTime`: Duration from feasibility test to project completion
- `Phases`: Collection of timeline phases with tact time tracking

---

#### 2. **Data Transfer Objects** (`Backend/Model/Dto/TimelineDto/TimelineDto.cs`)
```csharp
public class TimelineDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EventTime { get; set; }
    public string TimelineType { get; set; }
    public string Status { get; set; }
    public string Color { get; set; }
    public TimeSpan? LeadTime { get; set; }
    public TimeSpan? CycleTime { get; set; }
    public List<TimelinePhaseDto> Phases { get; set; }
    // ... other fields
}
```

---

#### 3. **Service Layer** (`Backend/Services/TimelineService/TimelineService.cs`)

**Interface** (`ITimelineService.cs`):
```csharp
public interface ITimelineService
{
    Task<List<TimelineDto>> GetTimelineByProject(int projectId);
    Task<List<TimelineDto>> GetTimelineByProjectTask(int projectTaskId);
    Task<List<TimelineDto>> GetTimelineByMilestone(int milestoneId);
    Task<List<TimelineDto>> GetTimelineByIndependentTask(int independentTaskId);
    Task<List<TimelineDto>> GetTimelineEventsByFilters(int? projectId, DateTime? startDate, DateTime? endDate, string userId);
    Task<TimelineDto> AddDependencies(int timelineId, AddDependenciesDto dependencyData);
    Task<TimelineDto> UpdateTimelineTimes(int timelineId, UpdateTimelineTimesDto timeData);
    Task<TimelineDto> AddPhaseToTimeline(int timelineId, TimelinePhaseDto phaseDto);
    Task<TimelineDto> UpdatePhase(int timelineId, int phaseId, UpdatePhaseDto phaseDto);
    Task<List<TimelinePhaseDto>> GetTimelinePhases(int timelineId);
}
```

**Key Methods:**

##### `GetTimelineByProject(int projectId)`
Aggregates all timeline events for a specific project:
1. **Project**: Main project entity
2. **ProjectTasks**: All tasks assigned to the project
3. **Milestones**: Project milestones with dates
4. **Timeline Events**: Persisted custom timeline entries with phases
5. **ActivityLogs**: Field change tracking
6. **Comments**: Task-related comments
7. **Notifications**: Project-related notifications
8. **Escalations**: Escalated issues
9. **Messages**: Project communications

Returns sorted timeline events (most recent first).

##### `GetTimelineEventsByFilters()`
Filters timeline events by:
- `projectId`: Filter by specific project
- `startDate`: Events after this date
- `endDate`: Events before this date
- `userId`: Events triggered by specific user

##### `UpdateTimelineTimes()`
Updates time tracking metrics:
- `RequestVerificationDate`: Start of lead time
- `FeasibilityTestDate`: Start of cycle time
- `ProjectStartDate`: End of lead time
- `ProjectCompletionDate`: End of cycle time
- Automatically calculates `LeadTime` and `CycleTime`

##### `AddPhaseToTimeline()` & `UpdatePhase()`
Manages timeline phases for tact time tracking per project phase (Brainstorm, Assignment, Development, etc.)

---

#### 4. **API Controller** (`Backend/Controllers/TimelineController.cs`)

**Endpoints:**

| HTTP Method | Endpoint | Description |
|-------------|----------|-------------|
| GET | `/api/timeline/projects/{projectId}` | Get all timeline events for a project |
| GET | `/api/timeline/projecttask/{projectTaskId}` | Get timeline for a specific task |
| GET | `/api/timeline/milestone/{milestoneId}` | Get timeline for a milestone |
| GET | `/api/timeline/independenttask/{independentTaskId}` | Get timeline for independent task |
| GET | `/api/timeline/events?projectId=&startDate=&endDate=&userId=` | Get filtered timeline events |
| POST | `/api/timeline/{timelineId}/dependencies` | Add dependencies to timeline |
| POST | `/api/timeline/{timelineId}/times` | Update timeline time metrics |
| POST | `/api/timeline/{timelineId}/phases` | Add phase to timeline |
| PUT | `/api/timeline/{timelineId}/phases/{phaseId}` | Update existing phase |

**Example Requests:**

```http
GET /api/timeline/projects/123
Authorization: Bearer {token}

Response:
[
  {
    "id": 1,
    "title": "Project Created",
    "description": "New project initiated",
    "eventTime": "2024-01-15T10:30:00Z",
    "timelineType": "Project",
    "status": "initiated",
    "color": "#007bff",
    "projectId": 123,
    "userId": "user123"
  },
  // ... more events
]
```

---

### Frontend Components

#### 1. **Service Layer** (`Frontend/src/services/timelineService.ts`)

```typescript
export const timelineService = {
  // Get timeline events for a specific project
  async getProjectTimeline(projectId: number): Promise<TimelineDto[]>
  
  // Get filtered timeline events
  async getEventsByFilters(params?: {
    projectId?: number;
    startDate?: string;
    endDate?: string;
    userId?: string;
  }): Promise<TimelineDto[]>
}
```

Uses the centralized `apiClient` from `@/lib/api` for HTTP requests with authentication.

---

#### 2. **Timeline List Page** (`Frontend/src/pages/TimelineList.tsx`)

**Purpose**: Lists all projects in a table, allowing users to select a project to view its timeline.

**Features:**
- Fetches all projects via `projectService.getAllProjects()`
- Displays projects in a DataTable with columns: Title, Department, Owner, Priority, Status, Due Date
- Filter by priority (P1-Critical, P2-High, P3-Medium)
- Clicking a row navigates to `/timeline/{projectId}?name={projectName}`

**Data Flow:**
```
User visits /timeline
  ↓
TimelineList loads
  ↓
Fetch projects from projectService
  ↓
Map projects to table rows
  ↓
User clicks row → Navigate to TimelinePage
```

---

#### 3. **Timeline View Page** (`Frontend/src/pages/TimelinePage.tsx`)

**Purpose**: Displays the detailed timeline for a specific project.

**Features:**
- Extracts `projectId` from URL params
- Fetches project details and timeline events
- Renders `ProjectTimeline` component with events
- Back button to return to timeline list or dashboard

**Data Flow:**
```
User navigates to /timeline/{projectId}
  ↓
TimelinePage loads
  ↓
Extract projectId from params
  ↓
Fetch project details (projectService.getProjectById)
  ↓
Fetch timeline events (timelineService.getProjectTimeline)
  ↓
Map DTO to TimelineEvent format
  ↓
Render ProjectTimeline component
```

**Data Mapping:**
```typescript
const mapDtoToTimelineEvents = (dtos: any[]): TimelineEvent[] => {
  return dtos.map((d) => ({
    id: `${d.timelineType || 'evt'}-${d.id}`,
    title: d.title,
    description: d.description,
    actor: d.userId,
    date: new Date(d.eventTime || d.startDate || Date.now()).toISOString(),
    status: d.status,
  }));
};
```

---

#### 4. **Timeline Component** (`Frontend/src/components/ProjectTimeline.tsx`)

**Purpose**: Renders timeline events in two visual templates.

**Props:**
```typescript
type ProjectTimelineProps = {
  darkMode: boolean;
  projectName?: string;
  events?: TimelineEvent[];
};

export type TimelineEvent = {
  id: string;
  title: string;
  description?: string;
  actor?: string; // Who did it (department/role)
  date: string; // ISO string
  status?: string; // e.g., initiated, approved, in-progress, completed
  assignedTo?: string;
  assignedDate?: string;
  tactTimeDays?: number; // Duration in days
};
```

**Features:**

##### Template Toggle
- Two buttons at top-right to switch between views
- **Vertical Timeline** (LayoutList icon): Alternating left-right card layout
- **Horizontal Stepper** (LayoutGrid icon): Sequential numbered cards

##### Metrics Display
- **Lead Time**: From request to approval (calculated from first event to "approved" status event)
- **Cycle Time**: From approval to handover/completion
- Info tooltips explain each metric

##### Vertical Timeline Template
- Center vertical line connecting all events
- Alternating left/right card placement
- Purple circles (#581c87) for past events
- Gold circle (#D4AF37) for current/latest event
- Icons based on event status (User, Check, Clipboard, CheckCircle2, etc.)
- Click cards to reveal tact time details

##### Horizontal Stepper Template
- Numbered step circles (1, 2, 3...) with event icons
- Horizontal gradient progress line (purple to gold)
- Fixed-width cards (300px) in scrollable container
- Left border color matches step color
- "Current" badge on latest event
- Tact time displayed as inline badge
- Arrow connectors between steps
- Legend at bottom explaining colors

**Color Scheme:**
- Purple: `#581c87` (past/completed events)
- Gold: `#D4AF37` (current state)
- Dark Purple: `#2c0340`, `#1f1140` (titles)
- Light Purple: `#f3e8ff` (backgrounds)

**Tact Time Calculation:**
```typescript
const tactForEvent = (index: number) => {
  const e = sorted[index];
  if (typeof e.tactTimeDays === 'number') return e.tactTimeDays;
  const next = sorted[index + 1];
  if (!next) return null;
  return msToDays(new Date(next.date).getTime() - new Date(e.date).getTime());
};
```

---

#### 5. **Routing** (`Frontend/src/App.tsx`)

```tsx
// Timeline list route
<Route path="/timeline" element={
  <ProtectedRoute allowedRoles={[...allRoles]}>
    {renderDashboardLayout(<TimelineList darkMode={darkMode} />)}
  </ProtectedRoute>
} />

// Specific project timeline route
<Route path="/timeline/:projectId" element={
  <ProtectedRoute allowedRoles={[...allRoles]}>
    {renderDashboardLayout(<TimelinePage darkMode={darkMode} />)}
  </ProtectedRoute>
} />
```

---

#### 6. **Navigation** (`Frontend/src/components/Sidebar.tsx`)

Timeline menu item in sidebar:
```tsx
{ 
  name: "Timeline", 
  icon: Calendar, 
  href: "/timeline", 
  type: "item" 
}
```

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         USER ACTIONS                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     FRONTEND (React/TypeScript)                  │
├─────────────────────────────────────────────────────────────────┤
│ 1. Sidebar Navigation → Click "Timeline"                        │
│    └─> Navigate to /timeline (TimelineList.tsx)                 │
│                                                                  │
│ 2. TimelineList Component                                        │
│    ├─> projectService.getAllProjects()                          │
│    ├─> Display projects in DataTable                            │
│    └─> User clicks row → Navigate to /timeline/:projectId       │
│                                                                  │
│ 3. TimelinePage Component                                        │
│    ├─> Extract projectId from URL params                        │
│    ├─> projectService.getProjectById(projectId)                 │
│    ├─> timelineService.getProjectTimeline(projectId)            │
│    └─> Map DTOs to TimelineEvent[]                              │
│                                                                  │
│ 4. ProjectTimeline Component                                     │
│    ├─> Render template toggle buttons                           │
│    ├─> Calculate lead time & cycle time                         │
│    ├─> Sort events chronologically                              │
│    └─> Render selected template (Vertical or Horizontal)        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼ HTTP Requests
┌─────────────────────────────────────────────────────────────────┐
│                     API LAYER (REST Endpoints)                   │
├─────────────────────────────────────────────────────────────────┤
│ GET /api/projects                                                │
│ GET /api/projects/:id                                            │
│ GET /api/timeline/projects/:projectId                            │
│ GET /api/timeline/events?projectId=&startDate=&endDate=&userId=  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  BACKEND (ASP.NET Core C#)                       │
├─────────────────────────────────────────────────────────────────┤
│ 1. TimelineController                                            │
│    └─> Receives HTTP request                                    │
│    └─> Validates input & authentication                         │
│    └─> Calls TimelineService methods                            │
│                                                                  │
│ 2. TimelineService                                               │
│    ├─> GetTimelineByProject(projectId)                          │
│    │   ├─> Query Projects table                                 │
│    │   ├─> Query ProjectTasks table                             │
│    │   ├─> Query Milestones table                               │
│    │   ├─> Query Timelines table (persisted events)             │
│    │   ├─> Query ActivityLogs table                             │
│    │   ├─> Query Comments table                                 │
│    │   ├─> Query Notifications table                            │
│    │   ├─> Query Escalations table                              │
│    │   ├─> Query Messages table                                 │
│    │   └─> Aggregate & sort all events                          │
│    │                                                             │
│    └─> Map entities to TimelineDto                              │
│                                                                  │
│ 3. Return List<TimelineDto> to controller                       │
│    └─> Controller returns JSON response                         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                DATABASE (Entity Framework Core)                  │
├─────────────────────────────────────────────────────────────────┤
│ Tables:                                                          │
│ • Timelines (id, title, eventTime, timelineType, projectId...)  │
│ • TimelinePhases (id, phaseName, duration, timelineId...)       │
│ • Projects                                                       │
│ • ProjectTasks                                                   │
│ • Milestones                                                     │
│ • ActivityLogs                                                   │
│ • Comments                                                       │
│ • Notifications                                                  │
│ • Escalations                                                    │
│ • Messages                                                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Code Quality & Status

### ✅ Production Ready
- All timeline files are clean and error-free
- No TODO, FIXME, or BUG comments found
- TypeScript types properly defined
- C# interfaces and DTOs complete
- Error handling implemented
- Logging integrated

### 🔧 Intentionally Commented Code
The following sections are **intentionally commented out** for future implementation:

1. **TaktTime field** in `TimelinePhaseDto` - Currently using `Duration` instead
2. **TimelineDependency** entity - Dependency tracking between timeline items
3. **GetTimeAnalysis()** method - Advanced analytics (efficiency score, bottleneck detection)
4. **Time metrics endpoint** - Project-level time aggregation

These are **not bugs** - they are features planned for future releases.

### 📋 File Checklist

**Backend Files:**
- ✅ `Backend/Model/Entities/Timeline.cs` - Entity models
- ✅ `Backend/Model/Dto/TimelineDto/TimelineDto.cs` - DTOs
- ✅ `Backend/Services/TimelineService/ITimelineService.cs` - Service interface
- ✅ `Backend/Services/TimelineService/TimelineService.cs` - Service implementation
- ✅ `Backend/Controllers/TimelineController.cs` - API endpoints

**Frontend Files:**
- ✅ `Frontend/src/services/timelineService.ts` - API client
- ✅ `Frontend/src/pages/TimelineList.tsx` - Project list view
- ✅ `Frontend/src/pages/TimelinePage.tsx` - Timeline viewer
- ✅ `Frontend/src/components/ProjectTimeline.tsx` - Timeline visualization
- ✅ `Frontend/src/App.tsx` - Route configuration
- ✅ `Frontend/src/components/Sidebar.tsx` - Navigation menu

---

## Usage Examples

### 1. View All Project Timelines
```
Navigate to: /timeline
→ See list of all projects
→ Filter by priority (P1/P2/P3)
→ Click any project row to view its timeline
```

### 2. View Specific Project Timeline
```
Navigate to: /timeline/123
→ See all events for project #123
→ Toggle between Vertical and Horizontal templates
→ Click events to see tact time details
→ View lead time and cycle time metrics
```

### 3. API Integration Example
```typescript
// Fetch timeline with filters
const events = await timelineService.getEventsByFilters({
  projectId: 123,
  startDate: '2024-01-01',
  endDate: '2024-12-31',
  userId: 'user123'
});
```

---

## Color Scheme Reference

| Color | Hex Code | Usage |
|-------|----------|-------|
| Purple | #581c87 | Past events, completed milestones |
| Gold | #D4AF37 | Current state, latest event |
| Dark Purple | #2c0340 | Titles, headings |
| Dark Purple | #1f1140 | Secondary titles |
| Light Purple | #f3e8ff | Light backgrounds, badges |
| Light Purple | #e9d5ff | Gradient backgrounds |

---

## Authentication & Authorization
- All timeline routes require authentication
- Protected by `ProtectedRoute` component
- Accessible to all user roles: developer, team_lead, manager, scrum_master, qa_lead, department_head, ceo, vice_president
- JWT token required for API requests

---

## Future Enhancements (Commented Out Features)

1. **Timeline Dependencies**: Track which tasks/milestones depend on others
2. **Advanced Time Analysis**: 
   - Efficiency scores
   - Bottleneck detection
   - Average takt time calculation
   - Phase-by-phase analysis
3. **Project Time Metrics Endpoint**: Aggregate statistics across all project timelines
4. **Export Functionality**: Export timeline as JSON, CSV, Excel, or PDF

---

## Testing Recommendations

### Backend Tests
- Unit tests for TimelineService methods
- Integration tests for API endpoints
- Test filtering logic
- Test time calculation accuracy

### Frontend Tests
- Component rendering tests
- Template toggle functionality
- Data mapping validation
- Date formatting tests
- Responsive design tests

---

## Maintenance Notes

- **Dependencies**: Entity Framework Core, ASP.NET Core, React, TypeScript, Lucide Icons, TailwindCSS
- **Database Migrations**: Run migrations when updating Timeline or TimelinePhase entities
- **API Versioning**: Consider versioning if breaking changes needed
- **Performance**: Timeline queries aggregate multiple tables - consider caching for large datasets

---

## Support & Contact

For questions or issues:
1. Check console logs (browser F12)
2. Check backend logs (ILogger)
3. Verify authentication tokens
4. Ensure backend is running and accessible

---

**Last Updated**: November 11, 2025
**Version**: 1.0
**Status**: ✅ Production Ready
