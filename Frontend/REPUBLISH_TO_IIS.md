# 🚀 How to Republish Backend to IIS

## ✅ **YOUR CHANGES ARE SAVED**

All backend changes are already in your code files. Now you need to republish to IIS.

---

## 📍 **YOUR CURRENT PUBLISH LOCATION**

Based on your publish profiles:
- **FolderProfile:** `C:\Users\pc\Music\publish`
- **FolderProfile1:** `C:\Users\abete\Music\publish`

---

## 🔧 **OPTION 1: Quick Republish (Command Line)**

### **From Backend folder** (where you are now):

```powershell
# Use the existing publish profile
dotnet publish -c Release /p:PublishProfile=FolderProfile
```

**Or if you used FolderProfile1:**
```powershell
dotnet publish -c Release /p:PublishProfile=FolderProfile1
```

---

## 🔧 **OPTION 2: Publish to Custom Location**

### **If you want to publish to IIS folder directly:**

```powershell
# Publish to IIS folder (adjust path to your IIS site)
dotnet publish -c Release -o "C:\inetpub\wwwroot\YourAPIName"
```

**Common IIS paths:**
- `C:\inetpub\wwwroot\PMS_API`
- `C:\inetpub\wwwroot\ProjectManagement`
- Or wherever your IIS site points to

---

## 🔧 **OPTION 3: Using Visual Studio (If Available)**

1. **Open** `PMS.sln` in Visual Studio
2. **Right-click** Backend project
3. **Click** "Publish"
4. **Select** existing publish profile (FolderProfile or FolderProfile1)
5. **Click** "Publish" button
6. Wait for completion
7. ✅ Done!

---

## 📋 **COMPLETE REPUBLISH PROCESS**

### **Step 1: Publish the Application**

```powershell
# You're already in Backend folder, so:
dotnet publish -c Release /p:PublishProfile=FolderProfile
```

**This will:**
- ✅ Build your code with all changes
- ✅ Create optimized Release build
- ✅ Copy files to: `C:\Users\pc\Music\publish`
- ✅ Include all dependencies

---

### **Step 2: Stop IIS Application Pool**

```powershell
# Open PowerShell as Administrator
# Replace "YourAppPoolName" with your actual app pool name

# Stop the app pool
Stop-WebAppPool -Name "YourAppPoolName"

# Wait a moment
Start-Sleep -Seconds 3
```

**Or use IIS Manager GUI:**
1. Open IIS Manager (inetmgr)
2. Click "Application Pools"
3. Find your app pool
4. Click "Stop"

---

### **Step 3: Copy Published Files to IIS**

```powershell
# Copy from publish folder to IIS folder
# Adjust paths to match your setup

xcopy "C:\Users\pc\Music\publish\*" "C:\inetpub\wwwroot\YourAPIName\" /E /I /Y
```

**Or manually:**
1. Open `C:\Users\pc\Music\publish`
2. Copy all files
3. Paste to your IIS application folder
4. Replace existing files

---

### **Step 4: Start IIS Application Pool**

```powershell
# Start the app pool
Start-WebAppPool -Name "YourAppPoolName"
```

**Or use IIS Manager GUI:**
1. IIS Manager
2. Application Pools
3. Your app pool
4. Click "Start"

---

### **Step 5: Verify Deployment**

```powershell
# Test the API
curl http://localhost/YourAPIName/api/health

# Or open browser:
# http://localhost/YourAPIName/swagger
```

---

## ⚡ **QUICK ONE-COMMAND SOLUTION**

If you want to do it all at once:

```powershell
# Publish to IIS directory directly
dotnet publish -c Release -o "C:\inetpub\wwwroot\YourAPIName" --force
```

**This:**
- ✅ Builds Release version
- ✅ Publishes directly to IIS folder
- ✅ Overwrites existing files
- ✅ No need to copy manually

**Note:** IIS might lock some files. If you get errors, stop the app pool first.

---

## 🎯 **RECOMMENDED APPROACH**

### **For Your Demo (Safest):**

```powershell
# 1. Publish to folder
dotnet publish -c Release /p:PublishProfile=FolderProfile

# 2. Stop IIS (in Admin PowerShell)
Stop-WebAppPool -Name "YourAppPoolName"

# 3. Copy files (adjust paths)
xcopy "C:\Users\pc\Music\publish\*" "C:\inetpub\wwwroot\YourAPIName\" /E /I /Y

# 4. Start IIS
Start-WebAppPool -Name "YourAppPoolName"

# 5. Test
# Open: http://localhost/YourAPIName/swagger
```

---

## 📋 **WHAT YOU NEED TO KNOW**

### **Your IIS Setup Info (Find these):**
- **App Pool Name:** (check IIS Manager)
- **Physical Path:** (where your API files are)
- **Site Bindings:** (what URL it uses)

**To find these:**
1. Open IIS Manager (Run: `inetmgr`)
2. Click on your application
3. Check "Basic Settings" on the right
4. Note the App Pool name and Physical Path

---

## 🔄 **ALTERNATIVE: Publish & Deploy Script**

Create a file: `Backend/publish-to-iis.ps1`

```powershell
# Publish & Deploy to IIS Script

$AppPoolName = "YourAppPoolName"  # ← Change this
$IISPath = "C:\inetpub\wwwroot\YourAPIName"  # ← Change this
$PublishFolder = "C:\Users\pc\Music\publish"

Write-Host "📦 Publishing application..." -ForegroundColor Cyan
dotnet publish -c Release /p:PublishProfile=FolderProfile

Write-Host "⏸️ Stopping IIS App Pool..." -ForegroundColor Yellow
Stop-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 3

Write-Host "📁 Copying files to IIS..." -ForegroundColor Cyan
xcopy "$PublishFolder\*" "$IISPath\" /E /I /Y

Write-Host "▶️ Starting IIS App Pool..." -ForegroundColor Green
Start-WebAppPool -Name $AppPoolName

Write-Host "✅ Deployment complete!" -ForegroundColor Green
Write-Host "🌐 Test at: http://localhost/YourAPIName/swagger" -ForegroundColor Cyan
```

**Then run:**
```powershell
# As Administrator
.\publish-to-iis.ps1
```

---

## 🎯 **SIMPLEST FOR NOW**

If you just want to update IIS quickly:

```powershell
# From Backend folder:
dotnet publish -c Release /p:PublishProfile=FolderProfile
```

**Then:**
- Manually copy files from `C:\Users\pc\Music\publish` to your IIS folder
- Restart your IIS app pool
- Test the API

---

## ✅ **SUMMARY**

**Your changes are saved** ✅  
**Need to republish** ✅  
**Command:** `dotnet publish -c Release /p:PublishProfile=FolderProfile`  
**Then:** Copy to IIS folder and restart app pool  

Would you like me to create a complete publish script for you?
