﻿using FSO.Common;
using FSO.Server.Framework.Aries;

namespace FSO.Server.Servers.Lot
{
    public class LotServerConfiguration : AbstractAriesServerConfig
    {
        public int Max_Lots = 1;

        public string SimNFS;
        public int RingBufferSize = 10;
        public bool Timeout_No_Auth = true;
        public bool LogJobLots = false;

        //Which cities to provide lot hosting for
        public LotServerConfigurationCity[] Cities;

        //How often to reconnect lost connections to city servers and report capacity
        public int CityReportingInterval = 10000;

        public ArchiveConfiguration Archive;
    }

    public class LotServerConfigurationCity
    {
        public int ID;
        public string Host;
    }
}
