using System;

namespace Presentation.Models
{
    public class PrintSettings
    {
        public bool Enabled { get; set; } = true;
        public bool AutoPrint { get; set; } = true;
        public string PrinterName { get; set; } = string.Empty;
        public string PaperSize { get; set; } = "4x6";
        public string FitMode { get; set; } = "Fit";
        public int Copies { get; set; } = 1;
        public string SaveDirectory { get; set; } = "C://PhotoBooth//Captures";
    }
}


