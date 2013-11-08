// EventId.cs
//
// Copyright (C) 2013 Fabrício Godoy
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

namespace FileInterchanger
{
    enum EventId : int
    {
        ServiceStateChanged = 0,
        ConfigFileAllSectionsInvalid = 1,
        ConfigFileReloadInvalid = 2,
        ConfigFileReloaded = 3,
        RefreshTiny = 4,
        ConfigFileLoadError = 5,
        ConfigFileParseError = 7,
        EventLogCreated = 8,
        ConfigFileNotFound = 9,
        CredentialConflict = 10,
        CredentialError = 11,
        InterchangeCompleted = 12,
        HostnameInvalid = 13,
        SessionOpenError = 14,
        UnexpectedError = 99
    }
}
