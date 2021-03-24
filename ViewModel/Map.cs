namespace Zebble.Mvvm
{
    using Olive;
    using Olive.GeoLocation;

    public class Map
    {
        public const int MinimumZoomLevel = 1;
        public const int MaximumZoomLevel = 20;
        public static int DefaultZoomLevel = 13;

        public readonly CollectionViewModel<Annotation> Annotations = new();

        public readonly CollectionViewModel<Route> Routes = new();


        public readonly TwoWayBindable<MapTypes> MapType = new();

        public readonly TwoWayBindable<GeoLocation> Center = new();

        /// <summary>
        /// The map zoom level from 1 to 20. Default is 13. The higher, the more zoomed (close up).
        /// </summary>
        public readonly TwoWayBindable<int> ZoomLevel = new(DefaultZoomLevel);

        public readonly TwoWayBindable<bool> Zoomable = new(true);

        public readonly TwoWayBindable<bool> ShowZoomControls = new(false);

        public readonly TwoWayBindable<bool> Rotatable = new(false);

        public readonly TwoWayBindable<bool> Pannable = new(true);

        public readonly TwoWayBindable<RadialRegion> VisibleRegion = new();

        public readonly Bindable<RectangularRegion> CenterOfVisibleRegion = new();
    }
}