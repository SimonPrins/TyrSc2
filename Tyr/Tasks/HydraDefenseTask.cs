using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class HydraDefenseTask : Task
    {
        public static List<HydraDefenseTask> Tasks = new List<HydraDefenseTask>();
        public Base Base;
        private Point2D IdleLocation;
        public int MaxDefenders = 3;

        public HydraDefenseTask(Base b) : base(6)
        {
            Base = b;
        }

        public static void Enable(bool excludeMainAndNatural)
        {
            if (Tasks.Count == 0)
                foreach (Base b in Tyr.Bot.BaseManager.Bases)
                {
                    HydraDefenseTask task = new HydraDefenseTask(b);
                    Tasks.Add(task);
                    Tyr.Bot.TaskManager.Add(task);
                }

            foreach (HydraDefenseTask task in Tasks)
            {
                if (!excludeMainAndNatural)
                    task.Stopped = false;
                else if (task.Base == Tyr.Bot.BaseManager.Main || task.Base == Tyr.Bot.BaseManager.Natural)
                    task.Stopped = true;
                else
                    task.Stopped = false;
            }
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.HYDRALISK
                && units.Count < MaxDefenders
                && agent.DistanceSq(Tyr.Bot.MapAnalyzer.StartLocation) <= 55 * 55;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Base.BaseLocation.Pos, Count = MaxDefenders - units.Count, UnitTypes = new HashSet<uint>() { UnitTypes.HYDRALISK} });
            return result;
        }

        public override bool IsNeeded()
        {
            return Base.Owner == Tyr.Bot.PlayerId;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (IdleLocation == null)
                IdleLocation = tyr.MapAnalyzer.Walk(Base.BaseLocation.Pos, tyr.MapAnalyzer.EnemyDistances, 8);

            float distance = 20 * 20;
            Unit target = null;
            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (unit.UnitType == UnitTypes.ADEPT_PHASE_SHIFT)
                    continue;

                if (unit.UnitType == UnitTypes.CHANGELING
                    || unit.UnitType == UnitTypes.CHANGELING_MARINE
                    || unit.UnitType == UnitTypes.CHANGELING_MARINE_SHIELD
                    || unit.UnitType == UnitTypes.CHANGELING_ZEALOT
                    || unit.UnitType == UnitTypes.CHANGELING_ZERGLING
                    || unit.UnitType == UnitTypes.CHANGELING_ZERGLING_WINGS)
                    continue;
                
                float newDist = SC2Util.DistanceSq(unit.Pos, Base.BaseLocation.Pos);
                
                if (newDist > distance)
                    continue;

                distance = newDist;
                target = unit;
            }

            if (target == null)
            {
                foreach (Agent agent in units)
                    if (agent.DistanceSq(IdleLocation) > 3 * 3)
                        agent.Order(Abilities.MOVE, IdleLocation);
            }
            else
            {
                foreach (Agent agent in units)
                    tyr.MicroController.Attack(agent, SC2Util.To2D(target.Pos));
            }
        }
    }
}
