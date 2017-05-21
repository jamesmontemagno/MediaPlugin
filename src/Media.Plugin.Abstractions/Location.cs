using System;

namespace Plugin.Media.Abstractions
{
    public struct Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double HorizontalAccuracy { get; set; }
        public double Speed { get; set; }
        public double Direction { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
