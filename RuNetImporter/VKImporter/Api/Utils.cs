using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

namespace rcsir.net.vk.importer.api
{
    public class Utils
    {
        // JObject utils
        public static String getStringField(String name, JObject o)
        {
            return o[name] != null ? o[name].ToString() : "";
        }

        public static long getLongField(String name, JObject o, long def = 0)
        {
            long result = def;

            if (o[name] != null)
            {
                string value = o[name].ToString();

                try
                {
                    result = Convert.ToInt64(value);
                }
                catch (OverflowException)
                {
                    Debug.WriteLine("The value is outside the range of the Int64 type: " + value);
                }
                catch (FormatException)
                {
                    Debug.WriteLine("The value is not in a recognizable format: " + value);
                }
            }

            return result;
        }

        public static String getStringField(String category, String name, JObject o)
        {
            if (o[category] != null &&
                o[category][name] != null)
            {
                return o[category][name].ToString();
            }
            return "";
        }

        public static long getLongField(String category, String name, JObject o, long def = 0)
        {
            long result = def;

            if (o[category] != null &&
                o[category][name] != null)
            {
                string value = o[category][name].ToString();

                try
                {
                    result = Convert.ToInt64(value);
                }
                catch (OverflowException)
                {
                    Debug.WriteLine("The value is outside the range of the Int64 type: " + value);
                }
                catch (FormatException)
                {
                    Debug.WriteLine("The value is not in a recognizable format: " + value);
                }
            }

            return result;
        }

        public static String getTextField(String name, JObject o)
        {
            String t = o[name] != null ? o[name].ToString() : "";
            if (t.Length > 0)
            {
                return Regex.Replace(t, @"\r\n?|\n", "");
            }
            return "";
        }

        public static String getStringDateField(String name, JObject o)
        {
            long l = o[name] != null ? o[name].ToObject<long>() : 0;
            DateTime d = timeToDateTime(l);
            return d.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime getDateField(String name, JObject o)
        {
            long l = o[name] != null ? o[name].ToObject<long>() : 0;
            return timeToDateTime(l);
        }

        public static DateTime timeToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }

        public static long getTimeNowMillis()
        {
            return DateTime.Now.Ticks / 10000;
        }

        public static long sleepTime(long timeLastCall)
        {
            long timeToSleep = 339 - (getTimeNowMillis() - timeLastCall);
            if (timeToSleep > 0)
                Thread.Sleep((int)timeToSleep);

            return getTimeNowMillis();
        }

    }
}
