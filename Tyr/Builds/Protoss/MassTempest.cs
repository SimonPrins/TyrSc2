using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class MassTempest : Build
    {
        public int RequiredSize = 3;
        public bool Expand = false;
        private WallInCreator WallIn;
        private WallInCreator MainWallIn;

        public override string Name()
        {
            return "MassTempest";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new StutterController());

            if (WallIn == null)
            {
                WallIn = new WallInCreator();
                WallIn.CreateFullNatural(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                WallIn.ReserveSpace();
            }
            if (MainWallIn == null)
            {
                MainWallIn = new WallInCreator();
                MainWallIn.Create(new List<uint>() { UnitTypes.GATEWAY, UnitTypes.PYLON, UnitTypes.GATEWAY });
                MainWallIn.ReserveSpace();
            }

            Set += ProtossBuildUtil.Pylons();
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 18);
            result.Train(UnitTypes.PROBE, 30, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.STALKER, 1, () => Expand);
            result.Train(UnitTypes.TEMPEST);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            if (Expand)
            {
                result.Building(UnitTypes.PYLON, Natural, WallIn.Wall[2].Pos, true);
                result.Building(UnitTypes.GATEWAY, Natural, WallIn.Wall[0].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.ASSIMILATOR);
                result.Building(UnitTypes.CYBERNETICS_CORE, Natural, WallIn.Wall[1].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
            }
            else
            {
                result.Building(UnitTypes.PYLON, Main, MainWallIn.Wall[1].Pos, true);
                result.If(() => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.GATEWAY, Main, MainWallIn.Wall[0].Pos, true);
                result.Building(UnitTypes.ASSIMILATOR);
                result.Building(UnitTypes.CYBERNETICS_CORE, Main, MainWallIn.Wall[2].Pos, true);
            }
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE);
            if (!Expand)
            {
                result.Building(UnitTypes.FORGE);
                result.Building(UnitTypes.PHOTON_CANNON, Main, MainWallIn.Wall[0].Pos, () => Completed(UnitTypes.FORGE) > 0);
                result.Building(UnitTypes.PHOTON_CANNON, Main, MainWallIn.Wall[2].Pos, () => Completed(UnitTypes.FORGE) > 0);
            }
            else
            {
                result.Building(UnitTypes.FORGE, Natural, WallIn.Wall[3].Pos, true, () => Completed(UnitTypes.PYLON) > 0);
                result.Building(UnitTypes.PHOTON_CANNON, Natural, new PotentialHelper(NaturalDefensePos, 2).To(Natural.BaseLocation.Pos).Get(), 2, () => Completed(UnitTypes.FORGE) > 0);
            }
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.TEMPEST) > 0);
            result.Building(UnitTypes.PHOTON_CANNON, 2, () => TotalEnemyCount(UnitTypes.BANSHEE) > 0 && Minerals() >= 500);
            if (Expand)
            {
                result.Building(UnitTypes.NEXUS, () => Minerals() >= 500 && Count(UnitTypes.TEMPEST) > 0 && Count(UnitTypes.STARGATE) >= 2);
                result.Building(UnitTypes.ASSIMILATOR, 2, () => Count(UnitTypes.NEXUS) >= 2);
                result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.TEMPEST) >= Completed(UnitTypes.TEMPEST) + 2);
            }

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            TimingAttackTask.Task.RequiredSize = RequiredSize;
            TimingAttackTask.Task.UnitType = UnitTypes.TEMPEST;

            bot.buildingPlacer.BuildCompact = true;
            
            float Z = 0;
            foreach (Agent agent in bot.Units())
                Z = System.Math.Max(Z, agent.Unit.Pos.Z);
            bot.DrawSphere(new SC2APIProtocol.Point() { X = WallIn.Wall[3].Pos.X, Y = WallIn.Wall[3].Pos.Y, Z = Z });

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 40;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 100;

            if (bot.Frame % 224 == 0)
            {
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.GATEWAY)
                    {
                        agent.Order(Abilities.MOVE, Natural.BaseLocation.Pos);
                        break;
                    }
            }
        }
    }
}
