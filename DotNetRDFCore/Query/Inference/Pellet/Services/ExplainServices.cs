/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using VDS.RDF.Parsing;
#if !NO_WEB
using System.Web;
#endif

namespace VDS.RDF.Query.Inference.Pellet.Services
{
    /// <summary>
    /// Represents the Explain Service provided by a Pellet Server
    /// </summary>
    public class ExplainService 
        : PelletService
    {
        private String _explainUri;
        /// <summary>
        /// Base Query for use with the Explain Service
        /// </summary>
        protected SparqlParameterizedString _baseQuery = new SparqlParameterizedString("SELECT * WHERE { @s @p @o . }");

        /// <summary>
        /// Creates a new Explain Service
        /// </summary>
        /// <param name="name">Service Name</param>
        /// <param name="obj">JSON Object</param>
        internal ExplainService(String name, JObject obj)
            : base(name, obj) 
        {
            if (!this.Endpoint.Uri.EndsWith("explain"))
            {
                this._explainUri = this.Endpoint.Uri.Substring(0, this.Endpoint.Uri.IndexOf("explain") + 7);
            }
            else
            {
                this._explainUri = this.Endpoint.Uri;
            }
        }

#if !NO_SYNC_HTTP

        /// <summary>
        /// Gets a Graph explaining the result of the SPARQL Query
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <returns></returns>
        public IGraph Explain(String sparqlQuery)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this._explainUri + "?query=" + HttpUtility.UrlEncode(sparqlQuery));
            request.Method = this.Endpoint.HttpMethods.First();
            request.Accept = MimeTypesHelper.CustomHttpAcceptHeader(this.MimeTypes, MimeTypesHelper.SupportedRdfMimeTypes);

            Tools.HttpDebugRequest(request);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Tools.HttpDebugResponse(response);
                    IRdfReader parser = MimeTypesHelper.GetParser(response.ContentType);
                    Graph g = new Graph();
                    parser.Load(g, new StreamReader(response.GetResponseStream()));

                    response.Close();
                    return g;
                }
            }
            catch (WebException webEx)
            {
                if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                throw new RdfReasoningException("A HTTP error occurred while communicating with the Pellet Server", webEx);
            }
        }

#endif

        /// <summary>
        /// Gets a Graph explaining the result of the SPARQL Query
        /// </summary>
        /// <param name="sparqlQuery">SPARQL Query</param>
        /// <param name="callback">Callback to invoke when the operation completes</param>
        /// <param name="state">State to pass to the callback</param>
        /// <remarks>
        /// If the operation succeeds the callback will be invoked normally, if there is an error the callback will be invoked with a instance of <see cref="AsyncError"/> passed as the state which provides access to the error message and the original state passed in.
        /// </remarks>
        public void Explain(String sparqlQuery, GraphCallback callback, Object state)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this._explainUri + "?query=" + HttpUtility.UrlEncode(sparqlQuery));
            request.Method = this.Endpoint.HttpMethods.First();
            request.Accept = MimeTypesHelper.CustomHttpAcceptHeader(this.MimeTypes, MimeTypesHelper.SupportedRdfMimeTypes);

            Tools.HttpDebugRequest(request);

            try
            {
                request.BeginGetResponse(result =>
                    {
                        try
                        {
                            using (HttpWebResponse response = (HttpWebResponse) request.EndGetResponse(result))
                            {
                                Tools.HttpDebugResponse(response);
                                IRdfReader parser = MimeTypesHelper.GetParser(response.ContentType);
                                Graph g = new Graph();
                                parser.Load(g, new StreamReader(response.GetResponseStream()));

                                response.Close();
                                callback(g, state);
                            }
                        }
                        catch (WebException webEx)
                        {
                            if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                            callback(null, new AsyncError(new RdfReasoningException("A HTTP error occurred while communicating with the Pellet Server, see inner exception for details", webEx), state));
                        }
                        catch (Exception ex)
                        {
                            callback(null, new AsyncError(new RdfReasoningException("An unexpected error occurred while communicating with the Pellet Server, see inner exception for details", ex), state));
                        }
                    }, null);
            }
            catch (WebException webEx)
            {
                if (webEx.Response != null) Tools.HttpDebugResponse((HttpWebResponse)webEx.Response);
                callback(null, new AsyncError(new RdfReasoningException("A HTTP error occurred while communicating with the Pellet Server, see inner exception for details", webEx), state));
            }
            catch (Exception ex)
            {
                callback(null, new AsyncError(new RdfReasoningException("An unexpected error occurred while communicating with the Pellet Server, see inner exception for details", ex), state));
            }
        }
    }

    /// <summary>
    /// Represents the Explan Unsatisfiable Service provided by a Pellet Server
    /// </summary>
    public class ExplainUnsatService 
        : ExplainService
    {
        /// <summary>
        /// Creates a new Explain Unsatisfiable Service
        /// </summary>
        /// <param name="name">Service Name</param>
        /// <param name="obj">JSON Object</param>
        internal ExplainUnsatService(String name, JObject obj)
            : base(name, obj) { }

#if !NO_SYNC_HTTP

        /// <summary>
        /// Gets a Graph explaining why a Class is unsatisfiable
        /// </summary>
        /// <param name="cls">Class</param>
        /// <returns></returns>
        public IGraph ExplainUnsatisfiable(INode cls)
        {
            this._baseQuery.SetParameter("s", cls);
            this._baseQuery.SetUri("p", UriFactory.Create(NamespaceMapper.RDFS + "subClassOf"));
            this._baseQuery.SetUri("o", UriFactory.Create(OwlHelper.OwlNothing));

            return base.Explain(this._baseQuery.ToString());
        }

#endif

        /// <summary>
        /// Gets a Graph explaining why a Class is unsatisfiable
        /// </summary>
        /// <param name="cls">Class</param>
        /// <param name="callback">Callback to invoke when the operation completes</param>
        /// <param name="state">State to pass to the callback</param>
        /// <remarks>
        /// If the operation succeeds the callback will be invoked normally, if there is an error the callback will be invoked with a instance of <see cref="AsyncError"/> passed as the state which provides access to the error message and the original state passed in.
        /// </remarks>
        public void ExplainUnsatisfiable(INode cls, GraphCallback callback, Object state)
        {
            this._baseQuery.SetParameter("s", cls);
            this._baseQuery.SetUri("p", UriFactory.Create(NamespaceMapper.RDFS + "subClassOf"));
            this._baseQuery.SetUri("o", UriFactory.Create(OwlHelper.OwlNothing));

            base.Explain(this._baseQuery.ToString(), callback, state);
        }
    }

    /// <summary>
    /// Represents the Explain Instance Service provided by a Pellet Server
    /// </summary>
    public class ExplainInstanceService
        : ExplainService
    {
        /// <summary>
        /// Creates a new Explain Instance Service
        /// </summary>
        /// <param name="name">Service Name</param>
        /// <param name="obj">JSON Object</param>
        internal ExplainInstanceService(String name, JObject obj)
            : base(name, obj) { }

#if !NO_SYNC_HTTP

        /// <summary>
        /// Gets a Graph explaining why an Instance is of the given Class
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <param name="cls">Class</param>
        /// <returns></returns>
        public IGraph ExplainInstance(INode instance, INode cls)
        {
            this._baseQuery.SetParameter("s", instance);
            this._baseQuery.SetUri("p", UriFactory.Create(RdfSpecsHelper.RdfType));
            this._baseQuery.SetParameter("o", cls);

            return base.Explain(this._baseQuery.ToString());
        }

#endif

        /// <summary>
        /// Gets a Graph explaining why an Instance is of the given Class
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <param name="cls">Class</param>
        /// <param name="callback">Callback to invoke when the operation completes</param>
        /// <param name="state">State to pass to the callback</param>
        /// <remarks>
        /// If the operation succeeds the callback will be invoked normally, if there is an error the callback will be invoked with a instance of <see cref="AsyncError"/> passed as the state which provides access to the error message and the original state passed in.
        /// </remarks>
        public void ExplainInstance(INode instance, INode cls, GraphCallback callback, Object state)
        {
            this._baseQuery.SetParameter("s", instance);
            this._baseQuery.SetUri("p", UriFactory.Create(RdfSpecsHelper.RdfType));
            this._baseQuery.SetParameter("o", cls);

            base.Explain(this._baseQuery.ToString(), callback, state);
        }
    }

    /// <summary>
    /// Represents the Explain Subclass Service provided by a Pellet Server
    /// </summary>
    public class ExplainSubclassService 
        : ExplainService
    {
        /// <summary>
        /// Creates a new Explain Subclass Service
        /// </summary>
        /// <param name="name">Service Name</param>
        /// <param name="obj">JSON Object</param>
        internal ExplainSubclassService(String name, JObject obj)
            : base(name, obj) { }

#if !NO_SYNC_HTTP

        /// <summary>
        /// Gets a Graph explaining why the given Class is a subclass of the given Super Class
        /// </summary>
        /// <param name="subclass">Class</param>
        /// <param name="superclass">Super Class</param>
        /// <returns></returns>
        public IGraph ExplainSubclass(INode subclass, INode superclass)
        {
            this._baseQuery.SetParameter("s", subclass);
            this._baseQuery.SetUri("p", UriFactory.Create(NamespaceMapper.RDFS + "subClassOf"));
            this._baseQuery.SetParameter("o", superclass);

            return base.Explain(this._baseQuery.ToString());
        }

#endif

        /// <summary>
        /// Gets a Graph explaining why the given Class is a subclass of the given Super Class
        /// </summary>
        /// <param name="subclass">Class</param>
        /// <param name="superclass">Super Class</param>
        /// <param name="callback">Callback to invoke when the operation completes</param>
        /// <param name="state">State to pass to the callback</param>
        /// <remarks>
        /// If the operation succeeds the callback will be invoked normally, if there is an error the callback will be invoked with a instance of <see cref="AsyncError"/> passed as the state which provides access to the error message and the original state passed in.
        /// </remarks>
        public void ExplainSubclass(INode subclass, INode superclass, GraphCallback callback, Object state)
        {
            this._baseQuery.SetParameter("s", subclass);
            this._baseQuery.SetUri("p", UriFactory.Create(NamespaceMapper.RDFS + "subClassOf"));
            this._baseQuery.SetParameter("o", superclass);

            base.Explain(this._baseQuery.ToString(), callback, state);
        }
    }

    /// <summary>
    /// Represents the Explain Inconsistent Service provided by a Pellet Server
    /// </summary>
    public class ExplainInconsistentService
        : ExplainService
    {
        /// <summary>
        /// Creates a new Explain Inconsistent Service
        /// </summary>
        /// <param name="name">Service Name</param>
        /// <param name="obj">JSON Object</param>
        internal ExplainInconsistentService(String name, JObject obj)
            : base(name, obj) { }

#if !NO_SYNC_HTTP

        /// <summary>
        /// Gets a Graph explaining why the Knowledge Base is inconsistent
        /// </summary>
        /// <returns></returns>
        public IGraph ExplainInconsistent()
        {
            return base.Explain(String.Empty);
        }

#endif

        /// <summary>
        /// Gets a Graph explaining why the Knowledge Base is inconsistent
        /// </summary>
        /// <param name="callback">Callback to invoke when the operation completes</param>
        /// <param name="state">State to pass to the callback</param>
        /// <remarks>
        /// If the operation succeeds the callback will be invoked normally, if there is an error the callback will be invoked with a instance of <see cref="AsyncError"/> passed as the state which provides access to the error message and the original state passed in.
        /// </remarks>
        public void ExplainInconsistent(GraphCallback callback, Object state)
        {
            base.Explain(String.Empty, callback, state);
        }
    }

    /// <summary>
    /// Represents the Explain Property Service provided by a Pellet Server
    /// </summary>
    public class ExplainPropertyService
        : ExplainService
    {
        /// <summary>
        /// Creates a new Explain Property Service
        /// </summary>
        /// <param name="name">Service Name</param>
        /// <param name="obj">JSON Object</param>
        internal ExplainPropertyService(String name, JObject obj)
            : base(name, obj) { }

#if !NO_SYNC_HTTP

        /// <summary>
        /// Gets a Graph explaining why the given Triple was derived
        /// </summary>
        /// <param name="subj">Subject</param>
        /// <param name="pred">Predicate</param>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public IGraph ExplainProperty(INode subj, INode pred, INode obj)
        {
            this._baseQuery.SetParameter("s", subj);
            this._baseQuery.SetParameter("p", pred);
            this._baseQuery.SetParameter("o", obj);

            return base.Explain(this._baseQuery.ToString());
        }

        /// <summary>
        /// Gets a Graph explaining why the given Triple was derived
        /// </summary>
        /// <param name="t">Triple</param>
        public IGraph ExplainProperty(Triple t)
        {
            return this.ExplainProperty(t.Subject, t.Predicate, t.Object);
        }

#endif

        /// <summary>
        /// Gets a Graph explaining why the given Triple was derived
        /// </summary>
        /// <param name="subj">Subject</param>
        /// <param name="pred">Predicate</param>
        /// <param name="obj">Object</param>
        /// <param name="callback">Callback to invoke when the operation completes</param>
        /// <param name="state">State to pass to the callback</param>
        /// <remarks>
        /// If the operation succeeds the callback will be invoked normally, if there is an error the callback will be invoked with a instance of <see cref="AsyncError"/> passed as the state which provides access to the error message and the original state passed in.
        /// </remarks>
        public void ExplainProperty(INode subj, INode pred, INode obj, GraphCallback callback, Object state)
        {
            this._baseQuery.SetParameter("s", subj);
            this._baseQuery.SetParameter("p", pred);
            this._baseQuery.SetParameter("o", obj);

            base.Explain(this._baseQuery.ToString(), callback, state);
        }

        /// <summary>
        /// Gets a Graph explaining why the given Triple was derived
        /// </summary>
        /// <param name="t">Triple</param>
        /// <param name="callback">Callback to invoke when the operation completes</param>
        /// <param name="state">State to pass to the callback</param>
        /// <remarks>
        /// If the operation succeeds the callback will be invoked normally, if there is an error the callback will be invoked with a instance of <see cref="AsyncError"/> passed as the state which provides access to the error message and the original state passed in.
        /// </remarks>
        public void ExplainProprety(Triple t, GraphCallback callback, Object state)
        {
            this.ExplainProperty(t.Subject, t.Predicate, t.Object, callback, state);
        }
    }
}
