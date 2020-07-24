using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ObserverHunterTask : Task
    {
        public static ObserverHunterTask Task = new ObserverHunterTask();

        private int UpdateTargetFrame = 0;
        private Unit Target = null;

        public ObserverHunterTask() : base(10)
        { }

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
            if (Units.Count == 0 && Target != null)
                result.Add(new UnitDescriptor() { Pos = SC2Util.To2D(Target.Pos), Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.OBSERVER } });
            return result;
        }

        public override bool IsNeeded()
        {
            UpdateTarget();
            return Target != null;
        }

        private void UpdateTarget()
        {
            if (Bot.Bot.Frame >= UpdateTargetFrame)
                return;
            UpdateTargetFrame = Bot.Bot.Frame;

            Target = null;
            if (DefenseTask.AirDefenseTask.IsDefending() || DefenseTask.GroundDefenseTask.IsDefending() || TimingAttackTask.Task.Units.Count > 0)
                return;

            Point2D reference;
            if (Units.Count > 0)
                reference = SC2Util.To2D(Units[0].Unit.Pos);
            else
                reference = SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation);


            float dist = 80 * 80;
            foreach (Unit enemy in Bot.Bot.CloakedEnemies())
            {
                if (enemy.UnitType != UnitTypes.OBSERVER)
                    continue;
                float newDist = SC2Util.DistanceSq(reference, enemy.Pos);
                if (newDist > dist)
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, Bot.Bot.MapAnalyzer.StartLocation) > 80 * 80)
                    continue;
                Target = enemy;
                dist = newDist;
            }
        }

        public override void OnFrame(Bot tyr)
        {
            UpdateTarget();
            if (Target == null)
                Clear();
            if (units.Count == 0)
                return;


            foreach (Agent agent in units)
                agent.Order(Abilities.MOVE, Target.Pos);
        }
    }
}
