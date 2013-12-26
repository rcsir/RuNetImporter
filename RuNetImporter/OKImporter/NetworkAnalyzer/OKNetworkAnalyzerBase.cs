using System;
using System.Diagnostics;
using System.Net;
using Smrf.AppLib;
using Smrf.NodeXL.GraphDataProviders;

namespace rcsir.net.ok.importer.NetworkAnalyzer
{
    //*****************************************************************************
    //  Class: OKNetworkAnalyzerBase
    //
    /// <summary>
    /// Base class for classes that analyze a OK network.
    /// </summary>
    //*****************************************************************************

    public abstract class OKNetworkAnalyzerBase : HttpNetworkAnalyzerBase
    {
        //*************************************************************************
        //  Constructor: OKNetworkAnalyzerBase()
        //
        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="OKNetworkAnalyzerBase" /> class.
        /// </summary>
        //*************************************************************************

        public OKNetworkAnalyzerBase()
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
                "The OK Web service didn't respond.";

            
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

        /// HTTP status codes that have special meaning with OK.  When they
        /// occur, the requests are not retried.

        protected static readonly HttpStatusCode[]
            HttpStatusCodesToFailImmediately = new HttpStatusCode[] {

        };


        //*************************************************************************
        //  Protected fields
        //*************************************************************************

        ///
        protected const String OKURL = "http://www.odnoklassniki.ru/";
        /// GraphML-attribute IDs.


        //*************************************************************************
        //  Embedded class: GetNetworkAsyncArgsBase()
        //
        /// <summary>
        /// Base class for classes that contain the arguments needed to
        /// asynchronously get a OK network.
        /// </summary>
        //*************************************************************************

        protected class GetNetworkAsyncArgsBase
        {
            ///
            public String AccessToken;
        };
    }

}
