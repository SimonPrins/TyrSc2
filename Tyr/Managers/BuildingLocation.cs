using SC2APIProtocol;

namespace SC2Sharp.Managers
{
    public class BuildingLocation
    {
        public ulong Tag;
        public Point Pos;
        public uint Type;
        public int LastSeen;
        public bool Flying;
    }
}