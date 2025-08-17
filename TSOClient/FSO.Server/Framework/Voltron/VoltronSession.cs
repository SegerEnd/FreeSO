﻿using FSO.Common.Security;
using FSO.Server.Framework.Aries;
using FSO.Server.Protocol.Voltron.Packets;
using Mina.Core.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace FSO.Server.Framework.Voltron
{
    public class VoltronSession : AriesSession, IVoltronSession
    {
        public uint UserId { get; set; }
        public uint AvatarId { get; set; }
        public int AvatarClaimId { get; set; }

        // Archive specific state
        public string DisplayName { get; set; }
        public uint ModerationLevel { get; set; }
        public uint SessionUID { get; set; }

        public bool IsAnonymous
        {
            get
            {
                return AvatarId == 0;
            }
        }

        public string IpAddress
        {
            get
            {
                return IoSession.RemoteEndPoint.ToString();
            }
        }

        public VoltronSession(IoSession ioSession) : base(ioSession){
        }

        public override void Close()
        {
            Write(new ServerByePDU() { }); //try and close the connection safely
            base.Close();
        }

        public bool HasModerationLevel(int threshold)
        {
            return ModerationLevel >= threshold;
        }


        public void DemandAvatar(uint id, AvatarPermissions permission)
        {
            if(AvatarId != id){
                throw new SecurityException("Permission " + permission + " denied for avatar " + AvatarId);
            }
        }

        public void DemandAvatars(IEnumerable<uint> ids, AvatarPermissions permission)
        {
            if (!ids.Contains(AvatarId))
            {
                throw new SecurityException("Permission " + permission + " denied for avatar " + AvatarId);
            }
        }

        public void DemandInternalSystem()
        {
            throw new SecurityException("Voltron sessions are not trusted internal systems");
        }
    }
}
