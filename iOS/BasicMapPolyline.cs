namespace Zebble
{
    using CoreLocation;
    using Foundation;
    using MapKit;
    using ObjCRuntime;
    using Olive;
    using System;
    using System.Linq;

    class BasicMapPolyline : MKPolyline
    {
        public Route Route { get; private set; }

        readonly MKPolyline Instance;

        public BasicMapPolyline(Route route) : base()
        {
            Route = route ?? throw new ArgumentNullException(nameof(route));
            Route.Native = this;

            var points = route.Points.Select(p => new MKMapPoint(p.Latitude, p.Longitude)).ToArray();
            Instance = FromPoints(points);
        }

        public override CLLocationCoordinate2D Coordinate => Instance.Coordinate;

        public override string Title { get => Instance.Title; set => Instance.Title = value; }

        public override string Subtitle { get => Instance.Subtitle; set => Instance.Subtitle = value; }

        public override void SetCoordinate(CLLocationCoordinate2D value) => Instance.SetCoordinate(value);

        public override nint PointCount => Instance.PointCount;

        public new MKMapPoint[] Points => Instance.Points;

        public override MKMapRect BoundingMapRect => Instance.BoundingMapRect;

        public override bool CanReplaceMapContent => Instance.CanReplaceMapContent;

        public override nfloat GetLocation(nuint pointIndex) => Instance.GetLocation(pointIndex);

        [return: BindAs(typeof(nfloat[]), OriginalType = typeof(NSNumber[]))]
        public override nfloat[] GetLocations(NSIndexSet indexes) => Instance.GetLocations(indexes);

        protected override void Dispose(bool disposing)
        {
            Route = null;
            base.Dispose(disposing);
        }
    }
}