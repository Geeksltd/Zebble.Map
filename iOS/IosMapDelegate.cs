namespace Zebble
{
    using MapKit;
    using ObjCRuntime;
    using UIKit;

    internal class IosMapDelegate : MKMapViewDelegate
    {
        Map Map;

        internal IosMapDelegate(Map map) => Map = map;

        public override MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            if (Runtime.GetNSObject(annotation.Handle) is MKUserLocation userLocationAnnotation)
                return default(MKAnnotationView);

            var pin = new MKAnnotationView(annotation, "defaultPin")
            {
                CanShowCallout = true,
                Annotation = annotation,
                RightCalloutAccessoryView = CreateCalloutButton(annotation)
            };

            var image = (annotation as BasicMapAnnotation)?.Image;
            if (image != null)
            {
                pin.Image = image;
                pin.ContentScaleFactor = Device.Screen.HardwareDensity;
                Device.UIThread.Post(() => pin.Image = image);
                pin.CenterOffset = new CoreGraphics.CGPoint(0, -image.Size.Height / 2);
            }

            return pin;
        }

        UIButton CreateCalloutButton(IMKAnnotation annotation)
        {
            var result = new UIButton(UIButtonType.DetailDisclosure);
            result.AddTarget((s, e) => OnClick(annotation), UIControlEvent.TouchUpInside);
            return result;
        }

        void OnClick(IMKAnnotation nativeAnnotation)
        {
            var annotation = (nativeAnnotation as BasicMapAnnotation)?.View;
            annotation?.RaiseTapped();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Map = null;
            }

            base.Dispose(disposing);
        }
    }
}