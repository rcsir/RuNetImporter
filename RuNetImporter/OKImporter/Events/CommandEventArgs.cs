using System;
using System.Windows.Forms;

namespace rcsir.net.ok.importer.Events
{
    public class CommandEventArgs : EventArgs
    {
        public enum Commands { GetAccessToken, LoadFriends, GetGraphByAreFriends, GetGraphByMutualFriends, GenerateGraphByAreFriends, GenerateGraphByMutualFriends, GenerateGraph, MakeAttributes };

        public readonly Commands CommandName;
        public readonly string Parameter;
        public readonly DataGridViewRow[] Rows;
        public readonly bool IsMeIncluding;

        public CommandEventArgs(Commands name, string parameter = null)
        {
            CommandName = name; //    (Commands)Enum.Parse(typeof(Commands), name);
            Parameter = parameter;
        }

        public CommandEventArgs(Commands name, DataGridViewRow[] rows, bool isMeIncluding)
        {
            CommandName = name; //    (Commands)Enum.Parse(typeof(Commands), name);
            Rows = rows;
            IsMeIncluding = isMeIncluding;
        }
    }
}
