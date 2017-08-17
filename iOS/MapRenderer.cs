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

        List<object> ActionList = new List<object>();

        public async Task<UIView> Render(Renderer renderer)
        {
            View = (Map)renderer.View;
            Result = new MKMapView
            {
                Frame = View.GetFrame(),
                ScrollEnabled = View.Pannable,
                RotateEnabled = View.Rotatable,
                ZoomEnabled = CanZoom(),
                CenterCoordinate = await GetCenter()
            };

            ApplyZoom();
            HandleEvents();

            // Load annotations:
            using (var mapDelegate = new IosMapDelegate(View))
                Result.GetViewForAnnotation = mapDelegate.GetViewForAnnotation;
            await View.Annotations.WhenAll(RenderAnnotation);

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

        void HandleEvents()
        {
            View.ZoomableChanged.HandleActionOn(Device.UIThread, () => Result.ZoomEnabled = CanZoom());
            View.ApiZoomChanged.HandleOn(Device.UIThread, () => ApplyZoom());
            View.PannableChanged.HandleOn(Device.UIThread, () => Result.ScrollEnabled = View.Pannable);
            View.RotatableChanged.HandleOn(Device.UIThread, () => Result.RotateEnabled = View.Rotatable);
            View.AddedAnnotation.HandleOn(Device.UIThread, a => RenderAnnotation(a));
            View.RemovedAnnotation.HandleOn(Device.UIThread, a => RemoveAnnotation(a));
            View.ApiCenterChanged.HandleOn(Device.UIThread, async () => Result.CenterCoordinate = await GetCenter());
            Result.RegionChanged += IosMap_RegionChanged;
        }

        bool CanZoom() => View.Zoomable || View.ShowZoomControls;

        async Task<CLLocationCoordinate2D> GetCenter() => (await View.GetCenter()).Render();

        /// <summary>Sets the map region based on its center and zoom.</summary>
        void ApplyZoom()
        {
            var mapRegion = new MKCoordinateRegion(Result.CenterCoordinate, Result.GetSpan(View.ZoomLevel));
            Result.SetRegion(mapRegion, animated: true);
        }

        async Task RenderAnnotation(Map.Annotation annotation)
        {
            var native = new BasicMapAnnotation(annotation);
            await native.AwaitImage();
            Result.AddAnnotation(native);
        }

        void RemoveAnnotation(Map.Annotation annotation)
        {
            if (annotation.Native is BasicMapAnnotation native)
                Result.RemoveAnnotation(native);
        }

        public void Dispose()
        {
            View = null;
            Result?.Dispose();
            Result = null;
        }
    }
}