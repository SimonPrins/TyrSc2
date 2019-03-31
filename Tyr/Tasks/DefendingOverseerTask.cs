using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class DefendingOverseerTask : Task
    {
        public static DefendingOverseerTask Task = new DefendingOverseerTask();

        public DefendingOverseerTask() : base(6)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.OVERSEER && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Tyr.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.OVERSEER } });
            return result;
        }

        public override bool IsNeeded()
        {
            return DefenseTask.GroundDefenseTask.GetTarget() != null;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (units.Count == 0)
                return;
            Unit targetEnemy = DefenseTask.GroundDefenseTask.GetTarget();
            if (targetEnemy == null)
            {
                Clear();
                return;
            }

            Unit fleeEnemy = null;
            float dist = 10 * 10;
            foreach (Unit enemy in tyr.Enemies())
            {
                if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                    continue;
                float newDist = units[0].DistanceSq(enemy);
                if (newDist < dist)
                {
                    fleeEnemy = enemy;
                    dist = newDist;
                }
            }

            if (fleeEnemy != null)
            {
                PotentialHelper helper = new PotentialHelper(units[0].Unit.Pos);
                helper.Magnitude = 4;
                helper.From(fleeEnemy.Pos);
                units[0].Order(Abilities.MOVE, helper.Get());
                return;
            }

            Point2D target = SC2Util.To2D(targetEnemy.Pos);
            
            Agent closest = null;
            dist = 1000000;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (!UnitTypes.CombatUnitTypes.Contains(agent.Unit.UnitType))
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
                    agent.Order(Abilities.MOVE, defenseLocation);
                else
                    agent.Order(Abilities.MOVE, SC2Util.To2D(closest.Unit.Pos));
            }
        }
    }
}
