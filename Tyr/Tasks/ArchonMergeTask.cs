using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class ArchonMergeTask : Task
    {
        public static ArchonMergeTask Task = new ArchonMergeTask();
        public Point2D MergePos = null;

        public ArchonMergeTask() : base(12)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return (agent.Unit.UnitType == UnitTypes.DARK_TEMPLAR || agent.Unit.UnitType == UnitTypes.HIGH_TEMPLAR) && SC2Util.DistanceGrid(agent.Unit.Pos, Tyr.Bot.MapAnalyzer.StartLocation) <= 80;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            for (int i = 0; i < units.Count - 1; i++)
            {
                if (MergePos != null 
                    && (units[i].DistanceSq(MergePos) >= 2 * 2 || units[i + 1].DistanceSq(MergePos) >= 2 * 2))
                {
                    units[i].Order(Abilities.MOVE, MergePos);
                    units[i + 1].Order(Abilities.MOVE, MergePos);
                }
                else
                    units[i].ArchonMerge(units[i + 1]);
            }
        }
    }
}
