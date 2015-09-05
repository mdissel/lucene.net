/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Util;

namespace Lucene.Net.Spatial
{
    /// <summary>
    /// A Spatial Filter implementing
    /// <see cref="SpatialOperation.IsDisjointTo">Org.Apache.Lucene.Spatial.Query.SpatialOperation.IsDisjointTo
    /// 	</see>
    /// in terms
    /// of a
    /// <see cref="SpatialStrategy">SpatialStrategy</see>
    /// 's support for
    /// <see cref="SpatialOperation.Intersects">Org.Apache.Lucene.Spatial.Query.SpatialOperation.Intersects
    /// 	</see>
    /// .
    /// A document is considered disjoint if it has spatial data that does not
    /// intersect with the query shape.  Another way of looking at this is that it's
    /// a way to invert a query shape.
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public class DisjointSpatialFilter : Filter
    {
        private readonly string field;

        private readonly Filter intersectsFilter;

        /// <param name="strategy">Needed to compute intersects</param>
        /// <param name="args">Used in spatial intersection</param>
        /// <param name="field">
        /// This field is used to determine which docs have spatial data via
        /// <see cref="Org.Apache.Lucene.Search.FieldCache.GetDocsWithField(Org.Apache.Lucene.Index.AtomicReader, string)
        /// 	">Org.Apache.Lucene.Search.FieldCache.GetDocsWithField(Org.Apache.Lucene.Index.AtomicReader, string)
        /// 	</see>
        /// .
        /// Passing null will assume all docs have spatial data.
        /// </param>
        public DisjointSpatialFilter(SpatialStrategy strategy, SpatialArgs args, string field
            )
        {
            //maybe null
            this.field = field;
            // TODO consider making SpatialArgs cloneable
            SpatialOperation origOp = args.Operation;
            //copy so we can restore
            args.Operation = SpatialOperation.Intersects;
            //temporarily set to intersects
            intersectsFilter = strategy.MakeFilter(args);
            args.Operation = origOp;
        }

        //restore so it looks like it was
        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (o == null || GetType() != o.GetType())
            {
                return false;
            }
            var that = (DisjointSpatialFilter)o;
            if (field != null ? !field.Equals(that.field) : that.field != null)
            {
                return false;
            }
            if (!intersectsFilter.Equals(that.intersectsFilter))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = field != null ? field.GetHashCode() : 0;
            result = 31 * result + intersectsFilter.GetHashCode();
            return result;
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs
            )
        {
            IBits docsWithField;
            if (field == null)
            {
                docsWithField = null;
            }
            else
            {
                //all docs
                //NOTE By using the FieldCache we re-use a cache
                // which is nice but loading it in this way might be slower than say using an
                // intersects filter against the world bounds. So do we add a method to the
                // strategy, perhaps?  But the strategy can't cache it.
                docsWithField = FieldCache.DEFAULT.GetDocsWithField((context.AtomicReader), field);
                int maxDoc = context.AtomicReader.MaxDoc;
                if (docsWithField.Length != maxDoc)
                {
                    throw new InvalidOperationException("Bits length should be maxDoc (" + maxDoc + ") but wasn't: "
                                                        + docsWithField);
                }
                if (docsWithField is Bits.MatchNoBits)
                {
                    return null;
                }
                else
                {
                    //match nothing
                    if (docsWithField is Bits.MatchAllBits)
                    {
                        docsWithField = null;
                    }
                }
            }
            //all docs
            //not so much a chain but a way to conveniently invert the Filter
            DocIdSet docIdSet = new ChainedFilter(new[] { intersectsFilter }, ChainedFilter.ANDNOT).GetDocIdSet(context,
                                                                                                              acceptDocs);
            return BitsFilteredDocIdSet.Wrap(docIdSet, docsWithField);
        }
    }
}