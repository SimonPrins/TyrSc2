using System.Collections.Generic;
using Tyr.CombatSim.ActionProcessors;
using Tyr.CombatSim.Actions;
using Tyr.CombatSim.Buffs;
using Tyr.CombatSim.DamageProcessors;

namespace Tyr.CombatSim
{
    public class CombatUnit
    {
        public int Owner;
        public float Health;
        public int HealthMax;
        public float Shield;
        public int ShieldMax;
        public float Energy;
        public int EnergyMax;
        public int Armor;
        public int ShieldArmor;
        public float BaseMovementSpeed;
        public float MovementSpeed;
        public long Tag;
        public uint UnitType;
        public bool IsAir;
        public bool IsGround;
        public int FramesUntilNextAttack;
        public CombatUnit PreviousAttackTarget;
        public int AdditionalAttacksRemaining;
        public int SecondaryAttackFrame;
        public CombatWeapon SecondaryAttackWeapon;
        public Point Pos;
        public bool Stunned;
        public List<UnitAttribute> Attributes = new List<UnitAttribute>();
        public List<CombatWeapon> Weapons = new List<CombatWeapon>();
        public List<CombatMicro.CombatMicro> CombatMicros = new List<CombatMicro.CombatMicro>();
        public List<ActionProcessor> ActionProcessors = new List<ActionProcessor>();
        public List<Buff> Buffs = new List<Buff>();
        public List<DamageProcessor> DamageProcessors = new List<DamageProcessor>();

        public float DistSq(CombatUnit enemy)
        {
            return (enemy.Pos.X - Pos.X) * (enemy.Pos.X - Pos.X) + (enemy.Pos.Y - Pos.Y) * (enemy.Pos.Y - Pos.Y);
        }

        public float DistSq(Point pos)
        {
            return (pos.X - Pos.X) * (pos.X - Pos.X) + (pos.Y - Pos.Y) * (pos.Y - Pos.Y);
        }

        public void Move(Point target)
        {
            Move(target, true);
        }

        public void Move(Point target, bool to)
        {
            float speed = MovementSpeed / 16f;
            if (DistSq(target) <= speed * speed && to)
            {
                Pos.X = target.X;
                Pos.Y = target.Y;
                return;
            }
            Point toward = new Point(target.X - Pos.X, target.Y - Pos.Y);
            float length = (float)System.Math.Sqrt(toward.X * toward.X + toward.Y * toward.Y);
            toward.X *= speed / length;
            toward.Y *= speed / length;
            if (to)
            {
                Pos.X += toward.X;
                Pos.Y += toward.Y;
            }
            else
            {
                Pos.X -= toward.X;
                Pos.Y -= toward.Y;
            }
        }

        public void AddBuff(Buff buff)
        {
            Buffs.Add(buff);
            ApplyBuff(buff);
            if (buff is DamageProcessor)
                DamageProcessors.Add((DamageProcessor)buff);
        }

        public void ApplyBuff(Buff buff)
        {
            MovementSpeed *= buff.SpeedMultiplier;
            if (buff.Stun)
                Stunned = true;
        }

        public void RecalculateBuffs()
        {
            MovementSpeed = BaseMovementSpeed;
            Stunned = false;
            foreach (Buff buff in Buffs)
                ApplyBuff(buff);
        }

        public void Attack(SimulationState state, CombatWeapon weapon, CombatUnit target, bool isSecondaryAttack)
        {
            Attack(state, weapon, target, isSecondaryAttack, 1);
        }

        public void Attack(SimulationState state, CombatWeapon weapon, CombatUnit target, bool isSecondaryAttack, float partDamage)
        {
            float singleAttackDamage = weapon.GetDamage(target) * partDamage;
            int framesUntilNextAttack = weapon.GetFramesUntilNextAttack();

            target.DealDamage(state, singleAttackDamage, false);

            if (!isSecondaryAttack)
                FramesUntilNextAttack = framesUntilNextAttack;
            PreviousAttackTarget = target;

            if (weapon.Attacks > 1)
            {
                if (isSecondaryAttack)
                    AdditionalAttacksRemaining--;
                else
                    AdditionalAttacksRemaining = weapon.Attacks - 1;
                SecondaryAttackFrame = state.SimulationFrame + weapon.FramesUntilSecondaryAttack;
                SecondaryAttackWeapon = weapon;
            }
        }

        public void DealDamage(SimulationState state, float damage, bool isSpellDamage)
        {
            foreach (DamageProcessor processor in DamageProcessors)
                damage = processor.Process(state, this, damage);

            if (Shield > 0)
            {
                if (!isSpellDamage)
                    damage -= ShieldArmor;
                Shield -= damage;
                if (Shield < 0)
                {
                    damage = -Shield;
                    Shield = 0;
                }
                else return;
            }

            if (!isSpellDamage)
                damage -= Armor;
            if (damage > 0)
                Health -= damage;

        }

        public Action GetAction(SimulationState simulationState)
        {
            if (Stunned)
                return new DoNothing();
            Action action = ApplyCombatMicros(simulationState);
            return ApplyActionProcessors(simulationState, action);
        }

        private Action ApplyCombatMicros(SimulationState simulationState)
        {
            foreach (CombatMicro.CombatMicro micro in CombatMicros)
            {
                Action action = micro.Act(simulationState, this);
                if (action is DoNothing)
                    continue;
                return action;
            }
            return new DoNothing();
        }

        private Action ApplyActionProcessors(SimulationState simulationState, Action action)
        {
            foreach (ActionProcessor processor in ActionProcessors)
                action = processor.Process(simulationState, this, action);
            return action;
        }

        public void PerformAdditionalAttack(SimulationState state)
        {
            Attack(state, SecondaryAttackWeapon, PreviousAttackTarget, true);
        }

        public CombatWeapon GetWeapon(CombatUnit target)
        {
            CombatWeapon picked = null;
            float damage = 0;
            float singleAttackDamage = 0;
            int framesUntilNextAttack = 0;
            foreach (CombatWeapon weapon in Weapons)
            {
                if (target.IsAir && !weapon.AttacksAir && !target.IsGround)
                    continue;
                if (target.IsGround && !weapon.AttacksGround && !target.IsAir)
                    continue;

                int newSingleAttackDamage = weapon.GetDamage(target);
                int newFramesUntilNextAttack = weapon.GetFramesUntilNextAttack();
                float newDamage = newSingleAttackDamage * weapon.Attacks / newFramesUntilNextAttack;

                if (newDamage < damage)
                    continue;

                picked = weapon;
                damage = newDamage;
                singleAttackDamage = newSingleAttackDamage;
                framesUntilNextAttack = newFramesUntilNextAttack;
            }
            return picked;
        }

        public bool HasAttribute(UnitAttribute attribute)
        {
            foreach (UnitAttribute attr in Attributes)
                if (attr.Equals(attribute))
                    return true;
            return false;
        }
    }
}
