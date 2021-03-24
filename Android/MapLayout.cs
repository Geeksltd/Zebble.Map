namespace Zebble
{
    using Android.Content;
    using Android.Views;
    using Android.Widget;

    class MapLayout : FrameLayout
    {
        ScrollView ScrollView;

        public MapLayout(Context context) : base(context) { }

        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            FindScrollView();

            var nativeScrollView = ScrollView?.Native as Android.Widget.ScrollView;

            switch (ev.Action)
            {
                case MotionEventActions.Up:
                case MotionEventActions.Down:
                    if (nativeScrollView == null) break;

                    nativeScrollView.RequestDisallowInterceptTouchEvent(disallowIntercept: true);
                    ScrollView.EnableScrolling = false;

                    break;
            }

            return base.DispatchTouchEvent(ev);
        }

        void FindScrollView()
        {
            if (ScrollView != null) return;

            foreach (var currentPage in UIRuntime.PageContainer.AllChildren.OfType<Page>())
            {
                foreach (var pageChild in currentPage.AllChildren)
                {
                    if (pageChild is Canvas scrollWrapper && scrollWrapper.Id != null && scrollWrapper.Id == "BodyScrollerWrapper")
                    {
                        ScrollView = scrollWrapper.AllChildren.FirstOrDefault() as ScrollView;
                        break;
                    }
                }

                if (ScrollView != null) break;
            }
        }
    }
}