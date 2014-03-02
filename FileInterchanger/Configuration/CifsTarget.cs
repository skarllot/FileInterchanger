// CifsTarget.cs
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
using YamlDotNet.RepresentationModel;

namespace FileInterchanger.Configuration
{
    class CifsTarget : BasicTarget, IAuthenticable
    {
        const string MY_TYPE = "cifs";
        string credential;

        public CifsTarget() : base(MY_TYPE) { }

        public string Credential { get { return credential; } }

        public override void LoadFromYaml(YamlMappingNode root)
        {
            base.LoadFromYaml(root);
            credential = YamlHelper.GetNodeValue(root, "credential");
        }

        public override object Clone()
        {
            CifsTarget clone = new CifsTarget();
            base.CopyTo(clone);
            clone.credential = this.credential;

            return clone;
        }
    }
}
