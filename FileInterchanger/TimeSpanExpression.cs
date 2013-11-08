// TimeSpanExpression.cs
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
using System.Text;

namespace FileInterchanger
{
    struct TimeSpanExpression
    {
        static readonly string[] SIGNS = new string[] { "=", "!=", ">", ">=", "<", "<=" };
        ExpressionSign sign;
        TimeSpan time;

        public ExpressionSign Sign { get { return sign; } set { sign = value; } }
        public TimeSpan TimeSpan { get { return time; } set { time = value; } }

        public static bool Match(TimeSpan a, TimeSpanExpression b)
        {
            switch (b.sign)
            {
                case ExpressionSign.Equal:
                    return (a == b.time);
                case ExpressionSign.NotEqual:
                    return (a != b.time);
                case ExpressionSign.GreaterThan:
                    return (a > b.time);
                case ExpressionSign.GreaterThanOrEqual:
                    return (a >= b.time);
                case ExpressionSign.LessThan:
                    return (a < b.time);
                case ExpressionSign.LessThanOrEqual:
                    return (a <= b.time);
            }

            return false;
        }

        public static bool Match(TimeSpanExpression a, TimeSpan b)
        {
            switch (a.sign)
            {
                case ExpressionSign.Equal:
                    return (a.time == b);
                case ExpressionSign.NotEqual:
                    return (a.time != b);
                case ExpressionSign.GreaterThan:
                    return (a.time > b);
                case ExpressionSign.GreaterThanOrEqual:
                    return (a.time >= b);
                case ExpressionSign.LessThan:
                    return (a.time < b);
                case ExpressionSign.LessThanOrEqual:
                    return (a.time <= b);
            }

            return false;
        }

        public static TimeSpanExpression Parse(string s)
        {
            TimeSpanExpression result = new TimeSpanExpression();
            int idx;
            result.sign = ParseSign(s, out idx);

            result.time = TimeSpan.Parse(s.Substring(idx));
            return result;
        }

        private static ExpressionSign ParseSign(string s, out int indexOfNext)
        {
            indexOfNext = 0;
            if (s.IndexOf(SIGNS[0], 0, 2) == 0)
            {
                indexOfNext = 1;
                return ExpressionSign.Equal;
            }
            else if (s.IndexOf(SIGNS[1], 0, 2) == 0)
            {
                indexOfNext = 2;
                return ExpressionSign.NotEqual;
            }
            else if (s.IndexOf(SIGNS[2], 0, 2) == 0)
            {
                indexOfNext = 1;
                return ExpressionSign.GreaterThan;
            }
            else if (s.IndexOf(SIGNS[3], 0, 2) == 0)
            {
                indexOfNext = 2;
                return ExpressionSign.GreaterThanOrEqual;
            }
            else if (s.IndexOf(SIGNS[4], 0, 2) == 0)
            {
                indexOfNext = 1;
                return ExpressionSign.LessThan;
            }
            else if (s.IndexOf(SIGNS[5], 0, 2) == 0)
            {
                indexOfNext = 2;
                return ExpressionSign.LessThanOrEqual;
            }

            return ExpressionSign.Equal;
        }

        /*public static bool TryParse(string s, out TimeSpanExpression tse)
        {
            tse = new TimeSpanExpression();

            if (string.IsNullOrEmpty(s))
                return false;

            int idx;
            tse.sign = ParseSign(s, out idx);

            return TimeSpan.TryParse(s.Substring(idx), out -tse.time);
        }*/

        public override string ToString()
        {
            string result = string.Empty;
            switch (sign)
            {
                case ExpressionSign.Equal:
                    result = SIGNS[0];
                    break;
                case ExpressionSign.NotEqual:
                    result = SIGNS[1];
                    break;
                case ExpressionSign.GreaterThan:
                    result = SIGNS[2];
                    break;
                case ExpressionSign.GreaterThanOrEqual:
                    result = SIGNS[3];
                    break;
                case ExpressionSign.LessThan:
                    result = SIGNS[4];
                    break;
                case ExpressionSign.LessThanOrEqual:
                    result = SIGNS[5];
                    break;
            }

            return result + time.ToString();
        }
    }

    enum ExpressionSign
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }
}
