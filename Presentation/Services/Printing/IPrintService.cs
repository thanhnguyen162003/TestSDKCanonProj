using System.Collections.Generic;

namespace Presentation.Services.Printing
{
    public interface IPrintService
    {
        void Enqueue(string imagePath, int copies = 1);
        IReadOnlyCollection<string> ListPrinters();
    }
}


