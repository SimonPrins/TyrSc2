using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class PhoenixScoutTask : Task
    {
        public static PhoenixScoutTask Task = new PhoenixScoutTask();

        private List<Base> Bases = new List<Base>();
        private Base Target;

        public static void Enable()
        {
            Enable(Task);
        }

        public PhoenixScoutTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.PHOENIX;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            DetermineTarget();
            if (Units.Count == 0)
                return;
            foreach (Agent agent in units)
            {
                Unit overlord = null;
                float dist = 20 * 20;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.OVERLORD
                        && enemy.UnitType != UnitTypes.OVERSEER)
                        continue;
                    float newDist = agent.DistanceSq(enemy);
                    if (newDist >= dist)
                        continue;
                    dist = newDist;
                    overlord = enemy;
                }
                if (overlord != null)
                    agent.Order(Abilities.MOVE, SC2Util.To2D(overlord.Pos));
                else if (Target != null)
                    agent.Order(Abilities.MOVE, Target.BaseLocation.Pos);
            }
        }

        private void DetermineTarget()
        {
            if (Target != null)
                foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                    if (SC2Util.DistanceSq(agent.Unit.Pos, Target.BaseLocation.Pos) <= 3 * 3)
                    {
                        Bases.RemoveAt(Bases.Count - 1);
                        Target = null;
                        break;
                    }
            if (Target != null)
                foreach (BuildingLocation building in Bot.Main.EnemyManager.EnemyBuildings.Values)
                    if (SC2Util.DistanceSq(building.Pos, Target.BaseLocation.Pos) <= 6 * 6)
                    {
                        Bases.RemoveAt(Bases.Count - 1);
                        Target = null;
                        break;
                    }


            if (Bases.Count == 0)
                foreach (Base b in Bot.Main.BaseManager.Bases)
                    Bases.Add(b);


            if (Target == null && Units.Count > 0)
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

        }
    }

}
