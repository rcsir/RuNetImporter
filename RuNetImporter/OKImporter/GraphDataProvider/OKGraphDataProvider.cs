using System;
using rcsir.net.ok.importer.Dialogs;
using Smrf.NodeXL.GraphDataProviders;

namespace rcsir.net.ok.importer.GraphDataProvider
{
    //*****************************************************************************
    //  Class: OKGraphDataProvider
    //
    /// <summary>
    /// Gets the network of OK friends.
    /// </summary>
    ///
    /// <remarks>
    /// Call <see cref="GraphDataProviderBase.TryGetGraphData" /> to get GraphML
    /// that describes a network of OK freinds.
    /// </remarks>
    //*****************************************************************************

    public class OKGraphDataProvider : GraphDataProviderBase
    {
        //*************************************************************************
        //  Constructor: OKGraphDataProvider()
        //
        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="OKGraphDataProvider" /> class.
        /// </summary>
        //*************************************************************************

        public OKGraphDataProvider()
            :
            base(GraphDataProviderName,
                "OK Graph data provider")
        {
            // (Do nothing.)

            AssertValid();
        }

        //*************************************************************************
        //  Method: CreateDialog()
        //
        /// <summary>
        /// Creates a dialog for getting graph data.
        /// </summary>
        ///
        /// <returns>
        /// A dialog derived from GraphDataProviderDialogBase.
        /// </returns>
        //*************************************************************************

        protected override GraphDataProviderDialogBase
        CreateDialog()
        {
            AssertValid();

            return (new OKDialog());
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

            // (Do nothing else.)
        }


        //*************************************************************************
        //  Public constants
        //*************************************************************************

        /// Value of the Name property.

        public const String GraphDataProviderName =
            "Odnoklassniki Network Importer (v.0.0.1)";


        //*************************************************************************
        //  Protected fields
        //*************************************************************************

        // (None.)
    }

}
