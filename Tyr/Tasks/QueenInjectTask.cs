using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class QueenInjectTask : Task
    {
        public static List<QueenInjectTask> Tasks = new List<QueenInjectTask>();
        private Base b;
        public static int DefenseRadius = 12;

        public QueenInjectTask(Base b) : base(10)
        {
            this.b = b;
        }

        public static void Enable()
        {
            if (Tasks.Count == 0)
                foreach (Base b in Bot.Main.BaseManager.Bases)
                {
                    QueenInjectTask queenInjectTask = new QueenInjectTask(b);
                    Tasks.Add(queenInjectTask);
                }

            foreach (Task task in Tasks)
            {
                task.Stopped = false;
                Bot.Main.TaskManager.Add(task);
            }
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = b.BaseLocation.Pos, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.QUEEN } });
            return result;
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.QUEEN && Units.Count == 0;
        }

        public override bool IsNeeded()
        {
            return b.Owner == Bot.Main.PlayerId;
        }

        public override void OnFrame(Bot bot)
        {
            if (b.ResourceCenter == null)
            {
                Clear();
                return;
            }
            foreach (Agent agent in units)
            {
                Unit defendEnemy = null;
                float dist = DefenseRadius * DefenseRadius;
                foreach (Unit enemy in bot.Enemies())
                {
                    float newDist = SC2Util.DistanceSq(b.BaseLocation.Pos, enemy.Pos);
                    if (newDist < dist)
                    {
                        defendEnemy = enemy;
                        dist = newDist;
                    }
                }

                if (defendEnemy != null)
                    Attack(agent, SC2Util.To2D(defendEnemy.Pos));
                else if (agent.DistanceSq(b.ResourceCenter) >= 7 * 7)
                    agent.Order(Abilities.MOVE, b.ResourceCenter.Unit.Tag);
                else if (agent.Unit.Energy >= 23)
                    agent.Order(251, b.ResourceCenter.Unit.Tag);
            }
        }
    }
}
