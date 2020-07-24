using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ArmyRavenTask : Task
    {
        public static ArmyRavenTask Task = new ArmyRavenTask();

        public ArmyRavenTask() : base(7)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.RAVEN && units.Count == 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Bot.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.RAVEN } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            if (units.Count == 0)
                return;

            Agent raven = Units[0];

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
                raven.Order(Abilities.MOVE, raven.From(fleeEnemy, 4));
                tyr.DrawText("Raven fleeing!");
                return;
            }

            if (tyr.Frame % 5 == 0)
                return;

            Point2D target = tyr.TargetManager.AttackTarget;
            
            Agent closest = null;
            dist = 1000000;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (agent.Unit.Tag == raven.Unit.Tag)
                    continue;

                if (!UnitTypes.CombatUnitTypes.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.IsFlying)
                    continue;

                if (agent.Unit.UnitType == UnitTypes.WIDOW_MINE
                    || agent.Unit.UnitType == UnitTypes.WIDOW_MINE_BURROWED)
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
                    tyr.DrawText("Raven returning!");
                    agent.Order(Abilities.MOVE, defenseLocation);
                }
                else
                {
                    tyr.DrawText("Raven moving out!");
                    agent.Order(Abilities.MOVE, SC2Util.To2D(closest.Unit.Pos));
                }
            }
        }
    }
}
