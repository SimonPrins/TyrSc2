using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class LeashController : CustomController
    {
        private HashSet<uint> LeashedFrom = new HashSet<uint>();
        private HashSet<uint> LeashedTo = new HashSet<uint>();
        private readonly float Range;

        public LeashController(uint from, uint to, float range)
        {
            LeashedFrom.Add(from);
            LeashedTo.Add(to);
            Range = range;
        }

        public LeashController(uint from, HashSet<uint> to, float range)
        {
            LeashedFrom.Add(from);
            LeashedTo = to;
            Range = range;
        }

        public LeashController(HashSet<uint> from, uint to, float range)
        {
            LeashedFrom = from;
            LeashedTo.Add(to);
            Range = range;
        }

        public LeashController(HashSet<uint> from, HashSet<uint> to, float range)
        {
            LeashedFrom = from;
            LeashedTo = to;
            Range = range;
        }

        public bool DetermineAction(Agent agent, Point2D target)
        {
            if (!LeashedFrom.Contains(agent.Unit.UnitType))
                return false;

            if (agent.DistanceSq(Tyr.Bot.MapAnalyzer.StartLocation) < 40 * 40)
                return false;
            float dist;

            Point2D retreatTo = null;
            dist = 15 * 15;
            foreach (Agent ally in Tyr.Bot.UnitManager.Agents.Values)
            {
                if (!LeashedTo.Contains(ally.Unit.UnitType))
                    continue;

                float newDist = agent.DistanceSq(ally);
                if (newDist < dist)
                {
                    retreatTo = SC2Util.To2D(ally.Unit.Pos);
                    dist = newDist;
                }
            }
            if (retreatTo != null && dist >= Range * Range)
            {
                agent.Order(Abilities.MOVE, retreatTo);
                return true;
            }

            return false;
        }
    }
}
