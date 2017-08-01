namespace Zebble.Plugin.Renderer
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;
    using Android.Views;
    using Android.Widget;
    using Zebble;
    using Zebble.Services;
    using static Zebble.Plugin.Map;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MapRenderer : INativeRenderer
    {
        Map View;
        FrameLayout Result;

        MapFragment Fragment;
        GoogleMap Map;
        const double DEGREE360 = 360;

        public async Task<Android.Views.View> Render(Renderer renderer)
        {
            View = (Map)renderer.View;
            Result = new FrameLayout(Renderer.Context) { Id = Android.Views.View.GenerateViewId() };

            await View.WhenShown(LoadMap);

            return Result;
        }

        Task FixThread() => Task.Delay(Animation.OneFrame);

        async Task LoadMap()
        {
            View.ZoomEnabledChanged.HandleOn(Device.UIThread, () => Map.UiSettings.ZoomControlsEnabled = View.ZoomEnable);
            View.ScrollEnabledChanged.HandleOn(Device.UIThread, () => Map.UiSettings.ScrollGesturesEnabled = View.ScrollEnabled);
            View.ZoomChanged.HandleOn(Device.UIThread, CalCulate);
            View.AnnotationsChanged.HandleOn(Device.UIThread, UpdateAnnotations);

            View.NativeRefreshControl = MoveToRegion;

            var options = new GoogleMapOptions()
                .InvokeZoomControlsEnabled(View.ShowZoomControls)
                .InvokeZoomGesturesEnabled(View.ZoomEnable)
                .InvokeMapType(GoogleMap.MapTypeNormal)
                .InvokeScrollGesturesEnabled(View.ScrollEnabled)
                .InvokeRotateGesturesEnabled(enabled: false);

            await FixThread();

            Fragment = SetUpMap(Result, options);

            await FixThread();

            await Device.UIThread.Run(GetMap);

            Device.UIThread.RunAction(async () => await UpdateAnnotations());

            await Task.CompletedTask;
        }

        MapFragment SetUpMap(FrameLayout view, GoogleMapOptions options)
        {
            var fragment = MapFragment.NewInstance(options);
            var transaction = UIRuntime.CurrentActivity.FragmentManager.BeginTransaction();
            view.Id = Android.Views.View.GenerateViewId();
            transaction.Add(view.Id, fragment);
            transaction.Commit();
            return fragment;
        }

        void Map_CameraChange(object sender, GoogleMap.CameraChangeEventArgs e)
        {
            UpdateVisibleRegion(e.Position.Target);
        }

        Task UpdateAnnotations()
        {
            return View.Annotations.WhenAll(async annotation =>
            {
                var markerOptions = new MarkerOptions();
                markerOptions.SetPosition(annotation.Location.Render());
                markerOptions.SetTitle(annotation.Title);
                markerOptions.SetSnippet(annotation.Content);
                if (annotation.Flat) markerOptions.Flat(annotation.Flat);
                if (annotation.Pin.IconPath.HasValue())
                {
                    var provider = ImageService.GetImageProvider(annotation.Pin.IconPath, new Size(annotation.Pin.Width, annotation.Pin.Height), Stretch.Fit);
                    var image = await provider.Result() as Android.Graphics.Bitmap;
                    markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(image));
                }
                annotation.Id = Map.AddMarker(markerOptions).Id;

                await Task.CompletedTask;
            });
        }

        async Task MoveToRegion()
        {
            var map = Map;

            var update = CameraUpdateFactory.NewCameraPosition(
                CameraPosition.FromLatLngZoom((await View.GetCenter()).Render(),
                View.ZoomLevel));

            try
            {
                Device.UIThread.RunAction(() => map.AnimateCamera(update));
            }
            catch (Java.Lang.IllegalStateException exc)
            {
                Device.Log.Error("MoveToRegion exception: " + exc);
                Device.Log.Warning($"Zebble AndroidMapView MoveToRegion exception: {exc}");
            }
        }

        Task UpdateVisibleRegion(LatLng _)
        {
            var map = Map;
            if (map == null) return Task.CompletedTask;

            var projection = map.Projection;
            var width = Fragment.View.Width;
            var height = Fragment.View.Height;
            var topLeft = projection.FromScreenLocation(new global::Android.Graphics.Point(0, 0));
            var bottomLeft = projection.FromScreenLocation(new global::Android.Graphics.Point(0, height));
            var bottomRight = projection.FromScreenLocation(new global::Android.Graphics.Point(width, height));
            View.VisibleRegion = new Span(GetGeoLocation(topLeft), GetGeoLocation(bottomLeft), GetGeoLocation(bottomRight));
            return Task.CompletedTask;
        }

        GeoLocation GetGeoLocation(LatLng point) => new GeoLocation(point.Latitude, point.Longitude);

        Task CalCulate()
        {
            var map = Map;

            var cameraUpdate = CameraUpdateFactory.ZoomBy(View.ZoomLevel);
            map.AnimateCamera(cameraUpdate);

            return Task.CompletedTask;
        }

        class MapReadyCallBack : Java.Lang.Object, IOnMapReadyCallback
        {
            Action<GoogleMap> Action;
            public MapReadyCallBack(Action<GoogleMap> action) { Action = action; }

            public void OnMapReady(GoogleMap googleMap) => Action(googleMap);
        }

        public async Task GetMap()
        {
            if (Map != null) return;

            await FixThread();

            var source = new TaskCompletionSource<GoogleMap>();
            Fragment.GetMapAsync(new MapReadyCallBack(source.SetResult));

            await FixThread();
            var map = await source.Task;
            await FixThread();

            map.CameraChange += Map_CameraChange;
            map.InfoWindowClick += (s, e) =>
            {
                View.Annotations.FirstOrDefault(a => a.Id == e.Marker.Id)?.Tapped?.Raise(new Map.Annotation
                {
                    Draggable = e.Marker.Draggable,
                    Flat = e.Marker.Flat,
                    Id = e.Marker.Id,
                    Location = new GeoLocation(e.Marker.Position.Latitude, e.Marker.Position.Longitude),
                    Title = e.Marker.Title,
                    Visible = e.Marker.Visible,
                    Native = e.Marker
                });
            };

            var cameraUpdate = CameraUpdateFactory.NewLatLngZoom(View.Center.Render(), View.ZoomLevel);
            map.AnimateCamera(cameraUpdate);

            Map = map;
        }

        public void Dispose()
        {
            Fragment?.Dispose();
            Fragment = null;
            View = null;
            Result?.Dispose();
            Result = null;
        }
    }

    public static class AndroidRenderExtensions
    {
        public static LatLng Render(this IGeoLocation coordinate) => new LatLng(coordinate.Latitude, coordinate.Longitude);
    }
}