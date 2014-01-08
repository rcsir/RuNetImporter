using System;

namespace rcsir.net.ok.importer.Events
{
    public class CommandEventArgs : EventArgs
    {
        public enum Commands { GetAccessToken, LoadFriends, GetGraphByAreFriends, GetGraphByMutualFriends, GenerateGraphByAreFriends, GenerateGraphByMutualFriends, UpdateAllAttributes };

        public readonly Commands CommandName;
        public readonly string Parameter;
        public readonly bool[] Rows;
        public readonly bool IsMeIncluding;

        public CommandEventArgs(Commands name, string parameter = null)
        {
            CommandName = name;
            Parameter = parameter;
        }

        public CommandEventArgs(Commands name, bool[] rows, bool isMeIncluding)
        {
            CommandName = name;
            Rows = rows;
            IsMeIncluding = isMeIncluding;
        }
    }
}
