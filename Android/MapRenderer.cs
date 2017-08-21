namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class MapRenderer : INativeRenderer
    {
        Map View;
        MapLayout Container; // The map will be drawn onto this after the page is rendered.
        MapFragment Fragment;
        GoogleMap Map;
        const double DEGREE360 = 360;
        public async Task<Android.Views.View> Render(Renderer renderer)
        {
            View = (Map)renderer.View;
            View.ShowZoomControlsChanged.HandleOn(Device.UIThread, () => Map.UiSettings.ZoomControlsEnabled = View.ShowZoomControls);
            View.ZoomableChanged.HandleOn(Device.UIThread, () => Map.UiSettings.ZoomControlsEnabled = View.Zoomable);
            View.PannableChanged.HandleOn(Device.UIThread, () => Map.UiSettings.ScrollGesturesEnabled = View.Pannable);
            View.PannableChanged.HandleOn(Device.UIThread, () => Map.UiSettings.RotateGesturesEnabled = View.Rotatable);
            View.ApiZoomChanged.HandleOn(Device.UIThread, () => Map.AnimateCamera(CameraUpdateFactory.ZoomBy(View.ZoomLevel)));
            View.AddedAnnotation.HandleOn(Device.UIThread, a => RenderAnnotation(a));
            View.RemovedAnnotation.HandleOn(Device.UIThread, a => RemoveAnnotation(a));
            View.ApiCenterChanged.HandleOn(Device.UIThread, MoveToRegion);
            Container = new MapLayout(Renderer.Context) { Id = Android.Views.View.GenerateViewId() };
            await View.WhenShown(() => { Device.UIThread.Run(LoadMap); });
            return Container;
        }

        Task FixThread() => Task.Delay(Animation.OneFrame);

        async Task LoadMap()
        {
            await Task.Delay(Animation.OneFrame);
            Fragment = CreateFragment(Container, View.RenderOptions());
            await Task.Delay(Animation.OneFrame); // Wait for the fragment to be created.
            await CreateMap();
            await View.Annotations.WhenAll(RenderAnnotation);
            var layoutParams = Fragment.View.LayoutParameters;
            await Task.CompletedTask;
        }

        MapFragment CreateFragment(MapLayout view, GoogleMapOptions options)
        {
            var fragment = MapFragment.NewInstance(options);
            var transaction = UIRuntime.CurrentActivity.FragmentManager.BeginTransaction();
            transaction.Add(view.Id, fragment);
            transaction.Commit();
            return fragment;
        }

        void Map_CameraChange(object _, GoogleMap.CameraChangeEventArgs args) => OnUserChangedRegion();

        async Task RenderAnnotation(Map.Annotation annotation)
        {
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

        void RemoveAnnotation(Map.Annotation annotation) => (annotation.Native as Marker)?.Remove();

        async Task MoveToRegion()
        {
            var update = CameraUpdateFactory.NewCameraPosition(CameraPosition.FromLatLngZoom((await View.GetCenter()).Render(), View.ZoomLevel));
            try
            {
                Device.UIThread.RunAction(() => Map?.AnimateCamera(update));
            }
            catch (Java.Lang.IllegalStateException exc)
            {
                Device.Log.Error("MoveToRegion exception: " + exc);
                Device.Log.Warning($"Zebble AndroidMapView MoveToRegion exception: {exc}");
            }
        }

        void OnUserChangedRegion()
        {
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
            View.UserChangedRegion.RaiseOn(Device.ThreadPool, region);
        }

        void OnApiZoomChanged() => Map.AnimateCamera(CameraUpdateFactory.ZoomTo(1 + View.ZoomLevel));

        async Task CreateMap()
        {
            var source = new TaskCompletionSource<GoogleMap>();
            Fragment.GetMapAsync(new MapReadyCallBack(source.SetResult));
            Map = await source.Task;
            Map.UiSettings.ZoomControlsEnabled = View.ShowZoomControls;
            Map.UiSettings.ZoomGesturesEnabled = View.Zoomable;
            Map.UiSettings.ScrollGesturesEnabled = View.Pannable;
            Map.UiSettings.RotateGesturesEnabled = View.Rotatable;
            Map.CameraChange += Map_CameraChange;
            //Map.MarkerClick += Map_MarkerClick;
            Map.InfoWindowClick += Map_InfoWindowClick;
            Map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(View.Center.Render(), View.ZoomLevel));
        }

        void Map_InfoWindowClick(object _, GoogleMap.InfoWindowClickEventArgs e) => RaiseTapped(e.Marker);

        //void Map_MarkerClick(object _, GoogleMap.MarkerClickEventArgs e)
        //{
        //    RaiseTapped(e.Marker);
        //    e.Handled = false;
        //}

        void RaiseTapped(Marker marker)
        {
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
            //Map.Perform(m => m.MarkerClick -= Map_MarkerClick);
            Map = null;
            Fragment?.Dispose();
            Fragment = null;
            View = null;
            Container?.Dispose();
            Container = null;
        }
    }
}