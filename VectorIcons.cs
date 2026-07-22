using System;
using Avalonia.Media;
using Avalonia.Controls.Shapes;

namespace EternSynth
{
    public static class VectorIcons
    {
        public const string Play = "M8,5.14V19.14L19,12.14L8,5.14Z";
        public const string Stop = "M6,6H18V18H6V6Z";
        public const string Volume = "M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 16.89,17.85 14,18.71V20.77C18,19.86 21,16.28 21,12C21,7.72 18,4.14 14,3.23M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16C15.5,15.29 16.5,13.77 16.5,12M3,9V15H7L12,20V4L7,9H3Z";
        public const string Trash = "M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z";
        public const string Folder = "M20,18A2,2 0 0,1 18,20H6A2,2 0 0,1 4,18V6C4,4.89 4.9,4 6,4H12L14,6H18A2,2 0 0,1 20,8V18M20,8H6V18H20V8Z";
        public const string Save = "M15,9H5V5H15M12,19A3,3 0 0,1 9,16A3,3 0 0,1 12,13A3,3 0 0,1 15,16A3,3 0 0,1 12,19M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3Z";
        public const string Copy = "M19,21H8V7H19M19,5H8A2,2 0 0,0 6,7V21A2,2 0 0,0 8,23H19A2,2 0 0,0 21,21V7A2,2 0 0,0 19,5M16,1H4A2,2 0 0,0 2,3V17H4V3H16V1Z";
        public const string Paste = "M19,2H14.82C14.4,0.84 13.3,0 12,0C10.7,0 9.6,0.84 9.18,2H5A2,2 0 0,0 3,4V20A2,2 0 0,0 5,22H19A2,2 0 0,0 21,20V4A2,2 0 0,0 19,2M12,2A1,1 0 0,1 13,3A1,1 0 0,1 12,4A1,1 0 0,1 11,3A1,1 0 0,1 12,2M19,20H5V4H7V7H17V4H19V20Z";
        public const string Link = "M10.59,13.41C11.37,14.19 12.63,14.19 13.41,13.41L20,6.83C21.11,5.72 21.11,3.93 20,2.82C18.89,1.71 17.1,1.71 16,2.82L9.41,9.41C8.63,10.19 8.63,11.45 9.41,12.23L10.59,13.41M13.41,10.59L12.23,9.41C11.45,8.63 10.19,8.63 9.41,9.41L2.82,16C1.71,17.1 1.71,18.89 2.82,20C3.93,21.11 5.72,21.11 6.83,20L13.41,13.41C14.19,12.63 14.19,11.37 13.41,10.59Z";
        public const string Close = "M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z";
        public const string Minimize = "M20,14H4V12H20V14Z";
        public const string Maximize = "M4,4H20V20H4V4M6,6V18H18V6H6Z";
        public const string Restore = "M4,8H8V4H20V16H16V20H4V8M16,8V14H18V6H10V8H16M6,10V18H14V10H6Z";

        public static Path GetIcon(string geometryData, IBrush brush, double size = 16)
        {
            return new Path
            {
                Data = Geometry.Parse(geometryData),
                Fill = brush,
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform
            };
        }
    }
}
