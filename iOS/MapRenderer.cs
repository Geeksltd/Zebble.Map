namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using CoreLocation;
    using MapKit;
    using Services;
    using UIKit;

    [EditorBrowsable(EditorBrowsableState.Never)]
    class MapRenderer : INativeRenderer
    {
        Map View;
        MKMapView Result;
        const double DEGREE360 = 360;

        const double MERCATOROFFSET = 268435456;
        const double MERCATORRADIUS = 85445659.44705395;

        List<object> ActionList = new List<object>();

        public async Task<UIView> Render(Renderer renderer)
        {
            View = (Map)renderer.View;
            Result = new MKMapView();
            await GenerateMap();
            return Result;
        }

        GeoLocation GetGeoLocation(CLLocationCoordinate2D point) => new GeoLocation(point.Latitude, point.Longitude);

        void IosMap_RegionChanged(object sender, MKMapViewChangeEventArgs e)
        {
            var centre = new GeoLocation { Latitude = Result.Region.Center.Latitude, Longitude = Result.Region.Center.Longitude };

            var region = GeoRegion.FromCentre(centre, Result.Region.Span.LatitudeDelta, Result.Region.Span.LongitudeDelta);
            var topLeft = Result.ConvertPoint(new CoreGraphics.CGPoint(x: 0, y: 0), toCoordinateFromView: Result);
            var bottomLeft = Result.ConvertPoint(new CoreGraphics.CGPoint(x: 0, y: Result.Bounds.Height), toCoordinateFromView: Result);
            var bottomRight = Result.ConvertPoint(new CoreGraphics.CGPoint(x: Result.Bounds.Width, y: Result.Bounds.Height), toCoordinateFromView: Result);
            View.VisibleRegion = new Map.Span(GetGeoLocation(topLeft), GetGeoLocation(bottomLeft), GetGeoLocation(bottomRight));
            View.UserChangedRegion.RaiseOn(Device.ThreadPool, region);
        }

        async Task GenerateMap()
        {
            if (View != null) Result.Frame = View.GetFrame();
            Result.ScrollEnabled = View.Pannable;
            Result.RotateEnabled = View.Rotatable;

            await SetCenter();
            ZoomControllingChanged();
            MoveToRegion();
            await View.Annotations.WhenAll(RenderAnnotation);
            View.ZoomableChanged.HandleActionOn(Device.UIThread, ZoomControllingChanged);
            View.ApiZoomChanged.HandleOn(Device.UIThread, () => MoveToRegion());
            View.PannableChanged.HandleOn(Device.UIThread, () => Result.ScrollEnabled = View.Pannable);
            View.RotatableChanged.HandleOn(Device.UIThread, () => Result.RotateEnabled = View.Rotatable);
            View.AddedAnnotation.HandleOn(Device.UIThread, a => RenderAnnotation(a));
            View.RemovedAnnotation.HandleOn(Device.UIThread, a => RemoveAnnotation(a));
            View.ApiCenterChanged.HandleOn(Device.UIThread, SetCenter);

            using (var mapDelegate = new IosMapDelegate(View))
                Result.GetViewForAnnotation = mapDelegate.GetViewForAnnotation;

            Result.RegionChanged += IosMap_RegionChanged;
        }

        async Task SetCenter() => Result.CenterCoordinate = (await View.GetCenter()).Render();

        void ZoomControllingChanged() => Result.ZoomEnabled = View.Zoomable || View.ShowZoomControls;

        async Task RenderAnnotation(Map.Annotation annotation)
        {
            var native = new BasicMapAnnotation(annotation);
            await native.AwaitImage();
            Result.AddAnnotation(native);
        }

        void RemoveAnnotation(Map.Annotation annotation)
        {
            var native = annotation.Native as BasicMapAnnotation;
            if (native != null)
                Result.RemoveAnnotation(native);
        }

        void MoveToRegion()
        {
            var span = SetCenterCoordinate(Result.CenterCoordinate, View.ZoomLevel);
            var mapRegion = new MKCoordinateRegion(Result.CenterCoordinate, span);
            Result.SetRegion(mapRegion, animated: true);
        }

        #region Map Span Conversation

        double LongitudeToPixelSpaceX(double longitude)
        {
            return Math.Round(MERCATOROFFSET + MERCATORRADIUS * longitude * Math.PI / 180.0);
        }

        double LatitudeToPixelSpaceY(double latitude)
        {
            return Math.Round(MERCATOROFFSET - MERCATORRADIUS * Math.Log((1 + Math.Sin(latitude * Math.PI / 180.0)) / (1 - Math.Sin(latitude * Math.PI / 180.0))) / 2.0);
        }

        double PixelSpaceXToLongitude(double pixelX)
        {
            return ((Math.Round(pixelX) - MERCATOROFFSET) / MERCATORRADIUS) * 180.0 / Math.PI;
        }

        double PixelSpaceYToLatitude(double pixelY)
        {
            return (Math.PI / 2.0 - 2.0 * Math.Atan(Math.Exp((Math.Round(pixelY) - MERCATOROFFSET) / MERCATORRADIUS))) * 180.0 / Math.PI;
        }

        MKCoordinateSpan CoordinateSpanWithMapView(MKMapView mapView, CLLocationCoordinate2D centerCoordinate, int zoomLevel)
        {
            // convert center coordiate to pixel space
            var centerPixelX = LongitudeToPixelSpaceX(centerCoordinate.Longitude);
            var centerPixelY = LatitudeToPixelSpaceY(centerCoordinate.Latitude);

            // determine the scale value from the zoom level
            var zoomExponent = 20 - zoomLevel;
            var zoomScale = 2.ToThePowerOf(zoomExponent);

            // scale the map’s size in pixel space
            var mapSizeInPixels = mapView.Bounds.Size;
            var scaledMapWidth = mapSizeInPixels.Width * zoomScale;
            var scaledMapHeight = mapSizeInPixels.Height * zoomScale;

            // figure out the position of the top-left pixel
            var topLeftPixelX = centerPixelX - (scaledMapWidth / 2);
            var topLeftPixelY = centerPixelY - (scaledMapHeight / 2);

            // find delta between left and right longitudes
            var minLng = PixelSpaceXToLongitude(topLeftPixelX);
            var maxLng = PixelSpaceXToLongitude(topLeftPixelX + scaledMapWidth);
            var longitudeDelta = maxLng - minLng;

            // find delta between top and bottom latitudes
            var minLat = PixelSpaceYToLatitude(topLeftPixelY);
            var maxLat = PixelSpaceYToLatitude(topLeftPixelY + scaledMapHeight);
            var latitudeDelta = -1 * (maxLat - minLat);

            // create and return the lat/lng span
            return new MKCoordinateSpan(latitudeDelta, longitudeDelta);
        }

        MKCoordinateSpan SetCenterCoordinate(CLLocationCoordinate2D centerCoordinate, int zoomLevel)
        {
            // clamp large numbers to 28
            zoomLevel = Math.Min(zoomLevel, 28);

            // use the zoom level to compute the region
            return CoordinateSpanWithMapView(Result, centerCoordinate, zoomLevel);
        }

        #endregion

        public void Dispose()
        {
            View = null;
            Result?.Dispose();
            Result = null;
        }
    }
}