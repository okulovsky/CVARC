﻿using CVARC.V2;
using HoMM.ClientClasses;
using HoMM.Robot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HoMM.Sensors
{
    public class MapSensor : Sensor<List<MapInfo>, IHommRobot>
    {
        public override List<MapInfo> Measure()
        {
            var heroes = Actor.World.Players
                .Select(x => new MapInfo { Hero = new Hero(x.Name, x.Army), Location = x.Location.ToLocationInfo() });

            var objects = Actor.World.Round.Map
                .SelectMany(tile => tile.Objects.Select(x => BuildMapInfo(tile, x)));

            return objects.Union(heroes).ToList();
        }

        private static MapInfo BuildMapInfo(Tile tile, TileObject obj)
        {
            var mapInfo = new MapInfo { Location = tile.Location.ToLocationInfo() };

            Hero owner = null;

            if (obj is CapturableObject)
            {
                var capt = (CapturableObject)obj;
                owner = capt.Owner == null ? null : new Hero(capt.Owner.Name, capt.Owner.Army);
            }

            if (obj is Wall)
            {
                mapInfo.Wall = new ClientClasses.Wall();
                return mapInfo;
            }

            if (obj is Garrison)
            {
                mapInfo.Garrison = new ClientClasses.Garrison(owner, ((Garrison)obj).Army);
                return mapInfo;
            }

            if (obj is NeutralArmy)
            {
                mapInfo.NeutralArmy = new ClientClasses.NeutralArmy(((NeutralArmy)obj).Army);
                return mapInfo;
            }

            if (obj is Mine)
            {
                mapInfo.Mine = new ClientClasses.Mine(((Mine)obj).Resource, owner);
                return mapInfo;
            }

            if (obj is Dwelling)
            {
                var dw = (Dwelling)obj;
                mapInfo.Dwelling = new ClientClasses.Dwelling(dw.Recruit.UnitType, dw.AvailableUnits);
                return mapInfo;
            }
            
            if (obj is ResourcePile)
            {
                var rp = (ResourcePile)obj;
                mapInfo.ResourcePile = new ClientClasses.ResourcePile(rp.Resource, rp.Quantity);
                return mapInfo;
            }

            throw new ArgumentException("unrecognized object");
        }
    }
}
