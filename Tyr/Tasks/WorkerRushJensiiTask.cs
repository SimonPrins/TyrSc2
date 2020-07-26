using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class WorkerRushJensiiTask : WorkerRushTask
    {
        public new static WorkerRushJensiiTask Task = new WorkerRushJensiiTask();
        bool DestroyedEnemyMain = false;

        public WorkerRushJensiiTask() : base()
        {
            TakeWorkers = 10; 
        }

        public new static void Enable()
        {
            Enable(Task);
        }

        public override void OnFrame(Bot bot)
        {
            ulong mineral = 0;
            if (bot.BaseManager.Main.BaseLocation.MineralFields.Count > 0)
                mineral = bot.BaseManager.Main.BaseLocation.MineralFields[0].Tag;
            int enemyWorkers = 0;
            foreach (Unit enemy in bot.Enemies())
                if (UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    enemyWorkers++;
            bool overwhelmingMajority = enemyWorkers <= 5
                && Units.Count >= 10;
            int resourceCenterAttackers = Units.Count / 2;
            if (!Close)
            {
                foreach (Agent agent in units)
                {
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
                    if (agent.DistanceSq(bot.TargetManager.PotentialEnemyStartLocations[0]) <= 8 * 8)
                        Close = true;
                }
                return;
            }

            foreach (Agent agent in units)
            {
                if (!regenerating.Contains(agent.Unit.Tag) && agent.Unit.Shield <= 3 && agent.Unit.UnitType == UnitTypes.PROBE)
                    regenerating.Add(agent.Unit.Tag);
                else if (regenerating.Contains(agent.Unit.Tag) && agent.Unit.Shield == agent.Unit.ShieldMax)
                    regenerating.Remove(agent.Unit.Tag);

                if (regenerating.Contains(agent.Unit.Tag))
                {
                    bool flee = false;
                    foreach (Unit enemy in bot.Observation.Observation.RawData.Units)
                    {
                        if (enemy.Alliance != Alliance.Enemy)
                            continue;
                        if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType) && !UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                            continue;

                        if (SC2Util.DistanceSq(agent.Unit.Pos, enemy.Pos) <= 3 * 3)
                        {
                            flee = true;
                            break;
                        }
                    }

                    if (flee)
                    {
                        if (mineral == 0)
                            agent.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                        else
                            agent.Order(Abilities.MOVE, mineral);
                    }
                    else
                        agent.Order(Abilities.ATTACK, bot.TargetManager.AttackTarget);
                }
                else
                {
                    Unit broodling = GetBroodling(agent);
                    if (broodling != null || agent.Unit.WeaponCooldown > 6)
                    {
                        if (mineral == 0)
                            agent.Order(Abilities.MOVE, SC2Util.To2D(bot.MapAnalyzer.StartLocation));
                        else
                            agent.Order(Abilities.MOVE, mineral);
                        continue;
                    }

                    Unit killTarget = null;
                    float dist = 20 * 20;
                    foreach (Unit enemy in bot.Enemies())
                    {
                        if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                            continue;
                        if (SC2Util.DistanceSq(enemy.Pos, bot.TargetManager.PotentialEnemyStartLocations[0]) >= 20 * 20)
                            continue;
                        float newDist = agent.DistanceSq(enemy);
                        if (newDist > dist)
                            continue;
                        dist = newDist;
                        killTarget = enemy;
                    }
                    if (killTarget != null)
                    {
                        if (!overwhelmingMajority
                            || resourceCenterAttackers <= 0)
                        {
                            agent.Order(Abilities.ATTACK, killTarget.Tag);
                            continue;
                        }
                        else resourceCenterAttackers--;
                    }
                    foreach (Unit enemy in bot.Enemies())
                    {
                        if (!UnitTypes.ResourceCenters.Contains(enemy.UnitType))
                            continue;
                        if (enemy.IsFlying)
                            continue;
                        if (SC2Util.DistanceSq(enemy.Pos, bot.TargetManager.PotentialEnemyStartLocations[0]) >= 20 * 20)
                            continue;
                        float newDist = agent.DistanceSq(enemy);
                        if (newDist > dist)
                            continue;
                        dist = newDist;
                        killTarget = enemy;
                        if (enemy.Health < 100)
                            DestroyedEnemyMain = true;
                    }
                    if (killTarget != null)
                    {
                        agent.Order(Abilities.ATTACK, killTarget.Tag);
                        continue;
                    }

                    if (agent.DistanceSq(bot.TargetManager.AttackTarget) <= 6 * 6 || DestroyedEnemyMain)
                        agent.Order(Abilities.ATTACK, bot.TargetManager.AttackTarget);
                    else
                        agent.Order(Abilities.MOVE, bot.TargetManager.AttackTarget);
                }
            }
        }

        private Unit GetBroodling(Agent agent)
        {
            Unit broodling = null;
            float dist = 6 * 6;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BROODLING)
                    continue;

                float newDist = agent.DistanceSq(enemy);
                if (newDist < dist)
                {
                    dist = newDist;
                    broodling = enemy;
                }
            }
            return broodling;
        }
    }
}
