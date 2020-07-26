using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Managers;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class DTArchon : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 20 };
        private AMoveTask aMoveTask = new AMoveTask() { UnitType = (int)UnitTypes.DARK_TEMPLAR };
        private bool stopRush = true;
        private bool enemyHasDetection = true;

        public override string Name()
        {
            return "DTArchon";
        }

        public override void OnStart(Bot bot)
        {
            bot.TaskManager.Add(new DefenseTask());
            bot.TaskManager.Add(attackTask);
            //bot.TaskManager.Add(aMoveTask);
            bot.TaskManager.Add(new WorkerScoutTask());



            bot.TaskManager.Add(new ArchonMergeTask());
        }

        public override void OnFrame(Bot bot)
        {
            Point2D main = SC2Util.To2D(bot.MapAnalyzer.StartLocation);
            Base mainBase = null;
            foreach (Base b in bot.BaseManager.Bases)
            {
                if (SC2Util.DistanceSq(b.BaseLocation.Pos, main) <= 6 * 6)
                {
                    mainBase = b;
                    break;
                }
            }

            if (!enemyHasDetection)
            {
                foreach (Unit unit in bot.Enemies())
                {
                    if (unit.UnitType == UnitTypes.OVERSEER
                        || unit.UnitType == UnitTypes.SPORE_CRAWLER
                        || unit.UnitType == UnitTypes.OBSERVER
                        || unit.UnitType == UnitTypes.PHOTON_CANNON
                        || unit.UnitType == UnitTypes.RAVEN
                        || unit.UnitType == UnitTypes.MISSILE_TURRET)
                    {
                        enemyHasDetection = true;
                        bot.TaskManager.Add(new ArchonMergeTask());
                        break;
                    }
                }
            }

            if (Count(UnitTypes.DARK_TEMPLAR) >= 2)
                stopRush = true;
            
            if (Count(UnitTypes.NEXUS) < 2
                && Completed(UnitTypes.GATEWAY) >= 3)
                Construct(UnitTypes.NEXUS);

            if (Count(UnitTypes.GATEWAY) > 0
                && Count(UnitTypes.ASSIMILATOR) == 0)
                Construct(UnitTypes.ASSIMILATOR);

            if (Count(UnitTypes.CYBERNETICS_CORE) > 0)
                Construct(UnitTypes.ASSIMILATOR);

            if (Minerals() >= 100
                && FoodUsed() + Count(UnitTypes.NEXUS) + Count(UnitTypes.GATEWAY) * 2 + Count(UnitTypes.ROBOTICS_FACILITY) * 2 >= ExpectedAvailableFood() - 2)
            {
                Construct(UnitTypes.PYLON, mainBase);
            }
            
            if (Minerals() >= 150
                && (Count(UnitTypes.GATEWAY) == 0 || Count(UnitTypes.CYBERNETICS_CORE) > 0)
                && Count(UnitTypes.GATEWAY) < 6
                && (Count(UnitTypes.GATEWAY) < 2 || Count(UnitTypes.DARK_SHRINE) > 0)
                && (Count(UnitTypes.GATEWAY) < 3 || Count(UnitTypes.NEXUS) >= 2))
            {
                Construct(UnitTypes.GATEWAY, mainBase);
            }

            if (Minerals() >= 150
                && Count(UnitTypes.CYBERNETICS_CORE) == 0
                && Completed(UnitTypes.GATEWAY) > 0)
            {
                Construct(UnitTypes.CYBERNETICS_CORE, mainBase);
            }

            if (Minerals() >= 150
                && Gas() >= 100
                && Count(UnitTypes.TWILIGHT_COUNSEL) == 0
                && Completed(UnitTypes.CYBERNETICS_CORE) > 0)
            {
                Construct(UnitTypes.TWILIGHT_COUNSEL, mainBase);
            }

            if (Minerals() >= 150
                && Gas() >= 150
                && Count(UnitTypes.DARK_SHRINE) == 0
                && Completed(UnitTypes.TWILIGHT_COUNSEL) > 0)
            {
                Construct(UnitTypes.DARK_SHRINE, mainBase);
            }
        }

        public override void Produce(Bot bot, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && (Count(UnitTypes.PROBE) < 22 - Completed(UnitTypes.ASSIMILATOR) || Count(UnitTypes.NEXUS) >= 2)
                && Count(UnitTypes.PROBE) < 39 - Completed(UnitTypes.ASSIMILATOR))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Bot.Main.EnemyRace == Race.Zerg)
                {
                    if (!stopRush && Completed(UnitTypes.DARK_SHRINE) > 0 && Count(UnitTypes.DARK_TEMPLAR) < 2)
                    {
                        if (Minerals() >= 125
                            && Gas() >= 125)
                            agent.Order(920);
                    }
                    else if (Minerals() >= 100
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Count(UnitTypes.ADEPT))
                        && (Completed(UnitTypes.DARK_SHRINE) == 0 || Gas() < 125))
                        agent.Order(916);
                    else if (Completed(UnitTypes.DARK_SHRINE)  > 0
                        && Minerals() >= 125
                        && Gas() >= 125)
                        agent.Order(920);
                    else if (Completed(UnitTypes.DARK_SHRINE) == 0
                        && Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && Minerals() >= 100
                        && Gas() >= 25)
                        agent.Order(922);
                }
                else
                {
                    if (!stopRush && Completed(UnitTypes.DARK_SHRINE) > 0 && Count(UnitTypes.DARK_TEMPLAR) < 2)
                    {
                        if (Minerals() >= 125
                            && Gas() >= 125)
                            agent.Order(920);
                    }
                    else if (Minerals() >= 100
                        && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Count(UnitTypes.STALKER))
                        && (Completed(UnitTypes.DARK_SHRINE) == 0 || Gas() < 125))
                        agent.Order(916);
                    else if (Completed(UnitTypes.DARK_SHRINE) > 0
                        && Minerals() >= 125
                        && Gas() >= 125)
                        agent.Order(920);
                    else if (Completed(UnitTypes.DARK_SHRINE) == 0
                        && Completed(UnitTypes.CYBERNETICS_CORE) > 0
                        && Minerals() >= 125
                        && Gas() >= 50)
                        agent.Order(917);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY)
            {
                if (Completed(UnitTypes.ROBOTICS_BAY) > 0
                    && Minerals() >= 300
                    && Gas() >= 200)
                {
                    agent.Order(978);
                }
            }
        }
    }
}
