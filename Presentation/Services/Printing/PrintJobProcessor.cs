using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Presentation.Models;

namespace Presentation.Services.Printing
{
    public class PrintJobProcessor : BackgroundService
    {
        private readonly WindowsPrintService _printService;
        private readonly PrintSettings _settings;

        public PrintJobProcessor(WindowsPrintService printService, PrintSettings settings)
        {
            _printService = printService;
            _settings = settings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_settings.Enabled)
                    {
                        await Task.Delay(500, stoppingToken);
                        continue;
                    }

                    if (_printService.TryDequeue(out var job))
                    {
                        try
                        {
                            _printService.PrintNow(job);
                        }
                        catch
                        {
                            job.Attempts++;
                            if (job.Attempts < 3)
                            {
                                // backoff and requeue
                                await Task.Delay(1000 * job.Attempts, stoppingToken);
                                _printService.Enqueue(job.ImagePath, job.Copies);
                            }
                            // else drop; in a real system we'd log persistently
                        }
                    }
                    else
                    {
                        await Task.Delay(200, stoppingToken);
                    }
                }
                catch
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}


