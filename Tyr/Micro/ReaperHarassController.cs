using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Micro
{
    public class ReaperHarassController : CustomController
    {
        private HashSet<ulong> RegeneratingReapers = new HashSet<ulong>();

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.REAPER)
                return false;

            if (agent.Unit.Health <= 15)
                RegeneratingReapers.Add(agent.Unit.Tag);

            if (agent.Unit.Health >= agent.Unit.HealthMax)
                RegeneratingReapers.Remove(agent.Unit.Tag);

            if (RegeneratingReapers.Contains(agent.Unit.Tag))
            {
                agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
                return true;
            }
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (unit.UnitType != UnitTypes.BUNKER
                    && unit.UnitType != UnitTypes.MARAUDER
                    && unit.UnitType != UnitTypes.QUEEN
                    && unit.UnitType != UnitTypes.SPINE_CRAWLER
                    && unit.UnitType != UnitTypes.ZERGLING
                    && unit.UnitType != UnitTypes.PHOTON_CANNON
                    && unit.UnitType != UnitTypes.STALKER
                    && unit.UnitType != UnitTypes.ZEALOT)
                    continue;
                int dist;
                if (unit.UnitType == UnitTypes.BUNKER || unit.UnitType == UnitTypes.SPINE_CRAWLER || unit.UnitType == UnitTypes.PHOTON_CANNON)
                    dist = 12 * 12;
                else if (unit.UnitType == UnitTypes.ZERGLING || unit.UnitType == UnitTypes.ZEALOT)
                    dist = 5 * 5;
                else
                    dist = 10 * 10;
                if (agent.DistanceSq(unit) < dist)
                {

                    if (unit.UnitType == UnitTypes.ZERGLING && agent.Unit.WeaponCooldown == 0)
                        return false;

                    agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Main.MapAnalyzer.StartLocation));
                    return true;
                }
            }

            if (agent.Unit.WeaponCooldown == 0)
                return false;

            float distance = 12 * 12;
            Unit killTarget = null;
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(unit.UnitType))
                    continue;
                float newDist = agent.DistanceSq(unit);
                if (newDist >= distance)
                    continue;
                distance = newDist;
                killTarget = unit;
            }

            if (killTarget != null)
            {
                if (distance >= 3 * 3)
                    agent.Order(Abilities.MOVE, SC2Util.To2D(killTarget.Pos));
                else
                    agent.Order(Abilities.MOVE, agent.From(killTarget, 4));
                return true;
            }

            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (UnitTypes.CanAttackGround(unit.UnitType)
                    && agent.DistanceSq(unit) <= 5 * 5)
                    return false;
            }

            
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (!UnitTypes.ResourceCenters.Contains(unit.UnitType))
                    continue;
                if (SC2Util.DistanceSq(unit.Pos, Bot.Main.MapAnalyzer.StartLocation) > 4)
                    continue;
                
                PotentialHelper potential = new PotentialHelper(unit.Pos);
                potential.Magnitude = 4;
                potential.From(Bot.Main.MapAnalyzer.StartLocation);
                Point2D targetLoc = potential.Get();

                if (agent.DistanceSq(targetLoc) < 4 * 4)
                    return false;

                agent.Order(Abilities.MOVE, targetLoc);
                return true;
            }

            return false;
        }
    }
}
