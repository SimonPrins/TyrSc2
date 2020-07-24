using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class ThorretTask : Task
    {
        public static List<ThorretTask> Tasks = new List<ThorretTask>();
        private Point2D IdleLocation;

        public ThorretTask() : base(10)
        { }

        public static void Enable()
        {
            Point2D startLocation = SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation);
            AddTask(SC2Util.Point(startLocation.X + 6, startLocation.Y + 6));
            AddTask(SC2Util.Point(startLocation.X - 6, startLocation.Y + 6));
            AddTask(SC2Util.Point(startLocation.X + 6, startLocation.Y - 6));
            AddTask(SC2Util.Point(startLocation.X - 6, startLocation.Y - 6));

            Tasks.Sort((a, b) => (int)(SC2Util.DistanceSq(a.IdleLocation, Bot.Bot.TargetManager.PotentialEnemyStartLocations[0]) - SC2Util.DistanceSq(b.IdleLocation, Bot.Bot.TargetManager.PotentialEnemyStartLocations[0])));
            int i = 0;
            foreach (Task task in Tasks)
            {
                task.Priority = 12 + i;
                i++;
            }

        }

        private static void AddTask(Point2D idleLocation)
        {
            ThorretTask task = new ThorretTask() { IdleLocation = idleLocation };
            Tasks.Add(task);
            Bot.Bot.TaskManager.Add(task);
            task.Stopped = false;
        }

        public override bool DoWant(Agent agent)
        {
            return (agent.Unit.UnitType == UnitTypes.THOR_SINGLE_TARGET)
                && units.Count == 0
                && agent.DistanceSq(Bot.Bot.MapAnalyzer.StartLocation) <= 55 * 55;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = IdleLocation, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.THOR_SINGLE_TARGET } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Stopped)
            {
                Clear();
                return;
            }
            
            float distance = 15 * 15;
            Unit target = null;
            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (unit.UnitType == UnitTypes.ADEPT_PHASE_SHIFT
                    || unit.UnitType == UnitTypes.KD8_CHARGE)
                    continue;

                if (unit.UnitType == UnitTypes.CHANGELING
                    || unit.UnitType == UnitTypes.CHANGELING_MARINE
                    || unit.UnitType == UnitTypes.CHANGELING_MARINE_SHIELD
                    || unit.UnitType == UnitTypes.CHANGELING_ZEALOT
                    || unit.UnitType == UnitTypes.CHANGELING_ZERGLING
                    || unit.UnitType == UnitTypes.CHANGELING_ZERGLING_WINGS)
                    continue;
                
                float newDist = SC2Util.DistanceSq(unit.Pos, IdleLocation);
                
                if (newDist > distance)
                    continue;

                distance = newDist;
                target = unit;
            }

            if (target == null)
            {
                foreach (Agent agent in units)
                {
                    if (agent.DistanceSq(IdleLocation) > 3 * 3)
                        tyr.MicroController.Attack(agent, IdleLocation);
                    else if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK)
                        agent.Order(Abilities.SIEGE);
                }
            }
            else
            {
                foreach (Agent agent in units)
                {
                    if (agent.Unit.UnitType == UnitTypes.SIEGE_TANK_SIEGED
                        && agent.DistanceSq(IdleLocation) <= 3 * 3)
                        continue;
                    tyr.MicroController.Attack(agent, SC2Util.To2D(target.Pos));
                }
            }
        }
    }
}
