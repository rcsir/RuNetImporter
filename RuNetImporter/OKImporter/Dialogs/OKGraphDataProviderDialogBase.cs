using Smrf.NodeXL.GraphDataProviders;

namespace rcsir.net.ok.importer.Dialogs
{
    //*****************************************************************************
    //  Class: OKGraphDataProviderDialogBase
    //
    /// <summary>
    /// Base class for dialogs that get OK graph data.
    /// </summary>
    //*****************************************************************************

    public partial class OKGraphDataProviderDialogBase :
        GraphDataProviderDialogBase
    {
        //*************************************************************************
        //  Constructor: OKGraphDataProviderDialogBase()
        //
        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="OKGraphDataProviderDialogBase" /> class.
        /// </summary>
        //*************************************************************************

        public OKGraphDataProviderDialogBase
        (
            HttpNetworkAnalyzerBase httpNetworkAnalyzer
        )
            : base(httpNetworkAnalyzer)
        {
            

            AssertValid();
        }

        //*************************************************************************
        //  Constructor: OKGraphDataProviderDialogBase()
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

        public OKGraphDataProviderDialogBase()
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
