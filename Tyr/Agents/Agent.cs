using SC2APIProtocol;
using System;
using SC2Sharp.Managers;
using SC2Sharp.Util;

namespace SC2Sharp.Agents
{
    public class Agent
    {
        public Unit Unit;
        public Unit PreviousUnit;
        public int LastOrderFrame = 0;
        public int LastAbility = 0;
        public ActionRawUnitCommand Command;

        // For a building, indicates to which base it belongs.
        public Base Base;
        public Point2D AroundLocation;
        public bool Exact;

        public CombatSim.CombatSimulationDecision CombatSimulationDecision = CombatSim.CombatSimulationDecision.None;

        public int CombatSimulationDecisionFrame = 0;

        public Agent(Unit unit)
        {
            Unit = unit;
        }

        public void Order(int ability)
        {
            Order(ability, (Point2D)null);
        }

        internal bool IsCarryingResources()
        {
            foreach (int buff in Unit.BuffIds)
                if (buff >= 271 && buff <= 275)
                    return true;
            return false;
        }

        public void Order(int ability, Point target)
        {
            Order(ability, SC2Util.To2D(target));
        }

        public void Order(int ability, Point2D target)
        {
            Order(ability, target, false);
        }

        public void Order(int ability, Point2D target, bool queue)
        {
            // Make sure blink doesn't get cancelled.
            if (LastAbility == Abilities.BLINK && Bot.Main.Frame - LastOrderFrame <= 20)
                return;
            // Short delay between orders to prevent order spam.
            if (!queue && LastAbility == ability && Bot.Main.Frame - LastOrderFrame <= 5)
                return;

            LastAbility = ability;

            // Ignore orders that are the same or similar to existing orders.
            if (target != null && 
                Unit.Orders.Count != 0 
                && Unit.Orders[0].TargetWorldSpacePos != null 
                && SC2Util.DistanceGrid(Unit.Orders[0].TargetWorldSpacePos, target) <= 3 
                && Unit.Orders[0].AbilityId == ability)
                return;

            LastOrderFrame = Bot.Main.Frame;
            Command = new ActionRawUnitCommand();
            Command.AbilityId = ability;
            Command.TargetWorldSpacePos = target;
            Command.UnitTags.Add(Unit.Tag);
            if (queue)
                Command.QueueCommand = true;
        }

        public bool FleeEnemies(bool returnFire)
        {
            return FleeEnemies(returnFire, 15);
        }

        public bool FleeEnemies(bool returnFire, float fleeDistance)
        {
            Point2D retreatFrom = null;
            Unit retreatUnit = null;
            float dist = fleeDistance * fleeDistance;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType == UnitTypes.CREEP_TUMOR
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                    || enemy.UnitType == UnitTypes.CREEP_TUMOR_QUEEN
                    || enemy.UnitType == UnitTypes.EGG
                    || enemy.UnitType == UnitTypes.OVERLORD
                    || enemy.UnitType == UnitTypes.OVERLORD_COCOON
                    || enemy.UnitType == UnitTypes.OVERSEER
                    || enemy.UnitType == UnitTypes.LARVA
                    || UnitTypes.ChangelingTypes.Contains(enemy.UnitType))
                    continue;
                float newDist = DistanceSq(enemy);
                if (newDist < dist)
                {
                    retreatFrom = SC2Util.To2D(enemy.Pos);
                    retreatUnit = enemy;
                    dist = newDist;
                }
            }
            foreach (UnitLocation enemy in Bot.Main.EnemyMineManager.Mines)
            {
                float newDist = DistanceSq(enemy.Pos);
                if (newDist < dist)
                {
                    retreatFrom = SC2Util.To2D(enemy.Pos);
                    dist = newDist;
                }
            }
            foreach (UnitLocation enemy in Bot.Main.EnemyTankManager.Tanks)
            {
                float newDist = DistanceSq(enemy.Pos);
                if (newDist < dist)
                {
                    retreatFrom = SC2Util.To2D(enemy.Pos);
                    dist = newDist;
                }
            }
            if (retreatFrom != null)
            {
                if (retreatUnit != null && returnFire && Unit.WeaponCooldown == 0)
                {
                    float range = GetRange(retreatUnit);
                    if (DistanceSq(retreatFrom) <= range * range)
                    {
                        Order(Abilities.ATTACK, retreatFrom);
                        return true;
                    }
                }
                Flee(retreatFrom);
                return true;
            }
            return false;
        }

        public void Flee(Point retreatFrom)
        {
            Flee(SC2Util.To2D(retreatFrom));
        }

        public void Flee(Point2D retreatFrom)
        {
            PotentialHelper potential = new PotentialHelper(Unit.Pos, 4);
            potential.From(retreatFrom);
            Point2D fleeTo;
            if (Unit.IsFlying)
                fleeTo = SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation);
            else
                fleeTo = Bot.Main.MapAnalyzer.Walk(SC2Util.To2D(Unit.Pos), Bot.Main.MapAnalyzer.MainDistances, 6);
            potential.To(fleeTo);
            Order(Abilities.MOVE, potential.Get());
        }

        public void Flee(Point retreatFrom, Point2D retreatTo)
        {
            Flee(SC2Util.To2D(retreatFrom), retreatTo);
        }

        public void Flee(Point2D retreatFrom, Point2D retreatTo)
        {
            PotentialHelper potential = new PotentialHelper(Unit.Pos, 4);
            potential.From(retreatFrom);
            potential.To(retreatTo);
            Order(Abilities.MOVE, potential.Get());
        }

        public bool CanAttackAir()
        {
            return UnitTypes.CanAttackAir(Unit.UnitType);
        }

        public bool CanAttackGround()
        {
            return UnitTypes.CanAttackGround(Unit.UnitType);
        }

        internal void ArchonMerge(Agent agent)
        {
            // Short delay between orders to prevent order spam.
            if (Bot.Main.Frame - LastOrderFrame <= 5)
                return;

            agent.Command = null;

            LastOrderFrame = Bot.Main.Frame;
            Command = new ActionRawUnitCommand();
            Command.AbilityId = 1766;
            Command.UnitTags.Add(Unit.Tag);
            Command.UnitTags.Add(agent.Unit.Tag);
        }

        public Agent GetAddOn()
        {
            if (!Bot.Main.UnitManager.Agents.ContainsKey(Unit.AddOnTag))
                return null;
            return Bot.Main.UnitManager.Agents[Unit.AddOnTag];
        }

        public uint CurrentAbility()
        {
            if (Unit.Orders == null || Unit.Orders.Count == 0)
                return 0;
            else
                return Unit.Orders[0].AbilityId;
        }

        public Weapon GetWeapon(Unit target)
        {
            foreach (Weapon weapon in UnitTypes.LookUp[Unit.UnitType].Weapons)
            {
                if (weapon.Type == Weapon.Types.TargetType.Any
                    || (weapon.Type == Weapon.Types.TargetType.Ground && !target.IsFlying)
                    || (weapon.Type == Weapon.Types.TargetType.Air && target.IsFlying))
                    return weapon;
            }
            return null;
        }

        public int GetDamage(Unit target)
        {
            Weapon weaponUsed = GetWeapon(target);
            if (weaponUsed == null)
                return 0;
            return (int)(weaponUsed.Damage - UnitTypes.LookUp[Unit.UnitType].Armor);
        }

        private float GetRange(Unit target)
        {
            Weapon weaponUsed = GetWeapon(target);
            if (weaponUsed == null)
                return -1;
            return weaponUsed.Range;
        }

        public void Order(int ability, ulong targetTag)
        {
            // Make sure blink doesn't get cancelled.
            if (LastAbility == Abilities.BLINK && Bot.Main.Frame - LastOrderFrame <= 20)
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

        public Point2D Toward(Agent target, float magnitude)
        {
            return Toward(target.Unit, magnitude);
        }

        public Point2D Toward(Unit target, float magnitude)
        {
            return Toward(target.Pos, magnitude);
        }

        public Point2D Toward(Point target, float magnitude)
        {
            return Toward(SC2Util.To2D(target), magnitude);
        }

        public Point2D Toward(Point2D target, float magnitude)
        {
            PotentialHelper helper = new PotentialHelper(Unit.Pos);
            helper.Magnitude = magnitude;
            helper.To(target);
            return helper.Get();
        }

        public Point2D From(Agent target, float magnitude)
        {
            return From(target.Unit, magnitude);
        }

        public Point2D From(Unit target, float magnitude)
        {
            return From(target.Pos, magnitude);
        }

        public Point2D From(Point target, float magnitude)
        {
            return From(SC2Util.To2D(target), magnitude);
        }

        public Point2D From(Point2D target, float magnitude)
        {
            PotentialHelper helper = new PotentialHelper(Unit.Pos);
            helper.Magnitude = magnitude;
            helper.From(target);
            return helper.Get();
        }
    }
}
