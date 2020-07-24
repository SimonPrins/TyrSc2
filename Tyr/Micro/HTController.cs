using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class HTController : CustomController
    {
        private List<Storm> Storms = new List<Storm>();
        private Dictionary<ulong, Storm> StormTargets = new Dictionary<ulong, Storm>();
        private int StormUpdateFrame = -1;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.HIGH_TEMPLAR)
                return false;
            if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(52))
                return false;
            if (agent.Unit.Energy < 73)
                return false;

            UpdateStorms();

            if (StormTargets.ContainsKey(agent.Unit.Tag))
            {
                foreach (Effect effect in Bot.Bot.Observation.Observation.RawData.Effects)
                {
                    if (effect.EffectId != 1)
                        continue;
                    if (SC2Util.DistanceSq(effect.Pos[0], StormTargets[agent.Unit.Tag].Location) <= 1)
                    {
                        StormTargets.Remove(agent.Unit.Tag);
                        return false;
                    }
                }

                agent.Order(1036, StormTargets[agent.Unit.Tag].Location);
                return true;
            }

            foreach (Unit unit in Bot.Bot.Enemies())
            {
                if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                bool alreadyStormed = false;
                foreach (Storm storm in Storms)
                {
                    if (SC2Util.DistanceSq(storm.Location, unit.Pos) <= 3 * 3)
                    {
                        alreadyStormed = true;
                        break;
                    }
                }
                if (alreadyStormed)
                    continue;

                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= 11 * 11)
                {
                    int count = 0;
                    foreach (Unit unit2 in Bot.Bot.Enemies())
                    {
                        if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                            continue;

                        if (unit.UnitType == UnitTypes.BROODLING
                            || unit.UnitType == UnitTypes.CREEP_TUMOR
                            || unit.UnitType == UnitTypes.CREEP_TUMOR_BURROWED
                            || unit.UnitType == UnitTypes.CREEP_TUMOR_QUEEN
                            || unit.UnitType == UnitTypes.EGG
                            || unit.UnitType == UnitTypes.LARVA
                            || unit.UnitType == UnitTypes.OVERLORD
                            || unit.UnitType == UnitTypes.OVERLORD_COCOON
                            || unit.UnitType == UnitTypes.OVERSEER
                            || UnitTypes.ChangelingTypes.Contains(unit.UnitType))
                            continue;

                        if (SC2Util.DistanceSq(unit.Pos, unit2.Pos) <= 3 * 3)
                            count += unit.UnitType == UnitTypes.ZERGLING || unit.UnitType == UnitTypes.DRONE ? 1 : 2;
                    }
                    if (count >= 10)
                    {
                        agent.Order(1036, SC2Util.To2D(unit.Pos));
                        Storm storm = new Storm() { Location = SC2Util.To2D(unit.Pos), Frame = Bot.Bot.Frame, Caster = agent.Unit.Tag };
                        Storms.Add(storm);
                        StormTargets.Add(agent.Unit.Tag, storm);
                        return true;
                    }
                }
            }

            return agent.FleeEnemies(false, 9);
        }

        private void UpdateStorms()
        {
            if (StormUpdateFrame >= Bot.Bot.Frame)
                return;
            StormUpdateFrame = Bot.Bot.Frame;
            for (int i = Storms.Count - 1; i >= 0; i--)
                if (Bot.Bot.Frame - Storms[i].Frame >= 70)
                {
                    if (StormTargets.ContainsKey(Storms[i].Caster))
                        StormTargets.Remove(Storms[i].Caster);
                    Storms.RemoveAt(i);
                }
        }
    }

    class Storm
    {
        public Point2D Location;
        public int Frame;
        public ulong Caster;
    }
}
