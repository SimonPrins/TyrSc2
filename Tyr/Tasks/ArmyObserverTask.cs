using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ArmyObserverTask : Task
    {
        public static ArmyObserverTask Task = new ArmyObserverTask();

        public ArmyObserverTask() : base(7)
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

            Agent observer = Units[0];

            Unit fleeEnemy = null;
            float dist = 10 * 10;
            foreach (Unit enemy in tyr.Enemies())
            {
                if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                    continue;
                float newDist = units[0].DistanceSq(enemy);
                if (enemy.UnitType == UnitTypes.WIDOW_MINE_BURROWED
                    && dist >= 6 * 6)
                    continue;
                if (newDist < dist)
                {
                    fleeEnemy = enemy;
                    dist = newDist;
                }
            }

            if (fleeEnemy != null)
            {
                observer.Order(Abilities.MOVE, observer.From(fleeEnemy, 4));
                return;
            }

            if (tyr.Frame % 5 == 0)
                return;

            Point2D target = tyr.TargetManager.AttackTarget;
            
            Agent closest = null;
            dist = 1000000;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.Tag == observer.Unit.Tag)
                    continue;

                if (!UnitTypes.CombatUnitTypes.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.IsFlying)
                    continue;
                
                float newDist = agent.DistanceSq(target);
                if (newDist < dist)
                {
                    closest = agent;
                    dist = newDist;
                }
            }

            int bases = 0;
            foreach (Base b in tyr.BaseManager.Bases)
                if (b.ResourceCenter != null)
                    bases++;

            Point2D defenseLocation;
            if (bases >= 2)
                defenseLocation = tyr.BaseManager.NaturalDefensePos;
            else defenseLocation = tyr.BaseManager.MainDefensePos;

            foreach (Agent agent in units)
            {
                if (closest == null)
                {
                    agent.Order(Abilities.MOVE, defenseLocation);
                }
                else
                {
                    agent.Order(Abilities.MOVE, SC2Util.To2D(closest.Unit.Pos));
                }
            }
        }
    }
}
