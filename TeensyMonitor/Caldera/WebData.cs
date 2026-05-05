using System.Text.Json.Serialization;

namespace TeensyMonitor.Caldera
{

    public sealed class WebMessage
    {
        [JsonPropertyName("type")]      public string Type { get; set; } = "";
        [JsonPropertyName("value")]     public SettingsChangeValue Value { get; set; } = new();
    }

    public sealed class SettingsChangeValue
    {
        [JsonPropertyName("photodiodeVoltage")] public double PhotodiodeVoltage { get; set; }
        [JsonPropertyName("wipers")]    public WiperValues Wipers { get; set; } = new();
    }

    public sealed class WiperValues
    {
        [JsonPropertyName("top")]       public int Top { get; set; }
        [JsonPropertyName("bot")]       public int Bot { get; set; }
        [JsonPropertyName("mid")]       public int Mid { get; set; }
        [JsonPropertyName("offset")]    public int Offset { get; set; }
        [JsonPropertyName("feedback")]  public int Feedback { get; set; }
    }
}
