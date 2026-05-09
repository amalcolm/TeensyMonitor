using PsycSerial;
using System.Text.Json.Serialization;

namespace TeensyMonitor.Caldera
{
    public interface IWebMesage
    {
        [JsonPropertyName("type")] public string Type { get; init; }
    }

    public sealed class VoltagesChangedMessage : IWebMesage
    {
        [JsonPropertyName("type")]      public string          Type     { get; init; } = "voltagesChanged";
        [JsonPropertyName("voltages")]  public VoltageValues   Voltages { get; init; } = new();
        
        public void CopyFrom(BlockPacket block) => Voltages.CopyFrom(block);
        
        public void CopyFrom(VoltagesChangedMessage other) => Voltages.CopyFrom(other.Voltages);
        
        [JsonIgnore] public bool IsValid { get => Voltages.IsValid; }
        
        public override bool Equals(object? obj)
        {
            if (obj is not VoltagesChangedMessage other) return false;
            return Type    .Equals(other.Type    ) &&
                   Voltages.Equals(other.Voltages);
        }
        override public int GetHashCode() => HashCode.Combine(Type, Voltages);
    }

    public sealed class WipersChangedMessage : IWebMesage
    {
        [JsonPropertyName("type")]      public string        Type     { get; init; } = "wipersChanged";
        [JsonPropertyName("wipers")]    public WiperValues   Wipers   { get; init; } = new();

        public void CopyFrom(BlockPacket block) =>  Wipers.CopyFrom(block);
        public void CopyFrom(WipersChangedMessage other) => Wipers.CopyFrom(other.Wipers);
        

        [JsonIgnore] public bool IsValid { get => Wipers.IsValid; }

        public override bool Equals(object? obj)
        {
            if (obj is not WipersChangedMessage other) return false;
            return Type    .Equals(other.Type    ) &&
                   Wipers  .Equals(other.Wipers  );
        }

        override public int GetHashCode() => HashCode.Combine(Type, Wipers);
    }

    public sealed class SetWipersMessage : IWebMesage
    {
        [JsonPropertyName("type")]      public string      Type   { get; init; } = "setWipers";
        [JsonPropertyName("wipers")]    public WiperValues Wipers { get; init; } = new();
    }

    public sealed class VoltageValues
    {
        [JsonPropertyName("sensor1")]   public float Sensor1    { get; set; }
        [JsonPropertyName("sensor2")]   public float Sensor2    { get; set; }

        private const float Scalar = 3.3f / 1023.0f;  // Assuming 10-bit ADC and 3.3V reference voltage

        public void CopyFrom(BlockPacket block)
        {
            if (block == null || block.Count <= 0) return;
    
            ref DataPacket data = ref block.BlockData[block.Count - 1];
    
            Sensor1 = data.Stage1_Sensor * Scalar;
            Sensor2 = data.Stage2_Sensor * Scalar;
        }
        public void CopyFrom(VoltageValues other)
        {
            Sensor1 = other.Sensor1;
            Sensor2 = other.Sensor2;
        }

        [JsonIgnore] public bool IsValid => Sensor1 != 0 || Sensor2 != 0;

        public override bool Equals(object? obj)
        {
            if (obj is not VoltageValues other) return false;
            return Sensor1 == other.Sensor1 &&
                    Sensor2 == other.Sensor2;
        }
        override public int GetHashCode() => HashCode.Combine(Sensor1, Sensor2);
    }

    public sealed class WiperValues
    {
        [JsonPropertyName("top")]       public int Top    { get; set; }
        [JsonPropertyName("bot")]       public int Bot    { get; set; }
        [JsonPropertyName("mid")]       public int Mid    { get; set; }
        [JsonPropertyName("offset")]    public int Offset { get; set; }
        [JsonPropertyName("gain")]      public int Gain   { get; set; }

        public void CopyFrom(BlockPacket block)
        {
            if (block == null || block.Count <= 0) return;

            ref DataPacket data = ref block.BlockData[block.Count - 1];

            Top    = data.Stage1_Top;
            Bot    = data.Stage1_Bot;
            Mid    = data.Stage1_Mid;
            Offset = data.Stage2_Offset;
            Gain   = data.Stage2_Gain;
        }

        public void CopyFrom(WiperValues other)
        {
            Top    = other.Top;
            Bot    = other.Bot;
            Mid    = other.Mid;
            Offset = other.Offset;
            Gain   = other.Gain;
        }

        [JsonIgnore] public bool IsValid { get => Top != 0 || Bot != 0 || Mid != 0 || Offset != 0 || Gain != 0; }

        public override bool Equals(object? obj)
        {
            if (obj is not WiperValues other) return false;

            return Top    == other.Top    &&
                   Bot    == other.Bot    &&
                   Mid    == other.Mid    &&
                   Offset == other.Offset &&
                   Gain   == other.Gain;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Top, Bot, Mid, Offset, Gain);
        }
    }
}
