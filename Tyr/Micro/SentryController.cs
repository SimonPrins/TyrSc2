using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Micro
{
    public class SentryController : CustomController
    {
        public bool FleeEnemies = true;
        public bool UseHallucaination = false;
        private int LastGuardianShieldFrame = -1000;
        private int LastHallucinationFrame = -1000;
        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.SENTRY)
                return false;
            if (GuardianShield(agent))
                return true;
            if (HallucinateZealots(agent))
                return true;
            if (HallucinateArchons(agent))
                return true;

            if (FleeEnemies && agent.FleeEnemies(false, 8))
                return true;

            return false;
        }

        private bool HallucinateZealots(Agent agent)
        {
            if (Bot.Main.Frame - LastHallucinationFrame < 22.4 || Bot.Main.Frame == LastHallucinationFrame)
                return false;
            if (!UseHallucaination)
                return false;
            if (agent.Unit.Energy < 75)
                return false;
            int zealots = Bot.Main.UnitManager.Completed(UnitTypes.ZEALOT);
            if (zealots >= 20)
                return false;
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) >= 12 * 12)
                    continue;
                if (unit.UnitType != UnitTypes.SIEGE_TANK
                    && unit.UnitType != UnitTypes.SIEGE_TANK_SIEGED)
                    continue;

                agent.Order(164);
                Bot.Main.UnitManager.UnitTraining(UnitTypes.ZEALOT);
                Bot.Main.UnitManager.UnitTraining(UnitTypes.ZEALOT);
                LastHallucinationFrame = Bot.Main.Frame;
                return true;
            }
            return false;

        }

        private bool HallucinateArchons(Agent agent)
        {
            if (Bot.Main.Frame - LastHallucinationFrame < 22.4 || Bot.Main.Frame == LastHallucinationFrame)
                return false;
            if (!UseHallucaination)
                return false;
            if (agent.Unit.Energy < 75)
                return false;
            int enemyCount = 0;
            int hallucinations = Bot.Main.UnitManager.Completed(UnitTypes.ARCHON) + Bot.Main.UnitManager.Completed(UnitTypes.COLOSUS);
            if (hallucinations >= 5)
                return false;
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) >= 10 * 10)
                    continue;
                if (UnitTypes.BuildingTypes.Contains(unit.UnitType)
                    && unit.UnitType != UnitTypes.BUNKER
                    && unit.UnitType != UnitTypes.PHOTON_CANNON
                    && unit.UnitType != UnitTypes.SPINE_CRAWLER
                    && unit.UnitType != UnitTypes.SPORE_CRAWLER
                    && unit.UnitType != UnitTypes.MISSILE_TURRET)
                    continue;
                enemyCount++;

                if (enemyCount >= 6)
                {
                    if (Bot.Main.UnitManager.Completed(UnitTypes.COLOSUS) == 0)
                    {
                        agent.Order(148);
                        Bot.Main.UnitManager.UnitTraining(UnitTypes.COLOSUS);

                    } else
                    {
                        agent.Order(146);
                        Bot.Main.UnitManager.UnitTraining(UnitTypes.ARCHON);
                    }
                    LastHallucinationFrame = Bot.Main.Frame;
                    return true;
                }
            }
            return false;

        }

        private bool GuardianShield(Agent agent)
        {
            if (Bot.Main.Frame - LastGuardianShieldFrame < 22.4 * 5)
                return false;
            if (agent.Unit.Energy < 75)
                return false;
            if (agent.Unit.BuffIds.Contains(18))
                return false;
            foreach (Unit unit in Bot.Main.Enemies())
            {
                if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) >= 10 * 10)
                    continue;
                if (UnitTypes.BuildingTypes.Contains(unit.UnitType)
                    && unit.UnitType != UnitTypes.BUNKER
                    && unit.UnitType != UnitTypes.PHOTON_CANNON
                    && unit.UnitType != UnitTypes.SPINE_CRAWLER
                    && unit.UnitType != UnitTypes.SPORE_CRAWLER
                    && unit.UnitType != UnitTypes.MISSILE_TURRET)
                    continue;
                if (UnitTypes.RangedTypes.Contains(unit.UnitType) || unit.UnitType == UnitTypes.INTERCEPTOR)
                {
                    LastGuardianShieldFrame = Bot.Main.Frame;
                    agent.Order(76);
                    return true;
                }
            }
            return false;
        }
    }
}
