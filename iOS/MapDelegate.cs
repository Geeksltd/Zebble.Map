namespace Zebble
{
    using Foundation;
    using MapKit;
    using ObjCRuntime;
    using UIKit;

    class MapDelegate : MKMapViewDelegate
    {
        MapView View;

        internal MapDelegate(MapView view) => View = view;

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
                pin.Image = image;
            else
            {
                var base64Icon = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAMAAACdt4HsAAAAA3NCSVQICAjb4U/gAAAACXBIWXMAAA9GAAAPRgFoUyCCAAAAGXRFWHRTb2Z0d2FyZQB3d3cuaW5rc2NhcGUub3Jnm+48GgAAAN5QTFRF/////wAA/1VVQICAv0BA4zk52zc33zAwWWSFVWCA2zcu3zgw3Tcw4Dgs3jcs3zUw3zMu3DQw3jYu3Dcv3jUu3jQu3DUv3TUu3jQt3TUu3jQu3jUv3DYu3jYu3DUu3jUu3TYu3TUu3jUu3TUvVWCA3jUu3DUuVWCA3DUu3TYu3DYv3TUu3TUt3TUu3TUu3TUu3TUu3jUu3TYuzDo43TUu3TUuVWCA3TUu3zky3zkz4Ts14Tw240A65UM+5kVA50ZC6EhE6k1J605L7FBN7VFO71RS71VT71ZU8FZU92NjhE3rcQAAADZ0Uk5TAAIDBAQJDhAXGBwgJSkuMDc7PUFNVFdpa3l6g4WKjJGmp6iqtLe4vMDCw8vV2Nnc6+zz+vv+KgRvMgAAAURJREFUWMPtlcd2wjAQRYXpvWN6jQkJYKqtdNKD//+HSBQCB1tlZDZZ6K3fvYuRNEKIGa3YGZnLpTnqFDUkn0hjgQ9ZNCKyfHWOTzKvSuHhLvakG4bz0TGmZByF8toQUzOEzrKNGWnD+KxF2rcPN26BlQUJDFJ+dRznxW0wIHyOVDfOT57chhx4Al9E8OFnCjPS3BLBp1swE/PJ3+YzEWw8B5EUCgr75vs3/+Y9yYJQUPmr3j/eUa5CRSjQMTe6UFDmC8pCQZ4vyAsFCb4gIT7HCY+fAC5SkydoAgQZi81bGchrGrAFA9BzTq1Z/DoF2yh1lqAOXYo9Ot+Db3WDxhsSez3Q8vKtgNTXUjJPcbMk+7eF9OkRn+oh5CPpWv96tbrq19LId+K2HUfnRAmUQAmUQAmUQAn+myBm27GzBOjiMsgv7AC7BbLTYGBGfQAAAABJRU5ErkJggg==";
                var url = NSUrl.FromString(base64Icon);
                var imageData = NSData.FromUrl(url);
                image = UIImage.LoadFromData(imageData);

                pin.Image = image;
            }

            pin.ContentScaleFactor = Device.Screen.HardwareDensity;
            Thread.UI.Post(() => pin.Image = image);
            pin.CenterOffset = new CoreGraphics.CGPoint(0, -image.Size.Height / 2);

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
                View = null;

            base.Dispose(disposing);
        }
    }
}