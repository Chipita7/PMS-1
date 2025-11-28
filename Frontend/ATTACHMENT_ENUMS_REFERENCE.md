# 📋 Attachment System - Enums Reference

## Backend Enums

### AttachmentCategory

```csharp
public enum AttachmentCategory
{
    Project = 0,         // Project Documents
    Task = 1,            // Task Attachments
    Deliverable = 2,     // Deliverables
    Communication = 3,   // Team Communications (CHAT MESSAGES)
    Financial = 4,       // Financial Records
    Risk = 5,            // Risk Materials
    Compliance = 6       // Compliance Docs
}
```

### AccessibilityLevel

```csharp
public enum AccessibilityLevel
{
    Public = 0,      // Everyone with entity access
    Private = 1,     // Only uploader
    Protected = 2,   // Specific users/roles
    Internal = 3     // Internal use only
}
```

---

## Frontend Usage

### Upload File

```typescript
// For chat messages
await attachmentService.uploadFile(
  file,
  "Message",
  "0",
  3 // Category: Communication = 3
);

// For project documents
await attachmentService.uploadFile(
  file,
  "Project",
  projectId.toString(),
  0 // Category: Project = 0
);

// For task attachments
await attachmentService.uploadFile(
  file,
  "ProjectTask",
  taskId.toString(),
  1 // Category: Task = 1
);

// For deliverables
await attachmentService.uploadFile(
  file,
  "Milestone",
  milestoneId.toString(),
  2 // Category: Deliverable = 2
);
```

### Category Values

| Category      | Value | Use Case                               |
| ------------- | ----- | -------------------------------------- |
| Project       | 0     | Project documentation, plans, specs    |
| Task          | 1     | Task-related files, work items         |
| Deliverable   | 2     | Milestone deliverables, outputs        |
| Communication | 3     | **Chat messages, team communications** |
| Financial     | 4     | Budget files, invoices, receipts       |
| Risk          | 5     | Risk assessment documents              |
| Compliance    | 6     | Compliance, legal, regulatory files    |

### Accessibility Values

| Level     | Value | Who Can Access                             |
| --------- | ----- | ------------------------------------------ |
| Public    | 0     | Everyone with entity access (CHAT DEFAULT) |
| Private   | 1     | Only the uploader                          |
| Protected | 2     | Specific users/roles (via permissions)     |
| Internal  | 3     | Internal system use                        |

---

## Chat Integration

### Current Implementation

**Upload for Chat:**

```typescript
const uploadedAttachment = await attachmentService.uploadFile(
  selectedFile,
  entityType, // "Message" or "Project"
  entityId, // "0" or projectId
  3 // Communication = 3 ✅
);
```

**Access Level:**

- Always `0` (Public)
- Anyone in the chat (dept/project/personal) can access
- Automatic access control based on entity

---

## Fixed Upload Issues

### Problem:

```
POST /api/Attachments/upload
404 Not Found ❌
```

### Root Causes:

1. ❌ Category sent as string "Message" (backend expects enum number)
2. ❌ AccessibilityLevel might have been wrong value

### Solution:

```typescript
// Before (WRONG):
formData.append("Category", "Message"); // ❌ String
formData.append("AccessibilityLevel", "1"); // ❌ Private

// After (CORRECT):
formData.append("Category", "3"); // ✅ Communication enum
formData.append("AccessibilityLevel", "0"); // ✅ Public
```

---

## Testing

### Verify Upload Works

1. **Refresh browser** (Ctrl+Shift+R)
2. **Open Chat**
3. **Click "📎 Attach"**
4. **Select a file**
5. **Click Send**
6. **Check console** - should see "📎 File uploaded successfully"
7. **No 404 error!** ✅

### Console Output Expected

```
📎 File uploaded successfully: a1b2c3d4-5678-90ab-cdef-1234567890ab
Message sent!
```

---

## For Future Features

### Task Attachments

```typescript
await attachmentService.uploadFile(
  file,
  "ProjectTask",
  taskId.toString(),
  1 // Task = 1
);
```

### Project Documents

```typescript
await attachmentService.uploadFile(
  file,
  "Project",
  projectId.toString(),
  0 // Project = 0
);
```

### Deliverables

```typescript
await attachmentService.uploadFile(
  file,
  "Milestone",
  milestoneId.toString(),
  2 // Deliverable = 2
);
```

---

## ✅ Fixed!

**Chat attachments now use:**

- ✅ Correct category enum (Communication = 3)
- ✅ Correct accessibility (Public = 0)
- ✅ Proper FormData structure
- ✅ Backend-compatible format

**Upload should work perfectly now! 🚀**
