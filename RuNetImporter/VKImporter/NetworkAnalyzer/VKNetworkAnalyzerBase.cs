
//  Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Xml;
using System.Web;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Smrf.AppLib;
using Smrf.XmlLib;
using Smrf.NodeXL.GraphDataProviders;

namespace rcsir.net.vk.importer.NetworkAnalyzer
{
    //*****************************************************************************
    //  Class: VKNetworkAnalyzerBase
    //
    /// <summary>
    /// Base class for classes that analyze a VK network.
    /// </summary>
    //*****************************************************************************

    public abstract class VKNetworkAnalyzerBase : HttpNetworkAnalyzerBase
    {
        //*************************************************************************
        //  Constructor: VKNetworkAnalyzerBase()
        //
        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="VKNetworkAnalyzerBase" /> class.
        /// </summary>
        //*************************************************************************

        public VKNetworkAnalyzerBase()
        {
            // (Do nothing.)

            AssertValid();
        }

        //*************************************************************************
        //  Method: ExceptionToMessage()
        //
        /// <summary>
        /// Converts an exception to an error message appropriate for a user
        /// interface.
        /// </summary>
        ///
        /// <param name="oException">
        /// The exception that occurred.
        /// </param>
        ///
        /// <returns>
        /// An error message appropriate for a user interface.
        /// </returns>
        //*************************************************************************

        public override String
        ExceptionToMessage
        (
            Exception oException
        )
        {
            Debug.Assert(oException != null);
            AssertValid();

            String sMessage = null;

            const String TimeoutMessage =
                "The VK Web service didn't respond.";

            
            if (oException is WebException)
            {
                WebException oWebException = (WebException)oException;

                if (oWebException.Response is HttpWebResponse)
                {
                    HttpWebResponse oHttpWebResponse =
                        (HttpWebResponse)oWebException.Response;

                    switch (oHttpWebResponse.StatusCode)
                    {
                        case HttpStatusCode.RequestTimeout:  // HTTP 408.

                            sMessage = TimeoutMessage;
                            break;

                        default:

                            break;
                    }
                }
                else
                {
                    switch (oWebException.Status)
                    {
                        case WebExceptionStatus.Timeout:

                            sMessage = TimeoutMessage;
                            break;

                        default:

                            break;
                    }
                }
            }

            if (sMessage == null)
            {
                sMessage = ExceptionUtil.GetMessageTrace(oException);
            }

            return (sMessage);
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
        //  Protected constants
        //*************************************************************************

        /// HTTP status codes that have special meaning with VK.  When they
        /// occur, the requests are not retried.

        protected static readonly HttpStatusCode[]
            HttpStatusCodesToFailImmediately = new HttpStatusCode[] {

        };


        //*************************************************************************
        //  Protected fields
        //*************************************************************************

        ///
        protected const String VKURL = "http://www.vk.com/";
        /// GraphML-attribute IDs.


        //*************************************************************************
        //  Embedded class: GetNetworkAsyncArgsBase()
        //
        /// <summary>
        /// Base class for classes that contain the arguments needed to
        /// asynchronously get a VK network.
        /// </summary>
        //*************************************************************************

        protected class GetNetworkAsyncArgsBase
        {
            ///
            public String AccessToken;
        };
    }

}
