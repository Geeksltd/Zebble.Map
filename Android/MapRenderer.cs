namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;
    using AndroidOS;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class MapRenderer : INativeRenderer
    {
        Map View;
        MapLayout Container; // The map will be drawn onto this after the page is rendered.
        MapFragment Fragment;
        GoogleMap Map;
        static int NextId;

        public async Task<Android.Views.View> Render(Renderer renderer)
        {
            View = (Map)renderer.View;
            View.ShowZoomControlsChanged.HandleOn(Thread.UI, () => Map.UiSettings.ZoomControlsEnabled = View.ShowZoomControls);
            View.ZoomableChanged.HandleOn(Thread.UI, () => Map.UiSettings.ZoomControlsEnabled = View.Zoomable);
            View.PannableChanged.HandleOn(Thread.UI, () => Map.UiSettings.ScrollGesturesEnabled = View.Pannable);
            View.PannableChanged.HandleOn(Thread.UI, () => Map.UiSettings.RotateGesturesEnabled = View.Rotatable);
            View.ApiZoomChanged.HandleOn(Thread.UI, () => Map.AnimateCamera(CameraUpdateFactory.ZoomBy(View.ZoomLevel.Value)));
            View.AddedAnnotation.HandleOn(Thread.UI, a => RenderAnnotation(a));
            View.RemovedAnnotation.HandleOn(Thread.UI, a => RemoveAnnotation(a));
            View.ApiCenterChanged.HandleOn(Thread.UI, MoveToRegion);
            Container = new MapLayout(Renderer.Context) { Id = FindFreeId() };

            await View.WhenShown(() => { Thread.UI.Run(LoadMap); });
            return Container;
        }

        int FindFreeId()
        {
            NextId++;
            while (UIRuntime.CurrentActivity.FindViewById(NextId) != null) NextId++;
            return NextId;
        }

        async Task LoadMap()
        {
            //We should wait until the view id is added to resources dynamically
            while (UIRuntime.CurrentActivity.FindViewById(Container.Id) == null) await Task.Delay(Animation.OneFrame);

            Fragment = CreateFragment(Container, View.RenderOptions());
            if (IsDisposing()) return;

            await CreateMap();
            if (IsDisposing()) return;

            await View.Annotations.WhenAll(RenderAnnotation);
            if (IsDisposing()) return;

            var layoutParams = Fragment.View.LayoutParameters;

            await Task.CompletedTask;
        }

        MapFragment CreateFragment(MapLayout view, GoogleMapOptions options)
        {
            var fragment = MapFragment.NewInstance(options);
            var transaction = UIRuntime.CurrentActivity.FragmentManager.BeginTransaction();

            transaction.Add(view.Id, fragment);
            transaction.Commit();
            UIRuntime.CurrentActivity.FragmentManager.ExecutePendingTransactions();
            return fragment;
        }

        void Map_CameraChange(object _, GoogleMap.CameraChangeEventArgs args) => OnUserChangedRegion();

        async Task RenderAnnotation(Map.Annotation annotation)
        {
            if (annotation == null) return;
            if (annotation.Location == null)
            {
                Device.Log.Warning("annotation's Location is null!");
                return;
            }

            await AwaitMapCreation();

            var markerOptions = new MarkerOptions();
            markerOptions.SetPosition(annotation.Location.Render());
            markerOptions.SetTitle(annotation.Title.OrEmpty());
            markerOptions.SetSnippet(annotation.Subtitle.OrEmpty());

            if (annotation.Flat) markerOptions.Flat(annotation.Flat);

            if (annotation.IconPath.HasValue())
            {
                var provider = await annotation.GetPinImageProvider();
                var image = await provider.Result() as Android.Graphics.Bitmap;
                markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(image));
            }

            var marker = Map.AddMarker(markerOptions);
            marker.Tag = new AnnotationRef(annotation);
            annotation.Native = marker;
        }

        async Task AwaitMapCreation()
        {
            for (var retry = 10; retry > 0; retry--)
            {
                if (Map != null) return;
                await Task.Delay(50);
                retry--;
            }
        }

        void RemoveAnnotation(Map.Annotation annotation) => (annotation?.Native as Marker)?.Remove();

        async Task MoveToRegion()
        {
            if (IsDisposing()) return;

            await AwaitMapCreation();

            var update = CameraUpdateFactory.NewCameraPosition(
                CameraPosition.FromLatLngZoom((await View.GetCenter()).Render(),
                View.ZoomLevel ?? 10));
            try
            {
                Thread.UI.RunAction(() => Map?.AnimateCamera(update));
            }
            catch (Java.Lang.IllegalStateException exc)
            {
                Device.Log.Error("MoveToRegion exception: " + exc);
            }
        }

        void OnUserChangedRegion()
        {
            if (IsDisposing()) return;

            var projection = Map?.Projection;
            if (projection == null) return;
            var width = Fragment.View.Width;
            var height = Fragment.View.Height;
            var topLeft = projection.FromScreenLocation(new Android.Graphics.Point(0, 0));
            var bottomLeft = projection.FromScreenLocation(new Android.Graphics.Point(0, height));
            var bottomRight = projection.FromScreenLocation(new Android.Graphics.Point(width, height));
            View.VisibleRegion = new Map.Span(topLeft.ToZebble(), bottomLeft.ToZebble(), bottomRight.ToZebble());

            var region = Services.GeoRegion.FromCentre(View.VisibleRegion.Center,
                View.VisibleRegion.LatitudeDegrees, View.VisibleRegion.LongitudeDegrees);
            View.UserChangedRegion.RaiseOn(Thread.Pool, region);
        }

        async Task CreateMap()
        {
            var source = new TaskCompletionSource<GoogleMap>();
            Fragment?.GetMapAsync(new MapReadyCallBack(source.SetResult));
            Map = await source.Task;
            Map.UiSettings.ZoomControlsEnabled = View.ShowZoomControls;
            Map.UiSettings.ZoomGesturesEnabled = View.Zoomable;
            Map.UiSettings.ScrollGesturesEnabled = View.Pannable;
            Map.UiSettings.RotateGesturesEnabled = View.Rotatable;
            Map.CameraChange += Map_CameraChange;
            Map.InfoWindowClick += Map_InfoWindowClick;

            ApplyZoom();
        }

        void ApplyZoom()
        {
            if (View.ZoomLevel.HasValue)
            {
                Map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(View.Center.Render(), View.ZoomLevel.Value));
            }
            else if (View.Annotations.Any())
            {
                var builder = new LatLngBounds.Builder();
                builder.Include(new LatLng(View.Center.Latitude, View.Center.Longitude));

                foreach (var annotation in View.Annotations)
                {
                    builder.Include(new LatLng(annotation.Location.Latitude, annotation.Location.Longitude));
                }

                var bounds = builder.Build();

                var width = Scaler.ToDevice(View.ActualWidth);
                var height = Scaler.ToDevice(View.ActualHeight);
                // Offset from edges of the map 10% of screen
                var padding = (int)(width * 0.10);

                var cameraUpdate = CameraUpdateFactory.NewLatLngBounds(bounds, width, height, padding);

                Map.AnimateCamera(cameraUpdate);
            }
            else
            {
                Map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(View.Center.Render(), Zebble.Map.DefaultZoomLevel));
            }
        }

        void Map_InfoWindowClick(object _, GoogleMap.InfoWindowClickEventArgs e) => RaiseTapped(e.Marker);

        void RaiseTapped(Marker marker)
        {
            if (IsDisposing()) return;

            var annotation = (marker?.Tag as AnnotationRef)?.Annotation;
            if (annotation == null)
                Device.Log.Error("No map annotation was found for the tapped annotation!");
            else
                annotation.RaiseTapped();
        }

        public void Dispose()
        {
            Map.Perform(m => m.CameraChange -= Map_CameraChange);
            Map.Perform(m => m.InfoWindowClick -= Map_InfoWindowClick);
            View = null;
            Map = null;
            Fragment?.Dispose();
            Fragment = null;
            Container?.Dispose();
            Container = null;
        }

        bool IsDisposing() => View?.IsDisposing != false;
    }
}