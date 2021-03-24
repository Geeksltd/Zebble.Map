namespace Zebble
{
    using Olive.GeoLocation;
    using System;
    using Zebble.Mvvm;

    /// <summary>
    /// A class to represent a route on the map
    /// </summary>
    public partial class Route : ViewModel
    {
        /// <summary>
        /// The color of the route
        /// </summary>
        /// <remarks>Default is **Colors.Yellow**</remarks>
        public Color Color { get; private set; }

        /// <summary>
        /// The thickness of the route
        /// </summary>
        /// <remarks>Default is **3**</remarks>
        public int Thickness { get; private set; }

        /// <summary>
        /// A list of IGeoLocation instances (points) which shapes the desired route on the map
        /// </summary>
        public IGeoLocation[] Points { get; private set; }

        public Route(IGeoLocation[] points) : this(points, Colors.Yellow)
        {
        }

        public Route(IGeoLocation[] points, Color color, int thickness = 3)
        {
            Points = points ?? throw new ArgumentNullException(nameof(points));
            Color = color ?? throw new ArgumentNullException(nameof(color));
            Thickness = thickness;
        }
    }
}