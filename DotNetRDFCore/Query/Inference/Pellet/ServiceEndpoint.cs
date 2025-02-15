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
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace VDS.RDF.Query.Inference.Pellet
{
    /// <summary>
    /// Represents the Service Endpoint for a Service provided by a Pellet Server
    /// </summary>
    public class ServiceEndpoint
    {
        private List<String> _methods = new List<string>();
        private String _uri;

        /// <summary>
        /// Creates a new Service Endpoint instance
        /// </summary>
        /// <param name="obj">JSON Object representing the Endpoint</param>
        internal ServiceEndpoint(JObject obj)
        {
            this._uri = (String)obj.SelectToken("url");
            JToken methods = obj.SelectToken("http-methods");
            foreach (JToken method in methods.Children())
            {
                this._methods.Add((String)method);
            }
        }

        /// <summary>
        /// Gets the URI of the Endpoint
        /// </summary>
        public String Uri
        {
            get
            {
                return this._uri;
            }
        }

        /// <summary>
        /// Gets the HTTP Methods supported by the Endpoint
        /// </summary>
        public IEnumerable<String> HttpMethods
        {
            get
            {
                return this._methods;
            }
        }
    }
}
