namespace Zebble
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Zebble.Device;
    using Olive;
    using Olive.GeoLocation;

    public enum MapTypes
    {
        Street = 0,
        Satelite = 1,
        Hybrid = 2
    }

    public partial class Map : View, IRenderedBy<MapRenderer>
    {
        public const int MinimumZoomLevel = 1;
        public const int MaximumZoomLevel = 20;
        public static double DefaultLatitude = 51.5074;
        public static double DefaultLongitude = -0.1278;
        public static int DefaultZoomLevel = 13;
        public readonly AsyncEvent<GeoLocation> MapTapped = new AsyncEvent<GeoLocation>();
        public readonly AsyncEvent<GeoLocation> MapLongPressed = new AsyncEvent<GeoLocation>();
        GeoLocation center;
        readonly List<Annotation> annotations = new List<Annotation>();
        public IEnumerable<Annotation> Annotations => annotations;
        internal readonly AsyncEvent ZoomableChanged = new AsyncEvent();
        internal readonly AsyncEvent ShowZoomControlsChanged = new AsyncEvent();
        internal readonly AsyncEvent RotatableChanged = new AsyncEvent();
        internal readonly AsyncEvent PannableChanged = new AsyncEvent();
        internal readonly AsyncEvent<Annotation> AddedAnnotation = new AsyncEvent<Annotation>(ConcurrentEventRaisePolicy.Queue);
        internal readonly AsyncEvent<Annotation> RemovedAnnotation = new AsyncEvent<Annotation>(ConcurrentEventRaisePolicy.Queue);
        internal readonly AsyncEvent ApiCenterChanged = new AsyncEvent(ConcurrentEventRaisePolicy.Queue);
        internal readonly AsyncEvent MapTypeChanged = new AsyncEvent(ConcurrentEventRaisePolicy.Queue);
        public readonly AsyncEvent<RectangularRegion> UserChangedRegion = new AsyncEvent<RectangularRegion>(ConcurrentEventRaisePolicy.Queue);
        MapTypes mapType;

        public MapTypes MapType
        {
            get => mapType;
            set
            {
                if (mapType == value) return;
                mapType = value;
                MapTypeChanged.RaiseOn(Thread.UI);
            }
        }

        public GeoLocation Center
        {
            get => center;
            set
            {
                if (center == value) return;
                center = value;
                ApiCenterChanged.RaiseOn(Thread.UI);
            }
        }

        bool zoomable = true;
        bool showZoomControls = false;
        bool rotatable = false;
        bool pannable = true;

        /// <summary>
        /// The map zoom level from 1 to 20. Default is 13. The higher, the more zoomed (close up).
        /// </summary>
        public TwoWayBindable<int> ZoomLevel = new(13);

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

        public RadialRegion VisibleRegion { get; internal set; }

        internal async Task<GeoLocation> GetCenter()
        {
            if (Center != null && (Center.Longitude != 0 || Center.Latitude != 0)) return Center;

            // Center of annotations:
            if (Annotations.Any())
            {
                var lat = (Annotations.Min(a => a.Location.Latitude) + Annotations.Max(a => a.Location.Latitude)) / 2;
                var lng = (Annotations.Min(a => a.Location.Longitude) + Annotations.Max(a => a.Location.Longitude)) / 2;
                return new GeoLocation(lat, lng);
            }
            else
            {
                var location = await Location.GetCurrentPosition(desiredAccuracy: 1000);
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

        public Task ClearAnnotations() => annotations.ToArray().AwaitAll(x => Remove(x));

        public override void Dispose()
        {
            UserChangedRegion?.Dispose();
            ZoomableChanged?.Dispose();
            PannableChanged?.Dispose();
            AddedAnnotation?.Dispose();
            RemovedAnnotation?.Dispose();
            ShowZoomControlsChanged?.Dispose();
            ApiCenterChanged?.Dispose();
            MapTypeChanged?.Dispose();
            RotatableChanged?.Dispose();
            annotations.Do(x => x.Dispose());
            annotations.Clear();
            base.Dispose();
        }
    }
}