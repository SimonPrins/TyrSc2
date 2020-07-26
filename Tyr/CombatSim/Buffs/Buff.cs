namespace SC2Sharp.CombatSim.Buffs
{
    public abstract class Buff
    {
        public int ExpireFrame;
        public float SpeedMultiplier;
        public bool Stun;
        public virtual void OnFrame(SimulationState state, CombatUnit unit) { }
    }
}
