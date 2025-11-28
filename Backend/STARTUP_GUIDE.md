# Backend Startup Guide

## Quick Start

1. **Navigate to the Backend directory:**
   ```bash
   cd Backend
   ```

2. **Start the backend:**
   ```bash
   dotnet run
   ```
   Or use Visual Studio and run the project (press F5).

3. **Verify the backend is running:**
   - Open your browser and go to: `http://localhost:8080/`
   - You should see the Swagger UI
   - If Swagger loads, the backend is running correctly

4. **Test the API:**
   - In Swagger, try the `/test` endpoint (GET) - it should return "API is running"
   - Try the `/api/Auth/login` endpoint with your credentials

## Troubleshooting

### Backend won't start on port 8080

1. **Check if port 8080 is already in use:**
   ```bash
   netstat -ano | findstr :8080
   ```
   If something is using port 8080, either:
   - Stop that application, OR
   - Change the port in `Properties/launchSettings.json`

2. **Check database connection:**
   - Ensure SQL Server is running
   - Verify the connection string in `appsettings.json`
   - The connection string should point to: `Server=localhost\\SQLExpress;Database=AppDb`

3. **Check for compilation errors:**
   ```bash
   dotnet build
   ```
   Fix any errors before running.

### Backend starts but frontend can't connect

1. **Verify CORS is configured:**
   - Check `Program.cs` - CORS should allow `http://localhost:5173` (Vite dev server)
   - If your frontend runs on a different port, add it to the CORS origins

2. **Check the backend URL:**
   - Frontend should be configured to use: `http://localhost:8080/api`
   - Verify in `Frontend/src/lib/api.ts`

3. **Check browser console:**
   - Look for CORS errors
   - Look for connection refused errors
   - Verify the exact error message

### Swagger shows "Failed to fetch /swagger/v1/swagger.json"

1. **Verify backend is running:**
   - Check the console output for any errors
   - Verify the backend is listening on port 8080

2. **Check Swagger configuration:**
   - In `Program.cs`, verify `app.UseSwagger()` and `app.UseSwaggerUI()` are called
   - Check that the route prefix is set correctly

3. **Try accessing Swagger JSON directly:**
   - Go to: `http://localhost:8080/swagger/v1/swagger.json`
   - If this loads, Swagger UI should work

## Common Issues

### Issue: "Database connection failed"
**Solution:** 
- Ensure SQL Server Express is running
- Check the connection string in `appsettings.json`
- Verify the database `AppDb` exists (it will be created automatically if using migrations)

### Issue: "Port 8080 is already in use"
**Solution:**
- Change the port in `Properties/launchSettings.json` to a different port (e.g., 8081)
- Update `appsettings.json` JWT settings to match the new port
- Update frontend `api.ts` to use the new port

### Issue: "JWT token validation failed"
**Solution:**
- Ensure JWT Issuer and Audience in `appsettings.json` match the backend URL
- If using HTTP (not HTTPS), ensure `RequireHttpsMetadata = false` in `Program.cs`
- Verify the SecretKey is the same Base64 string

## Verification Checklist

- [ ] Backend compiles without errors (`dotnet build`)
- [ ] Backend starts without errors (`dotnet run`)
- [ ] Swagger UI loads at `http://localhost:8080/`
- [ ] `/test` endpoint returns "API is running"
- [ ] Database connection is successful (check console logs)
- [ ] CORS is configured for frontend origin
- [ ] JWT settings match the backend URL
- [ ] Frontend can connect to `http://localhost:8080/api`

## Next Steps

Once the backend is running:
1. Start the frontend dev server
2. Try logging in through the frontend
3. Check browser console for any errors
4. Verify API calls are successful

