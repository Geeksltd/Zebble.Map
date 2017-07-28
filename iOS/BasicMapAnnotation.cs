namespace Zebble.Plugin
{
    using CoreLocation;
    using MapKit;
    using static Zebble.Plugin.Map;

    public class BasicMapAnnotation : MKAnnotation
    {
        public Annotation Info;
        CLLocationCoordinate2D coordinate;
        string title, subtitle, content;

        public BasicMapAnnotation(CLLocationCoordinate2D coordinate, string title, string subtitle, string content)
        {
            this.coordinate = coordinate;
            this.title = title;
            this.subtitle = subtitle;
            this.content = content;
        }

        public override CLLocationCoordinate2D Coordinate => coordinate;

        public override void SetCoordinate(CLLocationCoordinate2D value) => coordinate = value;

        public override string Title => title;

        public override string Subtitle => subtitle;

        public override string Description => content;

        protected override void Dispose(bool disposing)
        {
            Info = null;
            base.Dispose(disposing);
        }
    }
}