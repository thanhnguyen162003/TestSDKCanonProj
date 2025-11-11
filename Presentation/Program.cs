using Presentation.Kernels;
using Presentation.Constants;
using Microsoft.Extensions.Options;
using Presentation.Models;
using Presentation.Services.Printing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SDKHandler>();
builder.Services.Configure<PrintSettings>(builder.Configuration.GetSection("Printing"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<PrintSettings>>().Value);
builder.Services.AddSingleton<WindowsPrintService>();
builder.Services.AddSingleton<IPrintService>(sp => sp.GetRequiredService<WindowsPrintService>());
builder.Services.AddHostedService<PrintJobProcessor>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

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
// Wire auto-print subscription at startup
var sdkHandler = app.Services.GetRequiredService<SDKHandler>();
var printSettings = app.Services.GetRequiredService<PrintSettings>();
var printService = app.Services.GetRequiredService<IPrintService>();
try
{
    System.IO.Directory.CreateDirectory(printSettings.SaveDirectory);
    sdkHandler.ImageSaveDirectory = printSettings.SaveDirectory;
    sdkHandler.PhotoSaved += path =>
    {
        if (printSettings.Enabled && printSettings.AutoPrint)
        {
            printService.Enqueue(path, printSettings.Copies);
        }
    };
}
catch { }


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

app.MapGet("/api/cameras/diagnostics", (SDKHandler sdkHandler) =>
{
    try
    {
        var blockingProcesses = new List<string>();
        var canonProcessNames = new[] { "EOS Utility", "CameraWindow", "Canon Camera Connect", "EOS Utility 3", "EOS Utility 4", "DPP", "Digital Photo Professional" };
        
        foreach (var process in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                var processName = process.ProcessName;
                if (canonProcessNames.Any(canon => processName.Contains(canon, StringComparison.OrdinalIgnoreCase)))
                {
                    blockingProcesses.Add($"{processName} (PID: {process.Id})");
                }
            }
            catch
            {
            }
        }
        
        var cameraInfo = new List<object>();
        string cameraConnectivityStatus = "Unknown";
        
        try
        {
            var cameras = sdkHandler.GetCameraList();
            cameraConnectivityStatus = cameras.Count > 0 
                ? $"Found {cameras.Count} connected camera(s)" 
                : "No cameras detected";
            
            foreach (var camera in cameras)
            {
                cameraInfo.Add(new
                {
                    ProductName = camera.Info.szDeviceDescription,
                    PortName = camera.Info.szPortName,
                    Ref = camera.Ref.ToString(),
                    DeviceSubType = camera.Info.DeviceSubType
                });
            }
        }
        catch (Exception ex)
        {
            cameraConnectivityStatus = $"Error checking cameras: {ex.Message}";
        }
        
        var hasBlockingProcesses = blockingProcesses.Any();
        var hasCameras = cameraInfo.Any();
        
        string message;
        if (hasBlockingProcesses)
        {
            message = $"Found {blockingProcesses.Count} Canon process(es) that may be blocking camera access. Close these processes before opening a session.";
        }
        else if (!hasCameras)
        {
            message = "No cameras detected. Ensure camera is: 1) Powered ON, 2) Connected via USB, 3) In P/Tv/Av/M mode (not scene or video mode), 4) USB drivers installed correctly.";
        }
        else
        {
            message = "No blocking Canon processes detected. If you're still getting error 0x7, the camera may be in an unsupported mode. CRITICAL: Verify camera mode dial is in P/Tv/Av/M position (NOT scene modes, video, or auto). Try: 1) Power cycle camera (OFF → wait 5s → ON), 2) Unplug/replug USB cable, 3) Ensure camera is not in playback/review mode, 4) Check camera menu for any PC connection settings.";
        }
        
        return Results.Ok(new
        {
            BlockingProcesses = blockingProcesses,
            HasBlockingProcesses = hasBlockingProcesses,
            CameraConnectivity = cameraConnectivityStatus,
            AvailableCameras = cameraInfo,
            SessionOpen = sdkHandler.CameraSessionOpen,
            Message = message
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to check diagnostics: {ex.Message}");
    }
})
.WithName("GetCameraDiagnostics")
.WithOpenApi();

app.MapGet("/api/printing/printers", (IPrintService svc) =>
{
    return Results.Ok(new { Printers = svc.ListPrinters() });
}).WithName("ListPrinters").WithOpenApi();

app.MapGet("/api/printing/settings", (IOptions<PrintSettings> options) =>
{
    return Results.Ok(options.Value);
}).WithName("GetPrintSettings").WithOpenApi();

app.MapPut("/api/printing/settings", (PrintSettings input, IOptionsMonitor<PrintSettings> monitor, IServiceProvider sp) =>
{
    var field = typeof(OptionsMonitor<PrintSettings>).GetField("_currentValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    if (field != null && monitor is OptionsMonitor<PrintSettings> mon)
    {
        field.SetValue(mon, input);
    }
    return Results.Ok(new { Message = "Print settings updated in-memory" });
}).WithName("UpdatePrintSettings").WithOpenApi();

app.MapPost("/api/printing/test", (IPrintService svc, IOptions<PrintSettings> opts) =>
{
    var dir = opts.Value.SaveDirectory;
    if (!System.IO.Directory.Exists(dir)) return Results.BadRequest("Save directory does not exist");
    var file = new DirectoryInfo(dir).GetFiles("*.jpg").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault()
               ?? new DirectoryInfo(dir).GetFiles("*.jpeg").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
    if (file == null) return Results.BadRequest("No JPEG found to test print");
    svc.Enqueue(file.FullName, 1);
    return Results.Ok(new { Message = "Test print enqueued", File = file.FullName });
}).WithName("TestPrint").WithOpenApi();

app.MapGet("/api/printing/queue", (WindowsPrintService svc) =>
{
    var items = svc.SnapshotQueue().Select(j => new { j.ImagePath, j.Copies, j.Attempts, j.EnqueuedAt });
    return Results.Ok(items);
}).WithName("GetPrintQueue").WithOpenApi();

app.MapPost("/api/printing/reprint", (WindowsPrintService svc, string? path, IOptions<PrintSettings> opts) =>
{
    var target = path;
    if (string.IsNullOrWhiteSpace(target))
    {
        var dir = opts.Value.SaveDirectory;
        if (!System.IO.Directory.Exists(dir)) return Results.BadRequest("Save directory does not exist");
        var file = new DirectoryInfo(dir).GetFiles("*.jpg").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault()
                   ?? new DirectoryInfo(dir).GetFiles("*.jpeg").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
        if (file == null) return Results.BadRequest("No JPEG found to reprint");
        target = file.FullName;
    }
    svc.Enqueue(target, 1);
    return Results.Ok(new { Message = "Reprint enqueued", File = target });
}).WithName("Reprint").WithOpenApi();

app.MapPost("/api/cameras/session", async (string cameraRef, SDKHandler sdkHandler, ILogger<Program> logger) =>
{
    try
    {
        if (!IntPtr.TryParse(cameraRef, out var cameraRefPtr))
            return Results.BadRequest("Invalid camera reference format.");
        
        logger.LogInformation("Opening session for camera ref: {CameraRef}", cameraRef);
        
        if (sdkHandler.CameraSessionOpen)
        {
            logger.LogInformation("Closing existing session");
            try { sdkHandler.CloseSession(); }
            catch (Exception ex) { logger.LogWarning("Error closing session: {Error}", ex.Message); }
            await Task.Delay(500);
        }
        var cameras = sdkHandler.GetCameraList();
        var camera = cameras.FirstOrDefault(c => c.Ref == cameraRefPtr);

        if (camera == null)
            return Results.NotFound(LogConstants.CAMERA_NOT_FOUND);

        sdkHandler.OpenSession(camera);
        return Results.Ok(new 
        { 
            Message = LogConstants.SESSION_OPENED_SUCCESSFULLY, 
            CameraRef = cameraRef,
            CameraName = camera.Info.szDeviceDescription,
            PortName = camera.Info.szPortName
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to open session");
        
        var errorDetail = ex.Message;
        var suggestions = new List<string>();
        
        if (ex.Message.Contains("SDK Error: 0x7"))
        {
            errorDetail += " | NOT_SUPPORTED - Camera refuses connection.";
            suggestions.Add("Mode dial is in P/Tv/Av/M (physically verify)");
            suggestions.Add("Exit playback mode (press shutter halfway)");
            suggestions.Add("Close menu if open");
            suggestions.Add("Power cycle camera (OFF -> wait 10s -> ON)");
            suggestions.Add("Try Manual (M) mode instead of P");
            suggestions.Add("Ensure camera is not busy/processing");
            suggestions.Add("Check camera battery level");
            suggestions.Add("Disable Wi‑Fi/Bluetooth/NFC on camera (USB remote control requires Wi‑Fi OFF)");
            suggestions.Add("In camera menu, set USB connection to 'PC remote' or 'PC connection'");
            suggestions.Add("Unplug and replug USB cable directly to PC (avoid hubs)");
            suggestions.Add("Close any mobile/PC apps connected to the camera (Canon/third‑party)");
        }
        else if (ex.Message.Contains("SDK Error: 0xC0") || ex.Message.Contains("SDK Error: 0xc0"))
        {
            errorDetail += " | Port in use. Close Canon software, unplug USB, wait 10s, reconnect.";
        }
        else if (ex.Message.Contains("SDK Error: 0x201E") || ex.Message.Contains("SDK Error: 0x201e"))
        {
            errorDetail += " | Session already open. Call DELETE /api/cameras/session first.";
        }
        
        return Results.Problem(
            detail: $"{LogConstants.FAILED_TO_OPEN_SESSION}: {errorDetail}",
            extensions: suggestions.Any() ? new Dictionary<string, object?> { ["suggestions"] = suggestions } : null
        );
    }
})
.WithName("OpenSession")
.WithOpenApi();

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
