// ExchangerException.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace FileInterchanger
{
    class InterchangerException : Exception, ISerializable
    {
        EventId errorId;

        private InterchangerException()
        {
        }

        public InterchangerException(EventId errorId, string message)
            : this(errorId, message, null)
        {
        }

        public InterchangerException(EventId errorId, string message, Exception innerException)
            : base(message, innerException)
        {
            this.errorId = errorId;
        }

        public InterchangerException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            errorId = (EventId)info.GetInt32("errorId");
        }

        public EventId ErrorId { get { return errorId; } }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("errorId", (int)errorId);
        }
    }
}
