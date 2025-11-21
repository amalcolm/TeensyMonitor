namespace TeensyMonitor.Helpers
{

    public class BaseFrame
    {
        protected double ambient = 0;
        protected double red = 0;
        protected double ir = 0;

        protected BaseFrame() { }

        public BaseFrame(bool toWrite) : this()
        {
            ToWrite = toWrite;
        }

        public virtual bool ProcessFrame() => IsComplete;

        public bool IsComplete { get; set; } = false;
        public bool IsMalformed { get; set; } = false;

        public bool ToWrite { get; set; } = false;

        protected readonly List<byte> bytes = [];
        protected ushort crc = 0;

        protected int dateIndex = 0;
        protected int dataLen = 0;
        protected byte[] data = [];
        
        public byte Add(byte b)
        {
            bytes.Add(b);
            UpdateCRC(b);
            return b;
        }

        protected byte ReadByte()
        {
            if (dateIndex >= dataLen) { IsMalformed = true; return 0; }
            return data[dateIndex++];
        }
        protected ushort ReadWord()
        {
            if (dateIndex + 1 >= dataLen) { IsMalformed = true; return 0; }
            return (ushort)(data[dateIndex++] << 8 | data[dateIndex++]);
        }
        protected uint ReadDWord()
        {
            if (dateIndex + 3 >= dataLen) { IsMalformed = true; return 0; }
            return (uint)(data[dateIndex++] << 24 | data[dateIndex++] << 16 | data[dateIndex++] << 8 | data[dateIndex++]);
        }

        protected int ReadInt()
        {
            if (dateIndex + 3 >= dataLen) { IsMalformed = true; return 0; }
            return data[dateIndex++] << 24 | data[dateIndex++] << 16 | data[dateIndex++] << 8 | data[dateIndex++];
        }

        protected string ReadString()
        {
            if (dateIndex >= dataLen) { IsMalformed = true; return ""; }
            int len = dataLen - dateIndex;
            string str = System.Text.Encoding.UTF8.GetString(data, dateIndex, len);
            dateIndex += str.Length;
            return str;
        }

        protected int ReadInt24()
        {
            if (dateIndex + 2 >= dataLen) { IsMalformed = true; return 0; }
            int i = data[dateIndex++] << 16 | data[dateIndex++] << 8 | data[dateIndex++];
            if ((i & 0x800000) != 0) i -= 0x1000000;
            return i;
        }

        protected void UpdateCRC(byte b)
        {
            crc = (ushort)(crc << 8 ^ CRCTable[crc >> 8 ^ b]);
        }

        protected byte[] ToArrayCRC()
        {
            byte[] frame = new byte[bytes.Count + 2];
            Array.Copy(bytes.ToArray(), frame, bytes.Count);

            frame[bytes.Count] = (byte)(crc >> 8);
            frame[bytes.Count + 1] = (byte)(crc & 0xFF);

            return frame;
        }

        protected static readonly ushort[] CRCTable;

        public const byte DLE = 0x10;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;


        static BaseFrame()
        {
            const ushort GENERATE_POLYNOMIAL = 0x1021;

            CRCTable = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                ushort value = 0;
                ushort temp = (ushort)(i << 8);
                for (int j = 0; j < 8; j++)
                {
                    if (((value ^ temp) & 0x8000) != 0)
                        value = (ushort)(value << 1 ^ GENERATE_POLYNOMIAL);
                    else
                        value <<= 1;
                    temp <<= 1;
                }
                CRCTable[i] = value;
            }
        }

    }
}
