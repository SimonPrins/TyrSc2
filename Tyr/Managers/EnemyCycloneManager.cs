using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Managers
{
    public class EnemyCycloneManager : Manager
    {
        private Dictionary<ulong, int> LastHitFrame = new Dictionary<ulong, int>();
        public void OnFrame(Bot bot)
        {
            foreach (Agent agent in bot.Units())
            {
                if (agent.PreviousUnit == null)
                    continue;
                float damageTaken = agent.PreviousUnit.Health + agent.PreviousUnit.Shield - agent.Unit.Health - agent.Unit.Shield;
                if (damageTaken < 17.5)
                    continue;
                bool cycloneClose = false;
                int enemiesClose = 0;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                        continue;
                    if (agent.DistanceSq(enemy) > 15.5 * 15.5)
                        continue;
                    enemiesClose++;
                    if (enemy.UnitType == UnitTypes.CYCLONE)
                        cycloneClose = true;
                }

                if (cycloneClose && enemiesClose < 8)
                    CollectionUtil.Set(LastHitFrame, agent.Unit.Tag, bot.Frame);
            }
        }

        public int GetLastHitFrame(ulong tag)
        {
            if (!LastHitFrame.ContainsKey(tag))
                return 0;
            return LastHitFrame[tag];
        }
    }
}
