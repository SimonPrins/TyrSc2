using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    public class DefendRegionTask : Task
    {
        public static DefendRegionTask Task = new DefendRegionTask();
        public float DefendRange = 15;
        public float DrawDefendersRange = 40;
        public Point2D DefenseLocation = null;

        private int TargetUpdatedFrame = 0;

        Unit Target = null;

        public DefendRegionTask() : base(10) { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = DefenseLocation, UnitTypes = UnitTypes.CombatUnitTypes, MaxDist = DrawDefendersRange });
            return result;
        }

        public override bool IsNeeded()
        {
            return UnderAttack();
        }

        public bool UnderAttack()
        {
            if (TargetUpdatedFrame >= Bot.Main.Frame)
                return Target != null;

            Target = null;
            TargetUpdatedFrame = Bot.Main.Frame;

            if (DefenseLocation == null)
                return false;
            float dist = DefendRange * DefendRange;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                    continue;
                float newDist = SC2Util.DistanceSq(enemy.Pos, DefenseLocation);
                if (newDist > dist)
                    continue;

                dist = newDist;
                Target = enemy;
            }
            return Target != null;
        }

        public override void OnFrame(Bot bot)
        {
            if (Stopped || !UnderAttack())
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
            {
                bool enemyClose = false;
                foreach (Unit enemy in bot.Enemies())
                {
                    if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                        continue;
                    if (agent.DistanceSq(enemy) <= 12 * 12)
                        enemyClose = true;
                }

                if (agent.Unit.WeaponCooldown > 0
                    && agent.DistanceSq(Target) >= 15 * 15
                    && !enemyClose)
                    agent.Order(Abilities.MOVE, SC2Util.To2D(Target.Pos));
                else
                    bot.MicroController.Attack(agent, SC2Util.To2D(Target.Pos));
            }
        }
    }
}
