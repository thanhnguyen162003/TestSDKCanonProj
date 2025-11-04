using System;

namespace Presentation.Services.Printing
{
    public class PrintJob
    {
        public string ImagePath { get; }
        public int Copies { get; }
        public int Attempts { get; set; }
        public DateTime EnqueuedAt { get; } = DateTime.UtcNow;

        public PrintJob(string imagePath, int copies)
        {
            ImagePath = imagePath;
            Copies = copies < 1 ? 1 : copies;
        }
    }
}


