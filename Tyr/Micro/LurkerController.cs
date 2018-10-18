using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class LurkerController : CustomController
    {
        public Dictionary<ulong, int> LastEnemyFrame = new Dictionary<ulong, int>();
        public int KeepLurkerBurrowedTime = 5;

        public bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.LURKER
                && agent.Unit.UnitType != UnitTypes.LURKER_BURROWED)
                return false;

            bool closeEnemy = false;
            if (!LastEnemyFrame.ContainsKey(agent.Unit.Tag))
                LastEnemyFrame.Add(agent.Unit.Tag, 0);
            else if (Tyr.Bot.Frame - LastEnemyFrame[agent.Unit.Tag] <= 22.4 * KeepLurkerBurrowedTime)
                closeEnemy = true;


            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (enemy.IsFlying)
                    continue;

                if (enemy.UnitType == UnitTypes.CREEP_TUMOR
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_QUEEN)
                    continue;
                
                if (agent.DistanceSq(enemy) <= 8 * 8)
                {
                    closeEnemy = true;
                    LastEnemyFrame[agent.Unit.Tag] = Tyr.Bot.Frame;
                    break;
                }
            }

            if (agent.Unit.UnitType == UnitTypes.LURKER && closeEnemy)
                agent.Order(Abilities.BURROW_DOWN);
            else if (agent.Unit.UnitType == UnitTypes.LURKER_BURROWED && !closeEnemy)
                agent.Order(Abilities.BURROW_UP);
            else if (agent.Unit.UnitType == UnitTypes.LURKER)
                agent.Order(Abilities.MOVE, target);
            
            return true;
        }
    }
}
