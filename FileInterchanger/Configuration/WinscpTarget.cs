// WinscpTarget.cs
//
// Copyright (C) 2014 Fabrício Godoy
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using FileInterchanger.IO;
using System;
using YamlDotNet.RepresentationModel;

namespace FileInterchanger.Configuration
{
    class WinscpTarget : BasicTarget, IAuthenticable
    {
        const string MY_TYPE = "winscp";
        string credential;
        string fingerprint;
        WinSCP.FtpSecure ftpsecure = WinSCP.FtpSecure.None;
        string host;
        int? port = null;
        WinSCP.Protocol protocol = WinSCP.Protocol.Ftp;

        public WinscpTarget() : base(MY_TYPE) { }

        public string Credential { get { return credential; } }
        public string Fingerprint { get { return fingerprint; } }
        public WinSCP.FtpSecure FtpSecure { get { return ftpsecure; } }
        public string Host { get { return host; } }
        public int? Port { get { return port; } }
        public WinSCP.Protocol Protocol { get { return protocol; } }

        public override object Clone()
        {
            WinscpTarget clone = new WinscpTarget();
            base.CopyTo(clone);
            clone.credential = this.credential;
            clone.fingerprint = this.fingerprint;
            clone.ftpsecure = this.ftpsecure;
            clone.host = this.host;
            clone.port = this.port;
            clone.protocol = this.protocol;

            return clone;
        }

        public override void LoadFromYaml(YamlMappingNode root)
        {
            base.LoadFromYaml(root);

            string str;
            credential = YamlHelper.GetNodeValue(root, "credential");
            fingerprint = YamlHelper.GetNodeValue(root, "fingerprint");
            str = YamlHelper.GetNodeValue(root, "ftpsecure");
            if (!string.IsNullOrWhiteSpace(str))
                Enum.TryParse<WinSCP.FtpSecure>(str, true, out ftpsecure);
            host = YamlHelper.GetNodeValue(root, "host");
            str = YamlHelper.GetNodeValue(root, "port");
            if (!string.IsNullOrWhiteSpace(str))
            {
                int a;
                if (int.TryParse(str, out a))
                    port = a;
            }
            str = YamlHelper.GetNodeValue(root, "protocol");
            if (!string.IsNullOrWhiteSpace(str))
                Enum.TryParse<WinSCP.Protocol>(str, true, out protocol);
        }
    }
}
