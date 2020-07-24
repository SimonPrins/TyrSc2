using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Micro
{
    public class StalkerAttackNaturalController : CustomController
    {
        private Unit Bunker = null;
        private Point2D EnemyNatural;
        private int UpdateFrame = 0;

        public override bool DetermineAction(Agent agent, Point2D target)
        {
            if (agent.Unit.UnitType != UnitTypes.STALKER)
                return false;
            
            if (agent.Unit.WeaponCooldown == 0)
                return false;

            Unit bunker = GetNaturalBunker();
            if (bunker == null)
                return false;

            PotentialHelper potential = new PotentialHelper(EnemyNatural, 8);
            potential.From(bunker.Pos);
            Point2D attackTarget = potential.Get();
            Point2D minePos = null;
            float dist = 10 * 10;
            foreach (UnitLocation mine in Bot.Bot.EnemyMineManager.Mines)
            {
                float newDist = agent.DistanceSq(mine.Pos);
                if (newDist < dist)
                {
                    dist = newDist;
                    minePos = SC2Util.To2D(mine.Pos);
                }
            }
            potential = new PotentialHelper(agent.Unit.Pos, 4);
            potential.To(attackTarget, 2);
            if (minePos != null)
                potential.To(minePos, 1);

            agent.Order(Abilities.MOVE, potential.Get());

            return true;
        }

        private Unit GetNaturalBunker()
        {
            if (UpdateFrame == Bot.Bot.Frame)
                return Bunker;
            UpdateFrame = Bot.Bot.Frame;
            Bunker = null;

            if (EnemyNatural == null)
                EnemyNatural = Bot.Bot.MapAnalyzer.GetEnemyNatural().Pos;

            if (EnemyNatural == null)
                return null;
            Unit bunker = null;
            Unit cc = null;
            foreach (Unit enemy in Bot.Bot.Enemies())
            {
                if (enemy.UnitType == UnitTypes.BUNKER && enemy.BuildProgress >= 0.99)
                {
                    if (SC2Util.DistanceSq(enemy.Pos, EnemyNatural) <= 15 * 15)
                        bunker = enemy;
                }
                else if (enemy.UnitType == UnitTypes.COMMAND_CENTER || enemy.UnitType == UnitTypes.ORBITAL_COMMAND)
                {
                    if (SC2Util.DistanceSq(enemy.Pos, EnemyNatural) <= 15 * 15)
                        cc = enemy;
                }
            }
            if (cc == null)
                return null;
            Bunker = bunker;
            return bunker;
        }
    }
}
