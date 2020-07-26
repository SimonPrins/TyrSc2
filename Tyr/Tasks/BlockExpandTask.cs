using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class BlockExpandTask : Task
    {
        public static BlockExpandTask Task = new BlockExpandTask();
        private Point2D Target = null;
        private int Command = 0;

        public BlockExpandTask() : base(10)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.PROBE && units.Count == 0 && Bot.Main.Frame <= 220;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            if (Target == null)
                Target = bot.MapAnalyzer.GetEnemyNatural().Pos;
            if (Target != null && bot.Frame % 5 == 0)
                foreach (Agent agent in Units)
                {
                    if (agent.Unit.Orders.Count < 7)
                    {
                        if (Command % 6 == 0)
                            agent.Order(Abilities.MOVE, new Point2D() { X = Target.X, Y = Target.Y - 2.4f }, Command != 0);
                        else if (Command % 6 == 1)
                            agent.Order(Abilities.MOVE, new Point2D() { X = Target.X - 2f, Y = Target.Y + 0.7f }, true);
                        else if (Command % 6 == 2)
                            agent.Order(Abilities.MOVE, new Point2D() { X = Target.X - 1f, Y = Target.Y + 1.5f }, true);
                        else if (Command % 6 == 3)
                            agent.Order(Abilities.MOVE, new Point2D() { X = Target.X, Y = Target.Y + 0.5f }, true);
                        else if (Command % 6 == 4)
                            agent.Order(Abilities.MOVE, new Point2D() { X = Target.X + 1f, Y = Target.Y + 1.5f }, true);
                        else if (Command % 6 == 5)
                            agent.Order(Abilities.MOVE, new Point2D() { X = Target.X + 2f, Y = Target.Y + 0.7f }, true);
                        Command++;
                    }
                }
        }
    }
}
