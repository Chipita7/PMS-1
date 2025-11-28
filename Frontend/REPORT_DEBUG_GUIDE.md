# Report Generation Debugging Guide

## Current Issue
The generate button is sending a request but getting undefined response details, suggesting a network or configuration issue.

## Step-by-Step Debugging

### 1. Verify Backend is Running
Open your browser and go to: **http://localhost:8080/swagger/index.html**

✅ If Swagger loads → Backend is running  
❌ If it doesn't load → Start your backend

### 2. Test the Report Endpoint Directly

Open a new browser tab and try this in the console (F12):

```javascript
fetch('http://localhost:8080/api/Report/templates', {
  headers: {
    'Authorization': 'Bearer ' + localStorage.getItem('authToken')
  }
})
.then(r => r.json())
.then(d => console.log('✅ Success:', d))
.catch(e => console.error('❌ Error:', e))
```

**Expected Result:** Should return a list of report templates

**If this fails:** 
- Check CORS settings in backend
- Verify the token is valid
- Check if backend is on port 8080

### 3. Check Frontend Configuration

In your browser console, run:

```javascript
console.log('API Base URL:', import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080/api')
```

**Expected:** `http://localhost:8080/api`

### 4. Check the Network Tab

1. Open Browser DevTools (F12)
2. Go to **Network** tab
3. Click "Generate Report" button
4. Look for the request to `/api/Report/task-progress`

**Check these details:**
- **Status:** Should be 200 (success) or 400/500 (error)
- **Request URL:** Should be `http://localhost:8080/api/Report/task-progress`
- **Request Headers:** Should include `Authorization: Bearer ...`
- **Request Payload:** Should show your report data
- **Response:** Should show the error message from backend

**If you don't see the request at all:**
- Frontend isn't actually making the call
- Check browser console for JavaScript errors

**If request shows "CORS error":**
- Backend CORS settings need to be adjusted
- Should allow `http://localhost:5173` origin

**If request shows "(failed)" or "net::ERR_FAILED":**
- Backend is not running
- Or running on wrong port

### 5. Common Issues and Solutions

#### Issue: "CORS policy" error
**Solution:** Backend needs to allow frontend origin. Check `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// And later:
app.UseCors("AllowFrontendDev");
```

#### Issue: 401 Unauthorized
**Solution:** Token expired or invalid. Logout and login again.

#### Issue: 500 Internal Server Error
**Solution:** Backend code error. Check backend console logs for the actual error message.

#### Issue: Request not showing in Network tab
**Solution:** JavaScript error preventing the call. Check browser console.

### 6. Try Without Filters

Clear ALL filters and try:
1. Go to Reports page
2. Select "Task progress"
3. **Don't fill ANY fields** (no dates, no department, no IDs)
4. Click "Generate Report"

This should send minimal data:
```json
{
  "reportType": "TaskProgress",
  "exportFormat": "Json"
}
```

If this works, then the issue is with one of the filters causing backend to crash.

### 7. Check Backend Logs

When you click "Generate Report", your backend console should show something like:

**Good (working):**
```
info: ProjectManagementSystem1.Middleware.RequestResponseLoggingMiddleware[0]
      HTTP Request: POST /api/Report/task-progress | User: manager | IP: ::1
info: ProjectManagementSystem1.Controllers.ReportController[0]
      Generating task progress report...
```

**Bad (error):**
```
fail: ProjectManagementSystem1.Services.ReportService.ReportService[0]
      Error generating task progress report
      System.NullReferenceException: Object reference not set to an instance of an object.
         at ...ProjectAssignment.ProjectId...
```

If you see a NullReferenceException mentioning `ProjectAssignment`, that's the issue we discussed - some tasks don't have a ProjectAssignment.

### 8. Quick Fix Test

Try this in your browser console after clicking Generate Report:

```javascript
// This will show you what's actually being sent
const observer = new PerformanceObserver((list) => {
  list.getEntries().forEach((entry) => {
    if (entry.name.includes('Report')) {
      console.log('📊 Report Request:', entry.name);
      console.log('Duration:', entry.duration + 'ms');
    }
  });
});
observer.observe({ entryTypes: ['resource'] });
```

### 9. Alternative: Use Postman/Curl

Test the endpoint directly:

```bash
curl -X POST http://localhost:8080/api/Report/task-progress \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -d '{
    "reportType": "TaskProgress",
    "exportFormat": "Json"
  }'
```

Replace `YOUR_TOKEN_HERE` with the token from `localStorage.getItem('authToken')` in browser console.

### 10. Expected Behavior

When working correctly, you should see in browser console:

```
🌐 POST API Call: http://localhost:8080/api/Report/task-progress
📦 POST Data: { "reportType": "TaskProgress", "exportFormat": "Json" }
🔑 Auth Header: Bearer eyJ...
✅ POST Success: { reportTitle: "Task Progress Report", ... }
✅ POST Status: 200
```

Then the report should display on the page.

---

## Next Steps

1. Try the debugging steps above in order
2. Check both browser console AND backend console
3. Share any error messages you find
4. Try without filters first (minimal payload)

The most likely issues are:
1. 🔴 Backend returning 500 error (check backend logs)
2. 🔴 CORS blocking the request (check browser console)
3. 🔴 Token expired (logout/login)
4. 🔴 Backend not running on port 8080 (check Swagger)


