﻿using FSO.Server.Framework.Aries;

namespace FSO.Server.Servers.City
{
    public class CityServerContext
    {
        public int ShardId;
        public CityServerConfiguration Config;
        public ISessions Sessions;
    }
}
