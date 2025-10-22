using Presentation.Kernels;
using Presentation.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Inject 1 time
builder.Services.AddSingleton<SDKHandler>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// Global exception middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(string.Format(LogConstants.HTTP_ERROR_PREFIX, ex.Message));
    }
});

/*------------------------- Minimal API Endpoints -------------------------*/

app.MapGet("/api/cameras", (SDKHandler sdkHandler) =>
{
    try
    {
        var cameras = sdkHandler.GetCameraList();
        return Results.Ok(cameras.Select(c => new
        {
            ProductName = c.Info.szDeviceDescription,
            PortName = c.Info.szPortName,
            DeviceSubType = c.Info.DeviceSubType,
            Ref = c.Ref.ToString()
        }));
    }
    catch (Exception ex)
    {
        return Results.Problem($"{LogConstants.FAILED_TO_GET_CAMERA_LIST}: {ex.Message}");
    }
})
.WithName("GetCameras")
.WithOpenApi();

// Open session with a camera
app.MapPost("/api/cameras/{cameraRef}/session", (IntPtr cameraRef, SDKHandler sdkHandler) =>
{
    try
    {
        var cameras = sdkHandler.GetCameraList();
        var camera = cameras.FirstOrDefault(c => c.Ref == cameraRef);
        
        if (camera == null)
            return Results.NotFound(LogConstants.CAMERA_NOT_FOUND);

        sdkHandler.OpenSession(camera);
        return Results.Ok(new { Message = LogConstants.SESSION_OPENED_SUCCESSFULLY, CameraRef = cameraRef.ToString() });
    }
    catch (Exception ex)
    {
        return Results.Problem($"{LogConstants.FAILED_TO_OPEN_SESSION}: {ex.Message}");
    }
})
.WithName("OpenSession")
.WithOpenApi();

// Close session
app.MapDelete("/api/cameras/session", (SDKHandler sdkHandler) =>
{
    try
    {
        sdkHandler.CloseSession();
        return Results.Ok(new { Message = LogConstants.SESSION_CLOSED_SUCCESSFULLY });
    }
    catch (Exception ex)
    {
        return Results.Problem($"{LogConstants.FAILED_TO_CLOSE_SESSION}: {ex.Message}");
    }
})
.WithName("CloseSession")
.WithOpenApi();

// Get camera settings
app.MapGet("/api/cameras/settings/{propertyId}", (uint propertyId, SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        var value = sdkHandler.GetSetting(propertyId);
        return Results.Ok(new { PropertyId = propertyId, Value = value });
    }
    catch (Exception ex)
    {
        return Results.Problem($"{LogConstants.FAILED_TO_GET_SETTING}: {ex.Message}");
    }
})
.WithName("GetSetting")
.WithOpenApi();

// Set camera settings
app.MapPost("/api/cameras/settings/{propertyId}", (uint propertyId, uint value, SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.SetSetting(propertyId, value);
        return Results.Ok(new { Message = LogConstants.SETTING_UPDATED_SUCCESSFULLY, PropertyId = propertyId, Value = value });
    }
    catch (Exception ex)
    {
        return Results.Problem($"{LogConstants.FAILED_TO_SET_SETTING}: {ex.Message}");
    }
})
.WithName("SetSetting")
.WithOpenApi();

// Get available settings list
app.MapGet("/api/cameras/settings/{propertyId}/list", (uint propertyId, SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        var settings = sdkHandler.GetSettingsList(propertyId);
        return Results.Ok(new { PropertyId = propertyId, AvailableValues = settings });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to get settings list: {ex.Message}");
    }
})
.WithName("GetSettingsList")
.WithOpenApi();

// Take a photo
app.MapPost("/api/cameras/photo", (SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.TakePhoto();
        return Results.Ok(new { Message = LogConstants.PHOTO_COMMAND_SENT_SUCCESSFULLY });
    }
    catch (Exception ex)
    {
        return Results.Problem($"{LogConstants.FAILED_TO_TAKE_PHOTO}: {ex.Message}");
    }
})
.WithName("TakePhoto")
.WithOpenApi();

// Take a photo in bulb mode
app.MapPost("/api/cameras/photo/bulb", (uint bulbTime, SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.TakePhoto(bulbTime);
        return Results.Ok(new { Message = "Bulb photo command sent successfully", BulbTime = bulbTime });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to take bulb photo: {ex.Message}");
    }
})
.WithName("TakeBulbPhoto")
.WithOpenApi();

// Start live view
app.MapPost("/api/cameras/liveview/start", (SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.StartLiveView();
        return Results.Ok(new { Message = "Live view started successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to start live view: {ex.Message}");
    }
})
.WithName("StartLiveView")
.WithOpenApi();

// Stop live view
app.MapPost("/api/cameras/liveview/stop", (bool lvOff, SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.StopLiveView(lvOff);
        return Results.Ok(new { Message = "Live view stopped successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to stop live view: {ex.Message}");
    }
})
.WithName("StopLiveView")
.WithOpenApi();

// Start filming
app.MapPost("/api/cameras/filming/start", (SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.StartFilming();
        return Results.Ok(new { Message = "Filming started successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to start filming: {ex.Message}");
    }
})
.WithName("StartFilming")
.WithOpenApi();

// Stop filming
app.MapPost("/api/cameras/filming/stop", (SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.StopFilming();
        return Results.Ok(new { Message = "Filming stopped successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to stop filming: {ex.Message}");
    }
})
.WithName("StopFilming")
.WithOpenApi();

// Set focus
app.MapPost("/api/cameras/focus", (uint speed, SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.SetFocus(speed);
        return Results.Ok(new { Message = "Focus command sent successfully", Speed = speed });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to set focus: {ex.Message}");
    }
})
.WithName("SetFocus")
.WithOpenApi();

// Lock/Unlock camera UI
app.MapPost("/api/cameras/ui/lock", (bool lockState, SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.UILock(lockState);
        return Results.Ok(new { Message = $"Camera UI {(lockState ? "locked" : "unlocked")} successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to set UI lock state: {ex.Message}");
    }
})
.WithName("SetUILock")
.WithOpenApi();

// Set capacity
app.MapPost("/api/cameras/capacity", (int bytesPerSector, int numberOfFreeClusters, SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        sdkHandler.SetCapacity(bytesPerSector, numberOfFreeClusters);
        return Results.Ok(new { Message = "Capacity set successfully", BytesPerSector = bytesPerSector, NumberOfFreeClusters = numberOfFreeClusters });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to set capacity: {ex.Message}");
    }
})
.WithName("SetCapacity")
.WithOpenApi();

// Get camera status
app.MapGet("/api/cameras/status", (SDKHandler sdkHandler) =>
{
    try
    {
        return Results.Ok(new
        {
            SessionOpen = sdkHandler.CameraSessionOpen,
            LiveViewOn = sdkHandler.IsLiveViewOn,
            IsFilming = sdkHandler.IsFilming,
            MainCamera = sdkHandler.MainCamera?.Info.szDeviceDescription ?? LogConstants.NONE
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to get camera status: {ex.Message}");
    }
})
.WithName("GetCameraStatus")
.WithOpenApi();

// Get all camera entries (files and folders)
app.MapGet("/api/cameras/entries", (SDKHandler sdkHandler) =>
{
    try
    {
        if (!sdkHandler.CameraSessionOpen)
            return Results.BadRequest(LogConstants.NO_CAMERA_SESSION_OPEN);

        var entries = sdkHandler.GetAllEntries();
        return Results.Ok(SerializeCameraFileEntry(entries));
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to get camera entries: {ex.Message}");
    }
})
.WithName("GetCameraEntries")
.WithOpenApi();

// Serialize CameraFileEntry
object SerializeCameraFileEntry(CameraFileEntry entry)
{
    return new
    {
        Name = entry.Name,
        IsFolder = entry.IsFolder,
        SubEntries = entry.Entries?.Select(SerializeCameraFileEntry).ToArray()
    };
}

app.Run();
