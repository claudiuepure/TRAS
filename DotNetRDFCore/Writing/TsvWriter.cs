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
using System.Threading;
using VDS.RDF.Storage;
using VDS.RDF.Writing.Contexts;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Writing
{
    /// <summary>
    /// Class for generating TSV files from RDF Graphs
    /// </summary>
    public class TsvWriter 
        : IRdfWriter, IFormatterBasedWriter
    {
        private TsvFormatter _formatter = new TsvFormatter();

        /// <summary>
        /// Gets the type of the Triple Formatter used by this writer
        /// </summary>
        public Type TripleFormatterType
        {
            get
            {
                return this._formatter.GetType();
            }
        }

#if !NO_FILE
        /// <summary>
        /// Saves a Graph to TSV format
        /// </summary>
        /// <param name="g">Graph</param>
        /// <param name="filename">File to save to</param>
        public void Save(IGraph g, string filename)
        {
            this.Save(g, new StreamWriter(filename, false, new UTF8Encoding(Options.UseBomForUtf8)));
        }
#endif

        /// <summary>
        /// Saves a Graph to TSV format
        /// </summary>
        /// <param name="g">Graph</param>
        /// <param name="output">Writer to save to</param>
        public void Save(IGraph g, TextWriter output)
        {
            try
            {
                foreach (Triple t in g.Triples)
                {
                    this.GenerateNodeOutput(output, t.Subject, TripleSegment.Subject);
                    output.Write('\t');
                    this.GenerateNodeOutput(output, t.Predicate, TripleSegment.Predicate);
                    output.Write('\t');
                    this.GenerateNodeOutput(output, t.Object, TripleSegment.Object);
                    output.Write('\n');
                }

                output.Close();
            }
            catch
            {
                try
                {
                    output.Close();
                }
                catch
                {
                    //No error handling, just trying to clean up
                }
                throw;
            }
        }

        private void GenerateNodeOutput(TextWriter output, INode n, TripleSegment segment)
        {
            switch (n.NodeType)
            {
                case NodeType.GraphLiteral:
                    throw new RdfOutputException(WriterErrorMessages.GraphLiteralsUnserializable("TSV"));
                case NodeType.Blank:
                case NodeType.Literal:
                case NodeType.Uri:
                    output.Write(this._formatter.Format(n));
                    break;
                default:
                    throw new RdfOutputException(WriterErrorMessages.UnknownNodeTypeUnserializable("TSV"));
            }
        }

        /// <summary>
        /// Event which is raised if the Writer detects a non-fatal error with the RDF being output
        /// </summary>
        public event RdfWriterWarning Warning;

        /// <summary>
        /// Gets the String representation of the writer which is a description of the syntax it produces
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "TSV";
        }
    }

    /// <summary>
    /// Class for generating TSV output from RDF Datasets
    /// </summary>
    public class TsvStoreWriter 
        : IStoreWriter, IFormatterBasedWriter
    {
        private int _threads = 4;
        private TsvFormatter _formatter = new TsvFormatter();

        /// <summary>
        /// Gets the type of the Triple Formatter used by this writer
        /// </summary>
        public Type TripleFormatterType
        {
            get
            {
                return this._formatter.GetType();
            }
        }

#if !NO_FILE
        /// <summary>
        /// Saves a Triple Store to TSV format
        /// </summary>
        /// <param name="store">Triple Store to save</param>
        /// <param name="filename">File to save to</param>
        public void Save(ITripleStore store, String filename)
        {
            if (filename == null) throw new RdfOutputException("Cannot output to a null file");
            this.Save(store, new StreamWriter(filename));
        }
#endif

        /// <summary>
        /// Saves a Triple Store to TSV format
        /// </summary>
        /// <param name="store">Triple Store to save</param>
        /// <param name="writer">Writer to save to</param>
        public void Save(ITripleStore store, TextWriter writer)
        {
            if (store == null) throw new RdfOutputException("Cannot output a null Triple Store");
            if (writer == null) throw new RdfOutputException("Cannot output to a null writer");

            ThreadedStoreWriterContext context = new ThreadedStoreWriterContext(store, writer);

            //Check there's something to do
            if (context.Store.Graphs.Count == 0)
            {
                context.Output.Close();
                return;
            }

            //Queue the Graphs to be written
            foreach (IGraph g in context.Store.Graphs)
            {
                context.Add(g.BaseUri);
            }

            //Start making the async calls
            List<IAsyncResult> results = new List<IAsyncResult>();
            SaveGraphsDelegate d = new SaveGraphsDelegate(this.SaveGraphs);
            for (int i = 0; i < this._threads; i++)
            {
                results.Add(d.BeginInvoke(context, null, null));
            }

            //Wait for all the async calls to complete
            WaitHandle.WaitAll(results.Select(r => r.AsyncWaitHandle).ToArray());
            RdfThreadedOutputException outputEx = new RdfThreadedOutputException(WriterErrorMessages.ThreadedOutputFailure("TSV"));
            foreach (IAsyncResult result in results)
            {
                try
                {
                    d.EndInvoke(result);
                }
                catch (Exception ex)
                {
                    outputEx.AddException(ex);
                }
            }
            context.Output.Close();

            //If there were any errors we'll throw an RdfThreadedOutputException now
            if (outputEx.InnerExceptions.Any()) throw outputEx;
        }

        /// <summary>
        /// Delegate for the SaveGraphs method
        /// </summary>
        /// <param name="globalContext">Context for writing the Store</param>
        private delegate void SaveGraphsDelegate(ThreadedStoreWriterContext globalContext);

        /// <summary>
        /// Thread Worker method which writes Graphs to the output
        /// </summary>
        /// <param name="globalContext">Context for writing the Store</param>
        private void SaveGraphs(ThreadedStoreWriterContext globalContext)
        {
            try
            {
                Uri u = null;
                while (globalContext.TryGetNextUri(out u))
                {
                    //Get the Graph from the Store
                    IGraph g = globalContext.Store.Graphs[u];

                    //Generate the Graph Output and add to Stream
                    BaseWriterContext context = new BaseWriterContext(g, new System.IO.StringWriter());
                    String graphContent = this.GenerateGraphOutput(globalContext, context);
                    try
                    {
                        Monitor.Enter(globalContext.Output);
                        globalContext.Output.WriteLine(graphContent);
                        globalContext.Output.Flush();
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        Monitor.Exit(globalContext.Output);
                    }
                }
            }
#if !PORTABLE
            catch (ThreadAbortException)
            {
                //We've been terminated, don't do anything
#if !SILVERLIGHT
                Thread.ResetAbort();
#endif
            }
#endif
            catch (Exception ex)
            {
                throw new RdfStorageException("Error in Threaded Writer in Thread ID " + Thread.CurrentThread.ManagedThreadId, ex);
            }
        }

        /// <summary>
        /// Generates the Output for a Graph as a String in TSV syntax
        /// </summary>
        /// <param name="globalContext">Context for writing the Store</param>
        /// <param name="context">Context for writing the Graph</param>
        /// <returns></returns>
        private String GenerateGraphOutput(ThreadedStoreWriterContext globalContext, BaseWriterContext context)
        {
            if (context.Graph.BaseUri != null)
            {
                //Named Graphs have a fourth context field added
                foreach (Triple t in context.Graph.Triples)
                {
                    this.GenerateNodeOutput(context, t.Subject, TripleSegment.Subject);
                    context.Output.Write('\t');
                    this.GenerateNodeOutput(context, t.Predicate, TripleSegment.Predicate);
                    context.Output.Write('\t');
                    this.GenerateNodeOutput(context, t.Object, TripleSegment.Object);
                    context.Output.Write('\t');
                    context.Output.Write('<');
                    context.Output.Write(this._formatter.FormatUri(context.Graph.BaseUri));
                    context.Output.Write('>');
                    context.Output.Write('\n');
                }
            }
            else
            {
                //Default Graph has an empty field added
                foreach (Triple t in context.Graph.Triples)
                {
                    this.GenerateNodeOutput(context, t.Subject, TripleSegment.Subject);
                    context.Output.Write('\t');
                    this.GenerateNodeOutput(context, t.Predicate, TripleSegment.Predicate);
                    context.Output.Write('\t');
                    this.GenerateNodeOutput(context, t.Object, TripleSegment.Object);
                    context.Output.Write('\t');
                    context.Output.Write('\n');
                }
            }

            return context.Output.ToString();
        }

        /// <summary>
        /// Generates Output for the given Node
        /// </summary>
        /// <param name="context">Writer Context</param>
        /// <param name="n">Node</param>
        /// <param name="segment">Triple Context</param>
        private void GenerateNodeOutput(BaseWriterContext context, INode n, TripleSegment segment)
        {
            switch (n.NodeType)
            {
                case NodeType.GraphLiteral:
                    throw new RdfOutputException(WriterErrorMessages.GraphLiteralsUnserializable("TSV"));
                case NodeType.Blank:
                case NodeType.Literal:
                case NodeType.Uri:
                    context.Output.Write(this._formatter.Format(n));
                    break;
                default:
                    throw new RdfOutputException(WriterErrorMessages.UnknownNodeTypeUnserializable("TSV"));
            }
        }

        /// <summary>
        /// Event which is raised if the Writer detects a non-fatal error with the RDF being output
        /// </summary>
        public event StoreWriterWarning Warning;

        /// <summary>
        /// Gets the String representation of the writer which is a description of the syntax it produces
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "TSV";
        }
    }

}
