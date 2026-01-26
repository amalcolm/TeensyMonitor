#pragma once

using namespace System;

namespace PsycSerial
{

    public ref class Config
    {
    public:
        static UInt32 STATE_DURATION_uS     =  3'050;  // 20ms for each atate, mean's loop will be slightly longer than this

        static UInt32 HEAD_SETTLE_TIME_uS   =    440;  // delay between Head change and first A2D read

        static UInt32 POT_UPDATE_PERIOD_uS  =  1'111;  // Potentiometer update rate (900 Hz == A2D_READING_SPEED_Hz)
        static UInt32 POT_UPDATE_OFFSET_uS  =    667;  // A2D read -> Potentiometer update offset, minimizes interference

        static UInt32 A2D_SAMPLING_SPEED_Hz =  2'000;  // A2D sampling speed 
        static UInt32 A2D_READING_SPEED_Hz  =    900;  // A2D reading speed when in triggered mode

        static UInt32 MAX_BLOCKSIZE         =    164;  // max number of DataType entries in a BlockType

        static String^ ProgramVersion = "v0.2.3";
        static String^ DeviceVersion  = String::Empty;

        static double ChannelScale = 1.0 / 466010000.0;
        static double C0to1024     = 1.0 / 5000.0;

        static void ParseHandshakeResponse(System::String^ response);
    };

}