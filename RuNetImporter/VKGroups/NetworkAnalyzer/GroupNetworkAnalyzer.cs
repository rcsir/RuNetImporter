using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Smrf.XmlLib;
using Smrf.AppLib;
using rcsir.net.common.NetworkAnalyzer;
using rcsir.net.common.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace rcsir.net.vk.groups.NetworkAnalyzer
{
    class GroupNetworkAnalyzer : NetworkAnalyzerBase
    {
        private List<string> memberIds = new List<string>();
        private VertexCollection vertices = new VertexCollection();
        private EdgeCollection edges = new EdgeCollection();

        private String documentName {get; set;}

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

        };

        public GroupNetworkAnalyzer(String documentName)
        {
            this.documentName = documentName;
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

        protected override void AddVertexImageAttribute(XmlNode node, Vertex vertex, GraphMLXmlDocument xmlDocument)
        {
            // add picture
            if (vertex.Attributes.ContainsKey("photo_50") &&
                vertex.Attributes["photo_50"] != null)
            {
                xmlDocument.AppendGraphMLAttributeValue(node, ImageFileID, vertex.Attributes["photo_50"].ToString());
            }
        }

        public void addMemberVertex(JObject member)
        {
            string id = member["id"].ToString();
            // add user id to the friends list
            this.memberIds.Add(id);

            // add friend vertex
            AttributesDictionary<String> attributes = createAttributes(member);

            this.vertices.Add(new Vertex(id,
                member["first_name"].ToString() + " " + member["last_name"].ToString(),
                "Member", attributes));
        }

        public void AddFriendsEdge(String memberId, String friendId)
        {
            Vertex friend = vertices.FirstOrDefault(x => x.ID == memberId);
            Vertex friendsFriend = vertices.FirstOrDefault(x => x.ID == friendId);

            if (friend != null && friendsFriend != null)
            {
                // check for duplicates first
                Edge e = edges.FirstOrDefault(x => x.Vertex1.ID == friendsFriend.ID && x.Vertex2.ID == friend.ID);
                if (e == null)
                {
                    edges.Add(new Edge(friend, friendsFriend, "", "Friend", "", 1));
                }
            }
        }

        public VertexCollection GetVertices()
        {
            return this.vertices;
        }

        public EdgeCollection GetEdges()
        {
            return this.edges;
        }

        public List<string> GetMemeberIds()
        {
            return this.memberIds;
        }

        public XmlDocument GenerateGroupNetwork()
        {
            // create default attributes (values will be empty)
            AttributesDictionary<String> attributes = new AttributesDictionary<String>(GroupAttributes);
            return GenerateNetworkDocument(vertices, edges, attributes);
        }

        // Group Network GraphML document
        public bool SaveGroupNetwork()
        {
            XmlDocument graph = GenerateGroupNetwork();

            if (graph != null)
            {
                graph.Save(documentName);
                return true;
            }

            return false;
        }


    }
}
