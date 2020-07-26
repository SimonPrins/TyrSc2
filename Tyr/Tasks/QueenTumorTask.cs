using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class QueenTumorTask : Task
    {
        public static QueenTumorTask Task = new QueenTumorTask();
        private Dictionary<ulong, int> BurrowFrames = new Dictionary<ulong, int>();

        public bool PlaceTumorsInMain = false;

        public QueenTumorTask() : base(4)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return Bot.Main.Build.Count(UnitTypes.CREEP_TUMOR) + Bot.Main.Build.Count(UnitTypes.CREEP_TUMOR_QUEEN) < 3 && agent.Unit.UnitType == UnitTypes.QUEEN && agent.Unit.Energy >= 75 && Units.Count == 0;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Build.Count(UnitTypes.CREEP_TUMOR) + Bot.Main.Build.Count(UnitTypes.CREEP_TUMOR_QUEEN) + Bot.Main.Build.Count(UnitTypes.CREEP_TUMOR_BURROWED) < 6;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> descriptors = new List<UnitDescriptor>();
            if (Units.Count == 0)
                descriptors.Add(new UnitDescriptor(UnitTypes.QUEEN) { Count = 1 });
            return descriptors;
        }

        public override void OnFrame(Bot bot)
        {
            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType != UnitTypes.CREEP_TUMOR_BURROWED)
                    continue;
                if (!BurrowFrames.ContainsKey(agent.Unit.Tag))
                    BurrowFrames.Add(agent.Unit.Tag, bot.Frame);

               if (bot.Frame - BurrowFrames[agent.Unit.Tag] < 336
                    && bot.Frame - BurrowFrames[agent.Unit.Tag] >= 224)
               {
                    Point2D aroundLoc = bot.MapAnalyzer.Walk(SC2Util.To2D(agent.Unit.Pos), bot.MapAnalyzer.EnemyDistances, 9);
                    Point2D finalLoc = bot.buildingPlacer.FindPlacementLocal(aroundLoc, SC2Util.Point(1, 1), UnitTypes.CREEP_TUMOR, 10, SC2Util.To2D(agent.Unit.Pos), 9);
                    if (finalLoc != null)
                        agent.Order(1733, finalLoc);
                }
            }

            if (Bot.Main.Build.Count(UnitTypes.CREEP_TUMOR) + Bot.Main.Build.Count(UnitTypes.CREEP_TUMOR_QUEEN) + Bot.Main.Build.Count(UnitTypes.CREEP_TUMOR_BURROWED) >= 6)
                Clear();

            if (units.Count == 0)
                return;

            int bases = 0;
            foreach (Base b in bot.BaseManager.Bases)
                if (b.ResourceCenter != null)
                    bases++;


            Point2D tumorTarget;
            if (PlaceTumorsInMain)
            {
                Point2D main = bot.BaseManager.Main.BaseLocation.Pos;
                Point2D natural = bot.BaseManager.Natural.BaseLocation.Pos;
                Point2D halfway = SC2Util.Point((main.X + natural.X) / 2, (main.Y + natural.Y) / 2);
                tumorTarget = bot.buildingPlacer.FindPlacement(halfway, SC2Util.Point(1, 1), UnitTypes.CREEP_TUMOR); ;
            }
            else
            {
                Point2D target;
                Base defendBase = null;
                if (bases >= 2)
                {
                    target = bot.BaseManager.NaturalDefensePos;
                    defendBase = bot.BaseManager.Natural;
                }
                else
                {
                    target = bot.BaseManager.MainDefensePos;
                    defendBase = bot.BaseManager.Main;
                }
                tumorTarget = bot.buildingPlacer.FindPlacement(bot.MapAnalyzer.Walk(target, bot.MapAnalyzer.EnemyDistances, 4), SC2Util.Point(1, 1), UnitTypes.CREEP_TUMOR); ;
            }

            foreach (Agent queen in units)
            {
                if (queen.Unit.Energy >= 75)
                {
                    bot.DrawSphere(SC2Util.Point(tumorTarget.X, tumorTarget.Y, 0));
                    queen.Order(1694, tumorTarget);
                }
            }

            for (int i = units.Count - 1; i >= 0; i--)
                if (units[i].Unit.Energy < 75)
                {
                    IdleTask.Task.Add(units[i]);
                    RemoveAt(i);
                }
        }
    }
}
