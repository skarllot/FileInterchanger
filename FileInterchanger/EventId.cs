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

        // Configuration file related codes (10-29)
        ConfigFileAllSectionsInvalid = 10,
        ConfigFileReloadInvalid = 11,
        ConfigFileReloaded = 12,
        ConfigFileLoadError = 13,
        ConfigFileParseError = 14,
        ConfigFileNotFound = 15,
        HostnameInvalid = 16,
        SyncTargetInvalid = 17,

        // CIFS access related codes (30-39)
        CredentialConflict = 30,
        CredentialError = 31,

        // WinSCP related codes (40-49)
        SessionOpenError = 40,

        // Interchange related codes (50-59)
        InterchangeCompleted = 50,
        InterchangeCompletedEmpty = 51,
        InterchangeCompletedWithErrors = 52,
        InterchangeCompletedEmptyWithErrors = 53,

        // FTP to local interchange related codes (60-69)
        TransfLocalFileNotExists = 60,
        TransfLocalEmptyDownload = 61,
        TransfLocalMultFiles = 62,
        TransfLocalFilesNotMatch = 63,
        TransfLocalDownloadError = 64,

        // Local to FTP interchange related codes (70-79)
        TransfRemoteFileNotExists = 70,
        TransfRemoteEmptyUpload = 71,
        TransfRemoteMultFiles = 72,
        TransfRemoteFilesNotMatch = 73,
        TransfRemoteUploadError = 74,

        // Origin cleanup related codes (80-89)
        CleanupOriginCompleted = 80,
        CleanupOriginCompletedEmpty = 81,
        CleanupOriginCompletedWithErrors = 82,
        CleanupOriginCompletedEmptyWithErrors = 83,

        // Target cleanup related codes (90-99)
        CleanupTargetCompleted = 90,
        CleanupTargetCompletedEmpty = 91,
        CleanupTargetCompletedWithErrors = 92,
        CleanupTargetCompletedEmptyWithErrors = 93,

        // FTP cleanup related codes (100-109)
        CleanupRemoteRemoveError = 100,
        CleanupRemoteRemoveEmpty = 101,
        CleanupRemoteRemoveMultFiles = 102,

        // Local cleanup related codes (110-119)
        CleanupLocalRemoveIoError = 110,
        CleanupLocalRemoveUnauthorized = 111,
        CleanupLocalRemoveUnexpectedError = 119,

        // Unhandled error code (65535)
        UnexpectedError = UInt16.MaxValue
    }
}
