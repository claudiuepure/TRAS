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
using System.Diagnostics;
using System.Linq;
using System.Text;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Ordering;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query
{
    /// <summary>
    /// Helper Class used in the execution of Sparql Queries
    /// </summary>
    /// <remarks>
    /// </remarks>
    public abstract class SparqlResultBinder
        : IDisposable
    {
        private SparqlQuery _query;
        private Dictionary<int,BindingGroup> _groups = null;

        /// <summary>
        /// Internal Empty Constructor for derived classes
        /// </summary>
        protected internal SparqlResultBinder()
        {

        }

        /// <summary>
        /// Creates a new Results Binder
        /// </summary>
        /// <param name="query">Query this provides Result Binding to</param>
        public SparqlResultBinder(SparqlQuery query)
        {
            this._query = query;
        }

        /// <summary>
        /// Gets the Variables that the Binder stores Bindings for
        /// </summary>
        public abstract IEnumerable<String> Variables
        {
            get;
        }

        /// <summary>
        /// Gets the enumeration of valid Binding IDs
        /// </summary>
        public abstract IEnumerable<int> BindingIDs
        {
            get;
        }

        /// <summary>
        /// Gets the set of Groups that result from the Query this Binder provides Binding to
        /// </summary>
        public IEnumerable<BindingGroup> Groups
        {
            get
            {
                if (this._groups != null)
                {
                    return (from g in this._groups.Values
                            select g);
                }
                else
                {
                    return Enumerable.Empty<BindingGroup>();
                }
            }
        }

        /// <summary>
        /// Gets the Value bound to a given Variable for a given Binding ID
        /// </summary>
        /// <param name="name">Variable Name</param>
        /// <param name="bindingID">Binding ID</param>
        /// <returns></returns>
        public abstract INode Value(String name, int bindingID);

        /// <summary>
        /// Gets the Group referred to by the given ID
        /// </summary>
        /// <param name="groupID">Group ID</param>
        /// <returns></returns>
        public virtual BindingGroup Group(int groupID)
        {
            if (this._groups != null)
            {
                if (this._groups.ContainsKey(groupID))
                {
                    return this._groups[groupID];
                }
                else
                {
                    throw new RdfQueryException("The Group with ID " + groupID + " does not exist in the Result Binder");
                }
            }
            else
            {
                throw new RdfQueryException("Cannot lookup a Group when the Query has not been executed or does not contain Groups as part of it's Results");
            }
        }

        /// <summary>
        /// Checks whether the given ID refers to a Group
        /// </summary>
        /// <param name="groupID">Group ID</param>
        /// <returns></returns>
        public virtual bool IsGroup(int groupID)
        {
            if (this._groups == null)
            {
                return false;
            }
            else
            {
                return this._groups.ContainsKey(groupID);
            }
        }

        /// <summary>
        /// Sets the Group Context for the Binder
        /// </summary>
        /// <param name="accessContents">Whether you want to access the Group Contents or the Groups themselves</param>
        public virtual void SetGroupContext(bool accessContents)
        {
        }

        /// <summary>
        /// Disposes of a Result Binder
        /// </summary>
        public virtual void Dispose()
        {
            this._groups.Clear();
        }
    }

    /// <summary>
    /// Results Binder used by Leviathan
    /// </summary>
    public class LeviathanResultBinder
        : SparqlResultBinder
    {
        private SparqlEvaluationContext _context;
        private GroupMultiset _groupSet;

        /// <summary>
        /// Creates a new Leviathan Results Binder
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        public LeviathanResultBinder(SparqlEvaluationContext context)
            : base()
        {
            this._context = context;
        }

        /// <summary>
        /// Gets the Value for a given Variable from the Set with the given Binding ID
        /// </summary>
        /// <param name="name">Variable</param>
        /// <param name="bindingID">Set ID</param>
        /// <returns></returns>
        public override INode Value(string name, int bindingID)
        {
            return this._context.InputMultiset[bindingID][name];
        }

        /// <summary>
        /// Gets the Variables contained in the Input
        /// </summary>
        public override IEnumerable<string> Variables
        {
            get
            {
                return this._context.InputMultiset.Variables;
            }
        }

        /// <summary>
        /// Gets the IDs of Sets
        /// </summary>
        public override IEnumerable<int> BindingIDs
        {
            get
            {
                return this._context.InputMultiset.SetIDs;
            }
        }

        /// <summary>
        /// Determines whether a given ID is for of a Group
        /// </summary>
        /// <param name="groupID">Group ID</param>
        /// <returns></returns>
        public override bool IsGroup(int groupID)
        {
            if (this._context.InputMultiset is GroupMultiset || this._groupSet != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the Group with the given ID
        /// </summary>
        /// <param name="groupID">Group ID</param>
        /// <returns></returns>
        public override BindingGroup Group(int groupID)
        {
            if (this._context.InputMultiset is GroupMultiset)
            {
                GroupMultiset groupSet = (GroupMultiset)this._context.InputMultiset;
                return groupSet.Group(groupID);
            }
            else if (this._groupSet != null)
            {
                return this._groupSet.Group(groupID);
            }
            else
            {
                throw new RdfQueryException("Cannot retrieve a Group when the Input Multiset is not a Group Multiset");
            }
        }

        /// <summary>
        /// Sets the Group Context for the Binder
        /// </summary>
        /// <param name="accessContents">Whether you want to access the Group Contents or the Groups themselves</param>
        public override void SetGroupContext(bool accessContents)
        {
            if (accessContents)
            {
                if (this._context.InputMultiset is GroupMultiset)
                {
                    this._groupSet = (GroupMultiset)this._context.InputMultiset;
                    this._context.InputMultiset = this._groupSet.Contents;

                }
                else
                {
                    throw new RdfQueryException("Cannot set Group Context to access Contents data when the Input is not a Group Multiset, you may be trying to use a nested aggregate which is illegal");
                }
            }
            else
            {
                if (this._groupSet != null)
                {
                    this._context.InputMultiset = this._groupSet;
                    this._groupSet = null;
                }
                else
                {
                    throw new RdfQueryException("Cannot set Group Context to acess Group data when there is no Group data available");
                }
            }
        }
    }

    /// <summary>
    /// Special Temporary Results Binder used during LeftJoin's
    /// </summary>
    public class LeviathanLeftJoinBinder : SparqlResultBinder
    {
        private BaseMultiset _input;

        /// <summary>
        /// Creates a new LeftJoin Binder
        /// </summary>
        /// <param name="multiset">Input Multiset</param>
        public LeviathanLeftJoinBinder(BaseMultiset multiset)
            : base()
        {
            this._input = multiset;
        }

        /// <summary>
        /// Gets the Value for a given Variable from the Set with the given Binding ID
        /// </summary>
        /// <param name="name">Variable</param>
        /// <param name="bindingID">Set ID</param>
        /// <returns></returns>
        public override INode Value(string name, int bindingID)
        {
            return this._input[bindingID][name];
        }

        /// <summary>
        /// Gets the Variables in the Input Multiset
        /// </summary>
        public override IEnumerable<string> Variables
        {
            get 
            {
                return this._input.Variables;
            }
        }

        /// <summary>
        /// Gets the IDs of Sets
        /// </summary>
        public override IEnumerable<int> BindingIDs
        {
            get 
            {
                return this._input.SetIDs;
            }
        }
    }
}
