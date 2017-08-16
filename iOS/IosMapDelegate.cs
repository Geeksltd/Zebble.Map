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

            var pin = (MKPinAnnotationView)mapView.DequeueReusableAnnotation("defaultPin")
                ?? new MKPinAnnotationView(annotation, "defaultPin") { CanShowCallout = true };

            pin.Annotation = annotation;
            AttachGestureToPin(pin, annotation);

            var view = (annotation as BasicMapAnnotation)?.View;
            if (view.IconPath.HasValue())
            {
                try
                {
                    Device.UIThread.Run(async () =>
                    {
                        var provider = await view.GetPinImageProvider();
                        var image = await provider.Result() as UIImage;

                        for (var ensureOverridesDefaultImage = 4;
                        ensureOverridesDefaultImage > 0;
                        ensureOverridesDefaultImage--)
                        {
                            pin.Image = image;
                            await Task.Delay(Animation.OneFrame);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Device.Log.Error("An error happened loading annotation pin image:");
                    Device.Log.Error(ex.Message);
                    Device.Log.Message(ex.StackTrace);
                }
            }

            return pin;
        }

        void AttachGestureToPin(MKPinAnnotationView mapPin, IMKAnnotation annotation)
        {
            var recognizers = mapPin.GestureRecognizers;

            if (recognizers != null)
                foreach (var r in recognizers)
                    mapPin.RemoveGestureRecognizer(r);

            var recognizer = new UITapGestureRecognizer(g => OnClick(annotation))
            {
                ShouldReceiveTouch = (gestureRecognizer, touch) => true
            };

            mapPin.AddGestureRecognizer(recognizer);
        }

        void OnClick(IMKAnnotation nativeAnnotation)
        {
            var annotation = (nativeAnnotation as BasicMapAnnotation)?.View;
            annotation?.RaiseTapped();
        }
    }
}