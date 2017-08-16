namespace Zebble
{
    partial class Map
    {
        partial class Annotation
        {
            internal bool Flat
            {
                get;
                set;
            }
        }
    }

    internal class AnnotationRef : Java.Lang.Object
    {
        internal Map.Annotation Annotation;
        public AnnotationRef(Map.Annotation annotation) => Annotation = annotation;
        protected override void Dispose(bool disposing)
        {
            Annotation = null;
            base.Dispose(disposing);
        }
    }
}