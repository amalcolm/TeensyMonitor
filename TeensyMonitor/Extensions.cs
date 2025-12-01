using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TeensyMonitor
{
    internal static class Extensions
    {
        public static void Invoker(this Control control, MethodInvoker action)
        {
            control.Invoke( action );
        }

        public static string Description(this uint state)
        {
            switch (state)
            {
                case 0b00000000_00000000_00000000_00000001: return "RED1";
                case 0b00000000_00000000_00000000_00000010: return "RED2";
                case 0b00000000_00000000_00000000_00000100: return "RED3";
                case 0b00000000_00000000_00000000_00001000: return "RED4";
                case 0b00000000_00000000_00000000_00010000: return "RED5";
                case 0b00000000_00000000_00000000_00100000: return "RED6";
                case 0b00000000_00000000_00000000_01000000: return "RED7";
                case 0b00000000_00000000_00000000_10000000: return "RED8";
                case 0b00000000_00000000_00000001_00000000: return "RED9";

                case 0b00000000_00000001_00000000_00000000: return "IR1";
                case 0b00000000_00000010_00000000_00000000: return "IR2";
                case 0b00000000_00000100_00000000_00000000: return "IR3";
                case 0b00000000_00001000_00000000_00000000: return "IR4";
                case 0b00000000_00010000_00000000_00000000: return "IR5";
                case 0b00000000_00100000_00000000_00000000: return "IR6";
                case 0b00000000_01000000_00000000_00000000: return "IR7";
                case 0b00000000_10000000_00000000_00000000: return "IR8";
                case 0b00000001_00000000_00000000_00000000: return "IR9";
            }

            state &= 0b01111111_11111111_11111111_11111111; // Mask off error bit, if set

            var sb = new StringBuilder();

            void AppendSection(int offset, string prefix)
            {
                // Get the specific bits, shifted down to 0-based
                uint mask = (state >> offset) & 0xFFF;
                if (mask == 0) return;

                if (sb.Length > 0) sb.Append(": ");
                sb.Append(prefix);

                bool notFirst = false;
                while (mask != 0)
                {
                    if (notFirst) sb.Append('+');

                    // Find index of the least significant bit
                    int i = BitOperations.TrailingZeroCount(mask);
                    sb.Append(i + 1);

                    // Clear the least significant bit (Kernighan's algorithm style)
                    // or effectively: mask &= ~(1u << i);
                    mask ^= (1u << i);
                    notFirst = true;
                }
            }

            AppendSection( 0, "RED" );
            AppendSection(16, "IR");

            return sb.ToString();
        }
    }
}
