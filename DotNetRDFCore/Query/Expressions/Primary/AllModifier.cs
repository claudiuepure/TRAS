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
using System.Linq;
using System.Text;
using VDS.RDF.Nodes;

namespace VDS.RDF.Query.Expressions.Primary
{
    /// <summary>
    /// Class for representing the All Modifier
    /// </summary>
    public class AllModifier 
        : ISparqlExpression
    {
        /// <summary>
        /// Throws a <see cref="NotImplementedException">NotImplementedException</see> since this class is a placeholder and only used in parsing
        /// </summary>
        /// <param name="context">SPARQL Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public IValuedNode Evaluate(SparqlEvaluationContext context, int bindingID)
        {
            throw new NotImplementedException("This class is a placeholder only - aggregates taking this as an argument should apply over all rows");
        }

        /// <summary>
        /// Returns an empty enumerable
        /// </summary>
        public IEnumerable<string> Variables
        {
            get
            {
                return Enumerable.Empty<String>();
            }
        }

        /// <summary>
        /// Gets the Type of the Expression
        /// </summary>
        public SparqlExpressionType Type
        {
            get
            {
                return SparqlExpressionType.Primary;
            }
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public String Functor
        {
            get
            {
                return "*";
            }
        }

        /// <summary>
        /// Gets the Arguments of the Expression
        /// </summary>
        public IEnumerable<ISparqlExpression> Arguments
        {
            get
            {
                return Enumerable.Empty<ISparqlExpression>();
            }
        }

        /// <summary>
        /// Gets the String representation of the Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "*";
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return this;
        }

        /// <summary>
        /// Gets whether an expression can safely be evaluated in parallel
        /// </summary>
        public bool CanParallelise
        {
            get
            {
                return true;
            }
        }
    }
}
