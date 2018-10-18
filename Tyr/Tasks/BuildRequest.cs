using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Tasks
{
    public class BuildRequest
    {
        public uint Type;
        public Point2D Pos;
        public Agent worker;
        public Base Base;
        public int LastImprovementFrame;
        public float Closest;
    }
}