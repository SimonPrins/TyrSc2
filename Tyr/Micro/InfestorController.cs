using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Micro
{
    public class InfestorController : CustomController
    {
        public static Dictionary<ulong, ulong> NeuralControllers = new Dictionary<ulong, ulong>();
        public Dictionary<ulong, int> NeuralFrame = new Dictionary<ulong, int>();

        public static Dictionary<ulong, FungalTarget> FungalTargets = new Dictionary<ulong, FungalTarget>();
        int CleanFungalTargetsFrame = 0;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.INFESTOR)
                return false;

            if (Tyr.Bot.Frame != CleanFungalTargetsFrame)
            {
                CleanFungalTargetsFrame = Tyr.Bot.Frame;

                List<ulong> clearTags = new List<ulong>();
                foreach (FungalTarget fungal in FungalTargets.Values)
                    if (Tyr.Bot.Frame - fungal.Frame >= 22.4 * 3)
                        clearTags.Add(fungal.InfestorTag);

                foreach (ulong tag in clearTags)
                    FungalTargets.Remove(tag);
            }


            if (NeuralFrame.ContainsKey(agent.Unit.Tag) && Tyr.Bot.Frame - NeuralFrame[agent.Unit.Tag] < 22)
                return true;

            if (Fungal(agent))
                return true;

            if (NeuralParasite(agent))
                return true;

            Unit closestEnemy = null;
            float distance = agent.Unit.Energy < 70 ? 12 * 12 : 9 * 9;
            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (!UnitTypes.CanAttackGround(unit.UnitType))
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist >= distance)
                    continue;

                distance = newDist;
                closestEnemy = unit;
            }

            if (closestEnemy == null)
            {
                agent.Order(Abilities.MOVE, target);
                return true;
            }

            agent.Order(Abilities.MOVE, agent.From(closestEnemy, 4));
            return true;
        }

        public bool NeuralParasite(Agent agent)
        {
            if (agent.Unit.Energy < 100 || !UpgradeType.LookUp[UpgradeType.NeuralParasite].Done())
                return false;

            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (unit.UnitType != UnitTypes.BATTLECRUISER
                    && unit.UnitType != UnitTypes.TEMPEST
                    && unit.UnitType != UnitTypes.COLLOSUS
                    && unit.UnitType != UnitTypes.MOTHERSHIP
                    && unit.UnitType != UnitTypes.CARRIER)
                    continue;

                if (NeuralControllers.ContainsKey(unit.Tag))
                    continue;

                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= 10 * 10)
                {
                    agent.Order(249, unit.Tag);

                    if (!NeuralControllers.ContainsKey(unit.Tag))
                        NeuralControllers.Add(unit.Tag, agent.Unit.Tag);
                    else NeuralControllers[unit.Tag] = agent.Unit.Tag;

                    if (!NeuralFrame.ContainsKey(agent.Unit.Tag))
                        NeuralFrame.Add(agent.Unit.Tag, Tyr.Bot.Frame);
                    else NeuralFrame[agent.Unit.Tag] = Tyr.Bot.Frame;

                    return true;
                }
            }
            return false;
        }

        public bool Fungal(Agent agent)
        {
            if (agent.Unit.Energy < 75)
                return false;

            if (FungalTargets.ContainsKey(agent.Unit.Tag) && Tyr.Bot.Frame - FungalTargets[agent.Unit.Tag].Frame <= 22)
                return true;

            foreach (UnitLocation mine in Tyr.Bot.EnemyMineManager.Mines)
            {
                bool closeFungal = false;
                foreach (FungalTarget fungal in FungalTargets.Values)
                {
                    if (SC2Util.DistanceSq(mine.Pos, fungal.Pos) <= 3 * 3)
                    {
                        closeFungal = true;
                        break;
                    }
                }
                if (closeFungal)
                    continue;
                if (SC2Util.DistanceSq(mine.Pos, agent.Unit.Pos) <= 10 * 10)
                {
                    CollectionUtil.Add(FungalTargets, agent.Unit.Tag, new FungalTarget() { Pos = SC2Util.To2D(mine.Pos), Frame = Tyr.Bot.Frame, InfestorTag = agent.Unit.Tag });
                    agent.Order(74, SC2Util.To2D(mine.Pos));
                    return true;
                }
            }

            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                if (unit.UnitType == UnitTypes.ZERGLING
                    || unit.UnitType == UnitTypes.BROODLING
                    || unit.UnitType == UnitTypes.OVERLORD)
                    continue;

                bool closeFungal = false;
                foreach (FungalTarget fungal in FungalTargets.Values)
                {
                    if (SC2Util.DistanceSq(unit.Pos, fungal.Pos) <= 4 * 4)
                    {
                        closeFungal = true;
                        break;
                    }
                }
                if (closeFungal)
                    continue;

                if (unit.UnitType == UnitTypes.BANSHEE || unit.UnitType == UnitTypes.TEMPEST || (unit.UnitType == UnitTypes.BATTLECRUISER && unit.Health < 200))
                {
                    CollectionUtil.Add(FungalTargets, agent.Unit.Tag, new FungalTarget() { Pos = SC2Util.To2D(unit.Pos), Frame = Tyr.Bot.Frame, InfestorTag = agent.Unit.Tag });
                    agent.Order(74, SC2Util.To2D(unit.Pos));
                    return true;
                }

                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= 10 * 10)
                {
                    int count = 0;
                    foreach (Unit unit2 in Tyr.Bot.Enemies())
                    {
                        if (UnitTypes.BuildingTypes.Contains(unit.UnitType))
                            continue;

                        if (unit.UnitType == UnitTypes.ZERGLING
                            || unit.UnitType == UnitTypes.BROODLING
                            || unit.UnitType == UnitTypes.OVERLORD)
                            continue;

                        if (SC2Util.DistanceSq(unit.Pos, unit2.Pos) <= 3 * 3)
                            count++;
                    }
                    if (count >= 6)
                    {
                        CollectionUtil.Add(FungalTargets, agent.Unit.Tag, new FungalTarget() { Pos = SC2Util.To2D(unit.Pos), Frame = Tyr.Bot.Frame, InfestorTag = agent.Unit.Tag });
                        agent.Order(74, SC2Util.To2D(unit.Pos));
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class FungalTarget
    {
        public Point2D Pos;
        public int Frame;
        public ulong InfestorTag;
    }
}
