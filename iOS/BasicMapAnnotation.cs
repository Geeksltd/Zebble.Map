namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using CoreLocation;
    using MapKit;
    using UIKit;

    internal class BasicMapAnnotation : MKAnnotation
    {
        CLLocationCoordinate2D coordinate;
        public Map.Annotation View { get; private set; }
        internal UIImage Image;

        public BasicMapAnnotation(Map.Annotation view)
        {
            View = view;
            View.Native = this;
            coordinate = new CLLocationCoordinate2D(view.Location.Latitude, view.Location.Longitude);
        }

        internal async Task AwaitImage()
        {
            if (View.IconPath.HasValue())
            {
                var provider = await View.GetPinImageProvider();
                Image = await provider.Result() as UIImage;
            }
        }

        public override void SetCoordinate(CLLocationCoordinate2D value) => coordinate = value;

        public override CLLocationCoordinate2D Coordinate => coordinate;

        public override string Title => View.Title;

        public override string Subtitle => View.Subtitle;

        protected override void Dispose(bool disposing) { View = null; base.Dispose(disposing); }
    }
}