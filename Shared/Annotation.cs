namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Zebble.Services;
    using Olive.GeoLocation;

    partial class Map
    {
        public partial class Annotation : IDisposable
        {
            ImageService.ImageSource IconProvider;

            public readonly AsyncEvent Tapped = new AsyncEvent(ConcurrentEventRaisePolicy.Ignore);

            internal void RaiseTapped() => Tapped.RaiseOn(Thread.Pool);

            public string Title { get; set; } = string.Empty;

            /// <summary>
            /// This is ignored in UWP.
            /// </summary>
            public string Subtitle { get; set; } = string.Empty;

            public GeoLocation Location { get; set; } = new GeoLocation();

            public object Native { get; internal set; }

            public void Dispose()
            {
                Tapped.Dispose();
                IconProvider?.UnregisterViewer();
                IconProvider = null;
            }

            /// <summary>
            /// Path to the pin icon (optional).
            /// </summary>
            public string IconPath { get; set; } = string.Empty;

            public float IconWidth { get; set; } = 40;

            public float IconHeight { get; set; } = 60;

            internal async Task<ImageService.ImageSource> GetPinImageProvider()
            {
                var result = ImageService.GetSource(IconPath, new Size(IconWidth, IconHeight),
                    Stretch.Fit);

                result.RegisterViewer();

                await result.Result();
                return result;
            }
        }
    }
}