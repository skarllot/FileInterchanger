// BasicTarget.cs
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
    abstract class BasicTarget : IYamlDeserializable, ICloneable, INameable
    {
        static readonly BasicTarget[] TARGET_CHILDREN = new BasicTarget[] {
            new LocalTarget(), new CifsTarget(), new WinscpTarget()
        };

        string name;
        readonly string type;
        string path;
        string backup;

        protected BasicTarget(string type)
        {
            this.type = type;
        }

        public string Name { get { return name; } }
        public string Path { get { return path; } }
        public string Backup { get { return backup; } }

        public abstract object Clone();

        protected void CopyTo(BasicTarget other)
        {
            other.name = this.name;
            other.path = this.path;
            other.backup = this.backup;
        }

        public static BasicTarget GetInstance(YamlMappingNode root)
        {
            string type = YamlHelper.GetNodeValue(root, "type").ToLower();
            if (string.IsNullOrWhiteSpace(type))
                return null;

            foreach (BasicTarget bt in TARGET_CHILDREN)
            {
                if (bt.type == type)
                {
                    BasicTarget clone = (BasicTarget)bt.Clone();
                    clone.LoadFromYaml(root);
                    return clone;
                }
            }
            return null;
        }

        public virtual void LoadFromYaml(YamlMappingNode root)
        {
            name = YamlHelper.GetNodeValue(root, "name");
            path = YamlHelper.GetNodeValue(root, "path");
            backup = YamlHelper.GetNodeValue(root, "backup");
        }
    }
}
