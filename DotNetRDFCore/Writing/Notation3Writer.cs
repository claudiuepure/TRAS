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
using System.IO;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing.Contexts;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Writing
{
    /// <summary>
    /// Class for generating Notation 3 Concrete RDF Syntax which provides varying levels of Syntax Compression
    /// </summary>
    /// <threadsafety instance="true">Designed to be Thread Safe - should be able to call the Save() method from multiple threads on different Graphs without issue</threadsafety>
    public class Notation3Writer 
        : IRdfWriter, IPrettyPrintingWriter, IHighSpeedWriter, ICompressingWriter, INamespaceWriter, IFormatterBasedWriter
    {
        private bool _prettyprint = true;
        private bool _allowHiSpeed = true;
        private int _compressionLevel = WriterCompressionLevel.Default;
        private INamespaceMapper _defaultNamespaces = new NamespaceMapper();

        /// <summary>
        /// Creates a new Notation 3 Writer which uses the Default Compression Level
        /// </summary>
        public Notation3Writer()
        {

        }

        /// <summary>
        /// Creates a new Notation 3 Writer which uses the given Compression Level
        /// </summary>
        /// <param name="compressionLevel">Desired Compression Level</param>
        /// <remarks>See Remarks for this classes <see cref="Notation3Writer.CompressionLevel">CompressionLevel</see> property to see what effect different compression levels have</remarks>
        public Notation3Writer(int compressionLevel)
        {
            this._compressionLevel = compressionLevel;
        }

        /// <summary>
        /// Gets/Sets whether Pretty Printing is used
        /// </summary>
        public bool PrettyPrintMode
        {
            get
            {
                return this._prettyprint;
            }
            set
            {
                this._prettyprint = value;
            }
        }

        /// <summary>
        /// Gets/Sets whether High Speed Write Mode should be allowed
        /// </summary>
        public bool HighSpeedModePermitted
        {
            get
            {
                return this._allowHiSpeed;
            }
            set
            {
                this._allowHiSpeed = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Compression Level to be used
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the Compression Level is set to <see cref="WriterCompressionLevel.None">None</see> then High Speed mode will always be used regardless of the input Graph and the <see cref="Notation3Writer.HighSpeedModePermitted">HighSpeedMorePermitted</see> property.
        /// </para>
        /// <para>
        /// If the Compression Level is set to <see cref="WriterCompressionLevel.Minimal">Minimal</see> or above then full Predicate Object lists will be used for Triples.
        /// </para>
        /// <para>
        /// If the Compression Level is set to <see cref="WriterCompressionLevel.More">More</see> or above then Blank Node Collections and Collection syntax will be used if the Graph contains Triples that can be compressed in that way.</para>
        /// </remarks>
        public int CompressionLevel
        {
            get
            {
                return this._compressionLevel;
            }
            set
            {
                this._compressionLevel = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Default Namespaces that are always available
        /// </summary>
        public INamespaceMapper DefaultNamespaces
        {
            get
            {
                return this._defaultNamespaces;
            }
            set
            {
                this._defaultNamespaces = value;
            }
        }

        /// <summary>
        /// Gets the type of the Triple Formatter used by this writer
        /// </summary>
        public Type TripleFormatterType
        {
            get
            {
                return typeof(Notation3Formatter);
            }
        }

#if !NO_FILE
        /// <summary>
        /// Saves a Graph to a file using Notation 3 Syntax
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="filename">File to save to</param>
        public void Save(IGraph g, string filename)
        {
            this.Save(g, new StreamWriter(filename, false, new UTF8Encoding(Options.UseBomForUtf8)));
        }
#endif

        /// <summary>
        /// Saves a Graph to the given Stream using Notation 3 Syntax
        /// </summary>
        /// <param name="g">Graph to save</param>
        /// <param name="output">Stream to save to</param>
        public void Save(IGraph g, TextWriter output)
        {
            try
            {
                g.NamespaceMap.Import(this._defaultNamespaces);
                CompressingTurtleWriterContext context = new CompressingTurtleWriterContext(g, output, this._compressionLevel, this._prettyprint, this._allowHiSpeed);
                context.NodeFormatter = new Notation3Formatter(g);
                this.GenerateOutput(context);
            }
            finally
            {
                try
                {
                    output.Close();
                }
                catch
                {
                    //No Catch actions - just trying to clean up
                }
            }
        }

        /// <summary>
        /// Generates the Notation 3 Syntax for the Graph
        /// </summary>
        private void GenerateOutput(CompressingTurtleWriterContext context)
        {
            //Create the Header
            //Base Directive
            if (context.Graph.BaseUri != null)
            {
                context.Output.WriteLine("@base <" + context.UriFormatter.FormatUri(context.Graph.BaseUri) + ">.");
                context.Output.WriteLine();
            }
            //Prefix Directives
            foreach (String prefix in context.Graph.NamespaceMap.Prefixes)
            {
                if (TurtleSpecsHelper.IsValidQName(prefix + ":"))
                {
                    if (!prefix.Equals(String.Empty))
                    {
                        context.Output.WriteLine("@prefix " + prefix + ": <" + context.UriFormatter.FormatUri(context.Graph.NamespaceMap.GetNamespaceUri(prefix)) + ">.");
                    }
                    else
                    {
                        context.Output.WriteLine("@prefix : <" + context.UriFormatter.FormatUri(context.Graph.NamespaceMap.GetNamespaceUri(String.Empty)) + ">.");
                    }
                }
            }
            context.Output.WriteLine();

            //Decide on the Write Mode to use
            bool hiSpeed = false;
            bool contextWritten = false;
            double subjNodes = context.Graph.Triples.SubjectNodes.Count();
            double triples = context.Graph.Triples.Count;
            if ((subjNodes / triples) > 0.75) hiSpeed = true;

            if (context.CompressionLevel == WriterCompressionLevel.None || (hiSpeed && context.HighSpeedModePermitted))
            {
                this.RaiseWarning("High Speed Write Mode in use - minimal syntax compression will be used");
                context.CompressionLevel = WriterCompressionLevel.Minimal;
                context.NodeFormatter = new UncompressedNotation3Formatter();

                foreach (Triple t in context.Graph.Triples)
                {
                    if (!contextWritten && t.Context != null && t.Context is VariableContext)
                    {
                        VariableContext varContext = (VariableContext)t.Context;
                        contextWritten = this.GenerateVariableQuantificationOutput(context, varContext);
                    }
                    context.Output.WriteLine(this.GenerateTripleOutput(context, t));
                }
            }
            else
            {
                if (context.CompressionLevel >= WriterCompressionLevel.More)
                {
                    WriterHelper.FindCollections(context);
                }

                //Get the Triples as a Sorted List
                List<Triple> ts = context.Graph.Triples.Where(t => !context.TriplesDone.Contains(t)).ToList();
                ts.Sort(new FullTripleComparer(new FastNodeComparer()));

                //Variables we need to track our writing
                INode lastSubj, lastPred;
                lastSubj = lastPred = null;
                int subjIndent = 0, predIndent = 0;
                String temp;

                for (int i = 0; i < ts.Count; i++)
                {
                    Triple t = ts[i];

                    if (lastSubj == null || !t.Subject.Equals(lastSubj) || (t.Context != null && t.Context is VariableContext))
                    {
                        //Terminate previous Triples
                        if (lastSubj != null) context.Output.WriteLine(".");

                        //If there's a Variable Context insert the @forAll and @forSome
                        if (!contextWritten && t.Context != null && t.Context is VariableContext)
                        {
                            VariableContext varContext = (VariableContext)t.Context;
                            contextWritten = this.GenerateVariableQuantificationOutput(context, varContext);
                        }

                        //Start a new set of Triples
                        temp = this.GenerateNodeOutput(context, t.Subject, TripleSegment.Subject, 0);
                        context.Output.Write(temp);
                        context.Output.Write(" ");
                        if (temp.Contains('\n'))
                        {
                            subjIndent = temp.Split('\n').Last().Length + 1;
                        }
                        else
                        {
                            subjIndent = temp.Length + 1;
                        }
                        lastSubj = t.Subject;

                        //Write the first Predicate
                        temp = this.GenerateNodeOutput(context, t.Predicate, TripleSegment.Predicate, subjIndent);
                        context.Output.Write(temp);
                        context.Output.Write(" ");
                        predIndent = temp.Length + 1;
                        lastPred = t.Predicate;
                    }
                    else if (lastPred == null || !t.Predicate.Equals(lastPred))
                    {
                        //Terminate previous Predicate Object list
                        context.Output.WriteLine(";");

                        if (context.PrettyPrint) context.Output.Write(new String(' ', subjIndent));

                        //Write the next Predicate
                        temp = this.GenerateNodeOutput(context, t.Predicate, TripleSegment.Predicate, subjIndent);
                        context.Output.Write(temp);
                        context.Output.Write(" ");
                        predIndent = temp.Length + 1;
                        lastPred = t.Predicate;
                    }
                    else
                    {
                        //Continue Object List
                        context.Output.WriteLine(",");

                        if (context.PrettyPrint) context.Output.Write(new String(' ', subjIndent + predIndent));
                    }

                    //Write the Object
                    context.Output.Write(this.GenerateNodeOutput(context, t.Object, TripleSegment.Object, subjIndent + predIndent));
                }

                //Terminate Triples
                if (ts.Count > 0) context.Output.WriteLine(".");

                return;
            }

        }

        /// <summary>
        /// Generates Output for Triples as a single "s p o." Triple
        /// </summary>
        /// <param name="context">Writer Context</param>
        /// <param name="t">Triple to output</param>
        /// <returns></returns>
        /// <remarks>Used only in High Speed Write Mode</remarks>
        private String GenerateTripleOutput(CompressingTurtleWriterContext context, Triple t)
        {
            StringBuilder temp = new StringBuilder();
            temp.Append(this.GenerateNodeOutput(context, t.Subject, TripleSegment.Subject, 0));
            temp.Append(' ');
            temp.Append(this.GenerateNodeOutput(context, t.Predicate, TripleSegment.Predicate, 0));
            temp.Append(' ');
            temp.Append(this.GenerateNodeOutput(context, t.Object, TripleSegment.Object, 0));
            temp.Append('.');

            return temp.ToString();
        }

        /// <summary>
        /// Generates Output for Nodes in Notation 3 syntax
        /// </summary>
        /// <param name="context">Writer Context</param>
        /// <param name="n">Node to generate output for</param>
        /// <param name="segment">Segment of the Triple being output</param>
        /// <param name="indent">Indent to use for pretty printing</param>
        /// <returns></returns>
        private String GenerateNodeOutput(CompressingTurtleWriterContext context, INode n, TripleSegment segment, int indent)
        {
            StringBuilder output = new StringBuilder();

            switch (n.NodeType)
            {
                case NodeType.Blank:
                    if (context.Collections.ContainsKey(n))
                    {
                        output.Append(this.GenerateCollectionOutput(context, context.Collections[n], indent));
                    }
                    else
                    {
                        return context.NodeFormatter.Format(n, segment);
                    }
                    break;

                case NodeType.GraphLiteral:
                    if (segment == TripleSegment.Predicate) throw new RdfOutputException(WriterErrorMessages.GraphLiteralPredicatesUnserializable("Notation 3"));

                    output.Append("{");
                    IGraphLiteralNode glit = (IGraphLiteralNode)n;

                    StringBuilder temp = new StringBuilder();
                    CompressingTurtleWriterContext subcontext = new CompressingTurtleWriterContext(glit.SubGraph, new System.IO.StringWriter(temp));
                    subcontext.NodeFormatter = context.NodeFormatter;
                    bool contextWritten = false;

                    //Write Triples 1 at a Time on a single line
                    foreach (Triple t in subcontext.Graph.Triples) 
                    {
                        if (!contextWritten && t.Context != null && t.Context is VariableContext)
                        {
                            contextWritten = this.GenerateVariableQuantificationOutput(subcontext, (VariableContext)t.Context);
                            if (contextWritten) output.Append(temp.ToString());
                        }

                        output.Append(this.GenerateNodeOutput(subcontext, t.Subject, TripleSegment.Subject, 0));
                        output.Append(" ");
                        output.Append(this.GenerateNodeOutput(subcontext, t.Predicate, TripleSegment.Predicate, 0));
                        output.Append(" ");
                        output.Append(this.GenerateNodeOutput(subcontext, t.Object, TripleSegment.Object, 0));
                        output.Append(". ");
                    }

                    output.Append("}");
                    break;

                case NodeType.Literal:
                    if (segment == TripleSegment.Predicate) throw new RdfOutputException(WriterErrorMessages.LiteralPredicatesUnserializable("Notation 3"));
                    return context.NodeFormatter.Format(n, segment);

                case NodeType.Uri:
                    return context.NodeFormatter.Format(n, segment);

                case NodeType.Variable:
                    return context.NodeFormatter.Format(n, segment);

                default:
                    throw new RdfOutputException(WriterErrorMessages.UnknownNodeTypeUnserializable("Notation 3"));
            }

            return output.ToString();
        }

        /// <summary>
        /// Internal Helper method which converts a Collection into Notation 3 Syntax
        /// </summary>
        /// <param name="context">Writer Context</param>
        /// <param name="c">Collection to convert</param>
        /// <param name="indent">Indent to use for pretty printing</param>
        /// <returns></returns>
        private String GenerateCollectionOutput(CompressingTurtleWriterContext context, OutputRdfCollection c, int indent)
        {
            StringBuilder output = new StringBuilder();
            bool first = true;

            if (!c.IsExplicit)
            {
                output.Append('(');

                while (c.Triples.Count > 0)
                {
                    if (context.PrettyPrint && !first) output.Append(new String(' ', indent));
                    first = false;
                    output.Append(this.GenerateNodeOutput(context, c.Triples.First().Object, TripleSegment.Object, indent));
                    c.Triples.RemoveAt(0);
                    if (c.Triples.Count > 0)
                    {
                        output.Append(' ');
                    }
                }

                output.Append(')');
            }
            else
            {
                if (c.Triples.Count == 0)
                {
                    //Empty Collection
                    //Can represent as a single Blank Node []
                    output.Append("[]");
                }
                else
                {
                    output.Append('[');

                    while (c.Triples.Count > 0)
                    {
                        if (context.PrettyPrint && !first) output.Append(new String(' ', indent));
                        first = false;
                        String temp = this.GenerateNodeOutput(context, c.Triples.First().Predicate, TripleSegment.Predicate, indent);
                        output.Append(temp);
                        output.Append(' ');
                        int addIndent;
                        if (temp.Contains('\n'))
                        {
                            addIndent = temp.Split('\n').Last().Length;
                        }
                        else
                        {
                            addIndent = temp.Length;
                        }
                        output.Append(this.GenerateNodeOutput(context, c.Triples.First().Object, TripleSegment.Object, indent + 2 + addIndent));
                        c.Triples.RemoveAt(0);

                        if (c.Triples.Count > 0)
                        {
                            output.AppendLine(" ; ");
                            output.Append(' ');
                        }
                    }

                    output.Append(']');
                }
            }
            return output.ToString();
        }

        private bool GenerateVariableQuantificationOutput(CompressingTurtleWriterContext context, VariableContext varContext)
        {
            if (varContext.Type == VariableContextType.None)
            {
                return false;
            }
            else if (varContext.Type == VariableContextType.Existential)
            {
                context.Output.Write("@forSome ");
            }
            else
            {
                context.Output.Write("@forAll ");
            }
            foreach (INode var in varContext.Variables)
            {
                context.Output.Write(context.NodeFormatter.Format(var));
                context.Output.Write(' ');
            }
            context.Output.WriteLine('.');

            if (varContext.InnerContext != null)
            {
                this.GenerateVariableQuantificationOutput(context, varContext.InnerContext);
            }
            return true;
        }

        /// <summary>
        /// Helper method for generating Parser Warning Events
        /// </summary>
        /// <param name="message">Warning Message</param>
        private void RaiseWarning(String message)
        {
            RdfWriterWarning d = this.Warning;
            if (d != null)
            {
                d(message);
            }
        }

        /// <summary>
        /// Event which is raised when there is a non-fatal issue with the Graph being written
        /// </summary>
        public event RdfWriterWarning Warning;

        /// <summary>
        /// Gets the String representation of the writer which is a description of the syntax it produces
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Notation 3";
        }
    }
}
