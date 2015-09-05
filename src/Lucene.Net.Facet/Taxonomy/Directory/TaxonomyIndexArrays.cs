﻿using System;
using System.Diagnostics;

namespace Lucene.Net.Facet.Taxonomy.Directory
{

    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using MultiFields = Lucene.Net.Index.MultiFields;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using ArrayUtil = Lucene.Net.Util.ArrayUtil;

    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A <seealso cref="ParallelTaxonomyArrays"/> that are initialized from the taxonomy
    /// index.
    /// 
    /// @lucene.experimental
    /// </summary>
    internal class TaxonomyIndexArrays : ParallelTaxonomyArrays
    {

        private readonly int[] parents_Renamed;

        // the following two arrays are lazily intialized. note that we only keep a
        // single boolean member as volatile, instead of declaring the arrays
        // volatile. the code guarantees that only after the boolean is set to true,
        // the arrays are returned.
        private volatile bool initializedChildren = false;
        private int[] children_Renamed, siblings_Renamed;

        /// <summary>
        /// Used by <seealso cref="#add(int, int)"/> after the array grew. </summary>
        private TaxonomyIndexArrays(int[] parents)
        {
            this.parents_Renamed = parents;
        }

        public TaxonomyIndexArrays(IndexReader reader)
        {
            parents_Renamed = new int[reader.MaxDoc];
            if (parents_Renamed.Length > 0)
            {
                InitParents(reader, 0);
                // Starting Lucene 2.9, following the change LUCENE-1542, we can
                // no longer reliably read the parent "-1" (see comment in
                // LuceneTaxonomyWriter.SinglePositionTokenStream). We have no way
                // to fix this in indexing without breaking backward-compatibility
                // with existing indexes, so what we'll do instead is just
                // hard-code the parent of ordinal 0 to be -1, and assume (as is
                // indeed the case) that no other parent can be -1.
                parents_Renamed[0] = TaxonomyReader.INVALID_ORDINAL;
            }
        }

        public TaxonomyIndexArrays(IndexReader reader, TaxonomyIndexArrays copyFrom)
        {
            Debug.Assert(copyFrom != null);

            // note that copyParents.length may be equal to reader.maxDoc(). this is not a bug
            // it may be caused if e.g. the taxonomy segments were merged, and so an updated
            // NRT reader was obtained, even though nothing was changed. this is not very likely
            // to happen.
            int[] copyParents = copyFrom.Parents();
            this.parents_Renamed = new int[reader.MaxDoc];
            Array.Copy(copyParents, 0, parents_Renamed, 0, copyParents.Length);
            InitParents(reader, copyParents.Length);

            if (copyFrom.initializedChildren)
            {
                InitChildrenSiblings(copyFrom);
            }
        }

        private void InitChildrenSiblings(TaxonomyIndexArrays copyFrom)
        {
            lock (this)
            {
                if (!initializedChildren) // must do this check !
                {
                    children_Renamed = new int[parents_Renamed.Length];
                    siblings_Renamed = new int[parents_Renamed.Length];
                    if (copyFrom != null)
                    {
                        // called from the ctor, after we know copyFrom has initialized children/siblings
                        Array.Copy(copyFrom.Children(), 0, children_Renamed, 0, copyFrom.Children().Length);
                        Array.Copy(copyFrom.Siblings(), 0, siblings_Renamed, 0, copyFrom.Siblings().Length);
                        ComputeChildrenSiblings(copyFrom.parents_Renamed.Length);
                    }
                    else
                    {
                        ComputeChildrenSiblings(0);
                    }
                    initializedChildren = true;
                }
            }
        }

        private void ComputeChildrenSiblings(int first)
        {
            // reset the youngest child of all ordinals. while this should be done only
            // for the leaves, we don't know up front which are the leaves, so we reset
            // all of them.
            for (int i = first; i < parents_Renamed.Length; i++)
            {
                children_Renamed[i] = TaxonomyReader.INVALID_ORDINAL;
            }

            // the root category has no parent, and therefore no siblings
            if (first == 0)
            {
                first = 1;
                siblings_Renamed[0] = TaxonomyReader.INVALID_ORDINAL;
            }

            for (int i = first; i < parents_Renamed.Length; i++)
            {
                // note that parents[i] is always < i, so the right-hand-side of
                // the following line is already set when we get here
                siblings_Renamed[i] = children_Renamed[parents_Renamed[i]];
                children_Renamed[parents_Renamed[i]] = i;
            }
        }

        // Read the parents of the new categories
        private void InitParents(IndexReader reader, int first)
        {
            if (reader.MaxDoc == first)
            {
                return;
            }

            // it's ok to use MultiFields because we only iterate on one posting list.
            // breaking it to loop over the leaves() only complicates code for no
            // apparent gain.
            DocsAndPositionsEnum positions = MultiFields.GetTermPositionsEnum(reader, null, Consts.FIELD_PAYLOADS, Consts.PAYLOAD_PARENT_BYTES_REF, DocsAndPositionsEnum.FLAG_PAYLOADS);

            // shouldn't really happen, if it does, something's wrong
            if (positions == null || positions.Advance(first) == DocIdSetIterator.NO_MORE_DOCS)
            {
                throw new CorruptIndexException("Missing parent data for category " + first);
            }

            int num = reader.MaxDoc;
            for (int i = first; i < num; i++)
            {
                if (positions.DocID() == i)
                {
                    if (positions.Freq() == 0) // shouldn't happen
                    {
                        throw new CorruptIndexException("Missing parent data for category " + i);
                    }

                    parents_Renamed[i] = positions.NextPosition();

                    if (positions.NextDoc() == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (i + 1 < num)
                        {
                            throw new CorruptIndexException("Missing parent data for category " + (i + 1));
                        }
                        break;
                    }
                } // this shouldn't happen
                else
                {
                    throw new CorruptIndexException("Missing parent data for category " + i);
                }
            }
        }

        /// <summary>
        /// Adds the given ordinal/parent info and returns either a new instance if the
        /// underlying array had to grow, or this instance otherwise.
        /// <para>
        /// <b>NOTE:</b> you should call this method from a thread-safe code.
        /// </para>
        /// </summary>
        internal virtual TaxonomyIndexArrays Add(int ordinal, int parentOrdinal)
        {
            if (ordinal >= parents_Renamed.Length)
            {
                int[] newarray = ArrayUtil.Grow(parents_Renamed, ordinal + 1);
                newarray[ordinal] = parentOrdinal;
                return new TaxonomyIndexArrays(newarray);
            }
            parents_Renamed[ordinal] = parentOrdinal;
            return this;
        }

        /// <summary>
        /// Returns the parents array, where {@code parents[i]} denotes the parent of
        /// category ordinal {@code i}.
        /// </summary>
        public override int[] Parents()
        {
            return parents_Renamed;
        }

        /// <summary>
        /// Returns the children array, where {@code children[i]} denotes the youngest
        /// child of category ordinal {@code i}. The youngest child is defined as the
        /// category that was added last to the taxonomy as an immediate child of
        /// {@code i}.
        /// </summary>
        public override int[] Children()
        {
            if (!initializedChildren)
            {
                InitChildrenSiblings(null);
            }

            // the array is guaranteed to be populated
            return children_Renamed;
        }

        /// <summary>
        /// Returns the siblings array, where {@code siblings[i]} denotes the sibling
        /// of category ordinal {@code i}. The sibling is defined as the previous
        /// youngest child of {@code parents[i]}.
        /// </summary>
        public override int[] Siblings()
        {
            if (!initializedChildren)
            {
                InitChildrenSiblings(null);
            }

            // the array is guaranteed to be populated
            return siblings_Renamed;
        }

    }

}