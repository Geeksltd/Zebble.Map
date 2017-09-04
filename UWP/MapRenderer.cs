namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.Devices.Geolocation;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls.Maps;
    using Zebble.Services;

    [EditorBrowsable(EditorBrowsableState.Never)]
    class MapRenderer : INativeRenderer
    {
        Map View;
        MapControl Result;
        const double DEGREE360 = 360;

        public async Task<FrameworkElement> Render(Renderer renderer)
        {
            View = (Map)renderer.View;
            Result = new MapControl
            {
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch,
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
                Center = (await View.GetCenter()).Render(),
                ZoomLevel = View.ZoomLevel
            };

            Windows.Services.Maps.MapService.ServiceToken = "HaXjmFYYzKaodYsgdNp2~PK515Syb0c0Z5AeqFmfuWQ~Anxgm-h_lBeSoSpJmDR5JDBovDqEU8wqmirJ4RzVuOvZiHS8uo8NLKE8M9L4ZPYP";

            Result.MapServiceToken = "HaXjmFYYzKaodYsgdNp2~PK515Syb0c0Z5AeqFmfuWQ~Anxgm-h_lBeSoSpJmDR5JDBovDqEU8wqmirJ4RzVuOvZiHS8uo8NLKE8M9L4ZPYP";

            ZoomEnabledChanged();
            ScrollEnabledChanged();
            RotatableChanged();
            await MoveToRegion();

            foreach (var a in View.Annotations) await RenderAnnotation(a);

            Result.ZoomLevelChanged += (s, a) => UpdatedVisibleRegion();
            Result.CenterChanged += (s, a) => UpdatedVisibleRegion();

            Result.MapElementClick += Result_MapElementClick;
            Result.Loaded += Result_Loaded;

            HandleEvents();

            return Result;
        }

        void Result_Loaded(object _, RoutedEventArgs __)
        {
            Result.Loaded -= Result_Loaded;
            MoveToRegion().RunInParallel();
        }

        void Result_MapElementClick(MapControl sender, MapElementClickEventArgs ev)
        {
            var markers = ev.MapElements.OfType<MapIcon>().ToList();

            foreach (var marker in markers)
                View.Annotations.FirstOrDefault(a => a.Native == marker)?.RaiseTapped();
        }

        async Task MoveToRegion()
        {
            await Result.TrySetViewAsync((await View.GetCenter()).Render()).AsTask()
                .WithTimeout(1.Seconds(), timeoutAction: () => Device.Log.Warning("Map.TrySetViewAsync() timed out."));
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
            View.UserChangedRegion.RaiseOn(Device.ThreadPool, region);
        }

        GeoLocation GetGeoLocation(Geopoint point) => new GeoLocation(point.Position.Latitude, point.Position.Longitude);

        Task CalCulate()
        {
            return Result.TryZoomToAsync(View.ZoomLevel).AsTask()
                .WithTimeout(1.Seconds(), timeoutAction: () => Device.Log.Warning("Map.TryZoomToAsync() timed out."));
        }

        void HandleEvents()
        {
            View.ApiZoomChanged.HandleOn(Device.UIThread, ZoomChanged);
            View.ZoomableChanged.HandleOn(Device.UIThread, () => ZoomEnabledChanged());
            View.PannableChanged.HandleOn(Device.UIThread, () => ScrollEnabledChanged());
            View.RotatableChanged.HandleOn(Device.UIThread, () => RotatableChanged());
            View.AddedAnnotation.HandleOn(Device.UIThread, RenderAnnotation);
            View.RemovedAnnotation.HandleOn(Device.UIThread, a => RemoveAnnotation(a));
            View.ApiCenterChanged.HandleOn(Device.UIThread, MoveToRegion);
        }

        Task ZoomChanged() => CalCulate();

        void ZoomEnabledChanged()
        {
            if (View.Zoomable && View.ShowZoomControls) Result.ZoomInteractionMode = MapInteractionMode.GestureAndControl;
            else if (View.Zoomable) Result.ZoomInteractionMode = MapInteractionMode.GestureOnly;
            else if (View.ShowZoomControls) Result.ZoomInteractionMode = MapInteractionMode.ControlOnly;
            else Result.ZoomInteractionMode = MapInteractionMode.Disabled;
        }

        void ScrollEnabledChanged()
        {
            if (View.Pannable) Result.PanInteractionMode = MapPanInteractionMode.Auto;
            else Result.PanInteractionMode = MapPanInteractionMode.Disabled;
        }

        void RotatableChanged()
        {
            if (View.Rotatable) Result.RotateInteractionMode = MapInteractionMode.GestureAndControl;
            else Result.RotateInteractionMode = MapInteractionMode.Disabled;
        }

        async Task UpdateAnnotations()
        {
            foreach (var annotation in View.Annotations)
            {
                try
                {
                    await RenderAnnotation(annotation);
                }
                catch (Exception ex)
                {
                    Device.Log.Error("Failed to render the annotation: " + annotation + Environment.NewLine + Environment.NewLine +
                        ex.Message);
                }
            }
        }

        async Task RenderAnnotation(Map.Annotation annotation)
        {
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
                Device.Log.Message(file.FullName);
                poi.Image = RandomAccessStreamReference.CreateFromFile(await file.ToStorageFile());
            }

            Result.MapElements.Add(poi);
        }

        void RemoveAnnotation(Map.Annotation annotation)
        {
            var native = annotation.Native as MapElement;
            if (native == null) return;

            if (Result.MapElements.Contains(native))
                Result.MapElements.Remove(native);
        }

        public void Dispose() => Result = null;
    }

    public static class RenderExtensions
    {
        public static Geopoint Render(this Services.IGeoLocation location)
        {
            return new Geopoint(new BasicGeoposition
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude
            });
        }
    }
}