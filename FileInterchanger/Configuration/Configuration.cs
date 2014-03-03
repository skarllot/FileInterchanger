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
        Task[] tasks;

        public Version Version { get { return version; } }
        public int RefreshTime { get { return refreshTime; } }
        public int CredentialsCount { get { return credentials.Length; } }
        public int TargetsCount { get { return targets.Length; } }
        public int TasksCount { get { return tasks.Length; } }

        public Credential GetCredential(int index)
        {
            return GetArrayByIndex<Credential>(credentials, index);
        }

        public Credential GetCredential(string name)
        {
            return GetArrayByName<Credential>(credentials, name);
        }

        public BasicTarget GetTarget(int index)
        {
            return GetArrayByIndex<BasicTarget>(targets, index);
        }

        public BasicTarget GetTarget(string name)
        {
            return GetArrayByName<BasicTarget>(targets, name);
        }

        public Task GetTask(int index)
        {
            return GetArrayByIndex<Task>(tasks, index);
        }

        public Task GetTask(string name)
        {
            return GetArrayByName<Task>(tasks, name);
        }

        T GetArrayByIndex<T>(T[] array, int index) where T : class
        {
            if (index < array.Length && index > -1)
                return array[index];
            return null;
        }

        T GetArrayByName<T>(T[] array, string name) where T : class, INameable
        {
            foreach (T item in array)
            {
                if (item.Name == name)
                    return item;
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

            YamlMappingNode[] ytasks = YamlHelper.GetSubTreesFromValue(root, "tasks");
            if (ytasks != null && ytasks.Length > 0)
            {
                tasks = new Task[ytasks.Length];
                for (int i = 0; i < ytasks.Length; i++)
                {
                    tasks[i] = new Task();
                    tasks[i].LoadFromYaml(ytasks[i]);
                }
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
