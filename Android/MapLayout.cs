namespace Zebble
{
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using System;
    using Zebble.AndroidOS;

    class MapLayout : FrameLayout
    {
        View View;
        bool IsDisposed;

        public MapLayout(View view) : base(Renderer.Context) => View = view;

        [Preserve]
        protected MapLayout(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            var parentGestureView = this.TraverseUpToFind<AndroidGestureView>();
            if (parentGestureView is not null)
            {
                var originPoint = ev.GetPoint();
                var absolutePoint = originPoint.AbsoluteTo(View);

                var relativeEvent = MotionEvent.Obtain(ev.DownTime, ev.EventTime, ev.Action, absolutePoint.X, absolutePoint.Y, ev.MetaState);
                parentGestureView.OnTouchEvent(relativeEvent);
            }

            return base.OnInterceptTouchEvent(ev);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                RemoveAllViews();
                View = null;
            }

            base.Dispose(disposing);
        }
    }
}