namespace Zebble
{
    using Olive.GeoLocation;
    using Zebble.Mvvm;

    public partial class Annotation : ViewModel
    {
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// This is ignored in UWP.
        /// </summary>
        public string Subtitle { get; set; } = string.Empty;

        public GeoLocation Location { get; set; } = new();

        /// <summary>
        /// Path to the pin icon (optional).
        /// </summary>
        public string IconPath { get; set; } = string.Empty;

        public float IconWidth { get; set; } = 40;

        public float IconHeight { get; set; } = 60;
    }
}