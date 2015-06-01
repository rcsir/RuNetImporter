using System;
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

namespace rcsir.net.vk.content.NetworkAnalyzer
{
    class ContentNetworkAnalyzer : NetworkAnalyzerBase<long>
    {
        // content network
        public string GraphName { get; set; }
        private readonly VertexCollection<long> vertices = new VertexCollection<long>();
        private readonly EdgeCollection<long> edges = new EdgeCollection<long>();

        private static readonly List<AttributeUtils.Attribute> UserAttributes = new List<AttributeUtils.Attribute>()
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
            new AttributeUtils.Attribute("Receive Comments","rec_comments", "friends", false),
            new AttributeUtils.Attribute("Board Comments","board_comments", "friends", false)
        };

        public override List<AttributeUtils.Attribute> GetDefaultNetworkAttributes()
        {
            return UserAttributes;
        }

        public AttributesDictionary<String> CreateAttributes(JObject obj)
        {
            var attributes = new AttributesDictionary<String>(UserAttributes);
            var keys = new List<AttributeUtils.Attribute>(attributes.Keys);
            foreach (var key in keys)
            {
                var name = key.value;
                if (obj[name] != null)
                {
                    var value = "";

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
                xmlDocument.AppendGraphMLAttributeValue(node, ImageFileID, vertex.Attributes["photo_50"]);
            }
        }

        public void AddVertex(long id, string name, string type, JObject member)
        {
            AttributesDictionary<String> attributes = CreateAttributes(member);
            vertices.Add(new Vertex<long>(id, name, type, attributes));
        }

        // remove all edges
        public void ResetEdges()
        {
            edges.Clear();
        }

        public void AddEdge(long user1, long user2, string type, string relationship,
            string comment, int weight, int timestamp)
        {
            var vertex1 = vertices.FirstOrDefault(x => x.ID == user1);
            var vertex2 = vertices.FirstOrDefault(x => x.ID == user2);

            if (vertex1 != null && vertex2 != null)
            {
                // check for duplicates first
                Edge<long> e = edges.FirstOrDefault(x => x.Vertex1.ID == vertex2.ID && x.Vertex2.ID == vertex1.ID);
                if (e == null)
                {
                    edges.Add(new Edge<long>(vertex1, vertex2, type, relationship, comment, weight, timestamp,
                        EdgeDirection.Directed));
                }
            }
        }

        public void UpdateVertexAttributes(long id, Dictionary<String, String> attributes)
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

        public void UpdateVertexAttributes(long id, String key, String value)
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
        public XmlDocument GenerateU2UNetwork()
        {
            // create default attributes (values will be empty)
            var attributes = new AttributesDictionary<String>(UserAttributes);
            return GenerateNetworkDocument(vertices, edges, attributes, true); // directed graph
        }
    }
}
