using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;

namespace Tyr.Micro
{
    public class MineController : CustomController
    {
        public Dictionary<ulong, int> LastEnemyFrame = new Dictionary<ulong, int>();
        public Dictionary<ulong, int> LastCheckFrame = new Dictionary<ulong, int>();
        public int KeepMineBurrowedTime = 5;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.WIDOW_MINE
                && agent.Unit.UnitType != UnitTypes.WIDOW_MINE_BURROWED)
                return false;

            bool closeEnemy = false;
            if (agent.Unit.UnitType == UnitTypes.WIDOW_MINE_BURROWED
                && (!LastCheckFrame.ContainsKey(agent.Unit.Tag) || Tyr.Bot.Frame - LastCheckFrame[agent.Unit.Tag] > 22.4 * KeepMineBurrowedTime))
                LastEnemyFrame[agent.Unit.Tag] = Tyr.Bot.Frame;
            LastCheckFrame[agent.Unit.Tag] = Tyr.Bot.Frame;

            if (!LastEnemyFrame.ContainsKey(agent.Unit.Tag))
                LastEnemyFrame.Add(agent.Unit.Tag, 0);
            else if (Tyr.Bot.Frame - LastEnemyFrame[agent.Unit.Tag] <= 22.4 * KeepMineBurrowedTime)
                closeEnemy = true;


            foreach (Unit enemy in Tyr.Bot.Enemies())
            {
                if (UnitTypes.BuildingTypes.Contains(enemy.UnitType)
                    && enemy.UnitType != UnitTypes.BARRACKS)
                    continue;

                if (enemy.UnitType == UnitTypes.CREEP_TUMOR
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_QUEEN)
                    continue;

                if (enemy.UnitType == UnitTypes.ADEPT_PHASE_SHIFT
                    || enemy.UnitType == UnitTypes.KD8_CHARGE)
                    continue;

                int dist;
                if (UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    dist = 3;
                else
                    dist = agent.Unit.UnitType == UnitTypes.WIDOW_MINE_BURROWED ? 10 : 8;
                if (agent.DistanceSq(enemy) <= dist * dist)
                {
                    closeEnemy = true;
                    LastEnemyFrame[agent.Unit.Tag] = Tyr.Bot.Frame;
                    break;
                }
            }

            if (agent.Unit.UnitType == UnitTypes.WIDOW_MINE && closeEnemy)
                agent.Order(Abilities.WIDOW_MINE_BURROW);
            else if (agent.Unit.UnitType == UnitTypes.WIDOW_MINE_BURROWED && !closeEnemy)
                agent.Order(Abilities.WIDOW_MINE_UNBURROW);
            else if (agent.Unit.UnitType == UnitTypes.WIDOW_MINE)
                agent.Order(Abilities.MOVE, target);
            
            return true;
        }
    }
}
