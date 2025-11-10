## Connect Canon R100 and Print via DNS/Network Printer (Windows)

This guide shows how to run the app, connect a Canon EOS R100 using Canon EDSDK, and auto‑print photos to a DNS/network printer (e.g., DNP) on Windows.

### Prerequisites
- **Windows 10/11** PC. The app uses Windows printing and Canon EDSDK; macOS/Linux are not supported at runtime.
- **.NET 8 SDK** installed.
- **Canon EDSDK runtime/USB driver** (install Canon EOS Utility or Canon driver package) and connect your Canon R100 via USB.
- **Printer installed in Windows**:
  - Install the vendor’s Windows driver (e.g., DNP DS‑series).
  - Add the printer in “Printers & Scanners”. For a network printer, you can add by DNS name or IP (Standard TCP/IP port), or by UNC path `\\printer-host\queue`. Confirm it can print a test page from Windows.

### Build and Run
1) Open the solution `TestSDKProj.sln` in Rider/Visual Studio on Windows.
2) Set startup project to `Presentation`.
3) Ensure `Presentation/Application.csproj` has `PlatformTarget` set to `x86` (required by EDSDK) — it already is.
4) Build and Run. Swagger UI will be available at `https://localhost:<port>/swagger`.

### Configure Printing
Edit `Presentation/appsettings.json`:

```json
{
  "Printing": {
    "Enabled": true,
    "AutoPrint": true,
    "PrinterName": "Your Windows Printer Queue Name",
    "PaperSize": "4x6",
    "FitMode": "Fit",
    "Copies": 1,
    "SaveDirectory": "C://PhotoBooth//Captures"
  }
}
```

- **PrinterName**: Set to the exact Windows printer queue name as it appears in “Printers & Scanners” (e.g., `DNP DS-RX1HS`). You may also use a UNC like `\\\\print-server\\photo-printer` if applicable. The app uses `System.Drawing.Printing.PrinterSettings.PrinterName` and will validate it.
- **SaveDirectory**: JPEGs are downloaded here from the camera and the app auto‑enqueues prints when `AutoPrint` is true.

You can list recognized printers via API later to confirm the name.

### Canon R100 Flow (EDSDK)
The app initializes the Canon EDSDK once and exposes endpoints to control the camera. Typical flow:

1) Open Swagger: `https://localhost:<port>/swagger`.
2) Discover cameras:
   - GET `/api/cameras` → copy the `Ref` of your Canon R100.
3) Open a session with the camera:
   - POST `/api/cameras/{cameraRef}/session` (paste the `Ref`).
4) Optional checks/controls:
   - GET `/api/cameras/status` (session and camera name).
   - GET `/api/cameras/settings/{propertyId}` and `/list` to read available values.
   - POST `/api/cameras/settings/{propertyId}?value=...` to set values (ISO/Av/Tv/etc.).
5) Capture:
   - POST `/api/cameras/photo` to take a photo.
   - The image is saved to `Printing.SaveDirectory`. The app raises `PhotoSaved` and, if `Printing.Enabled` and `AutoPrint` are true, automatically enqueues the file to print.

Notes:
- The app sets SaveTo=Host and a large capacity so files are transferred to the PC automatically.
- One camera session is supported at a time.

### Printing Flow (DNS/Network Printers)
Printing is handled by `WindowsPrintService` using the Windows print spooler:

- The configured `PrinterName` must match an installed Windows printer. Network printers added by hostname/IP or UNC will appear in the installed printers list and are supported.
- Paper size is requested as 4x6; final behavior depends on driver capabilities and settings.
- Auto‑print occurs on every new photo saved when enabled.

Useful printing endpoints in Swagger:

- GET `/api/printing/printers` — Verify your target printer is listed.
- GET `/api/printing/settings` — Inspect current in‑memory settings.
- PUT `/api/printing/settings` — Update settings in memory (body is a `PrintSettings` JSON).
- POST `/api/printing/test` — Enqueue the most recent JPEG in `SaveDirectory` to test printing.
- GET `/api/printing/queue` — See current print queue snapshot.
- POST `/api/printing/reprint?path=C:\\path\\to\\file.jpg` — Reprint a specific file (or most recent if `path` omitted).

### End‑to‑End Quick Start (Swagger)
1) GET `/api/cameras` → copy `Ref`.
2) POST `/api/cameras/{cameraRef}/session`.
3) GET `/api/printing/printers` → confirm your `PrinterName` is listed.
4) POST `/api/cameras/photo` → photo is saved and auto‑printed if enabled.
5) GET `/api/printing/queue` → see queued/processed jobs.

### Troubleshooting
- Camera not found: ensure R100 is on, connected via USB, and drivers are installed.
- Session not open: call the session open endpoint before other operations.
- Printer not found/invalid: confirm the exact Windows queue name in `PrinterName`, and that a Windows test page prints successfully.
- No auto‑print: ensure `Enabled=true`, `AutoPrint=true`, and that `SaveDirectory` exists (the app also tries to create it at startup).

### Compatibility Notes
- Canon compatibility depends on the EDSDK version. The project references Canon EDSDK 3.6.x and targets x86 as required; Canon EOS R100 is supported by recent EDSDKs when proper drivers are installed.
- Printing uses `System.Drawing.Printing` on Windows; DNS/network printers are supported via Windows’ installed printer queues.

### Security/Operational Notes
- This service exposes device control endpoints. Restrict access on production networks.
- Do not run as admin unless required by your environment/policy.


