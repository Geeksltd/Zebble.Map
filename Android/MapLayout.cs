namespace Zebble
{
    using Android.Content;
    using Android.Views;
    using Android.Widget;
    using System;
    using Zebble.AndroidOS;

    class MapLayout : FrameLayout
    {
        FrameLayout OuterView;

        public MapLayout(Context context) : base(context) { }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            this.TraverseUpToFind<AndroidGestureView>()?.OnTouchEvent(ev);
            return base.OnInterceptTouchEvent(ev);
        }
    }
}