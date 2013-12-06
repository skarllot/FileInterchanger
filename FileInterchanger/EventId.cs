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

using System;

namespace FileInterchanger
{
    enum EventId : int
    {
        // Service related codes (0-9)
        ServiceStateChanged = 0,
        ServiceInsufficientWaitTime = 1,
        EventLogCreated = 2,

        // Configuration file related codes (10-19)
        ConfigFileAllSectionsInvalid = 10,
        ConfigFileReloadInvalid = 11,
        ConfigFileReloaded = 12,
        ConfigFileLoadError = 13,
        ConfigFileParseError = 14,
        ConfigFileNotFound = 15,
        HostnameInvalid = 16,

        // CIFS access related codes (20-29)
        CredentialConflict = 20,
        CredentialError = 21,

        // WinSCP related codes (30-39)
        SessionOpenError = 30,

        // Interchange related codes (40-49)
        InterchangeCompleted = 40,
        InterchangeCompletedEmpty = 41,
        InterchangeCompletedWithErrors = 42,
        InterchangeCompletedEmptyWithErrors = 43,

        // FTP to local interchange related codes (50-59)
        TransfLocalFileNotExists = 50,
        TransfLocalEmptyDownload = 51,
        TransfLocalMultFiles = 52,
        TransfLocalFilesNotMatch = 53,
        TransfLocalDownloadError = 54,

        // Local to FTP interchange related codes (60-69)
        TransfRemoteFileNotExists = 60,
        TransfRemoteEmptyUpload = 61,
        TransfRemoteMultFiles = 62,
        TransfRemoteFilesNotMatch = 63,
        TransfRemoteUploadError = 64,

        // Unhandled error code (65535)
        UnexpectedError = UInt16.MaxValue
    }
}
