using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

using Presentation.Models;

namespace Presentation.Services.Printing
{
    // Note: Requires Windows Desktop/WPF references. Intended for Windows 10/11 deployment.
    public class WindowsPrintService : IPrintService
    {
        private readonly PrintSettings _settings;
        private readonly List<PrintJob> _queue = new List<PrintJob>();

        public WindowsPrintService(PrintSettings settings)
        {
            _settings = settings;
        }

        public void Enqueue(string imagePath, int copies = 1)
        {
            _queue.Add(new PrintJob(imagePath, copies));
        }

        public IReadOnlyCollection<string> ListPrinters()
        {
            using var server = new LocalPrintServer();
            return server.GetPrintQueues().Select(p => p.FullName).ToArray();
        }

        public bool TryDequeue(out PrintJob job)
        {
            if (_queue.Count > 0)
            {
                job = _queue[0];
                _queue.RemoveAt(0);
                return true;
            }
            job = null;
            return false;
        }

        public IReadOnlyList<PrintJob> SnapshotQueue() => _queue.ToList();

        public void PrintNow(PrintJob job)
        {
            if (string.IsNullOrWhiteSpace(_settings.PrinterName))
                throw new InvalidOperationException("PrinterName is not configured");

            using var server = new LocalPrintServer();
            var queue = server.GetPrintQueues().FirstOrDefault(q => string.Equals(q.FullName, _settings.PrinterName, StringComparison.OrdinalIgnoreCase));
            if (queue == null)
                throw new InvalidOperationException($"Printer not found: {_settings.PrinterName}");

            var writer = PrintQueue.CreateXpsDocumentWriter(queue);

            var ticket = queue.UserPrintTicket ?? new PrintTicket();
            ticket.CopyCount = job.Copies;
            ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmerica4x6);
            ticket.PageBorderless = PageBorderless.Borderless;

            var doc = BuildFixedDocument(job.ImagePath, widthInInches: 6.0, heightInInches: 4.0);
            writer.Write(doc, ticket);
        }

        private static FixedDocument BuildFixedDocument(string imagePath, double widthInInches, double heightInInches)
        {
            // WPF uses 96 DPI device independent units
            double pageWidth = widthInInches * 96.0;
            double pageHeight = heightInInches * 96.0;

            var document = new FixedDocument
            {
                DocumentPaginator = { PageSize = new Size(pageWidth, pageHeight) }
            };

            var pageContent = new PageContent();
            var fixedPage = new FixedPage
            {
                Width = pageWidth,
                Height = pageHeight
            };

            var image = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(Path.GetFullPath(imagePath));
            bitmap.EndInit();
            image.Source = bitmap;

            var grid = new Grid
            {
                Width = pageWidth,
                Height = pageHeight,
                Background = Brushes.White
            };
            grid.Children.Add(image);

            FixedPage.SetLeft(grid, 0);
            FixedPage.SetTop(grid, 0);
            fixedPage.Children.Add(grid);

            ((IAddChild)pageContent).AddChild(fixedPage);
            document.Pages.Add(pageContent);
            return document;
        }
    }
}


