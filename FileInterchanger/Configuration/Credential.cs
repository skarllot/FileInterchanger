// Credential.cs
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
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace FileInterchanger.Configuration
{
    class Credential : IYamlDeserializable
    {
        string name;
        string user;
        string password;
        string domain;

        public string Name { get { return name; } }
        public string User { get { return user; } }
        public string Password { get { return password; } }
        public string Domain { get { return domain; } }

        public void LoadFromYaml(YamlMappingNode root)
        {
            name = YamlHelper.GetNodeValue(root, "name");
            user = YamlHelper.GetNodeValue(root, "user");
            password = YamlHelper.GetNodeValue(root, "password");
            domain = YamlHelper.GetNodeValue(root, "domain");
        }
    }
}
