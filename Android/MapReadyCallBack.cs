namespace Zebble
{
    using System;
    using Android.Gms.Maps;

    class MapReadyCallBack : Java.Lang.Object, IOnMapReadyCallback
    {
        Action<GoogleMap> Action;
        public MapReadyCallBack(Action<GoogleMap> action)
        {
            Action = action;
        }

        public void OnMapReady(GoogleMap googleMap) => Action(googleMap);
    }
}