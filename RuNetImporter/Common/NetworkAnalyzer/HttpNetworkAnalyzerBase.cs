using System;
using System.Xml;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Diagnostics;
using Smrf.AppLib;
using Smrf.XmlLib;
using rcsir.net.common.NetworkAnalyzer;

namespace Smrf.NodeXL.GraphDataProviders
{
//*****************************************************************************
//  Class: HttpNetworkAnalyzerBase
//
/// <summary>
/// Abstract base class for classes that analyze network information obtained
/// via HTTP Web requests.
/// </summary>
///
/// <remarks>
/// This base class implements properties related to HTTP Web requests, a
/// BackgroundWorker instance, and properties, methods, and events related to
/// the BackgroundWorker.  The derived class must implement a method to start
/// an analysis and implement the <see cref="BackgroundWorker_DoWork" />
/// method.
/// </remarks>
//*****************************************************************************

public abstract class HttpNetworkAnalyzerBase : NetworkAnalyzerBase
{
    //*************************************************************************
    //  Constructor: HttpNetworkAnalyzerBase()
    //
    /// <summary>
    /// Initializes a new instance of the <see
    /// cref="HttpNetworkAnalyzerBase" /> class.
    /// </summary>
    //*************************************************************************

    public HttpNetworkAnalyzerBase()
    {
        m_oBackgroundWorker = new BackgroundWorker();
        m_oBackgroundWorker.WorkerSupportsCancellation = true;
        m_oBackgroundWorker.WorkerReportsProgress = true;

        m_oBackgroundWorker.DoWork += new DoWorkEventHandler(
            BackgroundWorker_DoWork);

        m_oBackgroundWorker.ProgressChanged += new ProgressChangedEventHandler(
            BackgroundWorker_ProgressChanged);

        m_oBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                BackgroundWorker_RunWorkerCompleted);
    }

    //*************************************************************************
    //  Property: IsBusy
    //
    /// <summary>
    /// Gets a flag indicating whether an asynchronous operation is in
    /// progress.
    /// </summary>
    ///
    /// <value>
    /// true if an asynchronous operation is in progress.
    /// </value>
    //*************************************************************************

    public Boolean
    IsBusy
    {
        get
        {
            return (m_oBackgroundWorker.IsBusy);
        }
    }

    //*************************************************************************
    //  Method: CancelAsync()
    //
    /// <summary>
    /// Cancels the analysis started by an async method.
    /// </summary>
    ///
    /// <remarks>
    /// When the analysis cancels, the <see cref="AnalysisCompleted" /> event
    /// fires.  The <see cref="AsyncCompletedEventArgs.Cancelled" /> property
    /// will be true.
    ///
    /// <para>
    /// Important note: If the background thread started by an async method
    /// is running a Web request when <see cref="CancelAsync" /> is called, the
    /// cancel won't occur until the request completes.
    /// </para>
    ///
    /// </remarks>
    //*************************************************************************

    public void
    CancelAsync()
    {
        AssertValid();

        if (this.IsBusy)
        {
            m_oBackgroundWorker.CancelAsync();
        }
    }

    //*************************************************************************
    //  Method: CreateHttpWebRequest()
    //
    /// <summary>
    /// Gets an HttpWebRequest object to use.
    /// </summary>
    ///
    /// <param name="url">
    /// URL to use.
    /// </param>
    ///
    /// <returns>
    /// The HttpWebRequest object.
    /// </returns>
    //*************************************************************************

    public static HttpWebRequest
    CreateHttpWebRequest
    (
        String url
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(url) );

        HttpWebRequest oHttpWebRequest =
            (HttpWebRequest)WebRequest.Create(url);

        // Get the request to work if there is a Web proxy that requires
        // authentication.  More information:
        //
        // http://dangarner.co.uk/2008/03/18/webrequest-proxy-authentication/

        // Although Credentials is a static property that needs to be set once
        // only, setting it here guarantees that no Web request is ever made
        // before the credentials are set.

        WebRequest.DefaultWebProxy.Credentials =
            CredentialCache.DefaultCredentials;

        return (oHttpWebRequest);
    }

    //*************************************************************************
    //  Event: ProgressChanged
    //
    /// <summary>
    /// Occurs when progress is reported.
    /// </summary>
    //*************************************************************************

    public event ProgressChangedEventHandler ProgressChanged;


    //*************************************************************************
    //  Event: AnalysisCompleted
    //
    /// <summary>
    /// Occurs when the analysis started by an async method completes, is
    /// cancelled, or encounters an error.
    /// </summary>
    //*************************************************************************

    public event RunWorkerCompletedEventHandler AnalysisCompleted;


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

    public abstract String
    ExceptionToMessage
    (
        Exception oException
    );


    //*************************************************************************
    //  Property: ClassName
    //
    /// <summary>
    /// Gets the full name of this class.
    /// </summary>
    ///
    /// <value>
    /// The full name of this class, suitable for use in error messages.
    /// </value>
    //*************************************************************************

    protected String
    ClassName
    {
        get
        {
            return (this.GetType().FullName);
        }
    }

    //*************************************************************************
    //  Method: CheckIsBusy()
    //
    /// <summary>
    /// Throws an exception if an asynchronous operation is in progress.
    /// </summary>
    ///
    /// <param name="sMethodName">
    /// Name of the calling method.
    /// </param>
    //*************************************************************************

    protected void
    CheckIsBusy
    (
        String sMethodName
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sMethodName) );

        if (this.IsBusy)
        {
            throw new InvalidOperationException( String.Format(

                "{0}:{1}: An asynchronous operation is already in progress."
                ,
                this.ClassName,
                sMethodName
                ) );
        }
    }

    //*************************************************************************
    //  Method: GetXmlDocumentWithRetries()
    //
    /// <summary>
    /// Gets an XML document given an URL.  Retries after an error.
    /// </summary>
    ///
    /// <param name="sUrl">
    /// URL to use.
    /// </param>
    ///
    /// <param name="aeHttpStatusCodesToFailImmediately">
    /// An array of status codes that should be failed immediately, or null to
    /// retry all failures.  An example is HttpStatusCode.Unauthorized (401),
    /// which Twitter returns when information about a user who has "protected"
    /// status is requested.  This should not be retried, because the retries
    /// would produce exactly the same error response.
    /// </param>
    ///
    /// <param name="oRequestStatistics">
    /// A <see cref="RequestStatistics" /> object that is keeping track of
    /// requests made while getting the network.
    /// </param>
    ///
    /// <param name="asOptionalHeaderNameValuePairs">
    /// Array of name/value pairs for HTTP headers to add to the request, or
    /// null to not add any pairs.  Sample: {"Authorization", "Basic 36A4E798"}
    /// </param>
    ///
    /// <returns>
    /// The XmlDocument.
    /// </returns>
    ///
    /// <remarks>
    /// If the request fails and the HTTP status code is not one of the codes
    /// specified in <paramref name="aeHttpStatusCodesToFailImmediately" />,
    /// the request is retried.  If the retries also fail, an exception is
    /// thrown.
    ///
    /// <para>
    /// If the request fails with one of the HTTP status code contained in
    /// <paramref name="aeHttpStatusCodesToFailImmediately" />, an exception is
    /// thrown immediately.
    /// </para>
    ///
    /// <para>
    /// In either case, it is always up to the caller to handle the exceptions.
    /// This method never ignores an exception; it either retries it and throws
    /// it if all retries fail, or throws it immediately.
    /// </para>
    ///
    /// </remarks>
    //*************************************************************************

    protected XmlDocument
    GetXmlDocumentWithRetries
    (
        String sUrl,
        HttpStatusCode [] aeHttpStatusCodesToFailImmediately,
        RequestStatistics oRequestStatistics,
        params String[] asOptionalHeaderNameValuePairs
    )
    {
        Debug.Assert( !String.IsNullOrEmpty(sUrl) );
        Debug.Assert(oRequestStatistics != null);

        Debug.Assert(asOptionalHeaderNameValuePairs == null ||
            asOptionalHeaderNameValuePairs.Length % 2 == 0);

        AssertValid();

        Int32 iMaximumRetries = HttpRetryDelaysSec.Length;
        Int32 iRetriesSoFar = 0;

        while (true)
        {
            if (iRetriesSoFar > 0)
            {
                ReportProgress("Retrying request.");
            }

            // Important Note: You cannot use the same HttpWebRequest object
            // for the retries.  The object must be recreated each time.

            HttpWebRequest oHttpWebRequest = CreateHttpWebRequest(sUrl);

            Int32 iHeaderNamesAndValues =
                (asOptionalHeaderNameValuePairs == null) ?
                0 : asOptionalHeaderNameValuePairs.Length;

            for (Int32 i = 0; i < iHeaderNamesAndValues; i += 2)
            {
                String sHeaderName = asOptionalHeaderNameValuePairs[i + 0];
                String sHeaderValue = asOptionalHeaderNameValuePairs[i + 1];

                Debug.Assert( !String.IsNullOrEmpty(sHeaderName) );
                Debug.Assert( !String.IsNullOrEmpty(sHeaderValue) );

                oHttpWebRequest.Headers[sHeaderName] = sHeaderValue;
            }

            try
            {
                XmlDocument oXmlDocument =
                    GetXmlDocumentNoRetries(oHttpWebRequest);

                if (iRetriesSoFar > 0)
                {
                    ReportProgress("Retry succeeded, continuing...");
                }

                oRequestStatistics.OnSuccessfulRequest();

                return (oXmlDocument);
            }
            catch (Exception oException)
            {
                if ( !ExceptionIsWebOrXml(oException) )
                {
                    throw oException;
                }

                // A WebException or XmlException has occurred.

                if (iRetriesSoFar == iMaximumRetries)
                {
                    oRequestStatistics.OnUnexpectedException(oException);

                    throw (oException);
                }

                // If the status code is one of the ones specified in
                // aeHttpStatusCodesToFailImmediately, rethrow the exception
                // without retrying the request.

                if (aeHttpStatusCodesToFailImmediately != null &&
                    oException is WebException)
                {
                    if ( WebExceptionHasHttpStatusCode(
                            (WebException)oException,
                            aeHttpStatusCodesToFailImmediately) )
                    {
                        throw (oException);
                    }
                }

                Int32 iSeconds = HttpRetryDelaysSec[iRetriesSoFar];

                ReportProgress( String.Format(

                    "Request failed, pausing {0} {1} before retrying..."
                    ,
                    iSeconds,
                    StringUtil.MakePlural("second", iSeconds)
                    ) );

                System.Threading.Thread.Sleep(1000 * iSeconds);
                iRetriesSoFar++;
            }
        }
    }

    //*************************************************************************
    //  Method: GetXmlDocumentNoRetries()
    //
    /// <summary>
    /// Gets an XML document given an HttpWebRequest object.  Does not retry
    /// after an error.
    /// </summary>
    ///
    /// <param name="oHttpWebRequest">
    /// HttpWebRequest object to use.
    /// </param>
    ///
    /// <returns>
    /// The XmlDocument.
    /// </returns>
    ///
    /// <remarks>
    /// This method sets several properties on <paramref
    /// name="oHttpWebRequest" /> before it is used.
    /// </remarks>
    //*************************************************************************

    protected XmlDocument
    GetXmlDocumentNoRetries
    (
        HttpWebRequest oHttpWebRequest
    )
    {
        Debug.Assert(oHttpWebRequest != null);
        AssertValid();

        CheckCancellationPending();

        oHttpWebRequest.Timeout = HttpWebRequestTimeoutMs;

        // According to the Twitter API documentation, "Consumers using the
        // Search API but failing to include a User Agent string will
        // receive a lower rate limit."

        oHttpWebRequest.UserAgent = UserAgent;

        // This is to prevent "The request was aborted: The request was
        // canceled" WebExceptions that arose for Twitter on at least one
        // user's machine, at the expense of performance.  This is not a good
        // solution, but see this posting:
        //
        // http://arnosoftwaredev.blogspot.com/2006/09/
        // net-20-httpwebrequestkeepalive-and.html

        oHttpWebRequest.KeepAlive = false;

        HttpWebResponse oHttpWebResponse = null;
        Stream oStream = null;
        XmlDocument oXmlDocument;

        try
        {
            oHttpWebResponse = (HttpWebResponse)oHttpWebRequest.GetResponse();
            oStream = oHttpWebResponse.GetResponseStream();

            oXmlDocument = new XmlDocument();
            oXmlDocument.Load(oStream);
        }
        finally
        {
            if (oStream != null)
            {
                oStream.Close();
            }

            if (oHttpWebResponse != null)
            {
                oHttpWebResponse.Close();
            }
        }

        return (oXmlDocument);
    }

    //*************************************************************************
    //  Method: WebExceptionHasHttpStatusCode()
    //
    /// <summary>
    /// Determines whether a WebException has an HttpWebResponse with one of a
    /// specified set of HttpStatusCodes.
    /// </summary>
    ///
    /// <param name="oWebException">
    /// The WebException to check.
    /// </param>
    ///
    /// <param name="aeHttpStatusCodes">
    /// One or more HttpStatus codes to look for.
    /// </param>
    ///
    /// <returns>
    /// true if <paramref name="oWebException" /> has an HttpWebResponse with
    /// an HttpStatusCode contained within <paramref
    /// name="aeHttpStatusCodes" />.
    /// </returns>
    //*************************************************************************

    protected Boolean
    WebExceptionHasHttpStatusCode
    (
        WebException oWebException,
        params HttpStatusCode [] aeHttpStatusCodes
    )
    {
        Debug.Assert(oWebException != null);
        Debug.Assert(aeHttpStatusCodes != null);
        AssertValid();

        if ( !(oWebException.Response is HttpWebResponse) )
        {
            return (false);
        }

        HttpWebResponse oHttpWebResponse =
            (HttpWebResponse)oWebException.Response;

        return (Array.IndexOf<HttpStatusCode>(
            aeHttpStatusCodes, oHttpWebResponse.StatusCode) >= 0);
    }

    //*************************************************************************
    //  Method: ExceptionIsWebOrXml()
    //
    /// <summary>
    /// Determines whether an exception is a WebException or XmlException.
    /// </summary>
    ///
    /// <param name="oException">
    /// The exception to test.
    /// </param>
    ///
    /// <returns>
    /// true if the exception is a WebException or XmlException.
    /// </returns>
    //*************************************************************************

    protected Boolean
    ExceptionIsWebOrXml
    (
        Exception oException
    )
    {
        Debug.Assert(oException != null);

        return (oException is WebException || oException is XmlException);
    }

    //*************************************************************************
    //  Method: ReportProgress()
    //
    /// <summary>
    /// Reports progress.
    /// </summary>
    ///
    /// <param name="sProgressMessage">
    /// Progress message.  Can be empty but not null.
    /// </param>
    //*************************************************************************

    protected void
    ReportProgress
    (
        String sProgressMessage
    )
    {
        Debug.Assert(sProgressMessage != null);

        // This method is meant to be called when the derived class wants to
        // report progress.  It results in the
        // BackgroundWorker_ProgressChanged() method being called on the main
        // thread, which in turn fires the ProgressChanged event.

        m_oBackgroundWorker.ReportProgress(0, sProgressMessage);
    }

    //*************************************************************************
    //  Method: CheckCancellationPending()
    //
    /// <summary>
    /// Checks whether a cancellation is pending.
    /// </summary>
    ///
    /// <remarks>
    /// If an asynchronous operation is in progress and a cancellation is
    /// pending, this method throws a <see
    /// cref="CancellationPendingException" />.  When the asynchronous method
    /// catches this exception, it should set the DoWorkEventArgs.Cancel
    /// property to true and then return.
    /// </remarks>
    //*************************************************************************

    protected void
    CheckCancellationPending()
    {
        if (m_oBackgroundWorker.IsBusy &&
            m_oBackgroundWorker.CancellationPending)
        {
            throw new CancellationPendingException();
        }
    }

    //*************************************************************************
    //  Method: FireProgressChanged()
    //
    /// <summary>
    /// Fires the ProgressChanged event if appropriate.
    /// </summary>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    FireProgressChanged
    (
        ProgressChangedEventArgs e
    )
    {
        AssertValid();

        ProgressChangedEventHandler oProgressChanged = this.ProgressChanged;

        if (oProgressChanged != null)
        {
            oProgressChanged(this, e);
        }
    }

    //*************************************************************************
    //  Method: OnNetworkObtainedWithoutTerminatingException()
    //
    /// <summary>
    /// Call this when part or all of the network has been obtained without a
    /// terminating exception occurring.
    /// </summary>
    ///
    /// <param name="oGraphMLXmlDocument">
    /// GraphMLXmlDocument being populated.
    /// </param>
    ///
    /// <param name="oRequestStatistics">
    /// A <see cref="RequestStatistics" /> object that is keeping track of
    /// requests made while getting the network.
    /// </param>
    ///
    /// <param name="sNetworkDescription">
    /// A description of the network.
    /// </param>
    ///
    /// <remarks>
    /// If the entire network has been obtained, this method does nothing.
    /// Otherwise, a PartialNetworkException is thrown.
    /// </remarks>
    //*************************************************************************

    protected void
    OnNetworkObtainedWithoutTerminatingException
    (
        GraphMLXmlDocument oGraphMLXmlDocument,
        RequestStatistics oRequestStatistics,
        String sNetworkDescription
    )
    {
        Debug.Assert(oGraphMLXmlDocument != null);
        Debug.Assert(oRequestStatistics != null);
        Debug.Assert( !String.IsNullOrEmpty(sNetworkDescription) );
        AssertValid();

        XmlUtil2.SetAttributes(oGraphMLXmlDocument.GraphXmlNode, "description",
            sNetworkDescription);

        if (oRequestStatistics.UnexpectedExceptions > 0)
        {
            // The network is partial.

            throw new PartialNetworkException(oGraphMLXmlDocument,
                oRequestStatistics);
        }
    }

    //*************************************************************************
    //  Method: OnTerminatingException()
    //
    /// <summary>
    /// Handles an exception that unexpectedly terminated the process of
    /// getting the network.
    /// </summary>
    ///
    /// <param name="oException">
    /// The exception that occurred.
    /// </param>
    ///
    /// <remarks>
    /// This should be called only when an unexpected exception occurs,
    /// retrying the request doesn't fix it, and the process must be
    /// terminated.
    /// </remarks>
    //*************************************************************************

    protected void
    OnTerminatingException
    (
        Exception oException
    )
    {
        Debug.Assert(oException != null);
        AssertValid();

        // For now, just rethrow the exception.  In the future, some other code
        // that needs to run whenever an unexpected termination occurs can go
        // here rather than be duplicated in all the network analayzers.

        throw (oException);
    }

    //*************************************************************************
    //  Method: BackgroundWorker_DoWork()
    //
    /// <summary>
    /// Handles the DoWork event on the BackgroundWorker object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Source of the event.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event arguments.
    /// </param>
    //*************************************************************************

    protected abstract void
    BackgroundWorker_DoWork
    (
        object sender,
        DoWorkEventArgs e
    );

    //*************************************************************************
    //  Method: BackgroundWorker_ProgressChanged()
    //
    /// <summary>
    /// Handles the ProgressChanged event on the BackgroundWorker.
    /// </summary>
    ///
    /// <param name="sender">
    /// Standard event argument.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event argument.
    /// </param>
    //*************************************************************************

    protected void
    BackgroundWorker_ProgressChanged
    (
        object sender,
        ProgressChangedEventArgs e
    )
    {
        AssertValid();

        FireProgressChanged(e);
    }

    //*************************************************************************
    //  Method: BackgroundWorker_RunWorkerCompleted()
    //
    /// <summary>
    /// Handles the RunWorkerCompleted event on the BackgroundWorker object.
    /// </summary>
    ///
    /// <param name="sender">
    /// Source of the event.
    /// </param>
    ///
    /// <param name="e">
    /// Standard event arguments.
    /// </param>
    //*************************************************************************

    protected void
    BackgroundWorker_RunWorkerCompleted
    (
        object sender,
        RunWorkerCompletedEventArgs e
    )
    {
        AssertValid();

        FireProgressChanged( new ProgressChangedEventArgs(0, String.Empty) );

        // Forward the event.

        RunWorkerCompletedEventHandler oAnalysisCompleted =
            this.AnalysisCompleted;

        if (oAnalysisCompleted != null)
        {
            oAnalysisCompleted(this, e);
        }
    }


    //*************************************************************************
    /// <summary>
    /// Asserts if the object is in an invalid state.  Debug-only.
    /// </summary>
    //*************************************************************************
    public override void
    AssertValid()
    {
        Debug.Assert(m_oBackgroundWorker != null);
    }

    //*************************************************************************
    //  Public constants
    //*************************************************************************

    /// User agent to use for all Web requests.

    public const String UserAgent = "Microsoft NodeXL";

    /// The timeout to use for HTTP Web requests, in milliseconds.

    public const Int32 HttpWebRequestTimeoutMs = 30000;


    //*************************************************************************
    //  Protected constants
    //*************************************************************************

    /// URI of the Atom namespace.

    protected const String AtomNamespaceUri =
        "http://www.w3.org/2005/Atom";

    /// Time to wait between retries to the HTTP Web service, in seconds.  The
    /// length of the array determines the number of retries: three array
    /// elements means there will be up to three retries, or four attempts
    /// total.

    protected static Int32 [] HttpRetryDelaysSec =
        new Int32 [] {1, 1, 5,};


    //*************************************************************************
    //  Protected fields
    //*************************************************************************

    /// Used for asynchronous analysis.

    protected BackgroundWorker m_oBackgroundWorker;
}

}
