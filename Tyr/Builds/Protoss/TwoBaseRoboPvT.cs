using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class TwoBaseRoboPvT : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 35, RetreatSize = 12 };
        private TimingAttackTask PokeTask = new TimingAttackTask() { RequiredSize = 10 };
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask() { StartFrame = 600 };
        private DefenseTask defenseTask = new DefenseTask() { ExpandDefenseRadius = 18 };
        private bool Attacking = false;
        public bool DefendMech = false;
        private bool SmellCheese = false;

        public override string Name()
        {
            return "TwoBaseRobo";
        }

        public override void OnStart(Bot tyr)
        {
            tyr.TaskManager.Add(defenseTask);
            tyr.TaskManager.Add(attackTask);
            tyr.TaskManager.Add(WorkerScoutTask);
            tyr.TaskManager.Add(new ObserverScoutTask());
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            tyr.TaskManager.Add(new AdeptKillSquadTask());
            tyr.TaskManager.Add(new ElevatorChaserTask());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new ColloxenController());

            Set += ProtossBuildUtil.Pylons();
            Set += NaturalDefenses();
            Set += BuildStargatesAgainstLifters();
            Set += Nexii();
            foreach (Base b in tyr.BaseManager.Bases)
                if (b != Main && b != Natural)
                    Set += ExpansionDefenses(b);
            Set += MainBuild();
        }

        private BuildList NaturalDefenses()
        {
            BuildList result = new BuildList();

            result.If(() => { return SmellCheese && Count(UnitTypes.ADEPT) >= 10; });
            result.Building(UnitTypes.FORGE);
            result.If(() => { return Attacking; });
            result.Building(UnitTypes.PHOTON_CANNON, Main, MainDefensePos, 4);

            return result;
        }

        private BuildList BuildStargatesAgainstLifters()
        {
            BuildList result = new BuildList();

            result.If(() => { return Lifting.Get().Detected; });
            result += new BuildingStep(UnitTypes.GATEWAY);
            result += new BuildingStep(UnitTypes.ASSIMILATOR);
            result += new BuildingStep(UnitTypes.CYBERNETICS_CORE);
            result += new BuildingStep(UnitTypes.ASSIMILATOR);
            result += new BuildingStep(UnitTypes.STARGATE, 2);

            return result;
        }

        private BuildList Nexii()
        {
            BuildList result = new BuildList();

            result.If(() => { return Count(UnitTypes.GATEWAY) >= 2 && (!SmellCheese || Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) >= 8); });
            result.Building(UnitTypes.NEXUS, 2);
            result.If(() => { return Attacking; });
            result.Building(UnitTypes.NEXUS);

            return result;
        }

        private BuildList ExpansionDefenses(Base expansion)
        {
            BuildList result = new BuildList();

            result.If(() => { return expansion.Owner == Bot.Bot.PlayerId; });
            result.Building(UnitTypes.FORGE);
            result.Building(UnitTypes.PYLON, expansion);
            result.If(() => { return Completed(expansion, UnitTypes.PYLON) > 0; });
            result.Building(UnitTypes.PHOTON_CANNON, expansion);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            Point2D shieldBatteryPos = Bot.Bot.MapAnalyzer.Walk(NaturalDefensePos, Bot.Bot.MapAnalyzer.EnemyDistances, 3);

            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos, () => { return !SmellCheese; });
            result.Building(UnitTypes.PYLON, 2, () => { return Minerals() >= 350; });
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.GATEWAY, () => { return SmellCheese; });
            result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos, 2, () => { return SmellCheese; });
            result.If(() => { return Count(UnitTypes.NEXUS) >= 2; });
            result.Building(UnitTypes.GATEWAY, () => { return !SmellCheese; });
            result.Building(UnitTypes.SHIELD_BATTERY, Natural, shieldBatteryPos, () => { return !SmellCheese; });
            result.Building(UnitTypes.ASSIMILATOR, 3);
            result.If(() => { return Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) >= 10; });
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.PYLON, 2);
            result.Building(UnitTypes.ROBOTICS_BAY, () => { return !DefendMech; });
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => { return Count(UnitTypes.COLOSUS) > 0; });
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.PYLON, Natural);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Count(UnitTypes.ZEALOT) + Count(UnitTypes.ADEPT) + Count(UnitTypes.STALKER) + Count(UnitTypes.COLOSUS) + Count(UnitTypes.IMMORTAL) >= attackTask.RequiredSize)
                Attacking = true;

            if (FourRax.Get().Detected
                || (tyr.Frame >= 22.4 * 85 && !tyr.EnemyStrategyAnalyzer.NoProxyTerranConfirmed && tyr.TargetManager.PotentialEnemyStartLocations.Count == 1)
                || ReaperRush.Get().Detected)
            {
                SmellCheese = true;
                defenseTask.MainDefenseRadius = 21;
            }

            if (!TerranTech.Get().DetectedPreviously
                && (ReaperRush.Get().DetectedPreviously || FourRax.Get().DetectedPreviously))
                SmellCheese = true;

            if (tyr.EnemyRace == Race.Zerg && !PokeTask.Stopped)
            {
                if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ZERGLING) >= 10
                    || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.SPINE_CRAWLER) >= 2
                    || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.ROACH) >= 5)
                {
                    PokeTask.Stopped = true;
                    PokeTask.Clear();
                }
            }

            if (SmellCheese)
            {
                attackTask.RequiredSize = 15;
                attackTask.RetreatSize = 8;
            }

            if (SmellCheese && Completed(UnitTypes.ADEPT) < 8)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.NEXUS
                        && agent.Unit.BuildProgress < 0.99)
                        agent.Order(Abilities.CANCEL);
            }

            if (Bio.Get().Detected)
                DefendMech = false;
            else if (Mech.Get().Detected)
                DefendMech = true;
            else if (Bio.Get().DetectedPreviously)
                DefendMech = false;
            else if (Mech.Get().DetectedPreviously)
                DefendMech = true;
            else DefendMech = false;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (Count(UnitTypes.PROBE) >= 24
                && Count(UnitTypes.NEXUS) < 2
                && Minerals() < 450)
                return;
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 44 - Completed(UnitTypes.ASSIMILATOR)
                && (Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.PROBE) < 18 + 2 * Completed(UnitTypes.ASSIMILATOR)))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (SmellCheese)
                {
                    if (Count(UnitTypes.ADEPT) >= 15 && Count(UnitTypes.NEXUS) < 2)
                        return;
                }
                else
                {
                    if (Attacking && Count(UnitTypes.NEXUS) < 3)
                        return;
                }

                if (Completed(UnitTypes.CYBERNETICS_CORE) == 0)
                    return;

                if (Count(UnitTypes.STALKER) + Count(UnitTypes.ADEPT) >= 20
                    && Count(UnitTypes.ROBOTICS_FACILITY) == 0)
                    return;

                if (Count(UnitTypes.STALKER) + Count(UnitTypes.ADEPT) >= 23
                    && Count(UnitTypes.ROBOTICS_BAY) == 0 && !DefendMech)
                    return;

                if (Count(UnitTypes.STALKER) + Count(UnitTypes.ADEPT) >= 25
                    && Count(UnitTypes.COLOSUS) + Count(UnitTypes.IMMORTAL) == 0)
                    return;

                int extraAdepts;
                if (SmellCheese)
                    extraAdepts = 15;
                else
                    extraAdepts = 12;

                if (Minerals() >= 100
                    && Gas() >= 25
                    && (!DefendMech || SmellCheese || Count(UnitTypes.ADEPT) < 2)
                    && Count(UnitTypes.ADEPT) - extraAdepts < Count(UnitTypes.STALKER))
                    agent.Order(922);
                else if (Minerals() >= 125
                    && Gas() >= 50)
                    agent.Order(917);
                return;
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_FACILITY)
            {
                if (Attacking && Count(UnitTypes.NEXUS) < 3)
                    return;
                if (Count(UnitTypes.OBSERVER) == 0
                    && Minerals() >= 25
                    && Gas() >= 75)
                {
                    agent.Order(977);
                }
                else if (Completed(UnitTypes.ROBOTICS_BAY) > 0
                    && Minerals() >= 300
                    && Gas() >= 200
                    && !DefendMech)
                {
                    agent.Order(978);
                }
                else if (Minerals() >= 250
                    && Gas() >= 100
                    && DefendMech)
                {
                    agent.Order(979);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.ROBOTICS_BAY)
            {
                if (Minerals() >= 150
                    && Gas() >= 150
                    && !Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(50)
                    && !DefendMech
                    && Count(UnitTypes.COLOSUS) > 0)
                {
                    agent.Order(1097);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {
                if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100
                    && !DefendMech)
                    agent.Order(1594);
                else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(87)
                     && Minerals() >= 150
                     && Gas() >= 150)
                    agent.Order(1593);
            }
            else if (agent.Unit.UnitType == UnitTypes.STARGATE
                && Minerals() >= 250
                && Gas() >= 150
                && FoodUsed() + 4 <= 200)
                agent.Order(950);
        }
    }
}
