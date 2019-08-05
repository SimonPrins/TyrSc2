using System;

namespace Tyr.CombatSim
{
    public class CombatWeapon
    {
        public int Damage;
        public float Speed;
        public int BonusDamage;
        public UnitAttribute BonusDamageAttribute;
        public int Attacks;
        public int FramesUntilSecondaryAttack;
        public bool AttacksGround;
        public bool AttacksAir;
        public int Range;

        public int GetFramesUntilNextAttack()
        {
            return (int)Math.Round(Speed * 16.2);
        }

        public int GetDamage(CombatUnit target)
        {
            int result = Damage;
            if (BonusDamage > 0 && target.Attributes.Contains(BonusDamageAttribute))
                result += BonusDamage;
            return result;
        }
    }
}
