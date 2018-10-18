using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class QueenTumorTask : Task
    {
        public static QueenTumorTask Task = new QueenTumorTask();
        private Dictionary<ulong, int> BurrowFrames = new Dictionary<ulong, int>();
        public QueenTumorTask() : base(4)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return Tyr.Bot.Build.Count(UnitTypes.CREEP_TUMOR) + Tyr.Bot.Build.Count(UnitTypes.CREEP_TUMOR_QUEEN) < 3 && agent.Unit.UnitType == UnitTypes.QUEEN && agent.Unit.Energy >= 75;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType != UnitTypes.CREEP_TUMOR_BURROWED)
                    continue;
                if (!BurrowFrames.ContainsKey(agent.Unit.Tag))
                    BurrowFrames.Add(agent.Unit.Tag, tyr.Frame);

               if (tyr.Frame - BurrowFrames[agent.Unit.Tag] < 336
                    && tyr.Frame - BurrowFrames[agent.Unit.Tag] >= 224)
               {
                    Point2D aroundLoc = tyr.MapAnalyzer.Walk(SC2Util.To2D(agent.Unit.Pos), tyr.MapAnalyzer.EnemyDistances, 9);
                    Point2D finalLoc = tyr.buildingPlacer.FindPlacementLocal(aroundLoc, SC2Util.Point(1, 1), UnitTypes.CREEP_TUMOR, 10, SC2Util.To2D(agent.Unit.Pos), 9);
                    if (finalLoc != null)
                        agent.Order(1733, finalLoc);
                }
            }

            if (Tyr.Bot.Build.Count(UnitTypes.CREEP_TUMOR) + Tyr.Bot.Build.Count(UnitTypes.CREEP_TUMOR_QUEEN) + Tyr.Bot.Build.Count(UnitTypes.CREEP_TUMOR_BURROWED) >= 6)
                Clear();

            if (units.Count == 0)
                return;

            int bases = 0;
            foreach (Base b in tyr.BaseManager.Bases)
                if (b.ResourceCenter != null)
                    bases++;

            Point2D target;
            Base defendBase = null;
            if (bases >= 2)
            {
                target = tyr.BaseManager.NaturalDefensePos;
                defendBase = tyr.BaseManager.Natural;
            }
            else
            {
                target = tyr.BaseManager.MainDefensePos;
                defendBase = tyr.BaseManager.Main;
            }
            Point2D tumorTarget = tyr.buildingPlacer.FindPlacement(tyr.MapAnalyzer.Walk(target, tyr.MapAnalyzer.EnemyDistances, 4), SC2Util.Point(1, 1), UnitTypes.CREEP_TUMOR); ;
            
            foreach (Agent queen in units)
            {
                if (queen.Unit.Energy >= 75)
                {
                    tyr.DrawSphere(SC2Util.Point(tumorTarget.X, tumorTarget.Y, 0));
                    queen.Order(1694, tumorTarget);
                    continue;
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
