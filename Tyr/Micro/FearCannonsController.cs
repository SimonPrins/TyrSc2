using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class FearCannonsController : CustomController
    {
        public float Range = 12;
        public bool AttackCannonsInMain = true;
        public bool OnlyWhenLowShields = false;
        private HashSet<ulong> LowShieldUnits = new HashSet<ulong>();

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            float dist;
            if (OnlyWhenLowShields)
            {
                if (LowShieldUnits.Contains(agent.Unit.Tag)
                    && agent.Unit.Shield >= agent.Unit.ShieldMax - 5)
                    LowShieldUnits.Remove(agent.Unit.Tag);
                if (!LowShieldUnits.Contains(agent.Unit.Tag)
                    && agent.Unit.Shield <= 2)
                    LowShieldUnits.Add(agent.Unit.Tag);

                if (!LowShieldUnits.Contains(agent.Unit.Tag))
                    return false;
            }

            Point2D retreatFrom = null;
            dist = Range * Range;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if ((enemy.UnitType != UnitTypes.PHOTON_CANNON
                    && enemy.UnitType != UnitTypes.SPINE_CRAWLER
                    && enemy.UnitType != UnitTypes.BUNKER
                    )
                    || enemy.BuildProgress < 1)
                    continue;

                if (AttackCannonsInMain && Bot.Main.MapAnalyzer.MainAndPocketArea[SC2Util.To2D(enemy.Pos)])
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    retreatFrom = SC2Util.To2D(enemy.Pos);
                    dist = newDist;
                }
            }
            if (retreatFrom != null && dist < Range * Range)
            {
                agent.Order(Abilities.MOVE, Bot.Main.MapAnalyzer.StartLocation);
                return true;
            }

            return false;
        }
    }
}
