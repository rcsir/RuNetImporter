using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Smrf.XmlLib;
using Smrf.AppLib;
using rcsir.net.common.Network;
using rcsir.net.common.Utilities;

namespace rcsir.net.common.NetworkAnalyzer
{
    public class NetworkAnalyzerBase
    {
        //*************************************************************************
        //  Protected constants
        //*************************************************************************

        /// GraphML-attribute IDs for vertices.

        protected const String ImageFileID = "Image";
        ///
        protected const String LabelID = "Label";
        ///
        protected const String MenuTextID = "MenuText";
        ///
        protected const String MenuActionID = "MenuAction";

        /// GraphML-attribute IDs for edges.

        protected const String RelationshipID = "Relationship";

        /// NodeXL Excel template column names.

        protected const String ImageColumnName = "Image File";
        ///
        protected const String LabelColumnName = "Label";
        ///
        protected const String MenuTextColumnName = "Custom Menu Item Text";
        ///
        protected const String MenuActionColumnName = "Custom Menu Item Action";
        /// <summary>
        /// 
        /// </summary>
        protected const String TooltipID = "Tooltip";


        public XmlDocument GenerateNetworkDocument(List<Vertex> vertices, List<Edge> edges)
        {
            GraphMLXmlDocument graphMLXmlDocument = new GraphMLXmlDocument(false); // directed = falce
            
            // Image file attribute
            graphMLXmlDocument.DefineGraphMLAttribute(false, 
                ImageFileID,
                ImageColumnName, "string", null);

            // Custom Menu attribute
            graphMLXmlDocument.DefineGraphMLAttribute(false, 
                MenuTextID,
                MenuTextColumnName, "string", null);

            graphMLXmlDocument.DefineGraphMLAttribute(false, 
                MenuActionID,
                MenuActionColumnName, "string", null);

            graphMLXmlDocument.DefineGraphMLAttribute(false, 
                TooltipID,
                "Tooltip", "string", null);

            graphMLXmlDocument.DefineGraphMLAttribute(false, "type", "Type", "string", null);
            graphMLXmlDocument.DefineGraphMLAttribute(true, "e_type", "Edge Type", "string", null);
            graphMLXmlDocument.DefineGraphMLAttribute(true, "e_comment", "Tweet", "string", null);
            graphMLXmlDocument.DefineGraphMLAttribute(true, "e_origin", "Feed of Origin", "string", null);
            graphMLXmlDocument.DefineGraphMLAttribute(true, "e_timestamp", "Timestamp", "string", null);

            // Relationship attribute
            graphMLXmlDocument.DefineGraphMLAttribute(true, 
                RelationshipID,
                "Relationship", "string", null);

            /*
            foreach (KeyValuePair<AttributeUtils.Attribute, bool> kvp in attributes)
            {
                if (kvp.Value)
                {
                    if (kvp.Key.value.Equals("hometown_location"))
                    {
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "hometown",
                        "Hometown", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "hometown_city",
                        "Hometown City", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "hometown_state",
                        "Hometown State", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "hometown_country",
                        "Hometown Country", "string", null);
                    }
                    else if (kvp.Key.value.Equals("current_location"))
                    {
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "location",
                        "Current Location", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "location_city",
                        "Current Location City", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "location_state",
                        "Current Location State", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "location_country",
                        "Current Location Country", "string", null);
                    }
                    else
                    {
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, kvp.Key.value,
                        kvp.Key.name, "string", null);
                    }
                }
            }            
            */

            // add vertices
            XmlNode oVertexXmlNode;
            foreach (Vertex oVertex in vertices)
            {
                oVertexXmlNode = graphMLXmlDocument.AppendVertexXmlNode(oVertex.Name);
                AddVertexAttributes(oVertexXmlNode, oVertex, graphMLXmlDocument);
            }

            // add edges
            XmlNode oEdgeXmlNode;
            foreach (Edge oEdge in edges)
            {
                try
                {
                    oEdgeXmlNode =graphMLXmlDocument.AppendEdgeXmlNode(oEdge.Vertex1.Name,
                            oEdge.Vertex2.Name);
                    AddEdgeAttributes(oEdgeXmlNode, oEdge, graphMLXmlDocument);
                }
                catch (KeyNotFoundException ex)
                {
                    //Do Nothing.
                }
            }

            return graphMLXmlDocument;
        }


        private void AddVertexAttributes(XmlNode oVertexXmlNode, Vertex oVertex, GraphMLXmlDocument oGraphMLXmlDocument)
        {
            string sAttribtueValue;
            foreach (KeyValuePair<AttributeUtils.Attribute, JSONObject> kvp in oVertex.Attributes)
            {
                /*
                if (kvp.Value == null || (kvp.Value.String == null && !kvp.Value.IsDictionary))
                {
                    sAttribtueValue = "";
                }
                else if (kvp.Key.value.Equals("hometown_location"))
                {

                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "hometown", kvp.Value.Dictionary.ContainsKey("name") ? kvp.Value.Dictionary["name"].String : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "hometown_city", kvp.Value.Dictionary.ContainsKey("city") ? kvp.Value.Dictionary["city"].String : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "hometown_state", kvp.Value.Dictionary.ContainsKey("state") ? kvp.Value.Dictionary["state"].String : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "hometown_country", kvp.Value.Dictionary.ContainsKey("country") ? kvp.Value.Dictionary["country"].String : "");
                }
                else if (kvp.Key.value.Equals("current_location"))
                {
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "location", kvp.Value.Dictionary.ContainsKey("name") ? kvp.Value.Dictionary["name"].String : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "location_city", kvp.Value.Dictionary.ContainsKey("city") ? kvp.Value.Dictionary["city"].String : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "location_state", kvp.Value.Dictionary.ContainsKey("state") ? kvp.Value.Dictionary["state"].String : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "location_country", kvp.Value.Dictionary.ContainsKey("country") ? kvp.Value.Dictionary["country"].String : "");
                }
                else
                {
                    if (kvp.Value.String.Length > 8000)
                    {
                        sAttribtueValue = kvp.Value.String.Remove(8000);
                    }
                    else
                    {
                        sAttribtueValue = kvp.Value.String;
                    }

                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, kvp.Key.value, sAttribtueValue);
                }
                */

            }

            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "type", oVertex.Type);

            AppendVertexTooltipXmlNodes(oGraphMLXmlDocument, oVertexXmlNode, oVertex.Name, oVertex.ToolTip == null ? "" : oVertex.ToolTip);

            if (oVertex.Attributes.ContainsKey("pic_small") &&
                oVertex.Attributes["pic_small"] != null)
            {
                //oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "Image", oVertex.Attributes["pic_small"].String);
            }

        }

        private void AddEdgeAttributes ( XmlNode oEdgeXmlNode, Edge oEdge,GraphMLXmlDocument oGraphMLXmlDocument)
        {
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, "e_type", oEdge.Type);
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, "e_origin", oEdge.FeedOfOrigin);
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, RelationshipID, oEdge.Relationship);
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, "e_comment", oEdge.Comment);
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, "e_timestamp", oEdge.Timestamp == DateTime.MinValue ? "" : oEdge.Timestamp.ToString());

        }

        protected void AppendVertexTooltipXmlNodes (
            GraphMLXmlDocument oGraphMLXmlDocument,
            XmlNode oVertexXmlNode,
            String sVertex,
            String sDisplayString)
        {
            // The NodeXL template doesn't wrap long tooltip text.  Break the
            // status into lines so the entire tooltip will show in the graph
            // pane.

            sDisplayString = StringUtil.BreakIntoLines(sDisplayString, 30);

            String sTooltip = String.Format(
                "{0}\n\n{1}"
                ,
                sVertex,
                sDisplayString
                );

            oGraphMLXmlDocument.AppendGraphMLAttributeValue(
                oVertexXmlNode, TooltipID, sTooltip);

        }              
    }
}
