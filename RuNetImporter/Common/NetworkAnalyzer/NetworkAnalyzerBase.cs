using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;
using Smrf.XmlLib;
using Smrf.AppLib;
using Smrf.SocialNetworkLib;
using rcsir.net.common.Network;


namespace rcsir.net.common.NetworkAnalyzer
{
    public class NetworkAnalyzerBase<T>
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
        ///
        protected const String TooltipID = "Tooltip";


        public XmlDocument GenerateNetworkDocument(VertexCollection<T> vertices, 
            EdgeCollection<T> edges, 
            AttributesDictionary<String> attributes,
            bool directed = false)
        {
            GraphMLXmlDocument graphMLXmlDocument = new GraphMLXmlDocument(directed); // directed = falce
            
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
            graphMLXmlDocument.DefineGraphMLAttribute(true, "weight", "Weight", "string", null);

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
            foreach (Vertex<T> oVertex in vertices)
            {
                oVertexXmlNode = graphMLXmlDocument.AppendVertexXmlNode(oVertex.ID.ToString());
                AddVertexAttributes(oVertexXmlNode, oVertex, graphMLXmlDocument);
                AddVertexImageAttribute(oVertexXmlNode, oVertex, graphMLXmlDocument);
            }

            // add edges
            XmlNode oEdgeXmlNode;
            foreach (Edge<T> oEdge in edges)
            {
                try
                {
                    oEdgeXmlNode =graphMLXmlDocument.AppendEdgeXmlNode(oEdge.Vertex1.ID.ToString(),
                            oEdge.Vertex2.ID.ToString());
                    AddEdgeAttributes(oEdgeXmlNode, oEdge, graphMLXmlDocument);
                }
                catch (KeyNotFoundException)
                {
                    //Do Nothing.
                }
            }

            return graphMLXmlDocument;
        }

        /// <summary>
        /// The list of default network attributes
        /// </summary>
        /// <returns></returns>
        public virtual List<AttributeUtils.Attribute> GetDefaultNetworkAttributes()
        {
            return AttributeUtils.UserAttributes; // this list is based on Facebook attributes, override as needed
        }

        private void AddVertexAttributes(XmlNode oVertexXmlNode, Vertex<T> oVertex, GraphMLXmlDocument oGraphMLXmlDocument)
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

        }

        /// <summary>
        /// Adds Image attribute, if present
        /// </summary>
        /// <param name="oVertexXmlNode"></param>
        /// <param name="oVertex"></param>
        /// <param name="oGraphMLXmlDocument"></param>
        protected virtual void AddVertexImageAttribute(XmlNode oVertexXmlNode, Vertex<T> oVertex, GraphMLXmlDocument oGraphMLXmlDocument)
        {
            // add picture
            if (oVertex.Attributes.ContainsKey("pic_small") &&
                oVertex.Attributes["pic_small"] != null)
            {
                oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, "Image", oVertex.Attributes["pic_small"].ToString());
            }
        }

        private void AddEdgeAttributes ( XmlNode oEdgeXmlNode, Edge<T> oEdge,GraphMLXmlDocument oGraphMLXmlDocument)
        {
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, "e_type", oEdge.Type);
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, RelationshipID, oEdge.Relationship);
            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, "weight", oEdge.Weight);
            //oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode, "timestamp", oEdge.Timestamp);
        }

        //*************************************************************************
        //  Method: AppendVertexTooltipXmlNodes()
        //
        /// <summary>
        /// Appends a vertex tooltip XML node for each person in the network.
        /// </summary>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// The GraphMLXmlDocument being populated.
        /// </param>
        ///
        /// <param name="oVertexXmlNode">
        /// The XmlNode representing the vertex.
        /// </param>
        /// 
        /// <param name="sVertex">
        /// The screening name of the vertex. 
        /// </param>
        /// 
        /// <param name="sDisplayString">
        /// The string to be attached after the screening name.
        /// </param>
        //*************************************************************************

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

        //*************************************************************************
        //  Method: DefineImageFileGraphMLAttribute()
        //
        /// <summary>
        /// Defines a GraphML-Attribute for vertex image files.
        /// </summary>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// GraphMLXmlDocument being populated.
        /// </param>
        //*************************************************************************

        protected void
        DefineImageFileGraphMLAttribute
        (
            GraphMLXmlDocument oGraphMLXmlDocument
        )
        {
            Debug.Assert(oGraphMLXmlDocument != null);
            AssertValid();

            oGraphMLXmlDocument.DefineGraphMLAttribute(false, ImageFileID,
                ImageColumnName, "string", null);
        }

        //*************************************************************************
        //  Method: DefineLabelGraphMLAttribute()
        //
        /// <summary>
        /// Defines a GraphML-Attribute for vertex labels.
        /// </summary>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// GraphMLXmlDocument being populated.
        /// </param>
        //*************************************************************************

        protected void
        DefineLabelGraphMLAttribute
        (
            GraphMLXmlDocument oGraphMLXmlDocument
        )
        {
            Debug.Assert(oGraphMLXmlDocument != null);
            AssertValid();

            oGraphMLXmlDocument.DefineGraphMLAttribute(false, LabelID,
                LabelColumnName, "string", null);
        }

        //*************************************************************************
        //  Method: DefineCustomMenuGraphMLAttributes()
        //
        /// <summary>
        /// Defines the GraphML-Attributes for custom menu items.
        /// </summary>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// GraphMLXmlDocument being populated.
        /// </param>
        //*************************************************************************

        protected void
        DefineCustomMenuGraphMLAttributes
        (
            GraphMLXmlDocument oGraphMLXmlDocument
        )
        {
            Debug.Assert(oGraphMLXmlDocument != null);
            AssertValid();

            oGraphMLXmlDocument.DefineGraphMLAttribute(false, MenuTextID,
                MenuTextColumnName, "string", null);

            oGraphMLXmlDocument.DefineGraphMLAttribute(false, MenuActionID,
                MenuActionColumnName, "string", null);
        }

        //*************************************************************************
        //  Method: DefineRelationshipGraphMLAttribute()
        //
        /// <summary>
        /// Defines a GraphML-Attribute for edge relationships.
        /// </summary>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// GraphMLXmlDocument being populated.
        /// </param>
        //*************************************************************************

        protected void
        DefineRelationshipGraphMLAttribute
        (
            GraphMLXmlDocument oGraphMLXmlDocument
        )
        {
            Debug.Assert(oGraphMLXmlDocument != null);
            AssertValid();

            oGraphMLXmlDocument.DefineGraphMLAttribute(true, RelationshipID,
                "Relationship", "string", null);
        }

        //*************************************************************************
        //  Method: AppendStringGraphMLAttributeValue()
        //
        /// <summary>
        /// Appends a String GraphML-Attribute value to an edge or vertex XML node. 
        /// </summary>
        ///
        /// <param name="oXmlNodeToSelectFrom">
        /// Node to select from.
        /// </param>
        /// 
        /// <param name="sXPath">
        /// XPath expression to a String descendant of <paramref
        /// name="oXmlNodeToSelectFrom" />.
        /// </param>
        ///
        /// <param name="oXmlNamespaceManager">
        /// NamespaceManager to use, or null to not use one.
        /// </param>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// GraphMLXmlDocument being populated.
        /// </param>
        ///
        /// <param name="oEdgeOrVertexXmlNode">
        /// The edge or vertex XML node from <paramref
        /// name="oGraphMLXmlDocument" /> to add the GraphML attribute value to.
        /// </param>
        ///
        /// <param name="sGraphMLAttributeID">
        /// GraphML ID of the attribute.
        /// </param>
        ///
        /// <returns>
        /// true if the GraphML-Attribute was appended.
        /// </returns>
        ///
        /// <remarks>
        /// This method selects from <paramref name="oXmlNodeToSelectFrom" /> using
        /// the <paramref name="sXPath" /> expression.  If the selection is
        /// successful, the specified String value gets stored on <paramref
        /// name="oEdgeOrVertexXmlNode" /> as a Graph-ML Attribute.
        /// </remarks>
        //*************************************************************************

        protected Boolean
        AppendStringGraphMLAttributeValue
        (
            XmlNode oXmlNodeToSelectFrom,
            String sXPath,
            XmlNamespaceManager oXmlNamespaceManager,
            GraphMLXmlDocument oGraphMLXmlDocument,
            XmlNode oEdgeOrVertexXmlNode,
            String sGraphMLAttributeID
        )
        {
            Debug.Assert(oXmlNodeToSelectFrom != null);
            Debug.Assert(!String.IsNullOrEmpty(sXPath));
            Debug.Assert(oGraphMLXmlDocument != null);
            Debug.Assert(oEdgeOrVertexXmlNode != null);
            Debug.Assert(!String.IsNullOrEmpty(sGraphMLAttributeID));
            AssertValid();

            String sAttributeValue;

            if (XmlUtil2.TrySelectSingleNodeAsString(oXmlNodeToSelectFrom, sXPath,
                oXmlNamespaceManager, out sAttributeValue))
            {
                oGraphMLXmlDocument.AppendGraphMLAttributeValue(
                    oEdgeOrVertexXmlNode, sGraphMLAttributeID, sAttributeValue);

                return (true);
            }

            return (false);
        }

        //*************************************************************************
        //  Method: AppendInt32GraphMLAttributeValue()
        //
        /// <summary>
        /// Appends an Int32 GraphML-Attribute value to an edge or vertex XML node. 
        /// </summary>
        ///
        /// <param name="oXmlNodeToSelectFrom">
        /// Node to select from.
        /// </param>
        /// 
        /// <param name="sXPath">
        /// XPath expression to an Int32 descendant of <paramref
        /// name="oXmlNodeToSelectFrom" />.
        /// </param>
        ///
        /// <param name="oXmlNamespaceManager">
        /// NamespaceManager to use, or null to not use one.
        /// </param>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// GraphMLXmlDocument being populated.
        /// </param>
        ///
        /// <param name="oEdgeOrVertexXmlNode">
        /// The edge or vertex XML node from <paramref
        /// name="oGraphMLXmlDocument" /> to add the GraphML attribute value to.
        /// </param>
        ///
        /// <param name="sGraphMLAttributeID">
        /// GraphML ID of the attribute.
        /// </param>
        ///
        /// <returns>
        /// true if the GraphML-Attribute was appended.
        /// </returns>
        ///
        /// <remarks>
        /// This method selects from <paramref name="oXmlNodeToSelectFrom" /> using
        /// the <paramref name="sXPath" /> expression.  If the selection is
        /// successful, the specified Int32 value gets stored on <paramref
        /// name="oEdgeOrVertexXmlNode" /> as a Graph-ML Attribute.
        /// </remarks>
        //*************************************************************************

        protected Boolean
        AppendInt32GraphMLAttributeValue
        (
            XmlNode oXmlNodeToSelectFrom,
            String sXPath,
            XmlNamespaceManager oXmlNamespaceManager,
            GraphMLXmlDocument oGraphMLXmlDocument,
            XmlNode oEdgeOrVertexXmlNode,
            String sGraphMLAttributeID
        )
        {
            Debug.Assert(oXmlNodeToSelectFrom != null);
            Debug.Assert(!String.IsNullOrEmpty(sXPath));
            Debug.Assert(oGraphMLXmlDocument != null);
            Debug.Assert(oEdgeOrVertexXmlNode != null);
            Debug.Assert(!String.IsNullOrEmpty(sGraphMLAttributeID));
            AssertValid();

            Int32 iAttributeValue;

            if (XmlUtil2.TrySelectSingleNodeAsInt32(oXmlNodeToSelectFrom, sXPath,
                oXmlNamespaceManager, out iAttributeValue))
            {
                oGraphMLXmlDocument.AppendGraphMLAttributeValue(
                    oEdgeOrVertexXmlNode, sGraphMLAttributeID, iAttributeValue);

                return (true);
            }

            return (false);
        }

        //*************************************************************************
        //  Method: AppendDoubleGraphMLAttributeValue()
        //
        /// <summary>
        /// Appends a Double GraphML-Attribute value to an edge or vertex XML node. 
        /// </summary>
        ///
        /// <param name="oXmlNodeToSelectFrom">
        /// Node to select from.
        /// </param>
        /// 
        /// <param name="sXPath">
        /// XPath expression to a Double descendant of <paramref
        /// name="oXmlNodeToSelectFrom" />.
        /// </param>
        ///
        /// <param name="oXmlNamespaceManager">
        /// NamespaceManager to use, or null to not use one.
        /// </param>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// GraphMLXmlDocument being populated.
        /// </param>
        ///
        /// <param name="oEdgeOrVertexXmlNode">
        /// The edge or vertex XML node from <paramref
        /// name="oGraphMLXmlDocument" /> to add the GraphML attribute value to.
        /// </param>
        ///
        /// <param name="sGraphMLAttributeID">
        /// GraphML ID of the attribute.
        /// </param>
        ///
        /// <returns>
        /// true if the GraphML-Attribute was appended.
        /// </returns>
        ///
        /// <remarks>
        /// This method selects from <paramref name="oXmlNodeToSelectFrom" /> using
        /// the <paramref name="sXPath" /> expression.  If the selection is
        /// successful, the specified Double value gets stored on <paramref
        /// name="oEdgeOrVertexXmlNode" /> as a Graph-ML Attribute.
        /// </remarks>
        //*************************************************************************

        protected Boolean
        AppendDoubleGraphMLAttributeValue
        (
            XmlNode oXmlNodeToSelectFrom,
            String sXPath,
            XmlNamespaceManager oXmlNamespaceManager,
            GraphMLXmlDocument oGraphMLXmlDocument,
            XmlNode oEdgeOrVertexXmlNode,
            String sGraphMLAttributeID
        )
        {
            Debug.Assert(oXmlNodeToSelectFrom != null);
            Debug.Assert(!String.IsNullOrEmpty(sXPath));
            Debug.Assert(oGraphMLXmlDocument != null);
            Debug.Assert(oEdgeOrVertexXmlNode != null);
            Debug.Assert(!String.IsNullOrEmpty(sGraphMLAttributeID));
            AssertValid();

            Double dAttributeValue;

            if (XmlUtil2.TrySelectSingleNodeAsDouble(oXmlNodeToSelectFrom, sXPath,
                oXmlNamespaceManager, out dAttributeValue))
            {
                oGraphMLXmlDocument.AppendGraphMLAttributeValue(
                    oEdgeOrVertexXmlNode, sGraphMLAttributeID, dAttributeValue);

                return (true);
            }

            return (false);
        }

        //*************************************************************************
        //  Method: AppendEdgeXmlNode()
        //
        /// <summary>
        /// Appends an edge XML node to a GraphML document.
        /// </summary>
        ///
        /// <param name="oGraphMLXmlDocument">
        /// GraphMLXmlDocument being populated.
        /// </param>
        ///
        /// <param name="sVertex1ID">
        /// ID of the edge's first vertex.
        /// </param>
        ///
        /// <param name="sVertex2ID">
        /// ID of the edge's second vertex.
        /// </param>
        ///
        /// <param name="sRelationship">
        /// The value of the edge's RelationshipID GraphML-attribute.
        /// </param>
        ///
        /// <returns>
        /// The new edge XML node.
        /// </returns>
        //*************************************************************************

        protected XmlNode
        AppendEdgeXmlNode
        (
            GraphMLXmlDocument oGraphMLXmlDocument,
            String sVertex1ID,
            String sVertex2ID,
            String sRelationship
        )
        {
            Debug.Assert(oGraphMLXmlDocument != null);
            Debug.Assert(!String.IsNullOrEmpty(sVertex1ID));
            Debug.Assert(!String.IsNullOrEmpty(sVertex2ID));
            Debug.Assert(!String.IsNullOrEmpty(sRelationship));
            AssertValid();

            XmlNode oEdgeXmlNode = oGraphMLXmlDocument.AppendEdgeXmlNode(
                sVertex1ID, sVertex2ID);

            oGraphMLXmlDocument.AppendGraphMLAttributeValue(oEdgeXmlNode,
                RelationshipID, sRelationship);

            return (oEdgeXmlNode);
        }

        //*************************************************************************
        //  Method: NetworkLevelToString()
        //
        /// <summary>
        /// Converts a <see cref="NetworkLevel" /> value to a string suitable for
        /// use in a network description.
        /// </summary>
        ///
        /// <param name="eNetworkLevel">
        /// The <see cref="NetworkLevel" /> value to convert to a string.  Sample:
        /// NetworkLevel.OnePointFive.
        /// </param>
        ///
        /// <returns>
        /// A string suitable for use in a network description.  Sample:
        /// "1.5-level".
        /// </returns>
        //*************************************************************************

        protected String
        NetworkLevelToString
        (
            NetworkLevel eNetworkLevel
        )
        {
            AssertValid();

            String sNetworkLevel = String.Empty;

            switch (eNetworkLevel)
            {
                case NetworkLevel.One:

                    sNetworkLevel = "1";
                    break;

                case NetworkLevel.OnePointFive:

                    sNetworkLevel = "1.5";
                    break;

                case NetworkLevel.Two:

                    sNetworkLevel = "2";
                    break;

                case NetworkLevel.TwoPointFive:

                    sNetworkLevel = "2.5";
                    break;

                case NetworkLevel.Three:

                    sNetworkLevel = "3";
                    break;

                case NetworkLevel.ThreePointFive:

                    sNetworkLevel = "3.5";
                    break;

                case NetworkLevel.Four:

                    sNetworkLevel = "4";
                    break;

                case NetworkLevel.FourPointFive:

                    sNetworkLevel = "4.5";
                    break;

                default:

                    Debug.Assert(false);
                    break;
            }

            return (sNetworkLevel + "-level");
        }

        //*************************************************************************
        //  Method: GetNeedToRecurse()
        //
        /// <summary>
        /// Determines whether a method getting a recursive network needs to
        /// recurse.
        /// </summary>
        ///
        /// <param name="eNetworkLevel">
        /// Network level to include.  Must be NetworkLevel.One, OnePointFive, or
        /// Two.
        /// </param>
        ///
        /// <param name="iRecursionLevel">
        /// Recursion level for the current call.  Must be 1 or 2.
        /// </param>
        ///
        /// <returns>
        /// true if the caller needs to recurse.
        /// </returns>
        ///
        /// <remarks>
        /// This is meant for network analyzers that analyze a recursive network.
        /// Call this from the method that uses recursion to get the different
        /// network levels, and use the return value to determine whether to
        /// recurse.
        /// </remarks>
        //*************************************************************************

        protected Boolean
        GetNeedToRecurse
        (
            NetworkLevel eNetworkLevel,
            Int32 iRecursionLevel
        )
        {
            Debug.Assert(eNetworkLevel == NetworkLevel.One ||
                eNetworkLevel == NetworkLevel.OnePointFive ||
                eNetworkLevel == NetworkLevel.Two);

            Debug.Assert(iRecursionLevel == 1 || iRecursionLevel == 2);
            AssertValid();

            return (
                iRecursionLevel == 1
                &&
                (eNetworkLevel == NetworkLevel.OnePointFive ||
                eNetworkLevel == NetworkLevel.Two)
                );
        }

        //*************************************************************************
        //  Method: GetNeedToAppendVertices()
        //
        /// <summary>
        /// Determines whether a method getting a recursive network needs to
        /// add vertices for a specified network and recursion level.
        /// </summary>
        ///
        /// <param name="eNetworkLevel">
        /// Network level to include.  Must be NetworkLevel.One, OnePointFive, or
        /// Two.
        /// </param>
        ///
        /// <param name="iRecursionLevel">
        /// Recursion level for the current call.  Must be 1 or 2.
        /// </param>
        ///
        /// <returns>
        /// true if the caller needs to add vertices for the specified network and
        /// recursion levels.
        /// </returns>
        ///
        /// <remarks>
        /// This is meant for network analyzers that analyze a recursive network.
        /// Call this from the method that uses recursion to get the different
        /// network levels, and use the return value to determine whether to add
        /// vertices for the current network and recursion levels.
        /// </remarks>
        //*************************************************************************

        protected Boolean
        GetNeedToAppendVertices
        (
            NetworkLevel eNetworkLevel,
            Int32 iRecursionLevel
        )
        {
            Debug.Assert(eNetworkLevel == NetworkLevel.One ||
                eNetworkLevel == NetworkLevel.OnePointFive ||
                eNetworkLevel == NetworkLevel.Two);

            Debug.Assert(iRecursionLevel == 1 || iRecursionLevel == 2);
            AssertValid();

            return (
                (eNetworkLevel != NetworkLevel.OnePointFive ||
                iRecursionLevel == 1)
                );
        }

        //*************************************************************************
        //  Method: AssertValid()
        //
        /// <summary>
        /// Asserts if the object is in an invalid state.  Debug-only.
        /// </summary>
        //*************************************************************************

        [Conditional("DEBUG")]

        public virtual void
        AssertValid()
        {
           // nothing
        }

    }
}
