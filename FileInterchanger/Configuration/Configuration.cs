// Configuration.cs
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
    class Configuration : IYamlDeserializable
    {
        const string CURRENT_VERSION = "0.2";

        Version version;
        int refreshTime;
        Credential[] credentials;
        BasicTarget[] targets;

        public Version Version { get { return version; } }
        public int RefreshTime { get { return refreshTime; } }
        public int CredentialsCount { get { return credentials.Length; } }

        public Credential GetCredential(int index)
        {
            if (index < credentials.Length && index > -1)
                return credentials[index];
            return null;
        }

        public Credential GetCredential(string name)
        {
            foreach (Credential c in credentials)
            {
                if (c.Name == name)
                    return c;
            }

            return null;
        }

        public void LoadFromYaml(YamlMappingNode root)
        {
            string str;

            str = YamlHelper.GetNodeValue(root, "version");
            if (!string.IsNullOrWhiteSpace(str))
                Version.TryParse(str, out version);

            str = YamlHelper.GetNodeValue(root, "refreshTime");
            if (!string.IsNullOrWhiteSpace(str))
                int.TryParse(str, out refreshTime);

            YamlMappingNode[] ycredentials = YamlHelper.GetSubTreesFromValue(root, "credentials");
            if (ycredentials != null && ycredentials.Length > 0)
            {
                credentials = new Credential[ycredentials.Length];
                for (int i = 0; i < ycredentials.Length; i++)
                {
                    credentials[i] = new Credential();
                    credentials[i].LoadFromYaml(ycredentials[i]);
                }
            }

            YamlMappingNode[] ytargets = YamlHelper.GetSubTreesFromValue(root, "targets");
            if (ytargets != null && ytargets.Length > 0)
            {
                targets = new BasicTarget[ytargets.Length];
                for (int i = 0; i < ytargets.Length; i++)
                    targets[i] = BasicTarget.GetInstance(ytargets[i]);
            }
        }

        public static Configuration LoadFromFile(string file)
        {
            YamlMappingNode root = YamlHelper.GetRootFromFile(file);

            Configuration ret = new Configuration();
            ret.LoadFromYaml(root);
            return ret;
        }
    }
}
