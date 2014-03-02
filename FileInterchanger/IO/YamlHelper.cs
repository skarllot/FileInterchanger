// YamlHelper.cs
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

using YamlDotNet.RepresentationModel;

namespace FileInterchanger.IO
{
    class YamlHelper
    {
        static YamlScalarNode node = new YamlScalarNode();

        public static string GetNodeValue(YamlMappingNode root, string key)
        {
            node.Value = key;
            if (root.Children.ContainsKey(node))
                return ((YamlScalarNode)root.Children[node]).Value;

            return null;
        }

        public static YamlMappingNode GetRootFromFile(string file)
        {
            System.IO.StreamReader r = new System.IO.StreamReader(file);
            YamlStream s = new YamlStream();
            s.Load(r);
            r.Close();
            r.Dispose();

            if (s.Documents.Count != 1)
                return null;

            return (YamlMappingNode)s.Documents[0].RootNode;
        }

        public static YamlMappingNode[] GetSubTreesFromValue(YamlMappingNode root, string key)
        {
            node.Value = key;
            if (root.Children.ContainsKey(node))
            {
                YamlSequenceNode seqNode = ((YamlSequenceNode)root.Children[node]);
                YamlMappingNode[] subRoots = new YamlMappingNode[seqNode.Children.Count];

                for (int i = 0; i < subRoots.Length; i++)
                    subRoots[i] = (YamlMappingNode)seqNode.Children[i];
                return subRoots;
            }

            return null;
        }
    }
}
