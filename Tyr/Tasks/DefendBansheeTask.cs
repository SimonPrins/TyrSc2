using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
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
            return SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation)) <= 40 * 40;
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
                    Pos = SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation)
                });
            return descriptors;
        }

        public override void OnFrame(Bot tyr)
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
            if (AttackersUpdateFrame >= Bot.Bot.Frame)
                return;
            AttackersUpdateFrame = Bot.Bot.Frame;

            Unit enemy = null;

            foreach (Unit unit in Bot.Bot.CloakedEnemies())
            {
                if (unit.Tag == TargetTag)
                {
                    enemy = unit;
                    break;
                }
            }
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (unit.Tag == TargetTag)
                {
                    enemy = unit;
                    break;
                }
            }

            if (enemy != null)
            {
                dist = SC2Util.DistanceSq(enemy.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                if (dist >= 90 * 90)
                    enemy = null;
            }

            if (enemy != null)
            {
                bool nearBase = false;
                foreach (Base b in Bot.Bot.BaseManager.Bases)
                    if (b.Owner == Bot.Bot.PlayerId && SC2Util.DistanceSq(enemy.Pos, b.BaseLocation.Pos) <= 30 * 30)
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
            foreach (Unit unit in Bot.Bot.CloakedEnemies())
            {
                if (unit.UnitType != UnitTypes.BANSHEE)
                    continue;

                float newDist = SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                if (newDist >= dist)
                    continue;

                bool nearBase = newDist <= 30 * 30;
                if (!nearBase)
                {
                    foreach (Base b in Bot.Bot.BaseManager.Bases)
                        if (b.Owner == Bot.Bot.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= 20 * 20)
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
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (unit.UnitType != UnitTypes.BANSHEE)
                    continue;

                float newDist = SC2Util.DistanceSq(unit.Pos, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                if (newDist >= dist)
                    continue;

                bool nearBase = newDist <= 30 * 30;
                if (!nearBase)
                {
                    foreach (Base b in Bot.Bot.BaseManager.Bases)
                        if (b.Owner == Bot.Bot.PlayerId && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= 20 * 20)
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
