/*
 * =============================================================================
 * ======================== CRITICAL WARNING ===================================
 * =============================================================================
 *
 * This code will VERY LIKELY FAIL with a 'BadImageFormatException'.
 *
 * WHY?
 * 1.  **64-bit vs 32-bit:** The Canon EDSDK (EDSDK.dll) is a 32-BIT (x86) library,
 * as confirmed by the PDF you provided (page 12).
 * 2.  **Your Project:** A .NET 8 Web API project runs as a 64-BIT (x64) process
 * by default.
 *
 * A 64-bit process CANNOT load a 32-bit DLL.
 *
 * Even if you force your Web API to run in 32-bit mode (see .csproj notes),
 * running hardware-control SDKs inside a web server is not recommended.
 * The server may not have USB permissions, and managing the camera's
 * state (like an open session) in a web request is very unstable.
 *
 * This file is provided for testing *initialization* only.
 * The recommended solution is to create a separate 32-bit Desktop App
 * (like a WPF or Windows Forms app) to control the camera.
 *
 * =============================================================================
 */

// Import the namespace from the Canon.EDSDK NuGet package
using Canon.EDSDK;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

// This endpoint attempts to initialize the EDSDK and list cameras.
app.MapGet("/test-camera", () =>
{
    // --- IMPORTANT ---
    // This assumes the native Canon DLLs (EDSDK.dll, EdsImage.dll, etc.)
    // from your screenshot are in the output folder (e.g., bin/Debug/net8.0)
    // where this .NET API is running.
    // The NuGet package is just a .NET wrapper; it needs the native DLLs.

    try
    {
        // 1. Attempt to initialize the SDK
        // This is the line that will likely throw a BadImageFormatException
        // if you are running in 64-bit mode.
        EDSDK.InitializeSDK();

        // 2. Attempt to get the camera list
        IntPtr cameraList = IntPtr.Zero;
        EDSDK.GetCameraList(out cameraList);

        // 3. Get the camera count
        int cameraCount = 0;
        EDSDK.GetChildCount(cameraList, out cameraCount);

        // 4. Release the list
        if (cameraList != IntPtr.Zero)
        {
            EDSDK.Release(cameraList);
        }

        // 5. Terminate the SDK
        EDSDK.TerminateSDK();

        // If we got this far, it's a miracle!
        return Results.Ok($"SUCCESS: SDK Initialized. Found {cameraCount} cameras.");
    }
    catch (DllNotFoundException ex)
    {
        // This error means the native Canon DLLs (EDSDK.dll, etc.) were not
        // found in the .exe's running directory.
        return Results.Problem(
            statusCode: 500,
            title: "DLL Not Found",
            detail: $"Could not find the native Canon DLLs (e.g., EDSDK.dll). " +
                    $"Make sure they are copied to the API's output directory. " +
                    $"Error: {ex.Message}"
        );
    }
    catch (BadImageFormatException ex)
    {
        // *** THIS IS THE MOST LIKELY ERROR ***
        // This means you are running a 64-bit API (default) and trying
        // to load the 32-bit Canon DLLs.
        return Results.Problem(
            statusCode: 500,
            title: "Architecture Mismatch (64-bit vs 32-bit)",
            detail: $"A 64-bit process tried to load a 32-bit DLL. " +
                    $"Your .NET 8 API is 64-bit by default, but EDSDK.dll is 32-bit. " +
                    $"You must force your project to 32-bit (x86). " +
                    $"Error: {ex.Message}"
        );
    }
    catch (Exception ex)
    {
        // Catch-all for other SDK errors
        // (e.g., no camera connected, USB permissions issue)
        return Results.Problem(
            statusCode: 500,
            title: "SDK Error",
            detail: $"An error occurred while testing the SDK. Error: {ex.Message}"
        );
    }
});

app.Run();
