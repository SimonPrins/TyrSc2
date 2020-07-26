using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class FlyerAttackTask : Task
    {
        public static FlyerAttackTask Task = new FlyerAttackTask();

        public int RequiredSize { get; set; } = 14;
        public FlyerAttackTask() : base(5)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.VOID_RAY || agent.Unit.UnitType == UnitTypes.CARRIER || agent.Unit.UnitType == UnitTypes.TEMPEST;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.UnitManager.Completed(UnitTypes.VOID_RAY) + Bot.Main.UnitManager.Completed(UnitTypes.CARRIER) + Bot.Main.UnitManager.Completed(UnitTypes.TEMPEST) >= RequiredSize;
        }

        public override void OnFrame(Bot bot)
        {
            Dictionary<ulong, Unit> targets = new Dictionary<ulong, Unit>();
            bool attacking = false;
            Point2D defendTarget = null;
            foreach (Agent agent in units)
            {
                Unit target = null;
                float health = 10000;
                ulong tag = 0;
                foreach (Unit enemy in bot.Observation.Observation.RawData.Units)
                {
                    if (enemy.Alliance != Alliance.Enemy)
                        continue;

                    if (SC2Util.DistanceSq(agent.Unit.Pos, enemy.Pos) >= 8 * 8)
                        continue;

                    if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                        continue;

                    if (enemy.Health + enemy.Shield > health)
                        continue;

                    if (enemy.Health + enemy.Shield == health && tag > enemy.Tag)
                        continue;

                    health = enemy.Health + enemy.Shield;
                    target = enemy;
                    tag = enemy.Tag;
                    attacking = true;
                    defendTarget = SC2Util.To2D(agent.Unit.Pos);
                }
                
                targets.Add(agent.Unit.Tag, target);
            }
            foreach (Agent agent in units)
            {
                Unit target = targets[agent.Unit.Tag];
                if (target != null)
                    agent.Order(Abilities.ATTACK, target.Tag);
                else if (attacking && agent.DistanceSq(defendTarget) <= 15 * 15)
                    agent.Order(Abilities.MOVE, defendTarget);
                else
                    bot.MicroController.Attack(agent, bot.TargetManager.AttackTarget);

            }
        }
    }
}
