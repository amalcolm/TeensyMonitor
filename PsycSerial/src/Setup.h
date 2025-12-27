#pragma once

using namespace System;

namespace PsycSerial
{

    public ref class Setup
    {
    public:
        static int     LoopMS         = 20;
        static String^ ProgramVersion = "v1.12";
        static String^ DeviceVersion  = String::Empty;


        static void ParseHandshakeResponse(System::String^ response);
    };

}