namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using CoreLocation;
    using MapKit;
    using UIKit;
    using Olive;
    using Olive.GeoLocation;
    using Zebble.Mvvm;
    using Foundation;

    [EditorBrowsable(EditorBrowsableState.Never)]
    class MapRenderer : INativeRenderer
    {
        MapView View;
        MKMapView Result;

        public async Task<UIView> Render(Renderer renderer)
        {
            View = (MapView)renderer.View;
            Result = new MKMapView
            {
                Frame = View.GetFrame(),
                ScrollEnabled = View.Map.Pannable.Value,
                RotateEnabled = View.Map.Rotatable.Value,
                ZoomEnabled = CanZoom(),
                MapType = GetMapType(),
                Delegate = new MapDelegate()
            };

            ApplyZoom();
            HandleEvents();

            Thread.UI.Post(async () =>
            {
                Result.CenterCoordinate = await GetCenter();

                await View.Map.Annotations.AwaitAll(RenderAnnotation);
                await View.Map.Routes.AwaitAll(RenderRoute);
            });

            return Result;
        }

        GeoLocation GetGeoLocation(CLLocationCoordinate2D point) => new(point.Latitude, point.Longitude);

        void IosMap_RegionChanged(object sender, MKMapViewChangeEventArgs e)
        {
            var centre = new GeoLocation(Result.Region.Center.Latitude, Result.Region.Center.Longitude);

            var region = RectangularRegion.FromCentre(centre, Result.Region.Span.LatitudeDelta, Result.Region.Span.LongitudeDelta);
            var topLeft = Result.ConvertPoint(new CoreGraphics.CGPoint(x: 0, y: 0), toCoordinateFromView: Result);
            var bottomLeft = Result.ConvertPoint(new CoreGraphics.CGPoint(x: 0, y: Result.Bounds.Height), toCoordinateFromView: Result);
            var bottomRight = Result.ConvertPoint(new CoreGraphics.CGPoint(x: Result.Bounds.Width, y: Result.Bounds.Height), toCoordinateFromView: Result);
            View.Map.VisibleRegion.Set(new RadialRegion(GetGeoLocation(topLeft), GetGeoLocation(bottomLeft), GetGeoLocation(bottomRight)));
            View.Map.CenterOfVisibleRegion.Set(region);
        }

        void HandleEvents()
        {
            View.Map.Zoomable.ChangedBySource += () => Thread.UI.Run(() => Result.ZoomEnabled = CanZoom());
            View.Map.ZoomLevel.ChangedBySource += () => Thread.UI.Run(ApplyZoom);
            View.Map.Pannable.ChangedBySource += () => Thread.UI.Run(() => Result.ScrollEnabled = View.Map.Pannable.Value);
            View.Map.Rotatable.ChangedBySource += () => Thread.UI.Run(() => Result.RotateEnabled = View.Map.Rotatable.Value);
            View.Map.Annotations.Added += a => Thread.UI.Run(() => RenderAnnotation(a));
            View.Map.Annotations.Removing += a => Thread.UI.Run(() => RemoveAnnotation(a));
            View.Map.Routes.Added += r => Thread.UI.Run(() => RenderRoute(r));
            View.Map.Routes.Removing += r => Thread.UI.Run(() => RemoveRoute(r));
            View.Map.Center.ChangedBySource += () => Thread.UI.Run(async () => Result.CenterCoordinate = await GetCenter());
            View.Map.MapType.ChangedBySource += () => Thread.UI.Run(() => Result.MapType = GetMapType());
            Result.RegionChanged += IosMap_RegionChanged;
            Result.AddGestureRecognizer(new UITapGestureRecognizer(action: (uiTapGestureRecognizer) =>
            {
                var location = uiTapGestureRecognizer.LocationInView(Result);
                var coordinate = Result.ConvertPoint(location, Result);

                View.MapTapped.RaiseOn(Thread.UI, new GeoLocation(coordinate.Latitude, coordinate.Longitude));
            }));
        }

        bool CanZoom() => View.Map.Zoomable.Value || View.Map.ShowZoomControls.Value;

        async Task<CLLocationCoordinate2D> GetCenter()
        {
            var center = await View.GetCenter();
            return new CLLocationCoordinate2D(center.Latitude, center.Longitude);
        }

        MKMapType GetMapType()
        {
            switch (View.Map.MapType.Value)
            {
                case MapTypes.Satelite:
                    return MKMapType.Satellite;
                case MapTypes.Hybrid:
                    return MKMapType.Hybrid;
                default:
                    return MKMapType.Standard;
            }
        }

        /// <summary>Sets the map region based on its center and zoom.</summary>
        async void ApplyZoom()
        {
            var center = await GetCenter();

            if (View.Map.ZoomLevel.Value != default)
            {
                Result.CenterCoordinate = center;

                var mapRegion = new MKCoordinateRegion(Result.CenterCoordinate, Result.GetSpan(View.Map.ZoomLevel.Value));
                Result.SetRegion(mapRegion, animated: true);
            }
            else if (View.Map.Routes.Any() || View.Map.Routes.Any())
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

                foreach (var location in View.Map.Annotations.Select(a => a.Location).Concat(View.Map.Routes.SelectMany(r => r.Points)))
                {
                    topLeftCoord.Longitude = Math.Min(topLeftCoord.Longitude, location.Longitude);
                    topLeftCoord.Latitude = Math.Max(topLeftCoord.Latitude, location.Latitude);

                    bottomRightCoord.Longitude = Math.Max(bottomRightCoord.Longitude, location.Longitude);
                    bottomRightCoord.Latitude = Math.Min(bottomRightCoord.Latitude, location.Latitude);
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
                Result.SetRegion(region, animated: true);
            }
            else
            {
                Result.CenterCoordinate = await GetCenter();

                var mapRegion = new MKCoordinateRegion(Result.CenterCoordinate, Result.GetSpan(Map.DefaultZoomLevel));
                Result.SetRegion(mapRegion, animated: true);
            }
        }

        async Task RenderAnnotation(Annotation annotation)
        {
            if (annotation == null) return;
            if (annotation.Location == null)
            {
                Log.For(this).Warning("annotation's Location is null!");
                return;
            }

            var native = new BasicMapAnnotation(annotation);
            await native.AwaitImage();
            Result.AddAnnotation(native);
        }

        void RemoveAnnotation(Annotation annotation)
        {
            if (annotation?.Native is BasicMapAnnotation native)
                Result.RemoveAnnotation(native);
        }

        async Task RenderRoute(Route route)
        {
            if (route == null) return;
            if (route.Points.Length < 2)
            {
                Log.For(this).Warning("The route must contain at least two points!");
                return;
            }

            var native = new BasicMapPolyline(route);
            Result.AddOverlay(native);
        }

        void RemoveRoute(Route route)
        {
            if (route?.Native is MKPolygon native)
                Result.RemoveOverlay(native);
        }

        public void Dispose()
        {
            View = null;
            Result?.Annotations.Do(x => x.Dispose());
            Result?.Overlays.Do(x => x.Dispose());
            Result?.Dispose();
            Result = null;
        }
    }
}