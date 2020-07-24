using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;

namespace Tyr.Micro
{
    public class OracleController : CustomController
    {
        private Dictionary<ulong, int> RetreatFrame = new Dictionary<ulong, int>();
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.ORACLE)
                return false;

            if (EvadeMines(agent, target))
                return true;
            if (EvadeEnemies(agent, target))
                return true;

            if (Bot.Main.Frame % 22 == 0)
            {
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.IsFlying)
                        continue;
                    if (agent.DistanceSq(enemy) <= 10 * 10)
                    {
                        agent.Order(2375);
                        return true;
                    }
                }
            }
            
            agent.Order(Abilities.ATTACK, target);
            return true;
        }

        public bool EvadeMines(Agent agent, Point2D target)
        {
            Point mineLocation = null;
            float dist = 9 * 9;
            foreach (UnitLocation mine in Bot.Main.EnemyMineManager.Mines)
            {
                if (Bot.Main.EnemyMineManager.BurrowFrame.ContainsKey(mine.Tag) && Bot.Main.Frame - Bot.Main.EnemyMineManager.BurrowFrame[mine.Tag] <= 16)
                    continue;

                float newDist = agent.DistanceSq(mine.Pos);
                if (newDist < dist)
                {
                    dist = newDist;
                    mineLocation = mine.Pos;
                }
            }

            if (mineLocation != null)
            {
                bool alreadyRetreating = RetreatFrame.ContainsKey(agent.Unit.Tag) && Bot.Main.Frame - RetreatFrame[agent.Unit.Tag] <= 5;

                if (dist <= 7 * 7 || alreadyRetreating)
                {

                    if (RetreatFrame.ContainsKey(agent.Unit.Tag))
                        RetreatFrame[agent.Unit.Tag] = Bot.Main.Frame;
                    else
                        RetreatFrame.Add(agent.Unit.Tag, Bot.Main.Frame);
                    agent.Order(Abilities.MOVE, agent.From(mineLocation, 4));
                    return true;
                }

                PotentialHelper helper = new PotentialHelper(agent.Unit.Pos, 4);
                helper.From(mineLocation, 1);
                helper.To(target, 1);

                agent.Order(Abilities.MOVE, helper.Get());
                return true;
            }
            return false;
        }

        public bool EvadeEnemies(Agent agent, Point2D target)
        {
            Point enemyLocation = null;
            float dist = 9 * 9;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BUNKER)
                    continue;

                if (enemy.BuildProgress < 0.9)
                    continue;

                float newDist = agent.DistanceSq(enemy.Pos);
                if (newDist < dist)
                {
                    dist = newDist;
                    enemyLocation = enemy.Pos;
                }
            }

            if (enemyLocation != null)
            {
                bool alreadyRetreating = RetreatFrame.ContainsKey(agent.Unit.Tag) && Bot.Main.Frame - RetreatFrame[agent.Unit.Tag] <= 5;

                if (dist <= 7 * 7 || alreadyRetreating)
                {

                    if (RetreatFrame.ContainsKey(agent.Unit.Tag))
                        RetreatFrame[agent.Unit.Tag] = Bot.Main.Frame;
                    else
                        RetreatFrame.Add(agent.Unit.Tag, Bot.Main.Frame);
                    agent.Order(Abilities.MOVE, agent.From(enemyLocation, 4));
                    return true;
                }

                PotentialHelper helper = new PotentialHelper(agent.Unit.Pos, 4);
                helper.From(enemyLocation, 1);
                helper.To(target, 1);

                agent.Order(Abilities.MOVE, helper.Get());
                return true;
            }
            return false;
        }
    }
}
