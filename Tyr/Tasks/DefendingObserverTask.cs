using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class DefendingObserverTask : Task
    {
        public static DefendingObserverTask Task = new DefendingObserverTask();

        public DefendingObserverTask() : base(6)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
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

        public override void OnFrame(Bot tyr)
        {
            if (units.Count == 0)
                return;

            Unit fleeEnemy = null;
            float dist = 8 * 8;
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

            dist = 50 * 50;
            Unit scoutEnemy = null;
            foreach (Unit enemy in tyr.CloakedEnemies())
            {
                float newDist = Units[0].DistanceSq(enemy);
                if (newDist >= dist)
                    continue;
                bool close = false;
                foreach (Base b in tyr.BaseManager.Bases)
                {
                    if (b.Owner != tyr.PlayerId)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, b.BaseLocation.Pos) <= 30 * 30)
                    {
                        close = true;
                        break;
                    }
                }
                if (!close)
                    continue;
                scoutEnemy = enemy;
                dist = newDist;
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
                if (scoutEnemy == null)
                    agent.Order(Abilities.MOVE, defenseLocation);
                else
                    agent.Order(Abilities.MOVE, SC2Util.To2D(scoutEnemy.Pos));
            }
        }
    }
}
