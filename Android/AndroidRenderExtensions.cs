namespace Zebble
{
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;
    using Zebble.Services;

    static class AndroidRenderExtensions
    {
        public static LatLng Render(this IGeoLocation coordinate)
        {
            return new LatLng(coordinate.Latitude, coordinate.Longitude);
        }

        public static GoogleMapOptions RenderOptions(this Map map)
        {
            return new GoogleMapOptions().InvokeMapType(GoogleMap.MapTypeNormal)
                .InvokeZoomControlsEnabled(map.ShowZoomControls)
                .InvokeZoomGesturesEnabled(map.Zoomable)
                .InvokeScrollGesturesEnabled(map.Pannable)
                .InvokeRotateGesturesEnabled(map.Rotatable);
        }

        public static GeoLocation ToZebble(this LatLng point) => new GeoLocation(point.Latitude, point.Longitude);
    }
}