// Task.cs
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
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace FileInterchanger.Configuration
{
    class Task : IYamlDeserializable, INameable
    {
        string name;
        string origin;
        string destination;
        Regex filter;
        TimeSpanExpression? timeFilter;
        bool move;
        TimeSpanExpression? cleanup;
        bool cleanupDest;
        bool disableSkipEmpty;

        public string Name { get { return name; } }
        public string Origin { get { return origin; } }
        public string Destination { get { return destination; } }
        public Regex Filter { get { return filter; } }
        public TimeSpanExpression? TimeFilter { get { return timeFilter; } }
        public bool Move { get { return move; } }
        public TimeSpanExpression? Cleanup { get { return cleanup; } }
        public bool CleanupDest { get { return cleanupDest; } }
        public bool DisableSkipEmpty { get { return disableSkipEmpty; } }

        public void LoadFromYaml(YamlMappingNode root)
        {
            string str;

            name = YamlHelper.GetNodeValue(root, "name");
            origin = YamlHelper.GetNodeValue(root, "origin");
            destination = YamlHelper.GetNodeValue(root, "destination");

            str = YamlHelper.GetNodeValue(root, "filter");
            if (!string.IsNullOrWhiteSpace(str))
            {
                try { filter = new Regex(str, RegexOptions.IgnoreCase); }
                catch { throw new Exception("Invalid regular expression for 'filter'"); }
            }

            str = YamlHelper.GetNodeValue(root, "timeFilter");
            if (!string.IsNullOrWhiteSpace(str))
            {
                try { timeFilter = TimeSpanExpression.Parse(str); }
                catch { throw new Exception("Invalid value for 'timeFilter'"); }
            }

            str = YamlHelper.GetNodeValue(root, "move");
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (!bool.TryParse(str, out move))
                    throw new Exception("Invalid value for 'move'");
            }

            str = YamlHelper.GetNodeValue(root, "cleanup");
            if (!string.IsNullOrWhiteSpace(str))
            {
                try { cleanup = TimeSpanExpression.Parse(str); }
                catch { throw new Exception("Invalid value for 'cleanup'"); }
            }

            str = YamlHelper.GetNodeValue(root, "cleanupDest");
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (!bool.TryParse(str, out cleanupDest))
                    throw new Exception("Invalid value for 'cleanupDest'");
            }

            str = YamlHelper.GetNodeValue(root, "disableSkipEmpty");
            if (!string.IsNullOrWhiteSpace(str))
            {
                if (!bool.TryParse(str, out disableSkipEmpty))
                    throw new Exception("Invalid value for 'disableSkipEmpty'");
            }
        }
    }
}
