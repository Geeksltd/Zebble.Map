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
    using Zebble.Mvvm;
    using Windows.Services.Maps;

    [EditorBrowsable(EditorBrowsableState.Never)]
    class MapRenderer : INativeRenderer
    {
        MapView View;
        MapControl Result;

        public async Task<FrameworkElement> Render(Renderer renderer)
        {
            View = (MapView)renderer.View;
            Result = new MapControl
            {
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch,
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
            };

            MapService.ServiceToken = "HaXjmFYYzKaodYsgdNp2~PK515Syb0c0Z5AeqFmfuWQ~Anxgm-h_lBeSoSpJmDR5JDBovDqEU8wqmirJ4RzVuOvZiHS8uo8NLKE8M9L4ZPYP";

            Result.MapServiceToken = "HaXjmFYYzKaodYsgdNp2~PK515Syb0c0Z5AeqFmfuWQ~Anxgm-h_lBeSoSpJmDR5JDBovDqEU8wqmirJ4RzVuOvZiHS8uo8NLKE8M9L4ZPYP";

            ZoomEnabledChanged();
            ScrollEnabledChanged();
            RotatableChanged();
            Result.Style = GetMapType();

            Result.ZoomLevelChanged += Result_ZoomLevelChanged;
            Result.CenterChanged += Result_CenterChanged;
            Result.MapElementClick += Result_MapElementClick;
            Result.Loaded += Result_Loaded;

            HandleEvents();

            Thread.UI.Post(async () =>
            {
                await ApplyZoom();
                foreach (var a in View.Map.Annotations) await RenderAnnotation(a);
                foreach (var r in View.Map.Routes) await RenderRoute(r);
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
                View.Map.Annotations.FirstOrDefault(a => a.Native == marker)?.RaiseTapped();
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

            View.Map.VisibleRegion.Set(new RadialRegion(GetGeoLocation(topLeft), GetGeoLocation(bottomLeft), GetGeoLocation(bottomRight)));

            var region = RectangularRegion.FromCentre(View.Map.VisibleRegion.Value.Center,
                View.Map.VisibleRegion.Value.LatitudeDegrees, View.Map.VisibleRegion.Value.LongitudeDegrees);
            View.Map.CenterOfVisibleRegion.Set(region);
        }

        GeoLocation GetGeoLocation(Geopoint point) => new(point.Position.Latitude, point.Position.Longitude);

        async Task Calculate()
        {
            if (Result == null) return;

            await ApplyZoom();
        }

        void HandleEvents()
        {
            View.Map.ZoomLevel.ChangedBySource += () => Thread.UI.Run(Calculate);
            View.Map.Zoomable.ChangedBySource += () => Thread.UI.Run(ZoomEnabledChanged);
            View.Map.Pannable.ChangedBySource += () => Thread.UI.Run(ScrollEnabledChanged);
            View.Map.Rotatable.ChangedBySource += () => Thread.UI.Run(RotatableChanged);
            View.Map.Annotations.Added += a => Thread.UI.Run(() => RenderAnnotation(a));
            View.Map.Annotations.Removing += a => Thread.UI.Run(() => RemoveAnnotation(a));
            View.Map.Routes.Added += r => Thread.UI.Run(() => RenderRoute(r));
            View.Map.Routes.Removing += r => Thread.UI.Run(() => RemoveRoute(r));
            View.Map.Center.ChangedBySource += () => Thread.UI.Run(ApplyZoom);
            View.Map.MapType.ChangedBySource += () => Thread.UI.Run(() => Result.Style = GetMapType());
            Result.MapTapped += Result_MapTapped;
        }

        MapStyle GetMapType()
        {
            switch (View.Map.MapType.Value)
            {
                case MapTypes.Satelite:
                    return MapStyle.Aerial;
                case MapTypes.Hybrid:
                    return MapStyle.Road;
                default:
                    return MapStyle.None;
            }
        }

        void Result_MapTapped(MapControl sender, MapInputEventArgs args)
        {
            View.MapTapped.RaiseOn(Thread.UI, new GeoLocation(args.Location.Position.Latitude, args.Location.Position.Longitude));
        }

        void ZoomEnabledChanged()
        {
            if (Result == null) return;

            if (View.Map.Zoomable.Value && View.Map.ShowZoomControls.Value) Result.ZoomInteractionMode = MapInteractionMode.GestureAndControl;
            else if (View.Map.Zoomable.Value) Result.ZoomInteractionMode = MapInteractionMode.GestureOnly;
            else if (View.Map.ShowZoomControls.Value) Result.ZoomInteractionMode = MapInteractionMode.ControlOnly;
            else Result.ZoomInteractionMode = MapInteractionMode.Disabled;
        }

        void ScrollEnabledChanged()
        {
            if (Result == null) return;

            if (View.Map.Pannable) Result.PanInteractionMode = MapPanInteractionMode.Auto;
            else Result.PanInteractionMode = MapPanInteractionMode.Disabled;
        }

        void RotatableChanged()
        {
            if (Result == null) return;

            if (View.Map.Rotatable) Result.RotateInteractionMode = MapInteractionMode.GestureAndControl;
            else Result.RotateInteractionMode = MapInteractionMode.Disabled;
        }

        async Task RenderAnnotation(Annotation annotation)
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

        void RemoveAnnotation(Annotation annotation)
        {
            if (Result == null || annotation == null) return;

            var native = annotation.Native as MapElement;
            if (native == null) return;

            if (Result.MapElements.Contains(native))
                Result.MapElements.Remove(native);
        }

        async Task RenderRoute(Route route)
        {
            if (route == null || Result == null) return;
            if (route.Points.Length < 2)
            {
                Log.For(this).Warning("The route must contain at least two points!");
                return;
            }

            var path = route.Points.Select(l => l.Position()).ToArray();

            var polyline = new MapPolyline
            {
                Path = new Geopath(path),
                StrokeColor = route.Color.Render(),
                StrokeThickness = route.Thickness
            };

            route.Native = polyline;

            Result.MapElements.Add(polyline);
        }

        void RemoveRoute(Route route)
        {
            if (Result == null || route == null) return;

            var native = route.Native as MapPolyline;
            if (native == null) return;

            if (Result.MapElements.Contains(native))
                Result.MapElements.Remove(native);
        }

        async Task ApplyZoom()
        {
            if (Result == null) return;

            var center = (await View.GetCenter()).Render();

            if (View.Map.ZoomLevel.Value != default)
            {
                await Result.TrySetViewAsync(center, View.Map.ZoomLevel.Value).AsTask()
                    .WithTimeout(1.Seconds(), timeoutAction: () => Log.For(this).Warning("Map.TrySetViewAsync() timed out."));
            }
            else if (View.Map.Annotations.Any() || View.Map.Routes.Any())
            {
                var points = new List<Geopoint> { center };

                foreach (var location in View.Map.Annotations.Select(a => a.Location).Concat(View.Map.Routes.SelectMany(r => r.Points)))
                    points.Add(location.Render());

                var mapScene = MapScene.CreateFromLocations(points);

                await Result.TrySetSceneAsync(mapScene).AsTask()
                    .WithTimeout(1.Seconds(), timeoutAction: () => Log.For(this).Warning("Map.TrySetViewAsync() timed out."));
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
}