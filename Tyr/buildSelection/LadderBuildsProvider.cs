using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Builds;
using SC2Sharp.Builds.Protoss;
using SC2Sharp.Builds.Terran;
using SC2Sharp.Builds.Zerg;
using SC2Sharp.StrategyAnalysis;

namespace SC2Sharp.buildSelection
{
    class LadderBuildsProvider : BuildsProvider
    {
        public List<Build> GetBuilds(Bot bot, string[] lines)
        {
            List<Build> options;

            if (bot.MyRace == Race.Protoss)
                options = ProtossBuilds(bot);
            else if (bot.MyRace == Race.Zerg)
                options = ZergBuilds(bot);
            else if (bot.MyRace == Race.Terran)
                options = TerranBuilds(bot);
            else
                options = null;

            return options;
        }

        public List<Build> ZergBuilds(Bot bot)
        {
            List<Build> options = new List<Build>();

            if (bot.EnemyRace == Race.Protoss)
            {
                options.Add(new MassZergling() { AllowHydraTransition = true });
                options.Add(new MacroHydra());
            }
            else if (bot.EnemyRace == Race.Terran)
            {
                if (ProxyDetected.Get().DetectedPreviously
                    && Reaper.Get().DetectedPreviously
                    && Hellion.Get().DetectedPreviously
                    && Cyclone.Get().DetectedPreviously
                    && Banshee.Get().DetectedPreviously)
                {
                    options.Add(new Muukzor());
                    return options;
                }
                if (BattlecruiserRush.Get().DetectedPreviously)
                {
                    options.Add(new MacroHydra());
                    return options;
                }
                        
                options.Add(new MassZergling() { AllowHydraTransition = true });
                options.Add(new MacroHydra());
                options.Add(new Muukzor());
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                options.Add(new RoachRavager());
                options.Add(new MacroHydra());
                options.Add(new RushDefense());
            }
            else
            {
                options.Add(new MassZergling() { AllowHydraTransition = true });
                options.Add(new MacroHydra());
                options.Add(new RushDefense());
            }

            return options;
        }

        public List<Build> ProtossBuilds(Bot bot)
        {
            List<Build> options = new List<Build>();

            if (Bot.Debug)
                foreach (Strategy strategy in bot.EnemyStrategyAnalyzer.Strategies)
                    if (strategy.DetectedPreviously)
                        System.Console.WriteLine("Detected previous strategy: " + strategy.Name());

            if (bot.EnemyRace == Race.Terran)
            {
                if (Marine.Get().DetectedPreviously
                    && !Reaper.Get().DetectedPreviously
                    && !Marauder.Get().DetectedPreviously
                    && !Cyclone.Get().DetectedPreviously
                    && !Banshee.Get().DetectedPreviously
                    && !SiegeTank.Get().DetectedPreviously
                    && !Medivac.Get().DetectedPreviously
                    && !Viking.Get().DetectedPreviously
                    && !Raven.Get().DetectedPreviously
                    && !Battlecruiser.Get().DetectedPreviously
                    && !WidowMine.Get().DetectedPreviously
                    && !Hellion.Get().DetectedPreviously
                    && !Thor.Get().DetectedPreviously
                    && !Liberator.Get().DetectedPreviously)
                {
                    // ValinMarineBot
                    options.Add(new NinjaTurtles());
                    return options;
                }
                if (BattlecruiserRush.Get().DetectedPreviously
                    && Thor.Get().DetectedPreviously
                    && WidowMine.Get().DetectedPreviously)
                {
                    // BenBotBC
                    options.Add(new OneBaseStalkerImmortal());
                    return options;
                }
                if (Battlecruiser.Get().DetectedPreviously
                    && !BattlecruiserRush.Get().DetectedPreviously
                    && !Marauder.Get().DetectedPreviously
                    && !Banshee.Get().DetectedPreviously
                    && !Reaper.Get().DetectedPreviously
                    && !Cyclone.Get().DetectedPreviously
                    && !Medivac.Get().DetectedPreviously
                    && !Raven.Get().DetectedPreviously)
                {
                    options.Add(new MassVoidray() { SkipDefenses = true });
                    return options;
                }
                /*
                if (ProxyDetected.Get().DetectedPreviously
                    && !Marauder.Get().DetectedPreviously
                    && Banshee.Get().DetectedPreviously)
                {
                    options.Add(new AntiMicro());
                    return options;
                }
                */
                if (ProxyDetected.Get().DetectedPreviously
                    && Marauder.Get().DetectedPreviously
                    && Banshee.Get().DetectedPreviously)
                {
                    // MicroMachine
                    options.Add(new AntiMicro() { HuntProxies = true, CounterProxyMarauder = false });
                    //options.Add(new NinjaTurtles() { Expand = true });
                    //options.Add(new OneBaseTempest() { DefendingStalker = true });
                    return options;
                }
                if (ProxyDetected.Get().DetectedPreviously
                    && !Banshee.Get().DetectedPreviously)
                {
                    // Strelok
                    //options.Add(new PvTStalkerImmortal() { BuildReaperWall = true, ProxyPylon = false, DelayObserver = true, SendScout = true, MassTanksDetected = MassTank.Get().DetectedPreviously });
                    options.Add(new OneBaseStalkerImmortal() { UseSentry = true });
                    options.Add(new OneBaseTempest());
                    return options;
                }
                if (Marine.Get().DetectedPreviously
                    && Medivac.Get().DetectedPreviously
                    && Viking.Get().DetectedPreviously
                    && Reaper.Get().DetectedPreviously
                    && Raven.Get().DetectedPreviously
                    && !Cyclone.Get().DetectedPreviously
                    && !Marauder.Get().DetectedPreviously
                    && !Banshee.Get().DetectedPreviously)
                {
                    options.Add(new OneBaseStalkerImmortal());
                    //options.Add(new PvTStalkerImmortal() { BuildReaperWall = false, ProxyPylon = false, DelayObserver = true, SendScout = false, MassTanksDetected = true });
                    return options;
                }
                if (Marine.Get().DetectedPreviously
                    && Medivac.Get().DetectedPreviously
                    && Viking.Get().DetectedPreviously
                    && Reaper.Get().DetectedPreviously
                    && !Raven.Get().DetectedPreviously
                    && Marauder.Get().DetectedPreviously
                    && Liberator.Get().DetectedPreviously
                    && !Banshee.Get().DetectedPreviously)
                {
                    // Jensiiibot
                    //options.Add(new PvTStalkerTempest());
                    //options.Add(new Builds.Protoss.WorkerRush() { CounterJensiii = true, BuildStalkers = true });
                    //options.Add(new PvTStalkerImmortal() { BuildReaperWall = true, ProxyPylon = false, DelayObserver = true, MassTanksDetected = MassTank.Get().DetectedPreviously, UseColosus = false });
                    options.Add(new PvTZealotImmortal());
                    return options;
                }
                if (Marine.Get().DetectedPreviously
                    && Medivac.Get().DetectedPreviously
                    && Viking.Get().DetectedPreviously
                    && Reaper.Get().DetectedPreviously
                    && Cyclone.Get().DetectedPreviously
                    && !Marauder.Get().DetectedPreviously
                    && !Liberator.Get().DetectedPreviously
                    && !Banshee.Get().DetectedPreviously)
                {
                    // Rusty
                    options.Add(new OneBaseStalkerImmortal());
                    return options;
                }
                if (Marine.Get().DetectedPreviously
                    && Medivac.Get().DetectedPreviously
                    && Viking.Get().DetectedPreviously
                    && Reaper.Get().DetectedPreviously
                    && !Raven.Get().DetectedPreviously
                    && Cyclone.Get().DetectedPreviously
                    && Marauder.Get().DetectedPreviously
                    && !Liberator.Get().DetectedPreviously
                    && !Banshee.Get().DetectedPreviously
                    && Thor.Get().DetectedPreviously
                    && SiegeTank.Get().DetectedPreviously
                    && MassTank.Get().DetectedPreviously
                    && Hellbat.Get().DetectedPreviously)
                {
                    // MechSweep
                    options.Add(new OneBaseTempest() { RequiredSize = 3 });
                    return options;
                }

                options.Add(new PvTStalkerImmortal() { BuildReaperWall = true, ProxyPylon = false, DelayObserver = true, MassTanksDetected = MassTank.Get().DetectedPreviously, UseColosus = false });
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                if (Bot.Main.OpponentID == "eed44128-f488-4e31-b457-8e55f8a95628")
                {
                    options.Add(new PvZHjax() { CounterRoaches = false, DefendNydus = false });
                    return options;
                }
                options.Add(new PvZHjax());
                return options;
                /*
                if (Lurker.Get().DetectedPreviously)
                {
                    //Kagamine
                    //options.Add(new PvZAdeptIntoVoidray());
                    //options.Add(new WorkerRush());
                    options.Add(new PvZHjax());
                    return options;
                }
                if (Mutalisk.Get().DetectedPreviously
                    && !Lurker.Get().DetectedPreviously)
                {
                    options.Add(new OneBaseStalkerImmortal() { StartZealots = true });
                    return options;
                }
                if (Hydralisk.Get().DetectedPreviously && StrategyAnalysis.ZerglingRush.Get().DetectedPreviously)
                {
                    options.Add(new ZealotRush());
                    return options;
                }
                if (RoachRush.Get().DetectedPreviously || StrategyAnalysis.ZerglingRush.Get().DetectedPreviously)
                {
                    options.Add(new PvZRushDefense());
                    options.Add(new NinjaTurtles());
                    return options;
                }
                if (Roach.Get().DetectedPreviously
                    && Zergling.Get().DetectedPreviously
                    && !Hydralisk.Get().DetectedPreviously)
                {
                    options.Add(new PvZRushDefense());
                    options.Add(new NinjaTurtles());
                    return options;
                }
                if (Queen.Get().DetectedPreviously
                    && Zergling.Get().DetectedPreviously
                    && !Hydralisk.Get().DetectedPreviously
                    && !Roach.Get().DetectedPreviously)
                {
                    options.Add(new NinjaTurtles());
                    return options;
                }
                if (bot.PreviousEnemyStrategies.MassHydra
                    && MassRoach.Get().DetectedPreviously
                    && !Lurker.Get().DetectedPreviously)
                {
                    options.Add(new OneBaseTempest());
                    return options;
                }
                if (!Zergling.Get().DetectedPreviously
                    && !Roach.Get().DetectedPreviously
                    && !Hydralisk.Get().DetectedPreviously
                    && !Queen.Get().DetectedPreviously
                    && !Mutalisk.Get().DetectedPreviously)
                {
                    options.Add(new OneBaseTempest());
                    return options;
                }
                if (!Zergling.Get().DetectedPreviously
                    && Roach.Get().DetectedPreviously
                    && !Hydralisk.Get().DetectedPreviously
                    && Queen.Get().DetectedPreviously
                    && !Mutalisk.Get().DetectedPreviously)
                {
                    options.Add(new OneBaseTempest());
                    return options;
                }
                options.Add(new OneBaseStalkerImmortal() { StartZealots = true });
                */
            }
            else if (bot.EnemyRace == Race.Protoss)
            {
                if (Zealot.Get().DetectedPreviously
                    && VoidRay.Get().DetectedPreviously
                    && !Carrier.Get().DetectedPreviously
                    && !Tempest.Get().DetectedPreviously
                    && !Stalker.Get().DetectedPreviously
                    && !Adept.Get().DetectedPreviously
                    && !Immortal.Get().DetectedPreviously
                    && !StrategyAnalysis.CannonRush.Get().DetectedPreviously)
                {
                    // MavBot3
                    options.Add(new ZealotRush());
                    return options;
                }
                if (SkippedNatural.Get().DetectedPreviously
                    && !AdeptHarass.Get().DetectedPreviously
                    && VoidRay.Get().DetectedPreviously
                    && Immortal.Get().DetectedPreviously)
                {
                    // AdditionalPylons
                    options.Add(new DoubleRoboProxy());
                    return options;
                }
                if (Oracle.Get().DetectedPreviously
                    && ThreeGate.Get().DetectedPreviously
                    && Zealot.Get().DetectedPreviously
                    && !Stalker.Get().DetectedPreviously
                    && !VoidRay.Get().DetectedPreviously
                    && !Immortal.Get().DetectedPreviously)
                {
                    // LuckyBot
                    options.Add(new OneBaseTempest());
                    return options;
                }
                if (Carrier.Get().DetectedPreviously
                    && Collosus.Get().DetectedPreviously
                    && SkyToss.Get().DetectedPreviously
                    && Tempest.Get().DetectedPreviously
                    && !Archon.Get().DetectedPreviously
                    && !HighTemplar.Get().DetectedPreviously)
                {
                    // TheGoldenArmada
                    options.Add(new OneBaseTempest());
                    return options;
                }
                options.Add(new OneBaseStalkerImmortal() { DoubleRobo = true, EarlySentry = true, AggressiveMicro = true });
                return options;
                /*
                if (AdeptHarass.Get().DetectedPreviously
                    && SkyToss.Get().DetectedPreviously
                    && Carrier.Get().DetectedPreviously
                    && HighTemplar.Get().DetectedPreviously
                    && VoidRay.Get().DetectedPreviously
                    && !StrategyAnalysis.CannonRush.Get().DetectedPreviously)
                {
                    options.Add(new ZealotRush());
                    options.Add(new OneBaseStalkerImmortal());
                    return options;
                }
                if (AdeptHarass.Get().DetectedPreviously)
                {
                    // SharpenedEdge
                    options.Add(new OneBaseTempest());
                    options.Add(new Dishwasher());
                    return options;
                }
                if (StrategyAnalysis.CannonRush.Get().DetectedPreviously
                    && Tempest.Get().DetectedPreviously)
                {
                    // ThreeWayLover
                    //options.Add(new MassVoidray() { SkipDefenses = true });
                    options.Add(new OneBaseTempest());
                    return options;
                }
                if (Sentry.Get().DetectedPreviously
                    && Archon.Get().DetectedPreviously
                    && !Stalker.Get().DetectedPreviously
                    && !Zealot.Get().DetectedPreviously)
                {
                    options.Add(new TempestProxy());
                    return options;
                }
                if (Zealot.Get().DetectedPreviously
                    && StrategyAnalysis.CannonRush.Get().DetectedPreviously
                    && Oracle.Get().DetectedPreviously
                    && Phoenix.Get().DetectedPreviously
                    && Stalker.Get().DetectedPreviously
                    && !Immortal.Get().DetectedPreviously)
                {
                    //Gumby
                    options.Add(new NinjaTurtles());
                    return options;
                }
                if (Oracle.Get().DetectedPreviously)
                {
                    options.Add(new OneBaseTempest());
                    return options;
                }
                if (!Stalker.Get().DetectedPreviously
                    && !Zealot.Get().DetectedPreviously
                    && !Sentry.Get().DetectedPreviously)
                {
                    options.Add(new ZealotRush());
                    return options;
                }
                if (Zealot.Get().DetectedPreviously
                    && !Stalker.Get().DetectedPreviously)
                {
                    options.Add(new NinjaTurtles());
                    return options;
                }

                options.Add(new NinjaTurtles());
                options.Add(new PvPMothershipSiege());
            */
            }

            return options;
        }

        public List<Build> TerranBuilds(Bot bot)
        {
            List<Build> options = new List<Build>();

            if (bot.EnemyRace == Race.Terran)
            {
                options.Add(new BunkerRush());
                options.Add(new TankPushProbots());
                options.Add(new MarineRush());
            }
            else if (bot.EnemyRace == Race.Zerg)
            {
                options.Add(new BunkerRush());
                options.Add(new MechTvZ());
                options.Add(new MarineRush());
            }
            else if (bot.EnemyRace == Race.Protoss)
            {
                options.Add(new BunkerRush());
                options.Add(new TankPushTvPProbots());
                options.Add(new MarineRush());
            }
            else
            {
                options.Add(new BunkerRush());
                options.Add(new TankPushProbots());
                options.Add(new MarineRush());
            }

            return options;
        }
    }
}
