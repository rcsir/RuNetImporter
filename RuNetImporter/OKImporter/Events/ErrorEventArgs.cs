using System;

namespace rcsir.net.ok.importer.Events
{
    public class ErrorEventArgs : EventArgs
    {
        public readonly string Type;
        public readonly string Description;

        public ErrorEventArgs(string type, string description)
        {
            Type = type;
            Description = description;
        }
    }
}
