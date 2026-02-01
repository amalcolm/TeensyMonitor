using OpenTK.Mathematics;
using PsycSerial;

namespace TeensyMonitor.Plotter.Helpers
{

    public struct MyColour(float R, float G, float B, float A)
    {
        public static readonly MyColour Unset = Color.Magenta;

        public float r = R, g = G, b = B, a = A;



        public MyColour(Color c) : this(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f) { }


        public static implicit operator MyColour(Color c) => new(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);


        public static List<MyColour> BaseColours = [
            Color.FromArgb(0x4E, 0x79, 0xA7), // Muted Blue
            Color.FromArgb(0xF2, 0x8E, 0x2B), // Orange
            Color.FromArgb(0xE1, 0x57, 0x59), // Red
            Color.FromArgb(0x76, 0xB7, 0xB2), // Teal
            Color.FromArgb(0x59, 0xA1, 0x4F), // Green
            Color.FromArgb(0xED, 0xC9, 0x48), // Yellow
            Color.FromArgb(0xB0, 0x7A, 0xA1), // Purple
            Color.FromArgb(0xFF, 0x9D, 0xA7), // Pink
            Color.FromArgb(0x9C, 0x75, 0x5F), // Brown
            Color.FromArgb(0xBA, 0xB0, 0xAC), // Grey
            Color.FromArgb(0x1F, 0x77, 0xB4), // Bright Blue
            Color.FromArgb(0xFF, 0x7F, 0x0E), // Bright Orange
            Color.FromArgb(0x2C, 0xA0, 0x2C), // Bright Green
            Color.FromArgb(0xD6, 0x27, 0x28), // Bright Red
            Color.FromArgb(0x94, 0x67, 0xBD), // Bright Purple
            Color.FromArgb(0x8C, 0x56, 0x4B), // Dark Brown
            Color.FromArgb(0xE3, 0x77, 0xC2), // Bright Pink
            Color.FromArgb(0x7F, 0x7F, 0x7F), // Medium Grey
            Color.FromArgb(0xBC, 0xBD, 0x22), // Olive Green
            Color.FromArgb(0x17, 0xBE, 0xCF)  // Cyan
        ];

        public static MyColour GetFieldColour(FieldEnum field)
        {
            return field switch
            {
                FieldEnum.C0             => BaseColours[0],
                FieldEnum.Gain           => BaseColours[1],
                FieldEnum.Offset1        => BaseColours[2],
                FieldEnum.Offset2        => BaseColours[3],
                FieldEnum.postGainSensor => BaseColours[4],
                FieldEnum.preGainSensor  => BaseColours[5],
                FieldEnum.Timestamp      => BaseColours[6],

                _                        => Color.Magenta
            };
        }

        private static int _colourIndex = 0;

        public static MyColour GetNextColour()
        {
            _colourIndex %= BaseColours.Count;
            return BaseColours[_colourIndex++];
        }

        public static void Reset()
        {
            _colourIndex = 0;
        }
    }
}
