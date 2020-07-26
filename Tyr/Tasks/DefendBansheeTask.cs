using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class DefendBansheeTask : Task
    {
        public static DefendBansheeTask Task = new DefendBansheeTask();
        ulong TargetTag = 0;
        Unit TargetEnemy = null;
        private int AttackersUpdateFrame = 0;

        public DefendBansheeTask() : base(8)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (agent.Unit.UnitType != UnitTypes.STALKER)
                return false;
            return SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation)) <= 40 * 40;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> descriptors = new List<UnitDescriptor>();
            int requiredCount = 2 - Units.Count;
            if (requiredCount > 0)
                descriptors.Add(new UnitDescriptor(UnitTypes.STALKER)
                {
                    Count = requiredCount,
                    Pos = SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation)
                });
            return descriptors;
        }

        public override void OnFrame(Bot bot)
        {
            UpdateAttackers();

            foreach (Agent agent in units)
            {
                if (TargetEnemy != null)
                {
                    if (agent.DistanceSq(TargetEnemy) <= 6 * 6
                        && TargetEnemy.Cloak != CloakState.Cloaked)
                        agent.Order(Abilities.ATTACK, TargetEnemy.Tag);
                    else
                        agent.Order(Abilities.ATTACK, TargetEnemy.Pos);
                }
                else  if (IdleTask.Task.Target != null && agent.DistanceSq(IdleTask.Task.Target) >= 2 * 2)
                    agent.Order(Abilities.MOVE, IdleTask.Task.Target);
            }
        }

        private int GetDefenders(Dictionary<ulong, int> assignedDefenders, ulong enemyTag)
        {
            if (assignedDefenders.ContainsKey(enemyTag))
                return assignedDefenders[enemyTag];
            return 0;
        }

        private void AddDefender(Dictionary<ulong, int> assignedDefenders, ulong enemyTag)
        {
            if (!assignedDefenders.ContainsKey(enemyTag))
                assignedDefenders.Add(enemyTag, 1);
            else
                assignedDefenders[enemyTag]++;
        }

        private void UpdateAttackers()
        {
            float dist;
            if (AttackersUpdateFrame >= Bot.Main.Frame)
                return;
            AttackersUpdateFrame = Bot.Main.Frame;

            Unit enemy = null;

            foreach (Unit unit in Bot.Main.CloakedEnemies())
            {
                if (unit.Tag == TargetTag)
                {
                    enemy = unit;
                    break;
                }
            }
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (unit.Tag == TargetTag)
                {
                    enemy = unit;
                    break;
                }
            }

            if (enemy != null)
            {
                dist = SC2Util.DistanceSq(enemy.Pos, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
                if (dist >= 90 * 90)
                    enemy = null;
            }

            if (enemy != null)
            {
                bool nearBase = false;
                foreach (Base b in Bot.Main.BaseManager.Bases)
                    if (b.Owner == Bot.Main.PlayerId && SC2Util.DistanceSq(enemy.Pos, b.BaseLocation.Pos) <= 30 * 30)
                    {
                        nearBase = true;
                        break;
                    }
                if (!nearBase)
                    enemy = null;
            }
            TargetEnemy = enemy;
            if (TargetEnemy != null)
                return;

            TargetTag = 0;

            dist = 80 * 80;
            foreach (Unit unit in Bot.Main.CloakedEnemies())
            {
                if (unit.UnitType != UnitTypes.BANSHEE)
                    continue;

                float newDist = SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
                if (newDist >= dist)
                    continue;

                bool nearBase = newDist <= 30 * 30;
                if (!nearBase)
                {
                    foreach (Base b in Bot.Main.BaseManager.Bases)
                        if (b.Owner == Bot.Main.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= 20 * 20)
                        {
                            nearBase = true;
                            break;
                        }
                }
                if (nearBase)
                {
                    TargetTag = unit.Tag;
                    TargetEnemy = unit;
                    dist = newDist;
                }
            }
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (unit.UnitType != UnitTypes.BANSHEE)
                    continue;

                float newDist = SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
                if (newDist >= dist)
                    continue;

                bool nearBase = newDist <= 30 * 30;
                if (!nearBase)
                {
                    foreach (Base b in Bot.Main.BaseManager.Bases)
                        if (b.Owner == Bot.Main.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= 20 * 20)
                        {
                            nearBase = true;
                            break;
                        }
                }
                if (nearBase)
                {
                    TargetTag = unit.Tag;
                    TargetEnemy = unit;
                    dist = newDist;
                }
            }
        }
    }
}
