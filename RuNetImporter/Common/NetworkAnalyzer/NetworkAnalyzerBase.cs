using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Smrf.XmlLib;
using Smrf.AppLib;
using rcsir.net.common.Network;
using Newtonsoft.Json.Linq;


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


        public XmlDocument GenerateNetworkDocument(VertexCollection vertices, EdgeCollection edges, AttributesDictionary<String> attributes)
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

            // Relationship attribute
            graphMLXmlDocument.DefineGraphMLAttribute(true, 
                RelationshipID,
                "Relationship", "string", null);

            foreach (KeyValuePair<AttributeUtils.Attribute, String> kvp in attributes)
            {
                graphMLXmlDocument.DefineGraphMLAttribute(
                    false, kvp.Key.value, kvp.Key.name, "string", null);
                
            }            

            // add vertices
            XmlNode oVertexXmlNode;
            foreach (Vertex oVertex in vertices)
            {
                oVertexXmlNode = graphMLXmlDocument.AppendVertexXmlNode(oVertex.ID);
                AddVertexAttributes(oVertexXmlNode, oVertex, graphMLXmlDocument);
            }

            // add edges
            XmlNode oEdgeXmlNode;
            foreach (Edge oEdge in edges)
            {
                try
                {
                    oEdgeXmlNode =graphMLXmlDocument.AppendEdgeXmlNode(oEdge.Vertex1.ID,
                            oEdge.Vertex2.ID);
                    AddEdgeAttributes(oEdgeXmlNode, oEdge, graphMLXmlDocument);
                }
                catch (KeyNotFoundException)
                {
                    //Do Nothing.
                }
            }

            return graphMLXmlDocument;
        }


        private void AddVertexAttributes(XmlNode oVertexXmlNode, Vertex oVertex, GraphMLXmlDocument oGraphMLXmlDocument)
        {
            string sAttribtueValue;
            foreach (KeyValuePair<AttributeUtils.Attribute, String> kvp in oVertex.Attributes)
            {
                if (kvp.Value == null)
                {
                    sAttribtueValue = "";
                }
                else
                {
                    sAttribtueValue = kvp.Value;
                    if (sAttribtueValue.Length > 8000)
                    {
                        sAttribtueValue = sAttribtueValue.Remove(8000);
                    }

                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, kvp.Key.value, sAttribtueValue);
                }

            }

            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "type", oVertex.Type);

            AppendVertexTooltipXmlNodes(oGraphMLXmlDocument, oVertexXmlNode, oVertex.Name, oVertex.ToolTip == null ? "" : oVertex.ToolTip);

            // add picture
            if (oVertex.Attributes.ContainsKey("pic_small") &&
                oVertex.Attributes["pic_small"] != null)
            {
                oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "Image", oVertex.Attributes["pic_small"].ToString());
            }

        }

        /*
         * 
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

         * 
         * 
         * else if (kvp.Key.value.Equals("hometown_location"))
                {
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "hometown", kvp.Value["name"] != null ? kvp.Value["name"].ToString() : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "hometown_city", kvp.Value["city"] != null ? kvp.Value["city"].ToString() : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "hometown_state", kvp.Value["state"] != null ? kvp.Value["state"].ToString() : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "hometown_country", kvp.Value["country"] != null ? kvp.Value["country"].ToString() : "");
                }
                else if (kvp.Key.value.Equals("current_location"))
                {
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "location", kvp.Value["name"] != null ? kvp.Value["name"].ToString() : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "location_city", kvp.Value["city"] != null ? kvp.Value["city"].ToString() : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "location_state", kvp.Value["state"] != null ? kvp.Value["state"].ToString() : "");
                    oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "location_country", kvp.Value["country"] != null ? kvp.Value["country"].ToString() : "");
                }
         * 
         * 
         * */

        private void AddEdgeAttributes ( XmlNode oEdgeXmlNode, Edge oEdge,GraphMLXmlDocument oGraphMLXmlDocument)
        {
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, "e_type", oEdge.Type);
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, RelationshipID, oEdge.Relationship);
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
