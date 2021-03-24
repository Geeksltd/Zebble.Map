namespace Zebble
{
    using Olive.GeoLocation;
    using CoreLocation;

    public static class RenderExtensions
    {
        public static GeoLocation ToLocation(this CLLocationCoordinate2D point)
        {
            return new(point.Latitude, point.Longitude);
        }
    }
}