using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class StayByCannonsController : CustomController
    {
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            Unit fleeEnemy = null;
            float enemyDist = 14 * 14;
            foreach (Unit unit in Bot.Main.Enemies())
            {

                if (unit.UnitType == UnitTypes.OVERLORD
                    || unit.UnitType == UnitTypes.OVERSEER
                    || unit.UnitType == UnitTypes.LARVA
                    || unit.UnitType == UnitTypes.EGG
                    || unit.UnitType == UnitTypes.CREEP_TUMOR
                    || unit.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || unit.UnitType == UnitTypes.CREEP_TUMOR_QUEEN
                    || unit.UnitType == UnitTypes.OBSERVER)
                    continue;

                if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                float newEnemyDist = agent.DistanceSq(unit);
                if (newEnemyDist >= enemyDist)
                    continue;

                float dist = 14 * 14;
                bool cannonClose = false;
                foreach (Agent cannon in Bot.Main.Units())
                {
                    if (cannon.Unit.UnitType != UnitTypes.PHOTON_CANNON)
                        continue;
                    if (cannon.Unit.BuildProgress < 0.99)
                        continue;
                    float newDist = cannon.DistanceSq(unit);
                    if (newDist >= dist)
                        continue;
                    dist = newDist;
                    cannonClose = true;
                    if (dist <= 8 * 8)
                    {
                        Bot.Main.DrawSphere(agent.Unit.Pos);
                        return false;
                    }
                }

                if (!cannonClose)
                    continue;
                fleeEnemy = unit;
                enemyDist = newEnemyDist;

            }
            if (fleeEnemy != null && enemyDist <= 12 * 12)
            {
                agent.Flee(fleeEnemy.Pos);
                return true;
            }
            return false;
        }
    }
}
