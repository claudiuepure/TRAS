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
using VDS.RDF.Query.Operators;

namespace VDS.RDF.Query.Expressions.Arithmetic
{
    /// <summary>
    /// Class representing Arithmetic Subtraction expressions
    /// </summary>
    public class SubtractionExpression
        : BaseBinaryExpression
    {
        /// <summary>
        /// Creates a new Subtraction Expression
        /// </summary>
        /// <param name="leftExpr">Left Hand Expression</param>
        /// <param name="rightExpr">Right Hand Expression</param>
        public SubtractionExpression(ISparqlExpression leftExpr, ISparqlExpression rightExpr) 
            : base(leftExpr, rightExpr) { }

        /// <summary>
        /// Calculates the Numeric Value of this Expression as evaluated for a given Binding
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public override IValuedNode Evaluate(SparqlEvaluationContext context, int bindingID)
        {
            IValuedNode a = this._leftExpr.Evaluate(context, bindingID);
            IValuedNode b = this._rightExpr.Evaluate(context, bindingID);

            IValuedNode[] inputs = new IValuedNode[] { a, b };
            ISparqlOperator op = null;
            if (SparqlOperators.TryGetOperator(SparqlOperatorType.Subtract, out op, inputs))
            {
                return op.Apply(inputs);
            }
            else
            {
                throw new RdfQueryException("Cannot apply addition to the given inputs");
            }

            //if (a == null || b == null) throw new RdfQueryException("Cannot apply subtraction when one/both arguments are null");
            
            //SparqlNumericType type = (SparqlNumericType)Math.Max((int)a.NumericType, (int)b.NumericType);

            //switch (type)
            //{
            //    case SparqlNumericType.Integer:
            //        return new LongNode(null, a.AsInteger() - b.AsInteger());
            //    case SparqlNumericType.Decimal:
            //        return new DecimalNode(null, a.AsDecimal() - b.AsDecimal());
            //    case SparqlNumericType.Float:
            //        return new FloatNode(null, a.AsFloat() - b.AsFloat());
            //    case SparqlNumericType.Double:
            //        return new DoubleNode(null, a.AsDouble() - b.AsDouble());
            //    default:
            //        throw new RdfQueryException("Cannot evalute an Arithmetic Expression when the Numeric Type of the expression cannot be determined");
            //}
        }

        /// <summary>
        /// Gets the String representation of this Expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            if (this._leftExpr.Type == SparqlExpressionType.BinaryOperator)
            {
                output.Append("(" + this._leftExpr.ToString() + ")");
            }
            else
            {
                output.Append(this._leftExpr.ToString());
            }
            output.Append(" - ");
            if (this._rightExpr.Type == SparqlExpressionType.BinaryOperator)
            {
                output.Append("(" + this._rightExpr.ToString() + ")");
            }
            else
            {
                output.Append(this._rightExpr.ToString());
            }
            return output.ToString();
        }

        /// <summary>
        /// Gets the Type of the Expression
        /// </summary>
        public override SparqlExpressionType Type
        {
            get
            {
                return SparqlExpressionType.BinaryOperator;
            }
        }

        /// <summary>
        /// Gets the Functor of the Expression
        /// </summary>
        public override string Functor
        {
            get
            {
                return "-";
            }
        }

        /// <summary>
        /// Transforms the Expression using the given Transformer
        /// </summary>
        /// <param name="transformer">Expression Transformer</param>
        /// <returns></returns>
        public override ISparqlExpression Transform(IExpressionTransformer transformer)
        {
            return new SubtractionExpression(transformer.Transform(this._leftExpr), transformer.Transform(this._rightExpr));
        }
    }
}
