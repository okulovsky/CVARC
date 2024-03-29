﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIRLab.Mathematics;
using System.Windows.Forms;
using Infrastructure;

namespace CVARC.V2
{
    public class GripperUnit : BaseGripperUnit<IGripperRules>
    {
        public GripperUnit(IActor actor) : base(actor) { }

        public string GrippedObjectId { get; private set; }
        public Func<string> FindDetail { get; set; }
        public Action<string, Frame3D> OnRelease { get; set; }
        public Action<string, Frame3D> OnGrip { get; set; }
        
        void Grip()
        {
            if (GrippedObjectId != null) return;
            var objectId = FindDetail(); 
            if (objectId == null) return;
            GrippedObjectId = objectId;
            actor.World.GetEngine<ICommonEngine>().Attach(
                GrippedObjectId,
                actor.ObjectId,
                GrippingPoint
                );
        }

        void Release()
        {
            if (GrippedObjectId == null) return;
            var engine = actor.World.GetEngine<ICommonEngine>();
            var detailId = GrippedObjectId;
            GrippedObjectId = null;
            var location = engine.GetAbsoluteLocation(detailId);
            if (OnRelease == null)
                engine.Detach(detailId, location);
            else
                OnRelease(detailId, location);
        }

        public override UnitResponse ProcessCommand(object _cmd)
        {
            var cmd = Compatibility.Check<IGripperCommand>(this, _cmd);
            Debugger.Log("Command comes to gripper, "+cmd.GripperCommand.ToString());
            switch (cmd.GripperCommand)
            {
                case GripperAction.No: return UnitResponse.Denied();
                case GripperAction.Grip:
                    Grip();
                    return UnitResponse.Accepted(rules.GrippingTime);
                case GripperAction.Release:
                    Release();
                    return UnitResponse.Accepted(rules.ReleasingTime);
            }
            throw new Exception("Cannot reach this part of code");
        }
    }
}
