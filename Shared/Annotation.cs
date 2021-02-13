namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Zebble.Services;

    partial class Map
    {
        public partial class Annotation : IDisposable
        {
            ImageService.ImageSource IconProvider;

            public readonly AsyncEvent Tapped = new AsyncEvent(ConcurrentEventRaisePolicy.Ignore);

            internal void RaiseTapped() => Tapped.RaiseOn(Thread.Pool);

            public object Native { get; internal set; }

            public void Dispose()
            {
                Tapped.Dispose();
                IconProvider?.UnregisterViewer();
                IconProvider = null;
            }

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