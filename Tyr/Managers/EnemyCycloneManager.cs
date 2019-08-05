using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Managers
{
    public class EnemyCycloneManager : Manager
    {
        private Dictionary<ulong, int> LastHitFrame = new Dictionary<ulong, int>();
        public void OnFrame(Tyr tyr)
        {
            foreach (Agent agent in tyr.Units())
            {
                if (agent.PreviousUnit == null)
                    continue;
                float damageTaken = agent.PreviousUnit.Health + agent.PreviousUnit.Shield - agent.Unit.Health - agent.Unit.Shield;
                if (damageTaken < 17.5)
                    continue;
                bool cycloneClose = false;
                int enemiesClose = 0;
                foreach (Unit enemy in Tyr.Bot.Enemies())
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
                    CollectionUtil.Set(LastHitFrame, agent.Unit.Tag, tyr.Frame);
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
