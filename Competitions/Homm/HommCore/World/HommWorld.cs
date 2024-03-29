﻿using System;
using System.Linq;
using CVARC.V2;
using HoMM.Engine;
using Infrastructure;
using HoMM.ClientClasses;

namespace HoMM.World
{
    public sealed class HommWorld : World<HommWorldState>
    {
        public IHommEngine HommEngine { get; private set; }
        public HommObjectsCreationHelper HommObjectsCreationHelper { get; private set; }
        public ICommonEngine CommonEngine { get; private set; }
        public Round Round { get; private set; }
        public Random Random { get; private set; }

        public Player[] Players { get; private set; }

        private string[] players;

        public HommWorld(params string[] players) : base()
        {
            this.players = players;
        }

        public override void CreateWorld()
        {
            Debugger.Log("Starting seed: " + WorldState.Seed);

            CommonEngine = GetEngine<ICommonEngine>();
            HommEngine = GetEngine<IHommEngine>();

            Random = new Random(WorldState.Seed);

            var map = WorldState.Debug? new MapGeneratorHelper().CreateDebugMap(Random) : new MapGeneratorHelper().CreateMap(Random);

            Players = players.Select(pid => CreatePlayer(pid, map)).ToArray();
            Round = new Round(map, Players);

            HommObjectsCreationHelper = new HommObjectsCreationHelper(Random, HommEngine);

            var uiEngine = GetEngine<IHommUserInterfaceEngine>();

            var roundConnecter = new RoundToUnityConnecter(HommEngine, CommonEngine, uiEngine, HommObjectsCreationHelper);
            roundConnecter.Connect(Round);

            foreach (var player in Players)
                Round.Update(player, player.Location);

            Clocks.AddTrigger(new TimerTrigger(_ => Round.DailyTick(), HommRules.Current.DailyTickInterval));
        }

        public Location GetRespawnLocation(string controllerId, Map map)
        {
            return controllerId == TwoPlayersId.Left
                ? Location.Zero
                : new Location(map.Size.Y - 1, map.Size.X - 1);
        }

        private Player CreatePlayer(string controllerId, Map map)
        {
            var player = new Player(controllerId, map, Clocks);
            player.Location = GetRespawnLocation(controllerId, map);
            ScoresConnector.Connect(Scores, player);
            return player;
        }

        public bool IsEnemySpawn(Location location, string controllerId)
        {
            if (Players.Length == 1) return false;

            var enemySpawn = GetRespawnLocation(TwoPlayersId.Ids.First(id => id != controllerId), Round.Map);

            return location == enemySpawn;
        }
    }
}
