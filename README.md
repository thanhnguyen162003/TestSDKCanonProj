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
