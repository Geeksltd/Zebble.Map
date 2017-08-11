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
        GeoLocation center;
        List<Annotation> annotations = new List<Annotation>();
        public IEnumerable<Annotation> Annotations => annotations;
        internal readonly AsyncEvent ApiZoomChanged = new AsyncEvent();
        internal readonly AsyncEvent ZoomableChanged = new AsyncEvent();
        internal readonly AsyncEvent ShowZoomControlsChanged = new AsyncEvent();
        internal readonly AsyncEvent RotatableChanged = new AsyncEvent();
        internal readonly AsyncEvent ScrollableChanged = new AsyncEvent();
        internal readonly AsyncEvent AnnotationsChanged = new AsyncEvent();
        public readonly AsyncEvent<GeoRegion> UserChanged = new AsyncEvent<GeoRegion>();
        internal Func<Task> NativeRefreshControl;
        public GeoLocation Center
        {
            get => center;
            set
            {
                if (center == value)
                    return;
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
                if (zoomLevel == value)
                    return;
                zoomLevel = value;
                ApiZoomChanged.Raise();
            }
        }

        bool zoomable = true;
        public bool Zoomable
        {
            get => zoomable;
            set
            {
                if (zoomable == value)
                    return;
                zoomable = value;
                ZoomableChanged.Raise();
            }
        }

        bool showZoomControls = false;
        public bool ShowZoomControls
        {
            get => showZoomControls;
            set
            {
                if (showZoomControls == value)
                    return;
                showZoomControls = value;
                ShowZoomControlsChanged.Raise();
            }
        }

        bool rotatable = true;
        public bool Rotatable
        {
            get => rotatable;
            set
            {
                if (rotatable == value)
                    return;
                rotatable = value;
                RotatableChanged.Raise();
            }
        }

        bool scrollable = true;
        public bool Scrollable
        {
            get => scrollable;
            set
            {
                if (scrollable == value)
                    return;
                scrollable = value;
                ScrollableChanged.Raise();
            }
        }

        public Span VisibleRegion
        {
            get;
            internal set;
        }

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
                if (location == null)
                    return new GeoLocation(DefaultLatitude, DefaultLongitude);
                return location;
            }
        }

        public Task Add(params Annotation[] annotations)
        {
            this.annotations.AddRange(annotations);
            AnnotationsChanged.Raise();
            return Task.CompletedTask;
        }

        public Task Remove(params Annotation[] annotations)
        {
            this.annotations.Remove(annotations);
            AnnotationsChanged.Raise();
            return Task.CompletedTask;
        }

        public async Task ClearAnnotations()
        {
            foreach (var a in annotations.ToArray())
                await Remove(a);
        }

        public override void Dispose()
        {
            UserChanged?.Dispose();
            ApiZoomChanged?.Dispose();
            ZoomableChanged?.Dispose();
            ScrollableChanged?.Dispose();
            AnnotationsChanged?.Dispose();
            annotations.Do(x => x.Dispose());
            annotations.Clear();
            base.Dispose();
        }
    }
}