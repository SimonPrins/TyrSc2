using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class KillScoutsTask : Task
    {
        public static KillScoutsTask Task = new KillScoutsTask();
        private Unit Target = null;

        public KillScoutsTask() : base(3)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (units.Count >= 1)
                return false;
            if (Target.IsFlying)
                return agent.CanAttackAir();
            else
                return agent.CanAttackGround();
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count == 0 && Target != null)
                result.Add(new UnitDescriptor() { Pos = SC2Util.To2D(Target.Pos), Count = 1, UnitTypes = UnitTypes.CombatUnitTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            GetTarget();
            return Target != null;
        }

        private void GetTarget()
        {
            if (Target != null)
            {
                bool stillThere = false;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.Tag == Target.Tag)
                    {
                        stillThere = true;
                        break;
                    }
                }
                if (!stillThere)
                    Target = null;
                else
                {
                    bool close = false;
                    foreach (Base b in Bot.Main.BaseManager.Bases)
                    {
                        if (b.Owner != Bot.Main.PlayerId)
                            continue;
                        if (SC2Util.DistanceSq(b.BaseLocation.Pos, Target.Pos) <= 20 * 20)
                        {
                            close = true;
                            break;
                        }
                    }
                    if (!close)
                        Target = null;
                    else return;
                }
            }
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.OVERLORD
                    && enemy.UnitType != UnitTypes.OVERSEER
                    && !UnitTypes.ChangelingTypes.Contains(enemy.UnitType)
                    && !UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;
                foreach (Base b in Bot.Main.BaseManager.Bases)
                {
                    if (b.Owner != Bot.Main.PlayerId)
                        continue;
                    if (SC2Util.DistanceSq(b.BaseLocation.Pos, enemy.Pos) <= 15 * 15)
                    {
                        Target = enemy;
                        return;
                    }
                }
            }


        }

        public override void OnFrame(Bot tyr)
        {
            if (units.Count == 0)
                return;

            if (Target == null)
            {
                Clear();
                return;
            }

            foreach (Agent agent in units)
                agent.Order(Abilities.ATTACK, Target.Tag);
        }
    }
}
