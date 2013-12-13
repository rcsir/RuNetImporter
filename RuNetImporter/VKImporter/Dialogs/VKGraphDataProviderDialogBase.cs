using System;
using System.Net;
using System.Diagnostics;
using Smrf.AppLib;
using Smrf.NodeXL.GraphDataProviders;

namespace rcsir.net.vk.importer.Dialogs
{
    //*****************************************************************************
    //  Class: VKGraphDataProviderDialogBase
    //
    /// <summary>
    /// Base class for dialogs that get VK graph data.
    /// </summary>
    //*****************************************************************************

    public partial class VKGraphDataProviderDialogBase :
        GraphDataProviderDialogBase
    {
        //*************************************************************************
        //  Constructor: VKGraphDataProviderDialogBase()
        //
        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="VKGraphDataProviderDialogBase" /> class.
        /// </summary>
        //*************************************************************************

        public VKGraphDataProviderDialogBase
        (
            HttpNetworkAnalyzerBase httpNetworkAnalyzer
        )
            : base(httpNetworkAnalyzer)
        {
            

            AssertValid();
        }

        //*************************************************************************
        //  Constructor: VKGraphDataProviderDialogBase()
        //
        /// <summary>
        /// Do not use this constructor.
        /// </summary>
        ///
        /// <remarks>
        /// Do not use this constructor.  It is for the Visual Studio designer
        /// only.
        /// </remarks>
        //*************************************************************************

        public VKGraphDataProviderDialogBase()
        {
            // (Do nothing.)
        }


        //*************************************************************************
        //  Method: AssertValid()
        //
        /// <summary>
        /// Asserts if the object is in an invalid state.  Debug-only.
        /// </summary>
        //*************************************************************************

        // [Conditional("DEBUG")]

        public override void
        AssertValid()
        {
            base.AssertValid();

            
        }


        //*************************************************************************
        //  Protected fields
        //*************************************************************************

        // This is static so that the dialog's controls will retain their values
        // between dialog invocations.  Most NodeXL dialogs persist control values
        // via ApplicationSettingsBase, but this plugin does not have access to
        // that and so it resorts to static fields.

        
    }
}
