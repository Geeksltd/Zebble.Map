namespace Zebble
{
    using System;
    using CoreLocation;
    using MapKit;
    using Olive;

    static class RegionCalculator
    {
        const double MERCATOR_OFFSET = 268435456;
        const double MERCATOR_RADIUS = 85445659.44705395;

        public static double ToPixelSpaceX(this CLLocationCoordinate2D center)
        {
            return MERCATOR_OFFSET + (MERCATOR_RADIUS * center.Longitude.ToRadians()).Round(0);
        }

        public static double ToPixelSpaceY(this CLLocationCoordinate2D center)
        {
            var latitude = center.Latitude;

            if (latitude.AlmostEquals(90.0)) return 0;
            if (latitude.AlmostEquals(-90.0)) return MERCATOR_OFFSET * 2;

            var sin = Math.Sin(latitude.ToRadians());

            return Math.Round(MERCATOR_OFFSET - MERCATOR_RADIUS * Math.Log((1 + sin) / (1 - sin)) / 2.0);
        }

        public static MKCoordinateSpan GetSpan(this MKMapView map, int zoomLevel)
        {
            // convert center coordiate to pixel space
            var centerPixelX = map.CenterCoordinate.ToPixelSpaceX();
            var centerPixelY = map.CenterCoordinate.ToPixelSpaceY();

            // determine the scale value from the zoom level            
            var zoomScale = 2.0.ToThePowerOf(1 + (20 - zoomLevel));

            // scale the map’s size in pixel space
            var mapSizeInPixels = map.Bounds.Size;
            var scaledMapWidth = mapSizeInPixels.Width * zoomScale;
            var scaledMapHeight = mapSizeInPixels.Height * zoomScale;

            // figure out the position of the top-left pixel
            var topLeftPixelX = centerPixelX - (scaledMapWidth / 2);
            var topLeftPixelY = centerPixelY - (scaledMapHeight / 2);

            // find delta between left and right longitudes
            var minLng = PixelSpaceXToLongitude(topLeftPixelX);
            var maxLng = PixelSpaceXToLongitude(topLeftPixelX + scaledMapWidth);

            // find delta between top and bottom latitudes
            var minLat = PixelSpaceYToLatitude(topLeftPixelY);
            var maxLat = PixelSpaceYToLatitude(topLeftPixelY + scaledMapHeight);

            // create and return the lat/lng span
            return new MKCoordinateSpan(Math.Abs(maxLat - minLat), maxLng - minLng);
        }

        static double PixelSpaceXToLongitude(double pixelX)
        {
            return (pixelX - MERCATOR_OFFSET) / MERCATOR_RADIUS.ToDegreeFromRadians();
        }

        static double PixelSpaceYToLatitude(double pixelY)
        {
            return (Math.PI / 2.0
                -
                2.0 * Math.Atan(Math.Exp((pixelY.Round(0) - MERCATOR_OFFSET) / MERCATOR_RADIUS)))
                .ToDegreeFromRadians();
        }
    }
}