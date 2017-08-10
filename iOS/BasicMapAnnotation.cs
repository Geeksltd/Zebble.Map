namespace Zebble.Plugin
{
    using CoreLocation;
    using MapKit;

    public class BasicMapAnnotation : MKAnnotation
    {
        public Map.Annotation View { get; private set; }
        CLLocationCoordinate2D coordinate;

        public BasicMapAnnotation(Map.Annotation view)
        {
            View = view;
            View.Native = this;
            coordinate = new CLLocationCoordinate2D(view.Location.Latitude, view.Location.Longitude);
        }

        public override CLLocationCoordinate2D Coordinate => coordinate;

        public override void SetCoordinate(CLLocationCoordinate2D value) => coordinate = value;

        public override string Title => View.Title;

        public override string Subtitle => View.SubTitle;

        public override string Description => View.Content;

        protected override void Dispose(bool disposing)
        {
            View = null;
            base.Dispose(disposing);
        }
    }
}