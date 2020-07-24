using SC2APIProtocol;
using Tyr.Agents;

namespace Tyr.Tasks
{
    class ShieldRegenTask : Task
    {
        public static ShieldRegenTask Task = new ShieldRegenTask();
        
        public static void Enable()
        {
            Enable(Task);
        }

        public ShieldRegenTask() : base(10) { }

        public override bool DoWant(Agent agent)
        {
            return agent.IsCombatUnit && agent.Unit.Shield <= 5 && agent.Unit.ShieldMax > 0 && DefensiveStructureClose(agent);
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public bool DefensiveStructureClose(Agent agent)
        {
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (enemy.UnitType != UnitTypes.BUNKER
                    && enemy.UnitType != UnitTypes.PLANETARY_FORTRESS
                    && enemy.UnitType != UnitTypes.PHOTON_CANNON)
                    continue;

                if (agent.DistanceSq(enemy) <= 9 * 9)
                    return true;
            }
            return false;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (Agent agent in units)
            {
                if (agent.FleeEnemies(false))
                    continue;
                Attack(agent, tyr.TargetManager.AttackTarget);
            }
            for (int i = units.Count - 1; i >= 0; i--)
                if (units[i].Unit.Shield >= units[i].Unit.ShieldMax / 2)
                    ClearAt(i);
        }
    }
}
