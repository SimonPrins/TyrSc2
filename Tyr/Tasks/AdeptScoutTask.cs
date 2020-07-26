using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class AdeptScoutTask : Task
    {
        public int UnitType = -1;
        public Dictionary<ulong, int> SpawnedFrame = new Dictionary<ulong, int>();

        public AdeptScoutTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.ADEPT_PHASE_SHIFT;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            if (bot.Frame % (22) == 0)
                foreach(Agent agent in bot.UnitManager.Agents.Values)
                {
                    if (agent.Unit.UnitType == UnitTypes.ADEPT
                        && SC2Util.DistanceSq(agent.Unit.Pos, bot.TargetManager.PotentialEnemyStartLocations[0]) <= 55 * 55)
                        agent.Order(2544, bot.TargetManager.PotentialEnemyStartLocations[0]);
                }

            foreach (Agent agent in units)
            {
                if (!SpawnedFrame.ContainsKey(agent.Unit.Tag))
                    SpawnedFrame.Add(agent.Unit.Tag, bot.Frame);

                if (bot.Frame - SpawnedFrame[agent.Unit.Tag] >= 154)
                    agent.Order(3659);
                else
                {
                    Unit enemyTarget = null;
                    foreach (Unit enemy in bot.Observation.Observation.RawData.Units)
                    {
                        if (enemy.Alliance != Alliance.Enemy)
                            continue;

                        if (!UnitTypes.ResourceCenters.Contains(enemy.UnitType))
                            continue;
                        if (SC2Util.DistanceSq(enemy.Pos, agent.Unit.Pos ) <= 9 * 9)
                        {
                            enemyTarget = enemy;
                            break;
                        }
                    }
                    if (enemyTarget != null)
                        agent.Order(Abilities.ATTACK, SC2Util.To2D(enemyTarget.Pos));
                    else
                        agent.Order(Abilities.ATTACK, bot.TargetManager.PotentialEnemyStartLocations[0]);
                }
            }
        }
    }
}
