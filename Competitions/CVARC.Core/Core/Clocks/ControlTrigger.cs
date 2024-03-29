﻿using System;
using Infrastructure;

namespace CVARC.V2
{
    public class ControlTrigger : RenewableTrigger
    {
        IController controller;
        IActor controllable;
        CommandFilterSet filterSet;

        public ControlTrigger(IController controller, IActor controllable, CommandFilterSet filterSet)
        {
            this.controller = controller;
            this.controllable = controllable;
            this.filterSet = filterSet;
        }

        public override string ToString()
        {
            return "Control trigger " + controllable.ControllerId;
        }

        bool FillBuffer()
        {
            Debugger.Log("Getting sensor data");
            var sensorData = controllable.GetSensorData();
            Debugger.Log("Sending sensor data");
            controller.SendSensorData(sensorData);
            controllable.World.Logger.AddOutgoingSensorData(controllable.ControllerId, sensorData);
            Debugger.Log("Getting command");
            var command = controller.GetCommand();
			if (command == null) return false;
            Debugger.Log("Command accepted in ControlTrigger");
			controllable.World.Logger.AddIncomingCommand(controllable.ControllerId, command);

            filterSet.ProcessCommand(controllable, command);
            if (!filterSet.CommandAvailable)
            {
                throw new Exception("The preprocessor has returned an empty set of commands. Unable to processd");
            }
            return true;
        }


        public override void Act(out double nextTime)
        {
            try
            {
                if (!filterSet.CommandAvailable)
                {
                    Debugger.Log("No command in buffer, trying to get it");
                    if (!FillBuffer())
                    {
                        nextTime = double.PositiveInfinity;
                        return;
                    }
                }
                Debugger.Log("Command available");
                var currentCommand = filterSet.GetNextCommand();
                double duration;
                Debugger.Log("Command goes to robot");
                controllable.ExecuteCommand(currentCommand, out duration);
                nextTime = base.ThisCall + duration;
            }
            catch(Exception e)
            {
                controller.SendError(e);
                nextTime = double.PositiveInfinity;
            }
        }
    }
}
