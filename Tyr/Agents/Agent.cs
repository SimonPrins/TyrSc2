using System;
using SC2APIProtocol;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Agents
{
    public class Agent
    {
        private int lastOrderFrame = 0;
        public int LastAbility = 0;
        public Agent(Unit unit)
        {
            Unit = unit;
        }

        public ActionRawUnitCommand Command;
        
        public void Order(int ability)
        {
            Order(ability, null);
        }

        public void Order(int ability, Point2D target)
        {
            // Make sure blink doesn't get cancelled.
            if (LastAbility == Abilities.BLINK && Tyr.Bot.Frame - lastOrderFrame <= 20)
                return;
            // Short delay between orders to prevent order spam.
            if (LastAbility == ability && Tyr.Bot.Frame - lastOrderFrame <= 5)
                return;

            LastAbility = ability;

            // Ignore orders that are the same or similar to existing orders.
            if (target != null && 
                Unit.Orders.Count != 0 
                && Unit.Orders[0].TargetWorldSpacePos != null 
                && SC2Util.DistanceGrid(Unit.Orders[0].TargetWorldSpacePos, target) <= 3 
                && Unit.Orders[0].AbilityId == ability)
                return;

            lastOrderFrame = Tyr.Bot.Frame;
            Command = new ActionRawUnitCommand();
            Command.AbilityId = ability;
            Command.TargetWorldSpacePos = target;
            Command.UnitTags.Add(Unit.Tag);
        }

        internal void ArchonMerge(Agent agent)
        {
            // Short delay between orders to prevent order spam.
            if (Tyr.Bot.Frame - lastOrderFrame <= 5)
                return;

            agent.Command = null;

            lastOrderFrame = Tyr.Bot.Frame;
            System.Console.WriteLine("Merging archon.");
            Command = new ActionRawUnitCommand();
            Command.AbilityId = 1766;
            Command.UnitTags.Add(Unit.Tag);
            Command.UnitTags.Add(agent.Unit.Tag);
        }

        public int GetDamage(Unit target)
        {
            Weapon weaponUsed = null;
            foreach (Weapon weapon in UnitTypes.LookUp[Unit.UnitType].Weapons)
            {
                if (weapon.Type == Weapon.Types.TargetType.Any
                    || (weapon.Type == Weapon.Types.TargetType.Ground && !target.IsFlying)
                    || (weapon.Type == Weapon.Types.TargetType.Air && target.IsFlying))
                    weaponUsed = weapon;
            }
            if (weaponUsed == null)
                return 0;
            return (int)(weaponUsed.Damage - UnitTypes.LookUp[Unit.UnitType].Armor);
        }

        public void Order(int ability, ulong targetTag)
        {
            // Make sure blink doesn't get cancelled.
            if (LastAbility == Abilities.BLINK && Tyr.Bot.Frame - lastOrderFrame <= 20)
                return;
            // Ignore orders that are the same or similar to existing orders.
            if (Unit.Orders.Count != 0 && Unit.Orders[0].TargetUnitTag == targetTag && Unit.Orders[0].AbilityId == ability)
                return;

            LastAbility = ability;

            Command = new ActionRawUnitCommand();
            Command.AbilityId = ability;
            Command.TargetUnitTag = targetTag;
            Command.UnitTags.Add(Unit.Tag);
        }

        public Unit Unit { get; set; }
        public bool IsBuilding
        {
            get
            {
                return UnitTypes.BuildingTypes.Contains(Unit.UnitType);
            }
        }
        public bool IsWorker
        {
            get
            {
                return UnitTypes.WorkerTypes.Contains(Unit.UnitType);
            }
        }

        public bool IsCombatUnit
        {
            get
            {
                return UnitTypes.CombatUnitTypes.Contains(Unit.UnitType);
            }
        }

        public bool IsResourceCenter
        {
            get
            {
                return UnitTypes.ResourceCenters.Contains(Unit.UnitType);
            }
        }

        public bool IsProductionStructure
        {
            get
            {
                return UnitTypes.ProductionStructures.Contains(Unit.UnitType);
            }
        }

        public float DistanceSq(Point2D pos)
        {
            return SC2Util.DistanceSq(Unit.Pos, pos);
        }

        public float DistanceSq(Point pos)
        {
            return SC2Util.DistanceSq(Unit.Pos, pos);
        }

        public float DistanceSq(Unit unit)
        {
            return SC2Util.DistanceSq(Unit.Pos, unit.Pos);
        }

        public float DistanceSq(Agent agent)
        {
            return SC2Util.DistanceSq(Unit.Pos, agent.Unit.Pos);
        }
    }
}
