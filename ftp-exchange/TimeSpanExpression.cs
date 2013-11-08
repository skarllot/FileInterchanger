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
