﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;
using Smrf.XmlLib;
using Smrf.AppLib;
using rcsir.net.common.NetworkAnalyzer;
using rcsir.net.common.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace rcsir.net.vk.groups.NetworkAnalyzer
{
    class GroupNetworkAnalyzer : NetworkAnalyzerBase<long>
    {
        // members network
        private VertexCollection<long> vertices = new VertexCollection<long>();
        private EdgeCollection<long> edges = new EdgeCollection<long>();

        private static List<AttributeUtils.Attribute> GroupAttributes = new List<AttributeUtils.Attribute>()
        {
            new AttributeUtils.Attribute("Name","name", "friends", false),
            new AttributeUtils.Attribute("First Name","first_name", "friends", true),
            new AttributeUtils.Attribute("Last Name","last_name", "friends", true),
            new AttributeUtils.Attribute("Picture","photo_50", "friends", true),
            new AttributeUtils.Attribute("Sex","sex", "friends", true),
            new AttributeUtils.Attribute("Birth Date","bdate", "friends", true),
            new AttributeUtils.Attribute("Relation","relation", "friends", false),
            new AttributeUtils.Attribute("City","city", "friends", false),
            new AttributeUtils.Attribute("Country","country", "friends", false),
            // posts stats
            new AttributeUtils.Attribute("Posts","posts", "friends", false),
            new AttributeUtils.Attribute("Comments","comments", "friends", false),
            new AttributeUtils.Attribute("Receive Likes","rec_likes", "friends", false),
            new AttributeUtils.Attribute("Likes","likes", "friends", false),
            new AttributeUtils.Attribute("Friends","friends", "friends", false),
        };

        public GroupNetworkAnalyzer()
        {
        }

        public override List<AttributeUtils.Attribute> GetDefaultNetworkAttributes()
        {
            return GroupAttributes;
        }

        private AttributesDictionary<String> createAttributes(JObject obj)
        {
            AttributesDictionary<String> attributes = new AttributesDictionary<String>(GroupAttributes);
            List<AttributeUtils.Attribute> keys = new List<AttributeUtils.Attribute>(attributes.Keys);
            foreach (AttributeUtils.Attribute key in keys)
            {
                String name = key.value;
                if (obj[name] != null)
                {
                    String value = "";

                    if(name == "city" ||
                        name == "country")
                    {
                        value = obj[name]["title"].ToString();
                    } 
                    else 
                    {
                        value = obj[name].ToString();
                    }

                    attributes[key] = value;
                }
            }

            return attributes;
        }

        protected override void AddVertexImageAttribute(XmlNode node, Vertex<long> vertex, GraphMLXmlDocument xmlDocument)
        {
            // add picture
            if (vertex.Attributes.ContainsKey("photo_50") &&
                vertex.Attributes["photo_50"] != null)
            {
                xmlDocument.AppendGraphMLAttributeValue(node, ImageFileID, vertex.Attributes["photo_50"].ToString());
            }
        }

        public void addVertex(long id, string name, string type, JObject member)
        {
            AttributesDictionary<String> attributes = createAttributes(member);
            this.vertices.Add(new Vertex<long>(id, name, type, attributes));
        }

        public void AddFriendsEdge(long memberId, long friendId)
        {
            Vertex<long> friend = vertices.FirstOrDefault(x => x.ID == memberId);
            Vertex<long> friendsFriend = vertices.FirstOrDefault(x => x.ID == friendId);

            if (friend != null && friendsFriend != null)
            {
                // check for duplicates first
                Edge<long> e = edges.FirstOrDefault(x => x.Vertex1.ID == friendsFriend.ID && x.Vertex2.ID == friend.ID);
                if (e == null)
                {
                    edges.Add(new Edge<long>(friend, friendsFriend, "", "Friend", "", 1));
                }
            }
        }

        public void updateVertexAttributes(long id, Dictionary<String, String> attributes)
        {
            Vertex<long> v = vertices[id];

            if (v != null)
            {
                AttributesDictionary<String> a = v.Attributes;
                if (a != null)
                {
                    foreach (KeyValuePair<string, string> entry in attributes)
                    {
                        // note, that entry key must be present in a!
                        a[entry.Key] = entry.Value;
                    }
                }
            }
            else
            {
                Debug.WriteLine("Vertex not found with id " + id);
            }
        }

        public void updateVertexAttributes(long id, String key, String value)
        {
            Vertex<long> v = vertices[id];

            if (v != null)
            {
                AttributesDictionary<String> a = v.Attributes;
                // note, that entry key must be present in a!
                if (a != null)
                {
                    a[key] = value;
                }
            }
            else
            {
                Debug.WriteLine("Vertex not found with id " + id);
            }
        }

        // Group Network GraphML document
        public XmlDocument GenerateGroupNetwork()
        {
            // create default attributes (values will be empty)
            AttributesDictionary<String> attributes = new AttributesDictionary<String>(GroupAttributes);
            return GenerateNetworkDocument(vertices, edges, attributes);
        }
    }
}
