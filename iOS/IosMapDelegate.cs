namespace Zebble
{
    using System;
    using System.Threading.Tasks;
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

            var pin = mapView.DequeueReusableAnnotation("defaultPin")
                ?? new MKAnnotationView(annotation, "defaultPin") { CanShowCallout = true };

            pin.Annotation = annotation;
            pin.RightCalloutAccessoryView = CreateCalloutButton(annotation);

            var image = (annotation as BasicMapAnnotation)?.Image;
            if (image != null)
            {
                pin.Image = image;
                Device.UIThread.Post(() => pin.Image = image);
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
    }
}