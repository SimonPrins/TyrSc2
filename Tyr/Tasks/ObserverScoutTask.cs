using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ObserverScoutTask : Task
    {
        public static ObserverScoutTask Task = new ObserverScoutTask();
        private List<Base> Bases = new List<Base>();
        private Base Target;

        public ObserverScoutTask() : base(10)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.OBSERVER && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Tyr.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.OBSERVER } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (units.Count == 0)
                return;

            if (Target != null)
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (SC2Util.DistanceSq(agent.Unit.Pos, Target.BaseLocation.Pos) <= 3 * 3)
                    {
                        Bases.RemoveAt(Bases.Count - 1);
                        Target = null;
                        break;
                    }
            if (Target != null)
                foreach (BuildingLocation building in tyr.EnemyManager.EnemyBuildings.Values)
                    if (SC2Util.DistanceSq(building.Pos, Target.BaseLocation.Pos) <= 6 * 6)
                    {
                        Bases.RemoveAt(Bases.Count - 1);
                        Target = null;
                        break;
                    }


            if (Bases.Count == 0)
                foreach (Base b in tyr.BaseManager.Bases)
                    Bases.Add(b);

            if (Target == null)
            {
                int closest = 0;
                float dist = SC2Util.DistanceSq(units[0].Unit.Pos, Bases[0].BaseLocation.Pos);
                for (int i = 1; i < Bases.Count; i++)
                {
                    float newDist = SC2Util.DistanceSq(units[0].Unit.Pos, Bases[i].BaseLocation.Pos);
                    if (newDist < dist)
                    {
                        dist = newDist;
                        closest = i;
                    }
                }
                Base temp = Bases[closest];
                Bases[closest] = Bases[Bases.Count - 1];
                Bases[Bases.Count - 1] = temp;

                Target = temp;
            }

            foreach (Agent agent in units)
                agent.Order(Abilities.MOVE, Target.BaseLocation.Pos);
        }
    }
}
