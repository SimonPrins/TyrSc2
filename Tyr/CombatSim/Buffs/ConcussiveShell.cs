using Newtonsoft.Json;

namespace Tyr.CombatSim.Buffs
{
    [JsonObject(MemberSerialization.Fields)]
    public class ConcussiveShell : Buff
    {
        public ConcussiveShell(int expireFrame)
        {
            ExpireFrame = expireFrame;
            SpeedMultiplier = 0.5f;
        }
    }
}
