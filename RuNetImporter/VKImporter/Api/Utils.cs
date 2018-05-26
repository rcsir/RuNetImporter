using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;
using rcsir.net.vk.importer.api.entity;

namespace rcsir.net.vk.importer.api
{
    public static class Utils
    {
        // JObject utils
        public static String GetStringField(String name, JObject o)
        {
            return o[name] != null ? o[name].ToString() : "";
        }

        public static long GetLongField(String name, JObject o, long def = 0)
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

        public static int GetIntField(String name, JObject o, int def = 0)
        {
            int result = def;

            if (o[name] != null)
            {
                string value = o[name].ToString();

                try
                {
                    result = Convert.ToInt32(value);
                }
                catch (OverflowException)
                {
                    Debug.WriteLine("The value is outside the range of the Int32 type: " + value);
                }
                catch (FormatException)
                {
                    Debug.WriteLine("The value is not in a recognizable format: " + value);
                }
            }

            return result;
        }

        public static String GetStringField(String category, String name, JObject o)
        {
            if (o[category] != null &&
                o[category][name] != null)
            {
                return o[category][name].ToString();
            }
            return "";
        }

        public static long GetLongField(String category, String name, JObject o, long def = 0)
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

        public static String GetTextField(String name, JObject o)
        {
            String t = o[name] != null ? o[name].ToString() : "";
            if (t.Length > 0)
            {
                return Regex.Replace(t, @"\r\n?|\n", "");
            }
            return "";
        }

        // gets a named JToken array from the parent object
        public static JToken[] GetArray(String name, JObject o)
        {
            var obj = o[name] != null ? o[name].ToArray() : null;
            return obj;
        }

        public static String GetStringDateField(String name, JObject o)
        {
            long l = o[name] != null ? o[name].ToObject<long>() : 0;
            DateTime d = TimeToDateTime(l);
            return d.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime GetDateField(String name, JObject o)
        {
            long l = o[name] != null ? o[name].ToObject<long>() : 0;
            return TimeToDateTime(l);
        }

        public static DateTime TimeToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }

        public static long GetTimeNowMillis()
        {
            return DateTime.Now.Ticks / 10000;
        }

        public static long SleepTime(long timeLastCall)
        {
            var timeToSleep = 339 - (GetTimeNowMillis() - timeLastCall);
            if (timeToSleep > 0)
                Thread.Sleep((int)timeToSleep);

            return GetTimeNowMillis();
        }

        // file utils
        // generate file name
        public static string GenerateFileName(String workingDir, IEntity entity, String extension = "txt")
        {
            var fileName = new StringBuilder(workingDir);
            fileName.Append("\\").
                Append(entity.Name()).
                Append(".").Append(extension);
            return fileName.ToString();
        }

        public static string GenerateFileName(String workingDir, decimal id, IEntity entity, String subName = "", String extension = "txt")
        {
            return GenerateFileName(workingDir, id, entity.Name(), subName, extension);
        }

        public static string GenerateFileName(String workingDir, decimal id, String entityName, String subName = "", String extension = "txt")
        {
            var fileName = new StringBuilder(workingDir);
            fileName.Append("\\").Append(Math.Abs(id)).Append("-").Append(entityName);
            if (!String.IsNullOrEmpty(subName))
            {
                fileName.Append("-").Append(subName);
            }
            fileName.Append(".").Append(extension);
            return fileName.ToString();
        }

        // print file header to a stream
        public static void PrintFileHeader(StreamWriter writer, IEntity entity)
        {
            writer.WriteLine(entity.FileHeader());
        }

        // print records to a stream 
        public static void PrintFileContent(StreamWriter writer, IEnumerable<IEntity> entities)
        {
            foreach (var e in entities)
            {
                writer.WriteLine(e.ToFileLine());
            }
        }
        // print single record to a stream
        public static void PrintFileContent(StreamWriter writer, IEntity entity)
        {
            writer.WriteLine(entity.ToFileLine());
        }
    }
}
