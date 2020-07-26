using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class ArmyObserverTask : Task
    {
        public static ArmyObserverTask Task = new ArmyObserverTask();

        public HashSet<uint> IgnoreAllyUnitTypes = new HashSet<uint>();

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
            result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.OBSERVER } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            if (units.Count == 0)
                return;



            Agent observer = Units[0];

            Unit fleeEnemy = null;
            float dist = 10 * 10;
            foreach (Unit enemy in bot.Enemies())
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

            Unit followEnemy = null;
            dist = 12 * 12;
            foreach (Unit enemy in bot.CloakedEnemies())
            {
                float newDist = units[0].DistanceSq(enemy);

                if (newDist < dist)
                {
                    followEnemy = enemy;
                    dist = newDist;
                }
            }
            if (followEnemy != null)
            {
                bot.DrawLine(observer.Unit.Pos, followEnemy.Pos);
                observer.Order(Abilities.MOVE, followEnemy.Pos);
                return;
            }

            if (bot.Frame % 5 == 0)
                return;

            Point2D target = bot.TargetManager.AttackTarget;
            
            Agent closest = null;
            dist = 1000000;
            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (agent.Unit.Tag == observer.Unit.Tag)
                    continue;

                if (IgnoreAllyUnitTypes.Contains(agent.Unit.UnitType))
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
            foreach (Base b in bot.BaseManager.Bases)
                if (b.ResourceCenter != null)
                    bases++;

            Point2D defenseLocation;
            if (bases >= 2)
                defenseLocation = bot.BaseManager.NaturalDefensePos;
            else defenseLocation = bot.BaseManager.MainDefensePos;

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
