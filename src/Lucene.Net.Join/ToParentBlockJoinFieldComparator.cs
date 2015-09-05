﻿using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Join
{
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
    /// A field comparator that allows parent documents to be sorted by fields
    /// from the nested / child documents.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class ToParentBlockJoinFieldComparator : FieldComparator<object>
    {
        private readonly Filter _parentFilter;
        private readonly Filter _childFilter;
        private readonly int _spareSlot;

        private FieldComparator _wrappedComparator;
        private FixedBitSet _parentDocuments;
        private FixedBitSet _childDocuments;

        private ToParentBlockJoinFieldComparator(FieldComparator wrappedComparator, Filter parentFilter, Filter childFilter, int spareSlot)
        {
            _wrappedComparator = wrappedComparator;
            _parentFilter = parentFilter;
            _childFilter = childFilter;
            _spareSlot = spareSlot;
        }

        public override int Compare(int slot1, int slot2)
        {
            return _wrappedComparator.Compare(slot1, slot2);
        }

        public override int Bottom
        {
            set
            {
                _wrappedComparator.Bottom = value;
            }
        }

        public override object TopValue
        {
            set
            {
                _wrappedComparator.TopValue = value;
            }
        }
        
        public override FieldComparator SetNextReader(AtomicReaderContext context)
        {
            DocIdSet innerDocuments = _childFilter.GetDocIdSet(context, null);
            if (IsEmpty(innerDocuments))
            {
                _childDocuments = null;
            }
            else if (innerDocuments is FixedBitSet)
            {
                _childDocuments = (FixedBitSet)innerDocuments;
            }
            else
            {
                DocIdSetIterator iterator = innerDocuments.GetIterator();
                _childDocuments = iterator != null ? ToFixedBitSet(iterator, context.AtomicReader.MaxDoc) : null;
            }
            DocIdSet rootDocuments = _parentFilter.GetDocIdSet(context, null);
            if (IsEmpty(rootDocuments))
            {
                _parentDocuments = null;
            }
            else if (rootDocuments is FixedBitSet)
            {
                _parentDocuments = (FixedBitSet)rootDocuments;
            }
            else
            {
                DocIdSetIterator iterator = rootDocuments.GetIterator();
                _parentDocuments = iterator != null ? ToFixedBitSet(iterator, context.AtomicReader.MaxDoc) : null;
            }

            _wrappedComparator = _wrappedComparator.SetNextReader(context);
            return this;
        }

        private static bool IsEmpty(DocIdSet set)
        {
            return set == null;
        }
        
        private static FixedBitSet ToFixedBitSet(DocIdSetIterator iterator, int numBits)
        {
            var set = new FixedBitSet(numBits);
            int doc;
            while ((doc = iterator.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
            {
                set.Set(doc);
            }
            return set;
        }

        public override IComparable Value(int slot)
        {
            return _wrappedComparator.Value(slot);
        }

        /// <summary>
        /// Concrete implementation of <see cref="ToParentBlockJoinSortField"/> to sorts the parent docs with the lowest values
        /// in the child / nested docs first.
        /// </summary>
        public sealed class Lowest : ToParentBlockJoinFieldComparator
        {
            /// <summary>
            /// Create ToParentBlockJoinFieldComparator.Lowest
            /// </summary>
            /// <param name="wrappedComparator">The <see cref="FieldComparator"/> on the child / nested level. </param>
            /// <param name="parentFilter">Filter (must produce FixedBitSet per-segment) that identifies the parent documents. </param>
            /// <param name="childFilter">Filter that defines which child / nested documents participates in sorting. </param>
            /// <param name="spareSlot">The extra slot inside the wrapped comparator that is used to compare which nested document
            ///                  inside the parent document scope is most competitive. </param>
            public Lowest(FieldComparator wrappedComparator, Filter parentFilter, Filter childFilter, int spareSlot) 
                : base(wrappedComparator, parentFilter, childFilter, spareSlot)
            {
            }
            
            public override int CompareBottom(int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments == null || _childDocuments == null)
                {
                    return 0;
                }

                // We need to copy the lowest value from all child docs into slot.
                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return 0;
                }

                // We only need to emit a single cmp value for any matching child doc
                int cmp = _wrappedComparator.CompareBottom(childDoc);
                if (cmp > 0)
                {
                    return cmp;
                }

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return cmp;
                    }
                    int cmp1 = _wrappedComparator.CompareBottom(childDoc);
                    if (cmp1 > 0)
                    {
                        return cmp1;
                    }
                    if (cmp1 == 0)
                    {
                        cmp = 0;
                    }
                }
            }
            
            public override void Copy(int slot, int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments == null || _childDocuments == null)
                {
                    return;
                }

                // We need to copy the lowest value from all child docs into slot.
                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return;
                }
                _wrappedComparator.Copy(_spareSlot, childDoc);
                _wrappedComparator.Copy(slot, childDoc);

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return;
                    }
                    _wrappedComparator.Copy(_spareSlot, childDoc);
                    if (_wrappedComparator.Compare(_spareSlot, slot) < 0)
                    {
                        _wrappedComparator.Copy(slot, childDoc);
                    }
                }
            }
            
            public override int CompareTop(int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments == null || _childDocuments == null)
                {
                    return 0;
                }

                // We need to copy the lowest value from all nested docs into slot.
                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return 0;
                }

                // We only need to emit a single cmp value for any matching child doc
                int cmp = _wrappedComparator.CompareBottom(childDoc);
                if (cmp > 0)
                {
                    return cmp;
                }

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return cmp;
                    }
                    int cmp1 = _wrappedComparator.CompareTop(childDoc);
                    if (cmp1 > 0)
                    {
                        return cmp1;
                    }
                    if (cmp1 == 0)
                    {
                        cmp = 0;
                    }
                }
            }

        }

        /// <summary>
        /// Concrete implementation of <see cref="ToParentBlockJoinSortField"/> to sorts the parent docs with the highest values
        /// in the child / nested docs first.
        /// </summary>
        public sealed class Highest : ToParentBlockJoinFieldComparator
        {
            /// <summary>
            /// Create ToParentBlockJoinFieldComparator.Highest
            /// </summary>
            /// <param name="wrappedComparator">The <see cref="FieldComparator"/> on the child / nested level. </param>
            /// <param name="parentFilter">Filter (must produce FixedBitSet per-segment) that identifies the parent documents. </param>
            /// <param name="childFilter">Filter that defines which child / nested documents participates in sorting. </param>
            /// <param name="spareSlot">The extra slot inside the wrapped comparator that is used to compare which nested document
            ///                  inside the parent document scope is most competitive. </param>
            public Highest(FieldComparator wrappedComparator, Filter parentFilter, Filter childFilter, int spareSlot) 
                : base(wrappedComparator, parentFilter, childFilter, spareSlot)
            {
            }
            
            public override int CompareBottom(int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments == null || _childDocuments == null)
                {
                    return 0;
                }

                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return 0;
                }

                int cmp = _wrappedComparator.CompareBottom(childDoc);
                if (cmp < 0)
                {
                    return cmp;
                }

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return cmp;
                    }
                    int cmp1 = _wrappedComparator.CompareBottom(childDoc);
                    if (cmp1 < 0)
                    {
                        return cmp1;
                    }
                    else
                    {
                        if (cmp1 == 0)
                        {
                            cmp = 0;
                        }
                    }
                }
            }
            
            public override void Copy(int slot, int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments == null || _childDocuments == null)
                {
                    return;
                }

                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return;
                }
                _wrappedComparator.Copy(_spareSlot, childDoc);
                _wrappedComparator.Copy(slot, childDoc);

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return;
                    }
                    _wrappedComparator.Copy(_spareSlot, childDoc);
                    if (_wrappedComparator.Compare(_spareSlot, slot) > 0)
                    {
                        _wrappedComparator.Copy(slot, childDoc);
                    }
                }
            }
            
            public override int CompareTop(int parentDoc)
            {
                if (parentDoc == 0 || _parentDocuments == null || _childDocuments == null)
                {
                    return 0;
                }

                int prevParentDoc = _parentDocuments.PrevSetBit(parentDoc - 1);
                int childDoc = _childDocuments.NextSetBit(prevParentDoc + 1);
                if (childDoc >= parentDoc || childDoc == -1)
                {
                    return 0;
                }

                int cmp = _wrappedComparator.CompareBottom(childDoc);
                if (cmp < 0)
                {
                    return cmp;
                }

                while (true)
                {
                    childDoc = _childDocuments.NextSetBit(childDoc + 1);
                    if (childDoc >= parentDoc || childDoc == -1)
                    {
                        return cmp;
                    }
                    int cmp1 = _wrappedComparator.CompareTop(childDoc);
                    if (cmp1 < 0)
                    {
                        return cmp1;
                    }
                    if (cmp1 == 0)
                    {
                        cmp = 0;
                    }
                }
            }
        }
    }
}