namespace Zebble.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Zebble;
    using Zebble.Services;

    public partial class Map : View, IRenderedBy<Renderer.MapRenderer>
    {
        public static double DefaultLatitude = 51.5074;
        public static double DefaultLongitude = -0.1278;

        public readonly List<Annotation> Annotations = new List<Annotation>();

        public readonly AsyncEvent ZoomChanged = new AsyncEvent();
        public readonly AsyncEvent ZoomEnabledChanged = new AsyncEvent();
        public readonly AsyncEvent ScrollEnabledChanged = new AsyncEvent();
        public readonly AsyncEvent AnnotationsChanged = new AsyncEvent();
        public readonly AsyncEvent<GeoRegion> UserChanged = new AsyncEvent<GeoRegion>();

        internal Func<Task> NativeRefreshControl;

        GeoLocation center;
        public GeoLocation Center
        {
            get => center;
            set
            {
                if (center == value) return;

                center = value;

                if (IsAlreadyRendered())
                {
                    if (NativeRefreshControl != null)
                        NativeRefreshControl().RunInParallel();
                    else
                        throw new NotImplementedException("The native control should provide NativeSetZoomFactors.");
                }
            }
        }

        int zoomLevel;
        public int ZoomLevel
        {
            get => zoomLevel;
            set
            {
                if (zoomLevel == value) return;

                zoomLevel = value;
                ZoomChanged.Raise();
            }
        }

        bool zoomEnable = true;
        public bool ZoomEnable
        {
            get => zoomEnable;
            set { if (zoomEnable == value) return; zoomEnable = value; ZoomEnabledChanged.Raise(); }
        }

        bool scrollEnabled = true;
        public bool ScrollEnabled
        {
            get => scrollEnabled;
            set { if (scrollEnabled == value) return; scrollEnabled = value; ScrollEnabledChanged.Raise(); }
        }

        public Span VisibleRegion { get; internal set; }

        public bool ShowZoomControls { get; set; } = true;

        internal async Task<GeoLocation> GetCenter()
        {
            if (Center != null && (Center.Longitude != 0 || Center.Latitude != 0))
                return Center;

            // Center of annotations:
            if (Annotations.Any())
            {
                var lat = (Annotations.Min(a => a.Location.Latitude) + Annotations.Max(a => a.Location.Latitude)) / 2;
                var lng = (Annotations.Min(a => a.Location.Longitude) + Annotations.Max(a => a.Location.Longitude)) / 2;
                return new GeoLocation(lat, lng);
            }
            else
            {
                var location = await Device.Location.GetCurrentPosition(desiredAccuracy: 1000);

                if (location == null) return new GeoLocation(DefaultLatitude, DefaultLongitude);

                return location;
            }
        }

        public Task Add(params Annotation[] annotations)
        {
            Annotations.AddRange(annotations); AnnotationsChanged.Raise();
            return Task.CompletedTask;
        }

        public Task Remove(params Annotation[] annotations)
        {
            Annotations.Remove(annotations);
            AnnotationsChanged.Raise();
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            UserChanged?.Dispose();
            ZoomChanged?.Dispose();
            ZoomEnabledChanged?.Dispose();
            ScrollEnabledChanged?.Dispose();
            AnnotationsChanged?.Dispose();
            base.Dispose();
        }
    }
}