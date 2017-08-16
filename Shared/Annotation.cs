namespace Zebble
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
            internal void RaiseTapped() => Tapped.RaiseOn(Device.ThreadPool);
            public string Title
            {
                get;
                set;
            }

            = string.Empty;
            /// <summary>
            /// This is ignored in UWP.
            /// </summary>
            public string Subtitle
            {
                get;
                set;
            }

            = string.Empty;
            public GeoLocation Location
            {
                get;
                set;
            }

            = new GeoLocation();
            public object Native
            {
                get;
                internal set;
            }

            public void Dispose() => Tapped.Dispose();
            /// <summary>
            /// Path to the pin icon (optional).
            /// </summary>
            public string IconPath
            {
                get;
                set;
            }

            = string.Empty;
            public float IconWidth
            {
                get;
                set;
            }

            = 40;
            public float IconHeight
            {
                get;
                set;
            }

            = 60;
            internal async Task<ImageService.ImageProvider> GetPinImageProvider()
            {
                var result = ImageService.GetImageProvider(IconPath, new Size(IconWidth, IconHeight), Stretch.Fit);
                await result.Result();
                return result;
            }
        }
    }
}