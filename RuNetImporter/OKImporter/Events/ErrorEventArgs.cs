
namespace rcsir.net.ok.importer.Events
{
    public class ErrorEventArgs
    {
        public readonly GraphEventArgs.Types Type;
        public readonly string Error;

        public ErrorEventArgs(GraphEventArgs.Types type, string error)
        {
            Type = type;
            Error = error;
        }
    }
}
