using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class ScoutProxyTask : Task
    {
        public static ScoutProxyTask Task;

        public int StartFrame = (int)(70 * 22.4);
        private bool Done;
        private Point2D Target;
        private ArrayBoolGrid NeedsScouting;
            
        public ScoutProxyTask(Point2D target) : base(10)
        {
            Target = target;
        }

        public static void Enable(Point2D target)
        {
            if (Task == null)
                Task = new ScoutProxyTask(target);
            else
                Task.Target = target;
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsWorker && units.Count == 0 && !Done;
        }

        public override bool IsNeeded()
        {
            return Tyr.Bot.Frame >= StartFrame;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (NeedsScouting == null)
                NeedsScouting = (ArrayBoolGrid)tyr.MapAnalyzer.MainAndPocketArea.GetAnd(tyr.MapAnalyzer.StartArea.Invert());
            
            if (units.Count > 0 && SC2Util.DistanceSq(units[0].Unit.Pos, Target) <= 6 * 6)
            {
                Target = GetTarget(units[0].Unit.Pos);
                if (Target == null || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.REAPER) > 0)
                {
                    Done = true;
                    Clear();
                }
            }

            foreach (Agent agent in units)
            {
                agent.Order(Abilities.MOVE, Target);

                for (int dx = -6; dx <= 6; dx++)
                    for (int dy = -6; dy <= 6; dy++)
                        if (dx * dx + dy * dy <= 6 * 6)
                            NeedsScouting[(int)agent.Unit.Pos.X + dx, (int)agent.Unit.Pos.Y + dy] = false;
            }
        }

        private Point2D GetTarget(Point cur)
        {
            for (int dist = 0; dist <= 30; dist++)
            {
                for (int dx = -dist; dx <= dist; dx++)
                {
                    if (NeedsScouting[(int)cur.X + dx, (int)cur.Y + dist])
                        return SC2Util.Point((int)cur.X + dx, (int)cur.Y + dist);
                    if (NeedsScouting[(int)cur.X + dx, (int)cur.Y - dist])
                        return SC2Util.Point((int)cur.X + dx, (int)cur.Y - dist);
                }
                for (int dy = -dist; dy <= dist; dy++)
                {
                    if (NeedsScouting[(int)cur.X + dist, (int)cur.Y + dy])
                        return SC2Util.Point((int)cur.X + dist, (int)cur.Y + dy);
                    if (NeedsScouting[(int)cur.X - dist, (int)cur.Y + dy])
                        return SC2Util.Point((int)cur.X - dist, (int)cur.Y + dy);
                }
            }
            return null;
        }
    }
}
