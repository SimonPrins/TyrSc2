using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class TankController : CustomController
    {
        public Dictionary<ulong, int> LastEnemyFrame = new Dictionary<ulong, int>();
        public int KeepTankSiegedTime = 5;

        public bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.SIEGE_TANK
                && agent.Unit.UnitType != UnitTypes.SIEGE_TANK_SIEGED)
                return false;

            bool closeEnemy = false;
            if (!LastEnemyFrame.ContainsKey(agent.Unit.Tag))
                LastEnemyFrame.Add(agent.Unit.Tag, 0);
            else if (Tyr.Bot.Frame - LastEnemyFrame[agent.Unit.Tag] <= 22.4 * KeepTankSiegedTime)
                closeEnemy = true;


            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.IsFlying)
                    continue;

                if (enemy.UnitType == UnitTypes.CREEP_TUMOR
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_QUEEN)
                    continue;

                if (enemy.UnitType == UnitTypes.ADEPT_PHASE_SHIFT)
                    continue;

                if (!UnitTypes.RangedTypes.Contains(enemy.UnitType)
                    && enemy.UnitType != UnitTypes.SPINE_CRAWLER
                    && enemy.UnitType != UnitTypes.PHOTON_CANNON
                    && enemy.UnitType != UnitTypes.BUNKER)
                    continue;

                int dist = agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED ? 13 : 10;
                if (agent.DistanceSq(enemy) <= dist * dist)
                {
                    closeEnemy = true;
                    LastEnemyFrame[agent.Unit.Tag] = Tyr.Bot.Frame;
                    break;
                }
            }

            if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK && closeEnemy)
                agent.Order(Abilities.SIEGE);
            else if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED && !closeEnemy)
                agent.Order(Abilities.UNSIEGE);
            else if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK)
                return false;
            
            return true;
        }
    }
}
