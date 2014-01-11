/*
 * Copyright 2010 Facebook, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may
 * not use this file except in compliance with the License. You may obtain
 * a copy of the License at
 *
 *  http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.Web.Script.Serialization;

namespace rcsir.net.common.Utilities
{
    public class XMLJSONConvert
    {
        public static string XmlToJSON(XmlDocument xmlDoc)
        {
            StringBuilder sbJSON = new StringBuilder();
            sbJSON.Append("{ ");
            XmlToJSONnode(sbJSON, xmlDoc.DocumentElement, true);
            sbJSON.Append("}");
            return sbJSON.ToString();
        }

        //  XmlToJSONnode:  Output an XmlElement, possibly as part of a higher array
        private static void XmlToJSONnode(StringBuilder sbJSON, XmlElement node, bool showNodeName)
        {
            if (showNodeName)
                sbJSON.Append("\"" + SafeJSON(node.Name) + "\": ");
            sbJSON.Append("{");
            // Build a sorted list of key-value pairs
            //  where   key is case-sensitive nodeName
            //          value is an ArrayList of string or XmlElement
            //  so that we know whether the nodeName is an array or not.
            SortedList childNodeNames = new SortedList();

            //  Add in all node attributes
            if (node.Attributes != null)
                foreach (XmlAttribute attr in node.Attributes)
                    StoreChildNode(childNodeNames, attr.Name, attr.InnerText);

            //  Add in all nodes
            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode is XmlText)
                    StoreChildNode(childNodeNames, "value", cnode.InnerText);
                else if (cnode is XmlElement)
                    StoreChildNode(childNodeNames, cnode.Name, cnode);
            }

            // Now output all stored info
            foreach (string childname in childNodeNames.Keys)
            {
                ArrayList alChild = (ArrayList)childNodeNames[childname];
                if (alChild.Count == 1)
                    OutputNode(childname, alChild[0], sbJSON, true);
                else
                {
                    sbJSON.Append(" \"" + SafeJSON(childname) + "\": [ ");
                    foreach (object Child in alChild)
                        OutputNode(childname, Child, sbJSON, false);
                    sbJSON.Remove(sbJSON.Length - 2, 2);
                    sbJSON.Append(" ], ");
                }
            }
            sbJSON.Remove(sbJSON.Length - 2, 2);
            sbJSON.Append(" }");
        }

        //  StoreChildNode: Store data associated with each nodeName
        //                  so that we know whether the nodeName is an array or not.
        private static void StoreChildNode(SortedList childNodeNames, string nodeName, object nodeValue)
        {
            // Pre-process contraction of XmlElement-s
            if (nodeValue is XmlElement)
            {
                // Convert  <aa></aa> into "aa":null
                //          <aa>xx</aa> into "aa":"xx"
                XmlNode cnode = (XmlNode)nodeValue;
                if (cnode.Attributes.Count == 0)
                {
                    XmlNodeList children = cnode.ChildNodes;
                    if (children.Count == 0)
                        nodeValue = null;
                    else if (children.Count == 1 && (children[0] is XmlText))
                        nodeValue = ((XmlText)(children[0])).InnerText;
                }
            }
            // Add nodeValue to ArrayList associated with each nodeName
            // If nodeName doesn't exist then add it
            object oValuesAL = childNodeNames[nodeName];
            ArrayList ValuesAL;
            if (oValuesAL == null)
            {
                ValuesAL = new ArrayList();
                childNodeNames[nodeName] = ValuesAL;
            }
            else
                ValuesAL = (ArrayList)oValuesAL;
            ValuesAL.Add(nodeValue);
        }

        private static void OutputNode(string childname, object alChild, StringBuilder sbJSON, bool showNodeName)
        {
            if (alChild == null)
            {
                if (showNodeName)
                    sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
                sbJSON.Append("null");
            }
            else if (alChild is string)
            {
                if (showNodeName)
                    sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
                string sChild = (string)alChild;
                sChild = sChild.Trim();
                sbJSON.Append("\"" + SafeJSON(sChild) + "\"");
            }
            else
                XmlToJSONnode(sbJSON, (XmlElement)alChild, showNodeName);
            sbJSON.Append(", ");
        }

        // Make a string safe for JSON
        private static string SafeJSON(string sIn)
        {
            StringBuilder sbOut = new StringBuilder(sIn.Length);
            foreach (char ch in sIn)
            {
                if (Char.IsControl(ch) || ch == '\'')
                {
                    int ich = (int)ch;
                    sbOut.Append(@"\u" + ich.ToString("x4"));
                    continue;
                }
                else if (ch == '\"' || ch == '\\' || ch == '/')
                {
                    sbOut.Append('\\');
                }
                sbOut.Append(ch);
            }
            return sbOut.ToString();
        }

    }


    /// <summary>
    /// Represents an object encoded in JSON. Can be either a dictionary
    /// mapping strings to other objects, an array of objects, or a single
    /// object, which represents a scalar.
    /// </summary>
    public class JSONObject
    {
        /// <summary>
        /// Creates a JSONObject by parsing a string.
        /// This is the only correct way to create a JSONObject.
        /// </summary>
        public static JSONObject CreateFromString(string s)
        {
            object o;
            JavaScriptSerializer js = new JavaScriptSerializer();

            if (s.Contains("xml")) //convert XML to json
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(s);
                s = XMLJSONConvert.XmlToJSON(doc);
            }

            try
            {
                o = js.DeserializeObject(s);
            }
            catch (ArgumentException)
            {
                throw new Exception("JSONException");
            }

            return Create(o);
        }

        /// <summary>
        /// Returns true if this JSONObject represents a dictionary.
        /// </summary>
        public bool IsDictionary
        {
            get
            {
                return _dictData != null;
            }
        }

        /// <summary>
        /// Returns true if this JSONObject represents an array.
        /// </summary>
        public bool IsArray
        {
            get
            {
                return _arrayData != null;
            }
        }

        /// <summary>
        /// Returns true if this JSONObject represents a string value.
        /// </summary>
        public bool IsString
        {
            get
            {
                return _stringData != null;
            }
        }

        /// <summary>
        /// Returns true if this JSONObject represents an integer value.
        /// </summary>
        public bool IsInteger
        {
            get
            {
                Int64 tmp;
                return Int64.TryParse(_stringData, out tmp);
            }
        }

        /// <summary>
        /// Returns true if this JSONOBject represents a boolean value.
        /// </summary>
        public bool IsBoolean
        {
            get
            {
                bool tmp;
                return bool.TryParse(_stringData, out tmp);
            }
        }

        /// <summary>
        /// Returns this JSONObject as a dictionary
        /// </summary>
        public Dictionary<string, JSONObject> Dictionary
        {
            get
            {
                return _dictData;
            }
        }

        /// <summary>
        /// Returns this JSONObject as an array
        /// </summary>
        public JSONObject[] Array
        {
            get
            {
                return _arrayData;
            }
        }

        /// <summary>
        /// Returns this JSONObject as a string
        /// </summary>
        public string String
        {
            get
            {
                return _stringData;
            }
        }

        /// <summary>
        /// Returns this JSONObject as an integer
        /// </summary>
        public Int64 Integer
        {
            get
            {
                return Convert.ToInt64(_stringData);
            }
        }

        /// <summary>
        /// Returns this JSONObject as a boolean
        /// </summary>
        public bool Boolean
        {
            get
            {
                return Convert.ToBoolean(_stringData);
            }
        }


        /// <summary>
        /// Prints the JSONObject as a formatted string, suitable for viewing.
        /// </summary>
        public string ToDisplayableString()
        {
            StringBuilder sb = new StringBuilder();
            RecursiveObjectToString(this, sb, 0);
            return sb.ToString();
        }

        #region Private Members

        private string _stringData;
        private JSONObject[] _arrayData;
        private Dictionary<string, JSONObject> _dictData;

        private JSONObject()
        { }

        /// <summary>
        /// Recursively constructs this JSONObject
        /// </summary>
        private static JSONObject Create(object o)
        {
            JSONObject obj = new JSONObject();
            if (o is object[])
            {
                object[] objArray = o as object[];
                obj._arrayData = new JSONObject[objArray.Length];
                for (int i = 0; i < obj._arrayData.Length; ++i)
                {
                    obj._arrayData[i] = Create(objArray[i]);
                }
            }
            else if (o is Dictionary<string, object>)
            {
                obj._dictData = new Dictionary<string, JSONObject>();
                Dictionary<string, object> dict =
                    o as Dictionary<string, object>;
                foreach (string key in dict.Keys)
                {
                    obj._dictData[key] = Create(dict[key]);
                }
            }
            else if (o != null) // o is a scalar
            {
                obj._stringData = o.ToString();
            }

            return obj;
        }

        private static void RecursiveObjectToString(JSONObject obj,
            StringBuilder sb, int level)
        {
            if (obj.IsDictionary)
            {
                sb.AppendLine();
                RecursiveDictionaryToString(obj, sb, level + 1);
            }
            else if (obj.IsArray)
            {
                foreach (JSONObject o in obj.Array)
                {
                    RecursiveObjectToString(o, sb, level);
                    sb.AppendLine();
                }
            }
            else // some sort of scalar value
            {
                sb.Append(obj.String);
            }
        }
        private static void RecursiveDictionaryToString(JSONObject obj,
            StringBuilder sb, int level)
        {
            foreach (KeyValuePair<string, JSONObject> kvp in obj.Dictionary)
            {
                sb.Append(' ', level * 2);
                sb.Append(kvp.Key);
                sb.Append(" => ");
                RecursiveObjectToString(kvp.Value, sb, level);
                sb.AppendLine();
            }
        }

        #endregion

    }
}