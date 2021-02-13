namespace Zebble
{
    partial class Annotation
    {
        internal bool Flat { get; set; }
    }

    class AnnotationRef : Java.Lang.Object
    {
        internal Annotation Annotation;

        public AnnotationRef(Annotation annotation) => Annotation = annotation;

        protected override void Dispose(bool disposing)
        {
            Annotation?.Dispose();
            Annotation = null;
            base.Dispose(disposing);
        }
    }
}