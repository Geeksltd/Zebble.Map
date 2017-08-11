namespace Zebble.Plugin
{
    using System;
    using Zebble.Services;

    partial class Map
    {
        public sealed class Span
        {
            const double EarthRadiusKm = 6371;
            const double EarthCircumferenceKm = EarthRadiusKm * 2 * Math.PI;
            const double MinimumRangeDegrees = 0.001 / EarthCircumferenceKm * 360; // 1 meter
            public Span(GeoLocation topLeft, GeoLocation bottomLeft, GeoLocation bottomRight)
            {
                TopLeft = topLeft;
                BottomLeft = bottomLeft;
                BottomRight = bottomRight;
                Center = new GeoLocation(topLeft.Latitude - bottomRight.Latitude, topLeft.Longitude - bottomRight.Longitude);
                TopRight = new GeoLocation{Latitude = Center.Latitude + (Center.Latitude - bottomLeft.Latitude), Longitude = Center.Longitude + (Center.Longitude - bottomLeft.Longitude)};
            }

            public GeoLocation TopLeft
            {
                get;
            }

            public GeoLocation TopRight
            {
                get;
            }

            public GeoLocation BottomLeft
            {
                get;
            }

            public GeoLocation BottomRight
            {
                get;
            }

            public bool IsNorthup => TopLeft.Latitude > BottomLeft.Latitude && TopLeft.Longitude == BottomLeft.Longitude;
            public GeoLocation Center
            {
                get;
            }

            public double LatitudeDegrees
            {
                get;
                private set;
            }

            public double LongitudeDegrees
            {
                get;
                private set;
            }

            public Distance Radius
            {
                get
                {
                    var latKm = LatitudeDegreesToKm(LatitudeDegrees);
                    var longKm = LongitudeDegreesToKm(Center, LongitudeDegrees);
                    return new Distance(1000 * Math.Min(latKm, longKm) / 2);
                }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                return obj is Span && Equals((Span)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Center.GetHashCode();
                    hashCode = (hashCode * 397) ^ LongitudeDegrees.GetHashCode();
                    hashCode = (hashCode * 397) ^ LatitudeDegrees.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(Span left, Span right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Span left, Span right)
            {
                return !Equals(left, right);
            }

            static double DistanceToLatitudeDegrees(Distance distance) => distance.Kilometers / EarthCircumferenceKm * 360;
            static double DistanceToLongitudeDegrees(GeoLocation position, Distance distance)
            {
                var latCircumference = LatitudeCircumferenceKm(position);
                return distance.Kilometers / latCircumference * 360;
            }

            bool Equals(Span other)
            {
                return Center.Equals(other.Center) && LongitudeDegrees.Equals(other.LongitudeDegrees) && LatitudeDegrees.Equals(other.LatitudeDegrees);
            }

            static double LatitudeCircumferenceKm(GeoLocation position)
            {
                return EarthCircumferenceKm * Math.Cos(position.Latitude.ToRadians());
            }

            public static double LatitudeDegreesToKm(double latitudeDegrees) => EarthCircumferenceKm * latitudeDegrees / 360;
            public static double LongitudeDegreesToKm(GeoLocation position, double longitudeDegrees)
            {
                var latCircumference = LatitudeCircumferenceKm(position);
                return latCircumference * longitudeDegrees / 360;
            }
        }

        public class GeographicalSpan
        {
            public double Horizontal
            {
                get;
                set;
            }

            public double Vertical
            {
                get;
                set;
            }
        }
    }
}