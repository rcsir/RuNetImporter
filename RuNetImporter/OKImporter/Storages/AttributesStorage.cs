using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Storages
{
    internal class AttributesStorage
    {
/*
        private static Dictionary<Attribute, bool> OkAttributes = new Dictionary<Attribute, bool> 
        {
            {new Attribute("Name","name"),true},
            {new Attribute("First Name","first_name"),true},
            {new Attribute("Last Name","last_name"),true},
            {new Attribute("Picture","pic_1"),true},
            {new Attribute("Sex","gender"),true},
            {new Attribute("Locale","locale"),true},
            {new Attribute("Age","age"),true},
            {new Attribute("Status","current_status"),true},
            {new Attribute("Hometown","location"),false},
            {new Attribute("Current Location","current_location"),false},
            {new Attribute("Birthday","birthday"),false},
            {new Attribute("Last online","last_online"),false},
            {new Attribute("Registered date","registered_date"),false},
            {new Attribute("About Me","url_profile"),false},
            {new Attribute("Online Presence","online"),false},            
            {new Attribute("Anonym access","allows_anonym_access"),false}                      
        };
*/
        private static Dictionary<string, string> attributeCommonOkMapping = new Dictionary<string, string>()
        {
            {"name", ""},
            {"middle_name", ""},
            {"birthday", "birthday"},
            {"hometown_location", "location"},
            {"pic_small", "pic_1"},
            {"sex", "gender"},
            {"timezone", ""},
            {"profile_update_time", ""},
            {"religion", ""},
            {"relationship_status", ""},
            {"political", ""},
            {"activities", ""},
            {"interests", ""},
            {"music", ""},
            {"tv", ""},
            {"movies", ""},
            {"books", ""},
            {"quotes",""},
            {"about_me","url_profile"},
            {"online_presence","online"},
            {"website",""}
        };

        private static readonly AttributesDictionary<bool> dialogAttributes = new AttributesDictionary<bool>()
        {
            {false},
            {false},
            {false},
            {false},
            {true},
            {false},
            {true},
            {true},
            {false},
            {false},
            {false},
            {false},
            {true},
            {false},
            {false},
            {false},
            {true},
            {false},
            {false},
            {false},
            {false},
            {false},            
            {false},
            {false},
            {false},
            {false},
            {true}
        };

        private AttributesDictionary<bool> okDialogAttributes;
        
        public AttributesDictionary<bool> OkDialogAttributes { get { return okDialogAttributes; } }

        private AttributesDictionary<string> graphAttributes;

        public AttributesDictionary<string> GraphAttributes { get { return graphAttributes; } }

        public AttributesStorage()
        {
            makeOkDialogAttributes();
            MakeGraphAttributes();
        }

        internal void UpdateAllAttributes(bool[] rows)
        {
            for (var i = 0; i < OkDialogAttributes.Count; i++ ) {
                var sKey = OkDialogAttributes.Keys.ElementAt(i);
                OkDialogAttributes[sKey] = rows[i];
            }
            MakeGraphAttributes();
        }

        internal string CreateRequiredFieldsString()
        {
            string permissionsString = "";
            foreach (KeyValuePair<AttributeUtils.Attribute, bool> kvp in OkDialogAttributes) {
                if (!isNeeded(kvp.Key.value) || !kvp.Value)
                    continue;
                permissionsString += "," + convertKey(kvp.Key.value);
            }
            return permissionsString;
        }

        internal AttributesDictionary<string> CreateVertexAttributes(JToken obj)
        {
            AttributesDictionary<string> attributes = new AttributesDictionary<string>();
            List<AttributeUtils.Attribute> keys = new List<AttributeUtils.Attribute>(attributes.Keys);
            foreach (AttributeUtils.Attribute key in keys) {
                if (!isNeeded(key.value)) {
                    attributes.Remove(key);
                    continue;
                }
                string name = convertKey(key.value);
                if (obj[name] != null) {
                    string value = name != "location" ? obj[name].ToString() : obj[name]["city"] + ", " + obj[name]["country"];
                    attributes[key] = value;
                }
            }
            return attributes;
        }

        private void MakeGraphAttributes()
        {
//            OkDialogAttributes["name"] = true;
            graphAttributes = new AttributesDictionary<string>();
            List<AttributeUtils.Attribute> keys = new List<AttributeUtils.Attribute>(graphAttributes.Keys);
            foreach (AttributeUtils.Attribute key in keys)
                if (!OkDialogAttributes.ContainsKey(key) || !OkDialogAttributes[key])
                    graphAttributes.Remove(key);
        }

        private static string convertKey(string keyValue)
        {
            if (!attributeCommonOkMapping.ContainsKey(keyValue))
                return keyValue;

            return attributeCommonOkMapping[keyValue];
        }

        private static bool isNeeded(string keyValue)
        {
            bool result = !attributeCommonOkMapping.ContainsKey(keyValue) || attributeCommonOkMapping[keyValue] != string.Empty;
            return result;
        }

        private void makeOkDialogAttributes()
        {
            okDialogAttributes = new AttributesDictionary<bool>();
            foreach (KeyValuePair<AttributeUtils.Attribute, bool> kvp in dialogAttributes)
                if (!isNeeded(kvp.Key.value))
                    okDialogAttributes.Remove(kvp.Key);
                else
                    okDialogAttributes[kvp.Key] = dialogAttributes[kvp.Key];
        }
    }

    internal struct Attribute
    {
        internal string Name, Value;

        internal Attribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
