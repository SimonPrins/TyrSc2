using System;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class DTAttackTask : Task
    {
        public DTAttackTask() : base(5)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit && agent.Unit.UnitType == UnitTypes.DARK_TEMPLAR;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            foreach (Agent agent in units)
            {
                bool detected = false;
                bool underAttack = false;
                foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                {
                    if (unit.Alliance != Alliance.Enemy)
                        continue;

                    if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= 12 * 12
                        && (unit.UnitType == UnitTypes.MISSILE_TURRET || unit.UnitType == UnitTypes.PHOTON_CANNON || unit.UnitType == UnitTypes.SPORE_CRAWLER
                         || unit.UnitType == UnitTypes.OVERSEER || unit.UnitType == UnitTypes.OBSERVER || unit.UnitType == UnitTypes.RAVEN))
                        detected = true;
                    if (SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos) <= 9 * 9
                        && (UnitTypes.CombatUnitTypes.Contains(unit.UnitType) || unit.UnitType == UnitTypes.SPINE_CRAWLER || unit.UnitType == UnitTypes.PHOTON_CANNON || unit.UnitType == UnitTypes.BUNKER))
                        underAttack = true;
                }
                foreach (Unit building in Tyr.Bot.Enemies())
                {
                    if (SC2Util.DistanceSq(building.Pos, agent.Unit.Pos) <= 12 * 12
                        && (building.UnitType == UnitTypes.MISSILE_TURRET || building.UnitType == UnitTypes.PHOTON_CANNON || building.UnitType == UnitTypes.SPORE_CRAWLER))
                        detected = true;
                    if (SC2Util.DistanceSq(building.Pos, agent.Unit.Pos) <= 9 * 9
                        && (building.UnitType == UnitTypes.SPINE_CRAWLER || building.UnitType == UnitTypes.PHOTON_CANNON || building.UnitType == UnitTypes.BUNKER))
                        underAttack = true;
                }
                bool retreat = detected && underAttack;
                if (retreat)
                    agent.Order(Abilities.MOVE, SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation));
                else
                    agent.Order(Abilities.ATTACK, tyr.TargetManager.AttackTarget);
            }
        }
    }
}
