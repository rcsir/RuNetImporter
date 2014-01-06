using System;
using Newtonsoft.Json.Linq;
using Smrf.AppLib;
using rcsir.net.common.Network;

namespace rcsir.net.ok.importer.Events
{
    public class GraphEventArgs : EventArgs
    {
        public enum Types
        {
//            GetAccessToken,
            UserInfoLoaded,
            FriendsLoaded,
            AreGraphLoaded,
            MutualGraphLoaded,
//            GenerateAreGraph,
//            GenerateMutualGraph,
            GraphGenerated
        };

        public readonly Types Type;
        public readonly JObject JData;

        public readonly VertexCollection Vertices;
        public readonly EdgeCollection Edges;
        public readonly AttributesDictionary<bool> DialogAttributes;
        public readonly AttributesDictionary<string> GraphAttributes;

        public GraphEventArgs(Types type, JObject data = null)
        {
            Type = type;
            JData = data;
        }

        public GraphEventArgs(VertexCollection vertices, EdgeCollection edges, AttributesDictionary<bool> dialogAttributes, AttributesDictionary<string> graphAttributes)
        {
            Type = Types.GraphGenerated;
            Vertices = vertices;
            Edges = edges;
            DialogAttributes = dialogAttributes;
            GraphAttributes = graphAttributes;
        }
    }
}
