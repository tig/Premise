// Copyright 2012 Kindel Systems, LLC
//   
// This file is part of PremiseWP7
//   

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;


//using System.Windows.Browser;

namespace Premise.Services {
    /*
     * There is an issue with HttpWebRequest and WebRequest.  If the web service does not return anything
     * in the message body, the object will still linger and new requests will start to fail after about 8.
     * So, the "responseExpected" field is used to Abort the request after a short period.  This allows the
     * request to go out, but then aborts waiting for a response.  SYS's response to a "e" POST will not
     * contain a message body.
    */

    public class HttpHelper {
        private static int _httpRequestsOutstanding;

        /// <summary>
        ///   OPTIMIZATION: Requests that expect a response are tracked in this list so that
        ///   we do not request mulitple times. This is only for requests with "response = true"
        /// </summary>
        //static private List<Uri> OutstandingResponseRequests = new List<Uri>();
        private readonly HttpWebRequest _request;

        // Content contains the data to be sent in the message body.
        public string Content;
        /*
         * Some requests do not send a response.  This is set in those cases so that we 
         * can "cancel" the request after a short period of time (i.e. long enough for the request
         * to be sent over.
         */

        public Object Context;
        public bool ResponseExpected;
        private Timer _canceltimer;

        public HttpHelper(Uri requestUri, string httpMethod, string content, bool response)
            : this(requestUri, httpMethod, content, response, null) {
        }

        public HttpHelper(Uri requestUri, string httpMethod, string content, bool response, object context) {
           Debug.WriteLine(String.Format("HttpHelper: requestURI = <{0}>, content = {1}, responseExpected = {2}", requestUri, content, response));

            _request = (HttpWebRequest) WebRequest.Create(requestUri);
            _request.ContentType = "application/x-www-form-urlencoded";
            _request.Method = httpMethod;

            Content = content;
            ResponseExpected = response;
            Context = context;
        }

        public HttpHelper(Uri requestUri, string httpMethod, string content, bool response, object context,
                          string username, string password)
            : this(requestUri, httpMethod, content, response, context) {
            _request.Credentials = new NetworkCredential(username, password);
        }

        private static int HttpRequestsOutstanding {
            get { return _httpRequestsOutstanding; }
            set {
                _httpRequestsOutstanding = value;
               // ViewModelLocator.MainViewModelStatic.ProgressIndicatorVisible = (HttpRequestsOutstanding > 0);
            }
        }

        public event HttpResponseCompleteEventHandler ResponseComplete;

        private void OnResponseComplete(HttpResponseCompleteEventArgs e) {
            if (ResponseComplete != null)
                ResponseComplete(e);
        }

        public void Execute() {
            _request.BeginGetRequestStream(BeginRequest, this);

        }

        private static void BeginRequest(IAsyncResult ar) {
            //Debug.WriteLine("BeginRequest");
            var helper = ar.AsyncState as HttpHelper;
            if (helper == null) return;
            if (helper._request == null) {
                Debug.WriteLine("request null");
                return;
            }

            //if (helper.responseExpected && OutstandingResponseRequests.Contains(helper.request.RequestUri))
            //{
            //    Debug.WriteLine("Request already outstanding: {0}", helper.request.RequestUri);
            //    return;
            //}

            if (helper.Content != null) {
                using (var writer = new StreamWriter(helper._request.EndGetRequestStream(ar))) {
                    writer.Write(helper.Content);
                }
            }

            //if (helper.responseExpected)
            //    OutstandingResponseRequests.Add(helper.request.RequestUri);

            ++HttpRequestsOutstanding;
            Debug.WriteLine("BeginGetResponse: " + helper._request.RequestUri);
            helper._request.BeginGetResponse(BeginResponse, helper);
        }

        private static void BeginResponse(IAsyncResult ar) {
            Debug.WriteLine("BeginResponse");
            var helper = ar.AsyncState as HttpHelper;

            // When we aren't expecting a response, the timer will call Abort and we fall into here.
            // Check the response flag to see whether we should continue.
            if ((helper == null) ) return;
            Debug.WriteLine("BeginResponse: uri = " + helper.Context.ToString());

            try {
                var response = (HttpWebResponse) helper._request.EndGetResponse(ar);
                if (response == null) return;
                Stream stream = response.GetResponseStream();
                if (stream == null) return;
                using (var reader = new StreamReader(stream)) {
                    helper.OnResponseComplete(new HttpResponseCompleteEventArgs(reader.ReadToEnd(),
                                                                                helper.Context, true));
                }
            }
            catch (WebException e) {
                Debug.WriteLine(e.Status + " + " + e.Message);
                helper.OnResponseComplete(new HttpResponseCompleteEventArgs(e.Status + " + " + e.Message,
                                                                            helper.Context, false));

                helper._request.Abort();
            }
            finally {
                //if (helper.responseExpected && OutstandingResponseRequests.Contains(helper.request.RequestUri))
                //    OutstandingResponseRequests.Remove(helper.request.RequestUri);
                --HttpRequestsOutstanding;
            }
        }
    }

    public delegate void HttpResponseCompleteEventHandler(HttpResponseCompleteEventArgs e);

    public class HttpResponseCompleteEventArgs : EventArgs {
        public string Response;
        public bool Succeeded;
        public Object Context;

        public HttpResponseCompleteEventArgs(string response, Object context, bool succeeded) {
            Response = response;
            this.Context = context;
            this.Succeeded = succeeded;
        }
    }
}