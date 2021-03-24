namespace Zebble
{
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;
    using Olive.GeoLocation;

    static class RenderExtensions
    {
        public static LatLng Render(this IGeoLocation coordinate)
        {
            return new LatLng(coordinate.Latitude, coordinate.Longitude);
        }

        public static GoogleMapOptions RenderOptions(this MapView view)
        {
            return new GoogleMapOptions().InvokeMapType(GoogleMap.MapTypeNormal)
                .InvokeZoomControlsEnabled(view.Map.ShowZoomControls.Value)
                .InvokeZoomGesturesEnabled(view.Map.Zoomable.Value)
                .InvokeScrollGesturesEnabled(view.Map.Pannable.Value)
                .InvokeRotateGesturesEnabled(view.Map.Rotatable.Value);
        }

        public static GeoLocation ToLocation(this LatLng point) => new(point.Latitude, point.Longitude);
    }
}