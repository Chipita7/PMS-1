# 🔄 How to Restart IIS

## ⚡ **SIMPLEST METHOD - One Command**

### **Step 1: Open PowerShell as Administrator**
1. Press `Win + X` (or right-click Start menu)
2. Click **"Windows PowerShell (Admin)"** or **"Terminal (Admin)"**
3. Click "Yes" on the security prompt

### **Step 2: Run IIS Reset**
```powershell
iisreset
```

**That's it!** This will:
- Stop IIS
- Apply your new backend files
- Start IIS again

---

## 🖱️ **ALTERNATIVE: Using IIS Manager GUI**

### **Option 1: Restart Entire IIS**
1. Press `Win + R`
2. Type `inetmgr` and press Enter
3. Click on your computer name in the left panel
4. On the right side, click **"Restart"** under "Manage Server"

### **Option 2: Restart Just Your App Pool**
1. Open IIS Manager (`Win + R` → `inetmgr`)
2. Expand your server
3. Click **"Application Pools"**
4. Find your API's app pool (usually named like "DefaultAppPool" or "PMS_API")
5. Right-click it
6. Click **"Recycle"** or **"Restart"**

---

## 📋 **DETAILED STEPS WITH SCREENSHOTS DESCRIPTIONS**

### **Method 1: PowerShell (Recommended - Fastest)**

**Step-by-step:**
```
1. Click Windows Start button
2. Type "PowerShell"
3. RIGHT-CLICK on "Windows PowerShell"
4. Click "Run as administrator"
5. Type: iisreset
6. Press Enter
7. Wait for:
   "Internet services successfully restarted"
8. Done!
```

---

### **Method 2: Services Application**

```
1. Press Win + R
2. Type: services.msc
3. Press Enter
4. Scroll down to find "World Wide Web Publishing Service"
5. Right-click it
6. Click "Restart"
7. Done!
```

---

## ⚠️ **IMPORTANT NOTES**

### **You MUST run as Administrator:**
- Regular PowerShell won't work
- You'll get "Access Denied" error
- Must use **"Run as Administrator"**

### **IIS Reset takes 10-30 seconds:**
- IIS stops (websites go offline briefly)
- New files are loaded
- IIS starts (websites come back online)
- Your API is updated!

---

## ✅ **VERIFY IIS RESTARTED**

After running `iisreset`, you should see:
```
Attempting stop...
Internet services successfully stopped
Attempting start...
Internet services successfully restarted
```

**Then test your API:**
- Open browser
- Go to your API URL (e.g., `http://localhost/YourAPI`)
- Should work with new changes!

---

## 🎯 **QUICK CHECKLIST**

- [ ] Open PowerShell as **Administrator**
- [ ] Type `iisreset`
- [ ] Press Enter
- [ ] Wait for "successfully restarted"
- [ ] Test your API
- [ ] ✅ Done!

---

## 🚨 **IF YOU GET ERRORS**

### **Error: "Access Denied"**
**Fix:** You didn't run PowerShell as Administrator
- Close PowerShell
- Right-click PowerShell
- Select "Run as administrator"

### **Error: "iisreset is not recognized"**
**Fix:** IIS might not be installed
- Use Services method instead
- Or install IIS first

### **Error: Service won't stop**
**Fix:** Something is holding files
- Open Task Manager
- End any w3wp.exe processes
- Try again

---

## 🎉 **THAT'S IT!**

**Simple command:**
```powershell
iisreset
```

**Your updated backend is now live!** 🚀

