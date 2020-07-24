using SC2APIProtocol;
using System;
using System.Threading.Tasks;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class TwoBaseZealotImmortal : Build
    {
        private Point2D OverrideDefenseTarget;
        private bool CannonRush = false;
        private OneBaseStalkerImmortal CannonDefenseBuild = new OneBaseStalkerImmortal() { RequiredSize = 6, AggressiveMicro = true, ExpandCondition = () => Bot.Main.Frame >= 22.4 * 60 * 8 , Scouting = false };
        private bool StalkerRushDetected;
        private OneBaseStalkerImmortal StalkerDefenseBuild = new OneBaseStalkerImmortal() { ObserverScout = true, RequiredSize = 16, UseSentry = true, UsePhoenixScout = false, AggressiveMicro = true, ExpandCondition = () => Bot.Main.Frame >= 22.4 * 60 * 6, Scouting = false };

        public override string Name()
        {
            return "TwoBaseZealotImmortal";
        }

        public override Build OverrideBuild()
        {
            if (!StalkerRushDetected)
            {
                if (EnemyCount(UnitTypes.GATEWAY) >= 3
                    && Bot.Main.Frame <= 22.4 * 60 * 2.5
                    && !Expanded.Get().Detected
                    && TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) == 0)
                {
                    StalkerRushDetected = true;
                    StalkerDefenseBuild.OnStart(Bot.Main);
                    CancelBuilding(UnitTypes.NEXUS);
                }
            }
            if (!CannonRush
                && Bot.Main.Frame <= 22.4 * 60 * 2
                && EnemyCount(UnitTypes.FORGE) + EnemyCount(UnitTypes.PHOTON_CANNON) > 0)
            {
                CannonRush = true;
                CannonDefenseBuild.OnStart(Bot.Main);
            }
            if (!CannonRush)
            {
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.PYLON)
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, Main.BaseLocation.Pos) <= 30 * 30)
                    {
                        CannonRush = true;
                        CannonDefenseBuild.OnStart(Bot.Main);
                        break;
                    }
                    if (SC2Util.DistanceSq(enemy.Pos, Natural.BaseLocation.Pos) <= 25 * 25)
                    {
                        CannonRush = true;
                        CannonDefenseBuild.OnStart(Bot.Main);
                        break;
                    }
                }
            }
            if (CannonRush)
                return CannonDefenseBuild;
            if (StalkerRushDetected)
                return StalkerDefenseBuild;
            return null;
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            WarpPrismTask.Enable();
            ArmyObserverTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            DefenseTask.Enable();
            OverrideDefenseTarget = tyr.MapAnalyzer.Walk(NaturalDefensePos, tyr.MapAnalyzer.EnemyDistances, 15);

            MicroControllers.Add(new SoftLeashController(UnitTypes.ZEALOT, UnitTypes.IMMORTAL, 6));
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new HTController());
            MicroControllers.Add(new ColloxenController());

            Set += ProtossBuildUtil.Pylons(() => Completed(UnitTypes.PYLON) >= 2);
            Set += ExpandBuildings();
            Set += Units();
            Set += MainBuild();
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => Count(UnitTypes.IMMORTAL) > 1);
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 19);
            result.Train(UnitTypes.PROBE, 30, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3);
            result.Train(UnitTypes.WARP_PRISM, 1);
            result.Train(UnitTypes.IMMORTAL, 2);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.IMMORTAL, 12);
            result.Upgrade(UpgradeType.Charge);
            result.Train(UnitTypes.ZEALOT, () => Count(UnitTypes.IMMORTAL) >=  2 && !TimingAttackTask.Task.AttackSent);
            result.Upgrade(UpgradeType.WarpGate);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, () => Count(UnitTypes.PROBE) >= 14);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.PROBE) >= 15);
            result.Building(UnitTypes.GATEWAY, Main, () => Count(UnitTypes.PROBE) >= 16);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.PYLON, Natural);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.GATEWAY, Main, 2, () => Count(UnitTypes.IMMORTAL) > 0);
            result.Building(UnitTypes.GATEWAY, Main, 2, () => Count(UnitTypes.IMMORTAL) >= 2);
            result.If(() => TimingAttackTask.Task.AttackSent && Count(UnitTypes.ZEALOT) >= 12);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.If(() => Count(UnitTypes.IMMORTAL) > 0);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.FORGE);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            WorkerScoutTask.Task.StartFrame = (int)(22.4 * 80);

            tyr.TargetManager.TargetAllBuildings = true;

            tyr.NexusAbilityManager.OnlyChronoPrioritizedUnits = Count(UnitTypes.ROBOTICS_FACILITY) > 0;
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.IMMORTAL].Ability);
            tyr.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.WARP_PRISM].Ability);

            if (TimingAttackTask.Task.AttackSent)
                GasWorkerTask.WorkersPerGas = 3;
            else if (Count(UnitTypes.IMMORTAL) * 100 + Gas() >= 200)
                GasWorkerTask.WorkersPerGas = 0;
            else if (Count(UnitTypes.PROBE) < 15)
                GasWorkerTask.WorkersPerGas = 0;
            else if (Count(UnitTypes.PROBE) < 18 && Count(UnitTypes.PROBE) >= 17)
                GasWorkerTask.WorkersPerGas = Math.Max(1, GasWorkerTask.WorkersPerGas);
            else if (Count(UnitTypes.PROBE) >= 18)
                GasWorkerTask.WorkersPerGas = 2;

            if (!WarpPrismTask.Task.WarpInObjectiveSet()
                && TimingAttackTask.Task.Units.Count > 0
                && Bot.Main.Frame % 22 == 0)
            {

                int warpInsReady = 0;
                RequestQuery query = new RequestQuery();
                foreach (Agent agent in tyr.Units())
                    if (agent.Unit.UnitType == UnitTypes.WARP_GATE)
                        query.Abilities.Add(new RequestQueryAvailableAbilities() { UnitTag = agent.Unit.Tag });
                Task<ResponseQuery> task = tyr.GameConnection.SendQuery(query);
                task.Wait();
                ResponseQuery response = task.Result;
                foreach (ResponseQueryAvailableAbilities availableAbilities in response.Abilities)
                {
                    foreach (AvailableAbility ability in availableAbilities.Abilities)
                    {
                        if (ability.AbilityId == TrainingType.LookUp[UnitTypes.ZEALOT].WarpInAbility)
                            warpInsReady++;
                    }
                }
                if (Completed(UnitTypes.WARP_GATE) == warpInsReady)
                {
                    int desiredZealots = Math.Min(Minerals() / 100, Completed(UnitTypes.WARP_GATE));
                    if (desiredZealots > 0)
                        WarpPrismTask.Task.AddWarpInObjective(UnitTypes.ZEALOT, desiredZealots);
                }
            }


            if (StrategyAnalysis.CannonRush.Get().Detected)
                TimingAttackTask.Task.RequiredSize = 5;
            else if (Completed(UnitTypes.IMMORTAL) >= 2
                && Completed(UnitTypes.WARP_PRISM) > 0)
                TimingAttackTask.Task.RequiredSize = 1;
            else
                TimingAttackTask.Task.RequiredSize = 20;

            TimingAttackTask.Task.RetreatSize = 0;
            
            if (Count(UnitTypes.NEXUS) >= 3)
                IdleTask.Task.OverrideTarget = OverrideDefenseTarget;
            else
                IdleTask.Task.OverrideTarget = null;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 25;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 30;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;
        }
    }
}
