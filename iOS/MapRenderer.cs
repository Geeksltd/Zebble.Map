namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
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
                
            };

            ApplyZoom();
            HandleEvents();

            Thread.UI.Post(async () =>
            {
                Result.CenterCoordinate = await GetCenter();

                // Load annotations:
                using (var mapDelegate = new IosMapDelegate(View))
                    Result.GetViewForAnnotation = mapDelegate.GetViewForAnnotation;
                await View.Annotations.WhenAll(RenderAnnotation);
            });

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
            View.UserChangedRegion.RaiseOn(Thread.Pool, region);
        }

        void HandleEvents()
        {
            View.ZoomableChanged.HandleActionOn(Thread.UI, () => Result.ZoomEnabled = CanZoom());
            View.ApiZoomChanged.HandleOn(Thread.UI, () => ApplyZoom());
            View.PannableChanged.HandleOn(Thread.UI, () => Result.ScrollEnabled = View.Pannable);
            View.RotatableChanged.HandleOn(Thread.UI, () => Result.RotateEnabled = View.Rotatable);
            View.AddedAnnotation.HandleOn(Thread.UI, a => RenderAnnotation(a));
            View.RemovedAnnotation.HandleOn(Thread.UI, a => RemoveAnnotation(a));
            View.ApiCenterChanged.HandleOn(Thread.UI, async () => Result.CenterCoordinate = await GetCenter());
            Result.RegionChanged += IosMap_RegionChanged;
        }

        bool CanZoom() => View.Zoomable || View.ShowZoomControls;

        async Task<CLLocationCoordinate2D> GetCenter() => (await View.GetCenter()).Render();

        /// <summary>Sets the map region based on its center and zoom.</summary>
        async void ApplyZoom()
        {
            var center = await GetCenter();

            if (View.ZoomLevel.HasValue)
            {
                Result.CenterCoordinate = center;

                var mapRegion = new MKCoordinateRegion(Result.CenterCoordinate, Result.GetSpan(View.ZoomLevel.Value));
                Result.SetRegion(mapRegion, animated: true);
            }
            else if (View.Annotations.Any())
            {
                CLLocationCoordinate2D topLeftCoord;
                topLeftCoord.Latitude = -90;
                topLeftCoord.Longitude = 180;

                CLLocationCoordinate2D bottomRightCoord;
                bottomRightCoord.Latitude = 90;
                bottomRightCoord.Longitude = -180;

                topLeftCoord.Longitude = Math.Min(topLeftCoord.Longitude, center.Longitude);
                topLeftCoord.Latitude = Math.Max(topLeftCoord.Latitude, center.Latitude);

                bottomRightCoord.Longitude = Math.Max(bottomRightCoord.Longitude, center.Longitude);
                bottomRightCoord.Latitude = Math.Min(bottomRightCoord.Latitude, center.Latitude);

                foreach (var annotation in View.Annotations)
                {
                    topLeftCoord.Longitude = Math.Min(topLeftCoord.Longitude, annotation.Location.Longitude);
                    topLeftCoord.Latitude = Math.Max(topLeftCoord.Latitude, annotation.Location.Latitude);

                    bottomRightCoord.Longitude = Math.Max(bottomRightCoord.Longitude, annotation.Location.Longitude);
                    bottomRightCoord.Latitude = Math.Min(bottomRightCoord.Latitude, annotation.Location.Latitude);
                }

                var region = new MKCoordinateRegion();
                region.Center.Latitude =
                    topLeftCoord.Latitude - (topLeftCoord.Latitude - bottomRightCoord.Latitude) * 0.5;
                region.Center.Longitude =
                    topLeftCoord.Longitude + (bottomRightCoord.Longitude - topLeftCoord.Longitude) * 0.5;

                // Add a little extra space on the sides
                region.Span.LatitudeDelta =
                    Math.Abs(topLeftCoord.Latitude - bottomRightCoord.Latitude) * 1.1;

                // Add a little extra space on the sides
                region.Span.LongitudeDelta =
                    Math.Abs(bottomRightCoord.Longitude - topLeftCoord.Longitude) * 1.1;

                var defaultRegion = new MKCoordinateRegion(region.Center, Result.GetSpan(Map.DefaultZoomLevel));
                if (region.Span.LatitudeDelta < defaultRegion.Span.LatitudeDelta ||
                    region.Span.LongitudeDelta < defaultRegion.Span.LongitudeDelta)
                {
                    region = defaultRegion;
                }

                Result.RegionThatFits(region);
                Result.SetRegion(region, true);
            }
            else
            {
                Result.CenterCoordinate = await GetCenter();

                var mapRegion = new MKCoordinateRegion(Result.CenterCoordinate, Result.GetSpan(Map.DefaultZoomLevel));
                Result.SetRegion(mapRegion, animated: true);
            }
        }

        async Task RenderAnnotation(Map.Annotation annotation)
        {
            if (annotation == null) return;
            if (annotation.Location == null)
            {
                Device.Log.Warning("annotation's Location is null!");
                return;
            }

            var native = new BasicMapAnnotation(annotation);
            await native.AwaitImage();
            Result.AddAnnotation(native);
        }

        void RemoveAnnotation(Map.Annotation annotation)
        {
            if (annotation?.Native is BasicMapAnnotation native)
                Result.RemoveAnnotation(native);
        }

        public void Dispose()
        {
            View = null;
            Result?.Annotations.Do(x => x.Dispose());
            Result?.Dispose();
            Result = null;
        }
    }
}