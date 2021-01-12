namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.Devices.Geolocation;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls.Maps;
    using Olive;
    using Olive.GeoLocation;

    [EditorBrowsable(EditorBrowsableState.Never)]
    class MapRenderer : INativeRenderer
    {
        Map View;
        MapControl Result;

        public async Task<FrameworkElement> Render(Renderer renderer)
        {
            View = (Map)renderer.View;
            Result = new MapControl
            {
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch,
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
            };

            Windows.Services.Maps.MapService.ServiceToken = "HaXjmFYYzKaodYsgdNp2~PK515Syb0c0Z5AeqFmfuWQ~Anxgm-h_lBeSoSpJmDR5JDBovDqEU8wqmirJ4RzVuOvZiHS8uo8NLKE8M9L4ZPYP";

            Result.MapServiceToken = "HaXjmFYYzKaodYsgdNp2~PK515Syb0c0Z5AeqFmfuWQ~Anxgm-h_lBeSoSpJmDR5JDBovDqEU8wqmirJ4RzVuOvZiHS8uo8NLKE8M9L4ZPYP";

            ZoomEnabledChanged();
            ScrollEnabledChanged();
            RotatableChanged();

            Result.ZoomLevelChanged += Result_ZoomLevelChanged;
            Result.CenterChanged += Result_CenterChanged;
            Result.MapElementClick += Result_MapElementClick;
            Result.Loaded += Result_Loaded;

            HandleEvents();

            Thread.UI.Post(async () =>
            {
                await ApplyZoom();
                foreach (var a in View.Annotations) await RenderAnnotation(a);
            });

            return Result;
        }

        void Result_CenterChanged(MapControl _, object __) => UpdatedVisibleRegion();

        void Result_ZoomLevelChanged(MapControl _, object __) => UpdatedVisibleRegion();

        void Result_Loaded(object _, RoutedEventArgs __)
        {
            if (Result == null) return;

            Result.Loaded -= Result_Loaded;
            ApplyZoom().RunInParallel();
        }

        void Result_MapElementClick(MapControl sender, MapElementClickEventArgs ev)
        {
            var markers = ev.MapElements.OfType<MapIcon>().ToList();

            foreach (var marker in markers)
                View.Annotations.FirstOrDefault(a => a.Native == marker)?.RaiseTapped();
        }

        void UpdatedVisibleRegion()
        {
            if (Result == null) return;

            Result.GetLocationFromOffset(new Windows.Foundation.Point(0, 0), out var topLeft);
            if (topLeft == null) return;

            Result.GetLocationFromOffset(new Windows.Foundation.Point(0, Result.ActualHeight), out var bottomLeft);
            if (bottomLeft == null) return;

            Result.GetLocationFromOffset(new Windows.Foundation.Point(Result.ActualWidth, Result.ActualHeight), out var bottomRight);
            if (bottomRight == null) return;

            View.VisibleRegion = new Map.Span(GetGeoLocation(topLeft), GetGeoLocation(bottomLeft), GetGeoLocation(bottomRight));

            var region = GeoRegion.FromCentre(View.VisibleRegion.Center,
                View.VisibleRegion.LatitudeDegrees, View.VisibleRegion.LongitudeDegrees);
            View.UserChangedRegion.RaiseOn(Thread.Pool, region);
        }

        GeoLocation GetGeoLocation(Geopoint point) => new GeoLocation(point.Position.Latitude, point.Position.Longitude);

        async Task Calculate()
        {
            if (Result == null) return;

            await ApplyZoom();
        }

        void HandleEvents()
        {
            View.ApiZoomChanged.HandleOn(Thread.UI, ZoomChanged);
            View.ZoomableChanged.HandleOn(Thread.UI, () => ZoomEnabledChanged());
            View.PannableChanged.HandleOn(Thread.UI, () => ScrollEnabledChanged());
            View.RotatableChanged.HandleOn(Thread.UI, () => RotatableChanged());
            View.AddedAnnotation.HandleOn(Thread.UI, RenderAnnotation);
            View.RemovedAnnotation.HandleOn(Thread.UI, a => RemoveAnnotation(a));
            View.ApiCenterChanged.HandleOn(Thread.UI, ApplyZoom);
            Result.MapTapped += Result_MapTapped;
        }

        void Result_MapTapped(MapControl sender, MapInputEventArgs args)
        {
            View.MapTapped.RaiseOn(Thread.UI, new GeoLocation( args.Location.Position.Latitude, args.Location.Position.Longitude));
        }

        Task ZoomChanged() => Calculate();

        void ZoomEnabledChanged()
        {
            if (Result == null) return;

            if (View.Zoomable && View.ShowZoomControls) Result.ZoomInteractionMode = MapInteractionMode.GestureAndControl;
            else if (View.Zoomable) Result.ZoomInteractionMode = MapInteractionMode.GestureOnly;
            else if (View.ShowZoomControls) Result.ZoomInteractionMode = MapInteractionMode.ControlOnly;
            else Result.ZoomInteractionMode = MapInteractionMode.Disabled;
        }

        void ScrollEnabledChanged()
        {
            if (Result == null) return;

            if (View.Pannable) Result.PanInteractionMode = MapPanInteractionMode.Auto;
            else Result.PanInteractionMode = MapPanInteractionMode.Disabled;
        }

        void RotatableChanged()
        {
            if (Result == null) return;

            if (View.Rotatable) Result.RotateInteractionMode = MapInteractionMode.GestureAndControl;
            else Result.RotateInteractionMode = MapInteractionMode.Disabled;
        }

        async Task RenderAnnotation(Map.Annotation annotation)
        {
            if (annotation == null || Result == null) return;
            if (annotation.Location == null)
            {
                Log.For(this).Warning("annotation's Location is null!");
                return;
            }

            var poi = new MapIcon
            {
                Location = annotation.Location.Render(),
                NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 1),
                Title = annotation.Title.OrEmpty(),
                Visible = true,
                ZIndex = 0,
            };

            annotation.Native = poi;

            if (annotation.IconPath.HasValue())
            {
                var provider = await annotation.GetPinImageProvider();
                var file = await provider.GetExactSizedFile();
                Log.For(this).Debug(file.FullName);
                poi.Image = RandomAccessStreamReference.CreateFromFile(await file.ToStorageFile());
            }

            Result.MapElements.Add(poi);
        }

        void RemoveAnnotation(Map.Annotation annotation)
        {
            if (Result == null || annotation == null) return;

            var native = annotation.Native as MapElement;
            if (native == null) return;

            if (Result.MapElements.Contains(native))
                Result.MapElements.Remove(native);
        }

        async Task ApplyZoom()
        {
            if (Result == null) return;

            var center = (await View.GetCenter()).Render();

            if (View.ZoomLevel.HasValue)
            {
                await Result.TrySetViewAsync(center, View.ZoomLevel.Value).AsTask()
                    .WithTimeout(1.Seconds(), timeoutAction: () => Log.For(this).Warning("Map.TrySetViewAsync() timed out."));
            }
            else if (View.Annotations.Any())
            {
                var points = new List<Geopoint>
                {
                    new Geopoint(new BasicGeoposition
                    {
                        Latitude = center.Position.Latitude,
                        Longitude = center.Position.Longitude
                    })
                };

                foreach (var annotation in View.Annotations)
                {
                    points.Add(new Geopoint(new BasicGeoposition
                    {
                        Latitude = annotation.Location.Latitude,
                        Longitude = annotation.Location.Longitude
                    }));
                }

                var mapScene = MapScene.CreateFromLocations(points);

                await Result.TrySetSceneAsync(mapScene).AsTask()
                    .WithTimeout(1.Seconds(), timeoutAction: () => Log.For(this).Warning("Map.TrySetViewAsync() timed out.")); ;
            }
            else
            {
                await Result.TrySetViewAsync(center, Map.DefaultZoomLevel).AsTask()
                    .WithTimeout(1.Seconds(), timeoutAction: () => Log.For(this).Warning("Map.TrySetViewAsync() timed out."));
            }
        }

        public void Dispose()
        {
            if (Result != null)
            {
                Result.ZoomLevelChanged -= Result_ZoomLevelChanged;
                Result.CenterChanged -= Result_ZoomLevelChanged;
                Result.MapElementClick -= Result_MapElementClick;
                Result.Loaded -= Result_Loaded;
            }

            Result = null;
        }
    }

    public static class RenderExtensions
    {
        public static Geopoint Render(this IGeoLocation location)
        {
            return new Geopoint(new BasicGeoposition
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude
            });
        }
    }
}