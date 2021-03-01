namespace Zebble
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Zebble.Device;
    using Olive;
    using Olive.GeoLocation;
    using Zebble.Mvvm;

    public partial class MapView : View, IRenderedBy<MapRenderer>
    {
        public static double DefaultLatitude = 51.5074;
        public static double DefaultLongitude = -0.1278;

        public readonly AsyncEvent<GeoLocation> MapTapped = new AsyncEvent<GeoLocation>();
        public readonly AsyncEvent<GeoLocation> MapLongPressed = new AsyncEvent<GeoLocation>();

        public Map Map { get; }

        public MapView()
        {
            Map = new Map();
        }

        internal async Task<GeoLocation> GetCenter()
        {
            var center = Map.Center?.Value;
            if (center != null && (center.Longitude != 0 || center.Latitude != 0)) return center;

            var annotations = Map.Annotations;
            if (annotations?.Any() == true)
            {
                var lat = (annotations.Min(a => a.Location.Latitude) + annotations.Max(a => a.Location.Latitude)) / 2;
                var lng = (annotations.Min(a => a.Location.Longitude) + annotations.Max(a => a.Location.Longitude)) / 2;
                return new GeoLocation(lat, lng);
            }

            return await Location.GetCurrentPosition(desiredAccuracy: 1000) ??
                new GeoLocation(DefaultLatitude, DefaultLongitude);
        }

        public override void Dispose()
        {
            Map.Annotations?.Do(x => x.Dispose());
            base.Dispose();
        }
    }
}