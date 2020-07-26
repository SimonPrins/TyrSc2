using System;
using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.CombatSim.ActionProcessors;
using SC2Sharp.CombatSim.DamageProcessors;

namespace SC2Sharp.CombatSim
{
    public class SimulationUtil
    {
        public static CombatUnit FromUnit(Unit unit, CombatMicro.CombatMicro micro, HashSet<uint> upgrades)
        {
            return FromUnit(unit, new List<CombatMicro.CombatMicro>() { micro }, upgrades);
        }

        public static CombatUnit FromUnit(Unit unit, List<CombatMicro.CombatMicro> combatMicro, HashSet<uint> upgrades)
        {
            CombatUnit result = new CombatUnit();

            UnitTypeData unitType = UnitTypes.LookUp[unit.UnitType];
            result.Health = unit.Health;
            result.HealthMax = (int)unit.HealthMax;
            result.Shield = unit.Shield;
            result.ShieldMax = (int)unit.ShieldMax;
            result.Energy = unit.Energy;
            result.EnergyMax = (int)unit.EnergyMax;
            result.BaseMovementSpeed = unitType.MovementSpeed;
            result.MovementSpeed = unitType.MovementSpeed;
            result.IsAir = unit.IsFlying || unit.UnitType == UnitTypes.COLOSUS;
            result.IsGround = !unit.IsFlying || unit.UnitType == UnitTypes.COLOSUS;
            result.Armor = (int)unitType.Armor;
            result.Owner = unit.Owner == Bot.Main.PlayerId ? 1 : 2;
            result.Pos = new Point(unit.Pos.X, unit.Pos.Y);
            result.Tag = (long)unit.Tag;
            result.UnitType = unit.UnitType;
            foreach (Weapon weapon in unitType.Weapons)
                result.Weapons.Add(FromWeapon(weapon));
            foreach (SC2APIProtocol.Attribute attribute in unitType.Attributes)
                result.Attributes.Add(FromAttribute(attribute));
            result.CombatMicros= combatMicro;

            if (unit.UnitType == UnitTypes.ZEALOT && upgrades.Contains(UpgradeType.Charge))
            {
                result.ActionProcessors.Add(new ZealotChargeProcessor());
                result.BaseMovementSpeed = 2.95f;
                result.MovementSpeed = 2.95f;
            }
            else if (unit.UnitType == UnitTypes.MARAUDER && upgrades.Contains(UpgradeType.ConcussiveShells))
                result.ActionProcessors.Add(new ConcussiveShellsProcessor());
            else if (unit.UnitType == UnitTypes.IMMORTAL)
                result.DamageProcessors.Add(new BarrierDamageProcessor());

            return result;
        }

        public static CombatWeapon FromWeapon(Weapon weapon)
        {
            CombatWeapon result = new CombatWeapon();

            result.Damage = (int)weapon.Damage;
            if (weapon.DamageBonus != null && weapon.DamageBonus.Count > 0)
            {
                if (weapon.DamageBonus.Count >= 2 && Bot.Debug)
                    throw new ArgumentException("Only one bonus damage supported.");
                result.BonusDamage = (int)weapon.DamageBonus[0].Bonus;
                result.BonusDamageAttribute = FromAttribute(weapon.DamageBonus[0].Attribute);
            }
            result.AttacksAir = weapon.Type == Weapon.Types.TargetType.Air || weapon.Type == Weapon.Types.TargetType.Any;
            result.AttacksGround = weapon.Type == Weapon.Types.TargetType.Ground || weapon.Type == Weapon.Types.TargetType.Any;
            result.Speed = weapon.Speed;
            result.Range = (int)weapon.Range;
            result.Attacks = (int)weapon.Attacks;
            if (weapon.Attacks > 1)
            {
                if (weapon.Speed < 1.24)
                    result.FramesUntilSecondaryAttack = 4;
                else
                    result.FramesUntilSecondaryAttack = 5;
            }

            return result;
        }

        public static UnitAttribute FromAttribute(SC2APIProtocol.Attribute attribute)
        {
            if (attribute == SC2APIProtocol.Attribute.Light)
                return UnitAttribute.Light;
            else if (attribute == SC2APIProtocol.Attribute.Armored)
                return UnitAttribute.Armored;
            else if (attribute == SC2APIProtocol.Attribute.Biological)
                return UnitAttribute.Biological;
            else if (attribute == SC2APIProtocol.Attribute.Mechanical)
                return UnitAttribute.Mechanical;
            else if (attribute == SC2APIProtocol.Attribute.Robotic)
                return UnitAttribute.Robotic;
            else if (attribute == SC2APIProtocol.Attribute.Psionic)
                return UnitAttribute.Psionic;
            else if (attribute == SC2APIProtocol.Attribute.Massive)
                return UnitAttribute.Massive;
            else if (attribute == SC2APIProtocol.Attribute.Structure)
                return UnitAttribute.Structure;
            else if (attribute == SC2APIProtocol.Attribute.Hover)
                return UnitAttribute.Hover;
            else if (attribute == SC2APIProtocol.Attribute.Heroic)
                return UnitAttribute.Heroic;
            else if (attribute == SC2APIProtocol.Attribute.Summoned)
                return UnitAttribute.Summoned;
            else if (Bot.Debug)
                throw new ArgumentException("Attribute " + attribute + " not supported.");
            else
                return UnitAttribute.None;
        }
    }
}
