using Newtonsoft.Json;

namespace SC2Sharp.CombatSim.Buffs
{
    [JsonObject(MemberSerialization.Fields)]
    public class Charge : Buff
    {
        private CombatUnit Target;
        public Charge(CombatUnit target, int expireFrame)
        {
            ExpireFrame = expireFrame;
            SpeedMultiplier = 2.2f;
            Stun = true;
            Target = target;
        }

        public override void OnFrame(SimulationState state, CombatUnit unit)
        {
            unit.Move(Target.Pos);
            if (unit.DistSq(Target) <= unit.Weapons[0].Range * unit.Weapons[0].Range)
            {
                Target.DealDamage(state, 8, true);

                if (unit.FramesUntilNextAttack == 0)
                    unit.Attack(state, unit.Weapons[0], Target, false);
                ExpireFrame = -1;
            }
        }
    }
}
