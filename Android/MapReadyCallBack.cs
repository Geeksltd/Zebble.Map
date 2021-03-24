namespace Zebble
{
    using System;
    using Android.Gms.Maps;

    class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        readonly Action<GoogleMap> Action;

        public MapReadyCallback(Action<GoogleMap> action) => Action = action;

        public void OnMapReady(GoogleMap googleMap) => Action(googleMap);
    }
}