using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;

using Presentation.Models;

namespace Presentation.Services.Printing
{
    public class WindowsPrintService : IPrintService
    {
        private readonly PrintSettings _settings;
        private readonly List<PrintJob> _queue = new List<PrintJob>();
        private readonly object _queueLock = new object();

        public WindowsPrintService(PrintSettings settings)
        {
            _settings = settings;
        }

        public void Enqueue(string imagePath, int copies = 1)
        {
            lock (_queueLock)
            {
                _queue.Add(new PrintJob(imagePath, copies));
            }
        }

        public IReadOnlyCollection<string> ListPrinters()
        {
            return PrinterSettings.InstalledPrinters.Cast<string>().ToArray();
        }

        public bool TryDequeue(out PrintJob job)
        {
            lock (_queueLock)
            {
                if (_queue.Count > 0)
                {
                    job = _queue[0];
                    _queue.RemoveAt(0);
                    return true;
                }
            }
            job = null;
            return false;
        }

        public IReadOnlyList<PrintJob> SnapshotQueue()
        {
            lock (_queueLock)
            {
                return _queue.ToList();
            }
        }

        public void PrintNow(PrintJob job)
        {
            if (string.IsNullOrWhiteSpace(_settings.PrinterName))
                throw new InvalidOperationException("PrinterName is not configured");

            if (!File.Exists(job.ImagePath))
                throw new FileNotFoundException($"Image file not found: {job.ImagePath}");

            using var printDoc = new PrintDocument();
            printDoc.PrinterSettings.PrinterName = _settings.PrinterName;

            if (!printDoc.PrinterSettings.IsValid)
                throw new InvalidOperationException($"Printer not found or invalid: {_settings.PrinterName}");

            // Set paper size to 4x6 inches (101.6 x 152.4 mm)
            printDoc.PrinterSettings.DefaultPageSettings.PaperSize = Get4x6PaperSize(printDoc.PrinterSettings);
            
            // Set copies
            printDoc.PrinterSettings.Copies = (short)job.Copies;

            // Load image
            using var img = Image.FromFile(job.ImagePath);
            
            printDoc.PrintPage += (sender, e) =>
            {
                var graphics = e.Graphics;
                var pageBounds = e.PageBounds;
                
                // Calculate aspect ratio and fit image
                float imgAspect = (float)img.Width / img.Height;
                float pageAspect = (float)pageBounds.Width / pageBounds.Height;
                
                Rectangle destRect;
                if (imgAspect > pageAspect)
                {
                    // Image is wider - fit to width
                    int height = (int)(pageBounds.Width / imgAspect);
                    destRect = new Rectangle(0, (pageBounds.Height - height) / 2, pageBounds.Width, height);
                }
                else
                {
                    // Image is taller - fit to height
                    int width = (int)(pageBounds.Height * imgAspect);
                    destRect = new Rectangle((pageBounds.Width - width) / 2, 0, width, pageBounds.Height);
                }
                
                graphics.DrawImage(img, destRect);
                e.HasMorePages = false;
            };

            printDoc.Print();
        }

        private static PaperSize Get4x6PaperSize(PrinterSettings settings)
        {
            // Try to find existing 4x6 paper size
            foreach (PaperSize size in settings.PaperSizes)
            {
                // Common names for 4x6 paper
                if (size.PaperName.Contains("4x6") || 
                    size.PaperName.Contains("4 x 6") ||
                    size.PaperName.Contains("Photo") && size.Width == 400 && size.Height == 600) // 4x6 in 1/100 inches
                {
                    return size;
                }
            }

            // Create custom 4x6 size (4" x 6" = 400 x 600 in 1/100 inches)
            // Width and Height are in 1/100 of an inch
            return new PaperSize("Custom 4x6", 400, 600);
        }
    }
}
