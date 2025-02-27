using LiteEntitySystem.Collections;
using LiteEntitySystem.Internal;
using LiteEntitySystem.Transport;

namespace LiteEntitySystem
{
    public enum NetPlayerState
    {
        Active, // Player is fully connected and actively participating
        WaitingForFirstInput, // Player received baseline but hasn't sent any input yet
        WaitingForFirstInputProcess, // Player sent first input, waiting for it to be processed
        RequestBaseline // Player needs initial game state data
    }

    internal struct InputInfo
    {
        public ushort            Tick;
        public InputPacketHeader Header;

        public InputInfo(ushort tick, InputPacketHeader header)
        {
            Tick = tick;
            Header = header;
        }
    }

    public class NetPlayer
    {
        // Unique byte identifier
        public readonly byte Id;

        // Network connection
        public readonly AbstractNetPeer Peer;

        internal ushort LastProcessedTick; // The most recent game tick where this player's input was processed
        internal ushort LastReceivedTick; // The most recent game tick received from this player's client
        internal ushort CurrentServerTick; // The most recent server tick that this client has acknowledged

        // Used for client-side interpolation between two states
        internal ushort StateATick;
        internal ushort StateBTick;
        internal float  LerpTime;

        //server only
        internal NetPlayerState                State; // Current state of the player
        internal SequenceBinaryHeap<InputInfo> AvailableInput; // A priority queue (SequenceBinaryHeap) storing recent player inputs, limited by MaxStoredInputs (30)

        internal NetPlayer(AbstractNetPeer peer, byte id)
        {
            Id = id;
            Peer = peer;
        }
    }
}