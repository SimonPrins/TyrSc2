using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class SoftLeashController : CustomController
    {
        private HashSet<uint> LeashedFrom = new HashSet<uint>();
        private HashSet<uint> LeashedTo = new HashSet<uint>();
        private readonly float Range;
        public float MaxRange = 1000;
        public float MinEnemyRange = 0;

        public SoftLeashController(uint from, uint to, float range)
        {
            LeashedFrom.Add(from);
            LeashedTo.Add(to);
            Range = range;
        }

        public SoftLeashController(uint from, HashSet<uint> to, float range)
        {
            LeashedFrom.Add(from);
            LeashedTo = to;
            Range = range;
        }

        public SoftLeashController(HashSet<uint> from, uint to, float range)
        {
            LeashedFrom = from;
            LeashedTo.Add(to);
            Range = range;
        }

        public SoftLeashController(HashSet<uint> from, HashSet<uint> to, float range)
        {
            LeashedFrom = from;
            LeashedTo = to;
            Range = range;
        }

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (!LeashedFrom.Contains(agent.Unit.UnitType))
                return false;

            if (MinEnemyRange > 0
                && agent.DistanceSq(Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]) <= MinEnemyRange * MinEnemyRange)
                return false;

            float dist;

            Point2D retreatTo = null;
            dist = MaxRange * MaxRange;
            foreach (Agent ally in Bot.Bot.UnitManager.Agents.Values)
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
                if (agent.Unit.WeaponCooldown == 0
                    || (agent.Unit.UnitType == UnitTypes.ZEALOT))
                    agent.Order(Abilities.ATTACK, retreatTo);
                else
                    agent.Order(Abilities.MOVE, retreatTo);
                return true;
            }

            return false;
        }
    }
}
