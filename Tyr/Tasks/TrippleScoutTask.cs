using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
{
    class TrippleScoutTask : Task
    {
        private bool done;
        private Dictionary<ulong, Point2D> Targets = new Dictionary<ulong, Point2D>();

        public TrippleScoutTask() : base(10)
        {
            done = Bot.Main.TargetManager.PotentialEnemyStartLocations.Count == 1;
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && units.Count < Bot.Main.TargetManager.PotentialEnemyStartLocations.Count && Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1;
        }

        public override bool IsNeeded()
        {
            return !done;
        }

        public override void OnFrame(Bot bot)
        {
            if (bot.TargetManager.PotentialEnemyStartLocations.Count == 0)
            {
                done = true;
                Clear();
                return;
            }

            List<Point2D> remaining = new List<Point2D>();
            foreach (Point2D loc in bot.TargetManager.PotentialEnemyStartLocations)
                remaining.Add(loc);

            foreach (Agent agent in Units)
            {
                if (!Targets.ContainsKey(agent.Unit.Tag))
                    Targets.Add(agent.Unit.Tag, null);
                else if (Targets[agent.Unit.Tag] != null && !bot.TargetManager.PotentialEnemyStartLocations.Contains(Targets[agent.Unit.Tag]))
                    Targets[agent.Unit.Tag] = null;

                if (Targets[agent.Unit.Tag] != null)
                    remaining.Remove(Targets[agent.Unit.Tag]);
            }

            for (int i = units.Count - 1; i >= 0; i--)
            {
                Agent agent = units[i];
                if (Targets[agent.Unit.Tag] == null)
                {
                    if (remaining.Count == 0)
                    {
                        units[i] = units[units.Count - 1];
                        units.RemoveAt(units.Count - 1);
                        IdleTask.Task.Add(agent);
                        continue;
                    }
                    Point2D target = remaining[remaining.Count - 1];
                    remaining.RemoveAt(remaining.Count - 1);
                    Targets[agent.Unit.Tag] = target;
                }
            }


            foreach (Agent agent in units)
                agent.Order(Abilities.MOVE, Targets[agent.Unit.Tag]);
        }
    }
}
