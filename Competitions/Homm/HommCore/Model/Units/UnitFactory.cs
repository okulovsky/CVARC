﻿using System;

namespace HoMM
{
    public static class UnitFactory
    {
        public static Unit FromType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry: return CreateInfantry();
                case UnitType.Ranged: return CreateRanged();
                case UnitType.Cavalry: return CreateCavalry();
                case UnitType.Militia: return CreateMilitia();
                default: throw new ArgumentException("Unsupported unit type!");
            }
        }
        public static Unit CreateInfantry()
        {
            return new Unit("Infantryman", UnitType.Infantry);
        }
        public static Unit CreateRanged()
        {
            return new Unit("Archer", UnitType.Ranged);
        }
        public static Unit CreateCavalry()
        {
            return new Unit("Horseman", UnitType.Cavalry);
        }

        public static Unit CreateMilitia()
        {
            return new Unit("Militiaman", UnitType.Militia);
        }
    }
}
