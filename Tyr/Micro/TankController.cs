using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class TankController : CustomController
    {
        public Dictionary<ulong, int> LastEnemyFrame = new Dictionary<ulong, int>();
        public Dictionary<ulong, int> LastCheckFrame = new Dictionary<ulong, int>();
        public int KeepTankSiegedTime = 5;

        public bool SiegeAgainstMelee = false;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.SIEGE_TANK
                && agent.Unit.UnitType != UnitTypes.SIEGE_TANK_SIEGED)
                return false;

            bool closeEnemy = false;
            if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED
                && (!LastCheckFrame.ContainsKey(agent.Unit.Tag) || Bot.Bot.Frame - LastCheckFrame[agent.Unit.Tag] > 22.4 * KeepTankSiegedTime))
                LastEnemyFrame[agent.Unit.Tag] = Bot.Bot.Frame;
            LastCheckFrame[agent.Unit.Tag] = Bot.Bot.Frame;

            if (!LastEnemyFrame.ContainsKey(agent.Unit.Tag))
                LastEnemyFrame.Add(agent.Unit.Tag, 0);
            else if (Bot.Bot.Frame - LastEnemyFrame[agent.Unit.Tag] <= 22.4 * KeepTankSiegedTime)
                closeEnemy = true;


            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.IsFlying)
                    continue;

                if (enemy.UnitType == UnitTypes.CREEP_TUMOR
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_QUEEN)
                    continue;

                if (enemy.UnitType == UnitTypes.ADEPT_PHASE_SHIFT
                    || enemy.UnitType == UnitTypes.KD8_CHARGE)
                    continue;

                if ( !SiegeAgainstMelee
                    && !UnitTypes.RangedTypes.Contains(enemy.UnitType)
                    && enemy.UnitType != UnitTypes.SPINE_CRAWLER
                    && enemy.UnitType != UnitTypes.PHOTON_CANNON
                    && enemy.UnitType != UnitTypes.BUNKER)
                    continue;

                int dist = agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED ? 13 : 10;
                if (agent.DistanceSq(enemy) <= dist * dist)
                {
                    closeEnemy = true;
                    LastEnemyFrame[agent.Unit.Tag] = Bot.Bot.Frame;
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
