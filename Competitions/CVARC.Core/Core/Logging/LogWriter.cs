﻿using AIRLab.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CVARC.V2
{



    public class LogWriter
    {
        
        IWorld world;
        private bool enableLog;
        private string logFile;
        private Infrastructure.GameSettings configuration;
        private object worldState;

        public LogWriter(IWorld world, bool enableLog, string logFile, Infrastructure.GameSettings configuration, object worldState) 
        {
            this.world = world;
            this.enableLog = enableLog;
            this.logFile = logFile;
            this.configuration = configuration;
            this.worldState = worldState;

            log.Add(configuration);
            log.Add(worldState);


            world.Clocks.AddTrigger(new TimerTrigger(LogPositions, world.LoggingPositionTimeInterval));

            world.Scores.ScoresChanged += Scores_ScoresChanged;
            world.Exit += World_Exit;

            
        }

        private void LogPositions(double time)
        {
            if (world.LoggingPositionObjectIds.Count == 0) return;


            Debugger.Log("Logging positions at " + CurrentTime);
            var engine = world.GetEngine<ICommonEngine>();
            var data = world.LoggingPositionObjectIds
                .Where(z=>engine.ContainBody(z))
                .ToDictionary(z => z, z => engine.GetAbsoluteLocation(z));

            var entry = new GameLogEntry
            {
                Time = CurrentTime,
                Type = GameLogEntryType.LocationCorrection,
                LocationCorrection = new LocationCorrectionLogEntry
                {
                    Locations = data
                }
            };
            AddEntry(entry);
        }

        private void World_Exit()
        {
            if (enableLog)
                File.WriteAllLines(logFile, log.Select(z => JsonConvert.SerializeObject(z)).ToArray());
        }

        private void Scores_ScoresChanged(string controllerId, int count, string reason, string type, int total)
        {
            var entry = new GameLogEntry
            {
                Time = CurrentTime,
                Type = GameLogEntryType.ScoresUpdate,
                ScoresUpdate = new ScoresUpdate
                {
                    ControllerId = controllerId,
                    Added = count,
                    Reason = reason,
                    Total = total,
                    Type=type,
                }
            };
            AddEntry(entry);
        }
        

        List<object> log = new List<object>();

        public double CurrentTime { get { return world.Clocks.CurrentTime; } }

        public void AddMethodInvocation(Type engine, string method, params object[] arguments)
        {
             

            var entry = new GameLogEntry
            {
                Time = CurrentTime,
                Type = GameLogEntryType.EngineInvocation,
                EngineInvocation = new EngineInvocationLogEntry
                {
                    EngineName = engine.Name,
                    MethodName = method,
                    Arguments = arguments.Select(z => z.ToString()).ToArray()
                }
            };
            AddEntry(entry);
        }

        private void AddEntry(GameLogEntry entry)
        {
            log.Add(entry);
        }

        public void AddIncomingCommand(string controllerId, object command)
        {
            var entry = new GameLogEntry
            {
                Time = CurrentTime,
                Type = GameLogEntryType.IncomingCommand,
                IncomingCommand = new IncomingCommandLogEntry
                {
                    Command = JObject.FromObject(command),
                    ControllerId = controllerId
                }
            };
            AddEntry(entry);
        }

    }
}
