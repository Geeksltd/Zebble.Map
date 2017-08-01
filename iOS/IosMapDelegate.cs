namespace Zebble.Plugin
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using MapKit;
    using ObjCRuntime;
    using UIKit;

    internal class IosMapDelegate : MKMapViewDelegate
    {
        Map Map;
        object LastTouchedView;

        internal IosMapDelegate(Map map) => Map = map;

        public override MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            if (Runtime.GetNSObject(annotation.Handle) is MKUserLocation userLocationAnnotation)
                return default(MKAnnotationView);

            var pin = (MKPinAnnotationView)mapView.DequeueReusableAnnotation("defaultPin");
            if (pin == null)
                pin = new MKPinAnnotationView(annotation, "defaultPin") { CanShowCallout = true };

            pin.Annotation = annotation;
            AttachGestureToPin(pin, annotation);

            var pinIcon = GetPin(annotation);
            if (pinIcon != null && pinIcon.IconPath.HasValue())
            {
                try
                {
                    var provider = Services.ImageService.GetImageProvider(pinIcon.IconPath, new Size(pinIcon.Width, pinIcon.Height), Stretch.Fit);
                    Device.UIThread.Run(async () =>
                    {
                        var image = await provider.Result() as UIImage;

                        for (var ensureOverridesDefaultImage = 4; ensureOverridesDefaultImage > 0; ensureOverridesDefaultImage--)
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

            Device.Log.Warning(pin);
            return pin;
        }

        void AttachGestureToPin(MKPinAnnotationView mapPin, IMKAnnotation annotation)
        {
            var recognizers = mapPin.GestureRecognizers;

            if (recognizers != null)
                foreach (var r in recognizers)
                    mapPin.RemoveGestureRecognizer(r);

            void action(UITapGestureRecognizer g) => OnClick(annotation);

            var recognizer = new UITapGestureRecognizer(action)
            {
                ShouldReceiveTouch = (gestureRecognizer, touch) => { LastTouchedView = touch.View; return true; }
            };

            mapPin.AddGestureRecognizer(recognizer);
        }

        void OnClick(IMKAnnotation nativeAnnotation)
        {
            if (LastTouchedView is MKAnnotationView)
                // Ignore it as it means the callout was shown (which is another view).
                return;

            var annotation = Runtime.GetNSObject(nativeAnnotation.Handle);
            if (annotation == null) return;

            var pin = Map.Annotations.FirstOrDefault(a => a.Native == annotation);
            if (pin != null) pin.Tapped.RaiseOn(Device.ThreadPool, pin);
        }

        Map.Annotation.Icon GetPin(IMKAnnotation nativeAnnotation)
        {
            var nativeAnnotationObject = Runtime.GetNSObject(nativeAnnotation.Handle);
            if (nativeAnnotationObject != null)
            {
                var annotation = Map.Annotations.FirstOrDefault(a => a.Native == nativeAnnotationObject);
                if (annotation != null) return annotation.Pin;
            }
            return null;
        }
    }
}