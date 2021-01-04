using Iot.Device.Graphics;
using Iot.Device.LEDMatrix;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System.Threading;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

namespace NegativeEddy.PresencePi.Display
{
    public class PresenceRgbDisplay : IHostedService
    {
        private readonly ILogger<PresenceRgbDisplay> _logger;
        private readonly PresenceStore _presenceStore;

        public PresenceRgbDisplay(ILogger<PresenceRgbDisplay> logger, PresenceStore presenceStore)
        {
            _logger = logger;
            _presenceStore = presenceStore;
            _presenceStore.PresenceChanging += Updater_PresenceChanging;
        }

#if !WINDOWS
        RGBLedMatrix _matrix;
#endif

        BdfFont _font;
        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("starting");

            await InitializeRgbMatrix();
            _font = BdfFont.Load(@"fonts/5x7.bdf");
            RenderPresence(new Presence { Activity = "Loading" }, _font);

            _logger.LogInformation("started");
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
// method must be async but will be empty on Windows builds
        private async Task InitializeRgbMatrix()
        {
#if !WINDOWS
            PinMapping mapping = PinMapping.MatrixBonnetMapping32;
            _matrix = new RGBLedMatrix(mapping, 32, 16);
            _matrix.StartRendering();
            await Task.Delay(100);

            _matrix.Fill(0, 0, 0);
#endif
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private void ShutdownRgbMatrix()
        {
#if !WINDOWS
            _matrix.StopRendering();
#endif
        }

        private void RenderToRgbMatrix(BdfFont font, string text, Color color)
        {
#if !WINDOWS
            for (int i = 0; i < _matrix.Width; i++)
            {
                _matrix.SetPixel(i, 0, color.R, color.G, color.B);
                _matrix.SetPixel(i, _matrix.Height - 1, color.R, color.G, color.B);
            }

            _matrix.DrawText(0, 2, text, font, color.R, color.G, color.B, 0, 0, 0);
#endif
        }

        private void RenderPresence(Presence presence, BdfFont font)
        {
            string text = presence.Activity;

            Color color = text switch
            {
                "Available" => Color.Green,
                "Away" => Color.Yellow,
                "Busy" => Color.Red,
                "BeRightBack" => Color.Yellow,
                "DoNotDisturb" => Color.Red,
                "OffWork" => Color.DarkGray,
                "Unknown" => Color.Orange,
                "ERROR" => Color.OrangeRed,
                _ => Color.Purple
            };

            _logger.LogTrace("drawing {Text} with color 0x{Color:x})", text, color.ToArgb());

            RenderToRgbMatrix(font, text, color);
        }

        private void Updater_PresenceChanging(object sender, PresenceChangingEventArgs e)
        {
            _logger.LogInformation("presence change detected");
            RenderPresence(e.New, _font);
        }

        public int DisplayRefreshDelay { get; set; } = 100;

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("stopping");
            ShutdownRgbMatrix();
            _logger.LogInformation("stopped");
            return Task.CompletedTask;
        }
    }
}
