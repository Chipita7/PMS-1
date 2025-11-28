# 🔧 Chat Troubleshooting Guide

## Issue 1: Attachment Upload 404 Error

### Error Message

```
POST http://localhost:8080/api/Attachments/upload 404 (Not Found)
```

### Possible Causes & Solutions

#### 1. Backend Not Running ❌

**Check:**

```
Is your backend server running?
Port 8080 accessible?
```

**Solution:**

```bash
# Start backend
cd Backend
dotnet run

# Or if using IIS, restart IIS
```

#### 2. Wrong API URL ❌

**Check console log:**

```
📤 Uploading to: http://localhost:8080/api/Attachments/upload
```

**Verify:**

- URL is correct
- Port matches backend
- No typos in endpoint

#### 3. Endpoint Not Registered ❌

**Backend might not have AttachmentsController registered**

**Check `Backend/Program.cs` or `Startup.cs`:**

```csharp
// Should have:
builder.Services.AddControllers();

// And:
app.MapControllers();
```

#### 4. Route Issue ❌

**Check `Backend/Controllers/AttachmentsController.cs`:**

```csharp
[ApiController]
[Route("api/[controller]")]  // Should be "api/[controller]"
public class AttachmentsController : ControllerBase
{
    [HttpPost("upload")]  // Results in: /api/Attachments/upload
    public async Task<IActionResult> UploadAttachment(...)
}
```

---

## Issue 2: Unread Counts Not Showing

### Error Description

```
Department and Project badges show no numbers
Only Personal badge shows numbers
```

### Debug Steps

#### Step 1: Check Console Logs

**Open browser console (F12) and look for:**

```
📊 Unread counts from backend: {
  personalUnread: 2,
  departmentUnread: 0,    // ← Should have numbers
  projectUnread: 0,       // ← Should have numbers
  groupUnread: 0,
  total: 2
}

🏷️ department tab unread count: 0
🏷️ project tab unread count: 0
🏷️ personal tab unread count: 2
🏷️ Full unreadCounts state: { personalUnread: 2, departmentUnread: 0, ... }
```

#### Step 2: If Backend Returns 0

**Most Common: Backend Not Restarted**

```bash
# STOP backend
# START backend again

# The .Include(r => r.Message) fix REQUIRES restart!
```

**Check backend logs for errors:**

```
Look for EntityFramework errors
Look for SQL query errors
```

#### Step 3: Check Database

**Verify MessageReadStatuses table:**

```sql
SELECT COUNT(*)
FROM MessageReadStatuses mrs
INNER JOIN Messages m ON mrs.MessageId = m.MessageId
WHERE mrs.UserId = 'your-user-id'
  AND mrs.IsRead = 0
  AND m.MessageType = 2;  -- Department

SELECT COUNT(*)
FROM MessageReadStatuses mrs
INNER JOIN Messages m ON mrs.MessageId = m.MessageId
WHERE mrs.UserId = 'your-user-id'
  AND mrs.IsRead = 0
  AND m.MessageType = 1;  -- Project
```

**If count > 0 in database but API returns 0:**

- Backend needs restart
- EF navigation property not loaded

#### Step 4: Verify Backend Code

**Check `Backend/Controllers/MessageController.cs` line 286-295:**

```csharp
var departmentUnread = await _context.MessageReadStatuses
    .Include(r => r.Message)  // ← This line MUST be there!
    .Where(r => r.UserId == userId && !r.IsRead && r.Message.MessageType == 2)
    .CountAsync();

var projectUnread = await _context.MessageReadStatuses
    .Include(r => r.Message)  // ← This line MUST be there!
    .Where(r => r.UserId == userId && !r.IsRead && r.Message.MessageType == 1)
    .CountAsync();
```

---

## Quick Fix Checklist

### For Attachment Upload:

- [ ] Backend is running
- [ ] Port 8080 is correct
- [ ] AttachmentsController exists
- [ ] `[Route("api/[controller]")]` is set
- [ ] `MapControllers()` is called in Program.cs

### For Unread Counts:

- [ ] Backend has `.Include(r => r.Message)` in GetUnreadCount()
- [ ] Backend has been restarted
- [ ] Browser has been refreshed (Ctrl+Shift+R)
- [ ] Console shows count values
- [ ] Database has unread messages

---

## Console Commands for Debugging

### Check Backend Endpoints

**In browser console:**

```javascript
// Test if backend is accessible
fetch("http://localhost:8080/api/message/unread-count", {
  headers: { Authorization: "Bearer " + localStorage.getItem("token") },
})
  .then((r) => r.json())
  .then((d) => console.log("Unread counts:", d));

// Test if Attachments controller exists
fetch("http://localhost:8080/api/Attachments/query").then((r) =>
  console.log("Attachments endpoint:", r.status)
);
```

---

## Most Likely Solutions

### For Attachment 404:

**Solution 1: Restart Backend**

```bash
# Most common - backend needs restart after any code changes
cd Backend
dotnet run
```

**Solution 2: Check swagger/API docs**

```
Open: http://localhost:8080/swagger
Look for: Attachments controller
Verify: /upload endpoint exists
```

### For Unread Counts:

**Solution 1: Restart Backend (95% of cases)**

```bash
# The .Include() fix MUST be in running code
cd Backend
dotnet run

# Then refresh browser
Ctrl + Shift + R
```

**Solution 2: Check if messages exist**

```
1. Login as another user
2. Send message to Department
3. Login back as original user
4. Check if count appears
```

---

## Expected Console Output (When Working)

### Attachment Upload:

```
📤 Uploading to: http://localhost:8080/api/Attachments/upload
📤 File: report.pdf 1234567 bytes
📤 EntityType: Message EntityId: 0 Category: 3
📤 Upload response status: 200 OK
📤 Upload success: { id: "guid", fileName: "report.pdf", ... }
📎 File uploaded successfully: guid
Message sent!
```

### Unread Counts:

```
📊 Unread counts from backend: {
  personalUnread: 2,
  departmentUnread: 5,  ← Should have number!
  projectUnread: 3,     ← Should have number!
  groupUnread: 8,
  total: 10
}

🏷️ department tab unread count: 5
🏷️ project tab unread count: 3
🏷️ personal tab unread count: 2
```

---

## Action Plan

### Step 1: Restart Backend

```bash
# This fixes 90% of issues
cd Backend
dotnet run
```

### Step 2: Refresh Browser

```
Ctrl + Shift + R
```

### Step 3: Open Console

```
F12 → Console tab
```

### Step 4: Navigate to Chat

```
Click Chat in sidebar
Watch console logs
```

### Step 5: Test Attachment

```
1. Click "📎 Attach"
2. Select file
3. Click Send
4. Watch console output
```

### Step 6: Share Console Logs

```
Copy the console output showing:
- 📤 Upload logs
- 📊 Unread count logs
- Any error messages
```

---

## 🎯 Summary

**Two separate issues:**

1. **Attachment Upload 404**

   - Likely: Backend not running or endpoint not registered
   - Fix: Restart backend, verify endpoint exists

2. **Unread Counts Zero**
   - Likely: Backend needs restart for `.Include()` fix
   - Fix: Restart backend, refresh browser

**Both fixes: RESTART BACKEND! 🔄**
