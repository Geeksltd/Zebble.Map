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

            internal void RaiseTapped()
            {
                if (Content.HasValue()) return;
                Tapped.RaiseOn(Device.ThreadPool);
            }

            public string Title { get; set; } = string.Empty;

            /// <summary>
            /// If specified, it will be displayed when the pin is clicked, but then the Tapped event will not be raised anymore.
            /// </summary>
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