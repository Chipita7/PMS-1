# Frontend-Backend Connection Guide

This guide explains how the Frontend and Backend are connected and how to use the API services.

## Overview

The frontend is now connected to the backend API through a comprehensive service layer that handles authentication, data fetching, and error management.

## Architecture

### 1. API Client (`src/lib/api.ts`)
- Centralized HTTP client with authentication handling
- Automatic token management
- Error handling and response formatting
- Support for file uploads

### 2. Service Layer (`src/services/`)
- **authService**: Authentication, user management, profile updates
- **projectService**: Project CRUD operations, team management, approvals
- **taskService**: Task management, assignments, progress tracking
- **milestoneService**: Milestone creation and management
- **messageService**: Chat and messaging functionality
- **notificationService**: User notifications

### 3. React Hooks (`src/hooks/useApi.ts`)
- `useApi`: For data fetching with loading states
- `useApiMutation`: For mutations (create, update, delete)

### 4. Error Handling
- **ErrorBoundary**: Catches and displays React errors
- **LoadingSpinner**: Reusable loading component
- **API Error Handling**: Automatic error display and token refresh

## Configuration

### Environment Variables
Create a `.env` file in the Frontend directory:
```env
VITE_API_BASE_URL=http://localhost:8080/api
VITE_NODE_ENV=development
```

### Vite Configuration
The `vite.config.ts` includes:
- Proxy configuration for development
- Path aliases (@/ for src/)
- Environment variable support

### CORS Configuration
The backend is configured to allow requests from:
- `http://localhost:3000` (React dev server)
- `http://localhost:5173` (Vite dev server)

## Usage Examples

### 1. Using API Services Directly

```typescript
import { authService, projectService } from '@/services';

// Login
const response = await authService.login({ email, password });
if (response.success) {
  // Handle success
}

// Fetch projects
const projects = await projectService.getAllProjects();
```

### 2. Using React Hooks

```typescript
import { useApi, useApiMutation } from '@/hooks/useApi';
import { projectService } from '@/services';

// Data fetching
const { data: projects, loading, error } = useApi(
  () => projectService.getAllProjects()
);

// Mutations
const { mutate: createProject, loading: creating } = useApiMutation(
  projectService.createProject
);
```

### 3. Error Handling

```typescript
import ErrorBoundary from '@/components/ErrorBoundary';
import LoadingSpinner from '@/components/LoadingSpinner';

// Wrap your app
<ErrorBoundary>
  <App />
</ErrorBoundary>

// Show loading state
{loading && <LoadingSpinner text="Loading projects..." />}
```

## Authentication Flow

1. User logs in through `authService.login()`
2. JWT token is stored in localStorage and API client
3. All subsequent requests include the token automatically
4. Token refresh is handled automatically
5. On logout, token is cleared

## API Endpoints

The backend provides the following main endpoints:

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh-token` - Token refresh

### Projects
- `GET /api/project/All-projects` - Get all projects
- `POST /api/project/create-project` - Create project
- `PUT /api/project/edit-project` - Update project
- `DELETE /api/project/delete-project` - Delete project

### Tasks
- `GET /api/projecttask/Get-all-tasks` - Get all tasks
- `POST /api/projecttask/create-task` - Create task
- `PUT /api/projecttask/update-task/{id}` - Update task

### And many more...

## Development Setup

1. **Start the Backend**:
   ```bash
   cd Backend
   dotnet run
   ```
   Backend runs on `http://localhost:8080`

2. **Start the Frontend**:
   ```bash
   cd Frontend
   npm run dev
   ```
   Frontend runs on `http://localhost:3000`

3. **Access the Application**:
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:/api
- Swagger UI: http://localhost:8080/Swagger

## Troubleshooting

### Common Issues

1. **CORS Errors**: Ensure backend CORS is configured for your frontend URL
2. **Authentication Errors**: Check if JWT token is being sent correctly
3. **API Connection**: Verify backend is running and accessible
4. **Environment Variables**: Ensure `.env` file is properly configured

### Debug Tips

1. Check browser Network tab for API requests
2. Check browser Console for JavaScript errors
3. Check backend logs for server-side errors
4. Use Swagger UI to test API endpoints directly

## Next Steps

1. Update remaining components to use the new API services
2. Add more comprehensive error handling
3. Implement real-time updates (WebSocket/SignalR)
4. Add offline support
5. Implement caching strategies
