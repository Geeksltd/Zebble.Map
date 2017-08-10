namespace Zebble.Plugin
{
    using System;
    using System.Threading.Tasks;
    using Zebble;
    using Zebble.Services;

    partial class Map
    {
        public partial class Annotation : IDisposable
        {
            public readonly AsyncEvent Tapped = new AsyncEvent();

            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public GeoLocation Location { get; set; } = new GeoLocation();
            public object Native { get; internal set; }

            public void Dispose() => Tapped.Dispose();

            /// <summary>
            /// Path to the pin icon (optional).
            /// </summary>
            public string IconPath { get; set; } = string.Empty;

            public float IconWidth { get; set; } = 40;

            public float IconHeight { get; set; } = 60;

            internal async Task<ImageService.ImageProvider> GetPinImageProvider()
            {
                var result = ImageService.GetImageProvider(IconPath, new Size(IconWidth, IconHeight), Stretch.Fit);
                await result.Result();
                return result;
            }
        }
    }
}