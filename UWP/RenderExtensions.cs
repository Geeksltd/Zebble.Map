namespace Zebble
{
    using Windows.Devices.Geolocation;
    using Olive.GeoLocation;

    public static class RenderExtensions
    {
        public static BasicGeoposition Position(this IGeoLocation location)
        {
            return new BasicGeoposition
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };
        }

        public static Geopoint Render(this IGeoLocation location)
        {
            return new Geopoint(location.Position());
        }
    }
}