using PsycSerial.Packets;
using System.Text.Json.Serialization;

namespace TeensyMonitor.Caldera
{
    internal sealed class StateChangedMessage : IWebMessage
    {
        [JsonPropertyName("type" )]  public string Type { get; } = "stateChanged";

        [JsonPropertyName("state")]  public int State { get; set; }

        public StateChangedMessage()
        {
        }

        public StateChangedMessage(int state)
        {
            State = state;
        }

        public void CopyFrom(StateChangedMessage other)
        {
            State = other.State;
        }

        public override bool Equals(object? obj)
            => obj is StateChangedMessage other && State == other.State;

        public override int GetHashCode()
            => State;
    }
}
