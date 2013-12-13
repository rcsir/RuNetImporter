

//  Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using Smrf.NodeXL.GraphDataProviders;
using rcsir.net.vk.importer.Dialogs;

namespace rcsir.net.vk.importer.GraphDataProvider
{
    //*****************************************************************************
    //  Class: VKGraphDataProvider
    //
    /// <summary>
    /// Gets the network of VK friends.
    /// </summary>
    ///
    /// <remarks>
    /// Call <see cref="GraphDataProviderBase.TryGetGraphData" /> to get GraphML
    /// that describes a network of VK freinds.
    /// </remarks>
    //*****************************************************************************

    public class VKGraphDataProvider : GraphDataProviderBase
    {
        //*************************************************************************
        //  Constructor: VKGraphDataProvider()
        //
        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="VKGraphDataProvider" /> class.
        /// </summary>
        //*************************************************************************

        public VKGraphDataProvider()
            :
            base(GraphDataProviderName,
                "VK Graph data provider")
        {
            // (Do nothing.)

            AssertValid();
        }

        public VKDialog VKDialog
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
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

            return (new VKDialog());
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
            "VK Network Importer (v.0.0.1)";


        //*************************************************************************
        //  Protected fields
        //*************************************************************************

        // (None.)
    }

}
