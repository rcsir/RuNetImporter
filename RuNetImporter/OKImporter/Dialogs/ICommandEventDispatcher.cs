using System;
using Smrf.AppLib;
using rcsir.net.ok.importer.Events;

namespace rcsir.net.ok.importer.Dialogs
{
    public interface ICommandEventDispatcher
    {
        OKLoginDialog LoginDialog { get; }
        AttributesDictionary<bool> DialogAttributes { set; }
        string AuthUri { set; }
        
        event EventHandler<CommandEventArgs> CommandEventHandler;

        void OnData(object obj, GraphEventArgs graphEvent = null);
    }
}