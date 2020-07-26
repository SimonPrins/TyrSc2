using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;

namespace SC2Sharp.Micro
{
    public class LurkerController : CustomController
    {
        public Dictionary<ulong, int> LastEnemyFrame = new Dictionary<ulong, int>();
        public int KeepLurkerBurrowedTime = 5;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.LURKER
                && agent.Unit.UnitType != UnitTypes.LURKER_BURROWED)
                return false;

            bool closeEnemy = false;
            if (!LastEnemyFrame.ContainsKey(agent.Unit.Tag))
                LastEnemyFrame.Add(agent.Unit.Tag, 0);
            else if (Bot.Main.Frame - LastEnemyFrame[agent.Unit.Tag] <= 22.4 * KeepLurkerBurrowedTime)
                closeEnemy = true;


            foreach (Unit enemy in Bot.Main.Enemies())
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
                    LastEnemyFrame[agent.Unit.Tag] = Bot.Main.Frame;
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
