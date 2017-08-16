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
        internal readonly AsyncEvent PannableChanged = new AsyncEvent();
        internal readonly AsyncEvent<Annotation> AddedAnnotation = new AsyncEvent<Annotation>();
        internal readonly AsyncEvent<Annotation> RemovedAnnotation = new AsyncEvent<Annotation>();
        internal readonly AsyncEvent ApiCenterChanged = new AsyncEvent();
        public readonly AsyncEvent<GeoRegion> UserChangedRegion = new AsyncEvent<GeoRegion>();

        public GeoLocation Center
        {
            get => center;
            set
            {
                if (center == value) return;
                center = value;
                ApiCenterChanged.RaiseOn(Device.UIThread);
            }
        }

        int zoomLevel = 13;
        bool zoomable = true;
        bool showZoomControls = false;
        bool rotatable = true;
        bool pannable = true;

        public int ZoomLevel
        {
            get => zoomLevel;
            set
            {
                if (zoomLevel == value) return;
                zoomLevel = value;
                ApiZoomChanged.Raise();
            }
        }

        public bool Zoomable
        {
            get => zoomable;
            set
            {
                if (zoomable == value) return;
                zoomable = value;
                ZoomableChanged.Raise();
            }
        }

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

        public bool Pannable
        {
            get => pannable;
            set
            {
                if (pannable == value) return;
                pannable = value;
                PannableChanged.Raise();
            }
        }

        public Span VisibleRegion { get; internal set; }

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

        public async Task Add(params Annotation[] annotations)
        {
            foreach (var a in annotations)
            {
                this.annotations.Add(a);
                await AddedAnnotation.Raise(a);
            }
        }

        public async Task Remove(params Annotation[] annotations)
        {
            foreach (var a in annotations)
            {
                this.annotations.Remove(a);
                await RemovedAnnotation.Raise(a);
            }
        }

        public Task ClearAnnotations() => annotations.ToArray().WhenAll(x => Remove(x));

        public override void Dispose()
        {
            UserChangedRegion?.Dispose();
            ApiZoomChanged?.Dispose();
            ZoomableChanged?.Dispose();
            PannableChanged?.Dispose();
            AddedAnnotation?.Dispose();
            RemovedAnnotation?.Dispose();
            ShowZoomControlsChanged?.Dispose();
            ApiCenterChanged?.Dispose();
            RotatableChanged?.Dispose();
            annotations.Do(x => x.Dispose());
            annotations.Clear();
            base.Dispose();
        }
    }
}