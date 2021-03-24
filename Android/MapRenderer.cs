namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;
    using Olive;
    using Olive.GeoLocation;

    [EditorBrowsable(EditorBrowsableState.Never)]
    class MapRenderer : INativeRenderer
    {
        MapView View;
        MapLayout Container; // The map will be drawn onto this after the page is rendered.
        MapFragment Fragment;
        GoogleMap Map;
        static int NextId;

        public async Task<Android.Views.View> Render(Renderer renderer)
        {
            View = (MapView)renderer.View;

            View.Map.ShowZoomControls.ChangedBySource += () => Thread.UI.Run(() => Map.UiSettings.ZoomControlsEnabled = View.Map.ShowZoomControls.Value);
            View.Map.Zoomable.ChangedBySource += () => Thread.UI.Run(() => Map.UiSettings.ZoomControlsEnabled = View.Map.Zoomable.Value);
            View.Map.Pannable.ChangedBySource += () => Thread.UI.Run(() => Map.UiSettings.ScrollGesturesEnabled = View.Map.Pannable.Value);
            View.Map.Rotatable.ChangedBySource += () => Thread.UI.Run(() => Map.UiSettings.RotateGesturesEnabled = View.Map.Rotatable.Value);
            View.Map.ZoomLevel.ChangedBySource += () => Thread.UI.Run(() => Map.AnimateCamera(CameraUpdateFactory.ZoomBy(View.Map.ZoomLevel.Value)));
            View.Map.Annotations.Added += a => Thread.UI.Run(() => RenderAnnotation(a));
            View.Map.Annotations.Removing += a => Thread.UI.Run(() => RemoveAnnotation(a));
            View.Map.Routes.Added += r => Thread.UI.Run(() => RenderRoute(r));
            View.Map.Routes.Removing += r => Thread.UI.Run(() => RemoveRoute(r));
            View.Map.Center.ChangedBySource += () => Thread.UI.Run(MoveToRegion);
            View.Map.MapType.ChangedBySource += () => Thread.UI.Run(() => Map.MapType = GetMapType());

            Container = new MapLayout(Renderer.Context) { Id = FindFreeId() };

            Thread.UI.Post(async () => await LoadMap());

            return Container;
        }

        int FindFreeId()
        {
            NextId++;
            while (UIRuntime.CurrentActivity.FindViewById(NextId) != null) NextId++;
            return NextId;
        }

        int GetMapType()
        {
            return View.Map.MapType.Value switch
            {
                MapTypes.Satelite => GoogleMap.MapTypeSatellite,
                MapTypes.Hybrid => GoogleMap.MapTypeHybrid,
                _ => GoogleMap.MapTypeNormal,
            };
        }

        async Task LoadMap()
        {
            //We should wait until the view id is added to resources dynamically
            while (UIRuntime.CurrentActivity.FindViewById(Container.Id) == null) await Task.Delay(Animation.OneFrame);

            Fragment = CreateFragment(Container, View.RenderOptions());
            if (IsDisposing()) return;

            await CreateMap();
            if (IsDisposing()) return;

            await View.Map.Annotations.AwaitAll(RenderAnnotation);
            await View.Map.Routes.AwaitAll(RenderRoute);
            if (IsDisposing()) return;

            Map.MapType = GetMapType();

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

        async Task RenderAnnotation(Annotation annotation)
        {
            if (annotation == null) return;
            if (annotation.Location == null)
            {
                Log.For(this).Warning("annotation's Location is null!");
                return;
            }

            await AwaitMapCreation();

            var markerOptions = new MarkerOptions();
            markerOptions.SetPosition(annotation.Location.Render());
            markerOptions.SetTitle(annotation.Title.OrEmpty());
            markerOptions.SetSnippet(annotation.Subtitle.OrEmpty());

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

        void RemoveAnnotation(Annotation annotation) => (annotation?.Native as Marker)?.Remove();

        async Task RenderRoute(Route route)
        {
            if (route == null) return;
            if (route.Points.Length < 2)
            {
                Log.For(this).Warning("The route must contain at least two points!");
                return;
            }

            await AwaitMapCreation();

            var options = new PolylineOptions();

            options.InvokeColor(route.Color.Render());
            options.InvokeWidth(route.Thickness);

            foreach (var point in route.Points)
                options.Add(point.Render());

            route.Native = Map.AddPolyline(options);
        }

        void RemoveRoute(Route route) => (route?.Native as Polyline)?.Remove();

        async Task AwaitMapCreation()
        {
            for (var retry = 10; retry > 0; retry--)
            {
                if (Map != null) return;
                await Task.Delay(50);
                retry--;
            }
        }

        async Task MoveToRegion()
        {
            if (IsDisposing()) return;

            await AwaitMapCreation();

            var update = CameraUpdateFactory.NewCameraPosition(
                CameraPosition.FromLatLngZoom((await View.GetCenter()).Render(),
                View.Map.ZoomLevel.Value));

            try
            {
                Thread.UI.RunAction(() => Map?.AnimateCamera(update));
            }
            catch (Java.Lang.IllegalStateException exc)
            {
                Log.For(this).Error(exc, "MoveToRegion exception.");
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

            View.Map.VisibleRegion.Set(new RadialRegion(topLeft.ToLocation(), bottomLeft.ToLocation(), bottomRight.ToLocation()));

            var region = RectangularRegion.FromCentre(
                View.Map.VisibleRegion.Value.Center,
                View.Map.VisibleRegion.Value.LatitudeDegrees,
                View.Map.VisibleRegion.Value.LongitudeDegrees
            );
            View.Map.CenterOfVisibleRegion.Set(region);
        }

        async Task CreateMap()
        {
            var source = new TaskCompletionSource<GoogleMap>();
            Fragment?.GetMapAsync(new MapReadyCallback(source.SetResult));

            Map = await source.Task;
            Map.UiSettings.ZoomControlsEnabled = View.Map.ShowZoomControls.Value;
            Map.UiSettings.ZoomGesturesEnabled = View.Map.Zoomable.Value;
            Map.UiSettings.ScrollGesturesEnabled = View.Map.Pannable.Value;
            Map.UiSettings.RotateGesturesEnabled = View.Map.Rotatable.Value;
            Map.CameraChange += Map_CameraChange;
            Map.InfoWindowClick += Map_InfoWindowClick;
            Map.MapClick += Map_MapClick;
            Map.MapLongClick += Map_MapLongClick;

            await ApplyZoom();
        }

        void Map_MapLongClick(object sender, GoogleMap.MapLongClickEventArgs e)
        {
            View.MapLongPressed.RaiseOn(Thread.UI, new GeoLocation(e.Point.Latitude, e.Point.Longitude));
        }

        void Map_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            View.MapTapped.RaiseOn(Thread.UI, new GeoLocation(e.Point.Latitude, e.Point.Longitude));
        }

        async Task ApplyZoom()
        {
            var center = View.Map.Center.Value ?? await View.GetCenter();

            if (View.Map.ZoomLevel.Value != default)
            {
                Map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(center.Render(), View.Map.ZoomLevel.Value));
                return;
            }



            if (View.Map.Annotations.Any() || View.Map.Routes.Any())
            {
                using (var builder = new LatLngBounds.Builder())
                {
                    builder.Include(center.Render());

                    foreach (var location in View.Map.Annotations.Select(a => a.Location).Concat(View.Map.Routes.SelectMany(r => r.Points)))
                        builder.Include(location.Render());

                    var bounds = builder.Build();

                    var width = Zebble.Device.Scale.ToDevice(View.ActualWidth);
                    var height = Zebble.Device.Scale.ToDevice(View.ActualHeight);
                    // Offset from edges of the map 10% of screen
                    var padding = (int)(width * 0.10);

                    var cameraUpdate = CameraUpdateFactory.NewLatLngBounds(bounds, width, height, padding);

                    Map.AnimateCamera(cameraUpdate);
                }
            }
            else
            {
                Map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(center.Render(), Zebble.Mvvm.Map.DefaultZoomLevel));
            }
        }

        void Map_InfoWindowClick(object _, GoogleMap.InfoWindowClickEventArgs e) => RaiseTapped(e.Marker);

        void RaiseTapped(Marker marker)
        {
            if (IsDisposing()) return;

            var annotation = (marker?.Tag as AnnotationRef)?.Annotation;
            if (annotation == null)
                Log.For(this).Error("No map annotation was found for the tapped annotation!");
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