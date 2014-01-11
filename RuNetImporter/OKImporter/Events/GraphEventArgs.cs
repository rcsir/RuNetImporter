using System;
using Smrf.AppLib;
using rcsir.net.common.Network;
using rcsir.net.common.Utilities;

namespace rcsir.net.ok.importer.Events
{
    public class GraphEventArgs : EventArgs
    {
        public enum Types
        {
            UserInfoLoaded,
            FriendsListLoaded,
            FriendsLoaded,
            AreGraphLoaded,
            MutualGraphLoaded,
            GraphGenerated
        };

        public readonly Types Type;
        public readonly JSONObject JData;

        public readonly VertexCollection Vertices;
        public readonly EdgeCollection Edges;
        public readonly AttributesDictionary<bool> DialogAttributes;
        public readonly AttributesDictionary<string> GraphAttributes;
        public readonly int Count;

        public GraphEventArgs(Types type, JSONObject data = null)
        {
            Type = type;
            JData = data;
        }

        public GraphEventArgs(Types type, int cunt)
        {
            Type = type;
            Count = cunt;
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
