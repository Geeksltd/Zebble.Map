namespace Zebble.Plugin.Renderer
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Android.Gms.Maps;
    using Android.Gms.Maps.Model;
    using Android.Widget;
    using Zebble;
    using Zebble.Services;
    using static Zebble.Plugin.Map;

    static class AndroidRenderExtensions
    {
        public static LatLng Render(this IGeoLocation coordinate)
        {
            return new LatLng(coordinate.Latitude, coordinate.Longitude);
        }

        public static GoogleMapOptions RenderOptions(this Map map)
        {
            return new GoogleMapOptions()
              .InvokeZoomControlsEnabled(map.ShowZoomControls)
              .InvokeZoomGesturesEnabled(map.ZoomEnable)
              .InvokeMapType(GoogleMap.MapTypeNormal)
              .InvokeScrollGesturesEnabled(map.ScrollEnabled)
              .InvokeRotateGesturesEnabled(enabled: false);
        }

        public static GeoLocation ToZebble(this LatLng point) => new GeoLocation(point.Latitude, point.Longitude);
    }
}