using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class DTAttackTask : Task
    {
        public static DTAttackTask Task = new DTAttackTask();

        public DTAttackTask() : base(10)
        { }

        public static void Enable()
        {
            Enable(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit && agent.Unit.UnitType == UnitTypes.DARK_TEMPLAR;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (Agent agent in units)
            {
                bool detected = false;
                bool underAttack = false;
                foreach (Unit unit in Bot.Bot.Observation.Observation.RawData.Units)
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
                foreach (BuildingLocation building in Bot.Bot.EnemyManager.EnemyBuildings.Values)
                {
                    if (SC2Util.DistanceSq(building.Pos, agent.Unit.Pos) <= 12 * 12
                        && (building.Type == UnitTypes.MISSILE_TURRET || building.Type == UnitTypes.PHOTON_CANNON || building.Type == UnitTypes.SPORE_CRAWLER))
                        detected = true;
                    if (SC2Util.DistanceSq(building.Pos, agent.Unit.Pos) <= 9 * 9
                        && (building.Type == UnitTypes.SPINE_CRAWLER || building.Type == UnitTypes.PHOTON_CANNON || building.Type == UnitTypes.BUNKER))
                        underAttack = true;
                }
                bool retreat = detected && underAttack;
                if (retreat)
                    agent.Order(Abilities.MOVE, SC2Util.To2D(Bot.Bot.MapAnalyzer.StartLocation));
                else
                    agent.Order(Abilities.ATTACK, tyr.TargetManager.AttackTarget);
            }
        }
    }
}
