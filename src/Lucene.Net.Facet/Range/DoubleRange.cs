﻿using System;
using System.Collections.Generic;
using Lucene.Net.Support;

namespace Lucene.Net.Facet.Range
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


    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Filter = Lucene.Net.Search.Filter;
    using Bits = Lucene.Net.Util.Bits;
    using NumericUtils = Lucene.Net.Util.NumericUtils;

    /// <summary>
    /// Represents a range over double values.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public sealed class DoubleRange : Range
    {
        internal readonly double minIncl;
        internal readonly double maxIncl;

        /// <summary>
        /// Minimum. </summary>
        public readonly double Min;

        /// <summary>
        /// Maximum. </summary>
        public readonly double Max;

        /// <summary>
        /// True if the minimum value is inclusive. </summary>
        public readonly bool MinInclusive;

        /// <summary>
        /// True if the maximum value is inclusive. </summary>
        public readonly bool MaxInclusive;

        private const double EPSILON = 1E-14;
        /// <summary>
        /// Create a DoubleRange. </summary>
        public DoubleRange(string label, double minIn, bool minInclusive, double maxIn, bool maxInclusive)
            : base(label)
        {
            this.Min = minIn;
            this.Max = maxIn;
            this.MinInclusive = minInclusive;
            this.MaxInclusive = maxInclusive;

            // TODO: if DoubleDocValuesField used
            // NumericUtils.doubleToSortableLong format (instead of
            // Double.doubleToRawLongBits) we could do comparisons
            // in long space 

            if (double.IsNaN(Min))
            {
                throw new System.ArgumentException("min cannot be NaN");
            }
            if (!minInclusive)
            {
                minIn += EPSILON;
            }

            if (double.IsNaN(Max))
            {
                throw new System.ArgumentException("max cannot be NaN");
            }
            if (!maxInclusive)
            {
                // Why no Math.nextDown?
                maxIn = maxIn -= EPSILON;
            }

            if (minIn > maxIn)
            {
                FailNoMatch();
            }

            this.minIncl = minIn;
            this.maxIncl = maxIn;
        }

        /// <summary>
        /// True if this range accepts the provided value. </summary>
        public bool accept(double value)
        {
            return value >= minIncl && value <= maxIncl;
        }

        internal LongRange ToLongRange()
        {
            return new LongRange(Label, NumericUtils.DoubleToSortableLong(minIncl), true, NumericUtils.DoubleToSortableLong(maxIncl), true);
        }

        public override string ToString()
        {
            return "DoubleRange(" + minIncl + " to " + maxIncl + ")";
        }

        public override Filter GetFilter(Filter fastMatchFilter, ValueSource valueSource)
        {
            return new FilterAnonymousInnerClassHelper(this, fastMatchFilter, valueSource);
        }

        private class FilterAnonymousInnerClassHelper : Filter
        {
            private readonly DoubleRange outerInstance;

            private Filter fastMatchFilter;
            private ValueSource valueSource;

            public FilterAnonymousInnerClassHelper(DoubleRange outerInstance, Filter fastMatchFilter, ValueSource valueSource)
            {
                this.outerInstance = outerInstance;
                this.fastMatchFilter = fastMatchFilter;
                this.valueSource = valueSource;
            }


            public override string ToString()
            {
                return "Filter(" + outerInstance.ToString() + ")";
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {

                // TODO: this is just like ValueSourceScorer,
                // ValueSourceFilter (spatial),
                // ValueSourceRangeFilter (solr); also,
                // https://issues.apache.org/jira/browse/LUCENE-4251

                var values = valueSource.GetValues(new Dictionary<string,Lucene.Net.Search.Scorer>(), context);

                int maxDoc = context.Reader.MaxDoc;

                Bits fastMatchBits;
                if (fastMatchFilter != null)
                {
                    DocIdSet dis = fastMatchFilter.GetDocIdSet(context, null);
                    if (dis == null)
                    {
                        // No documents match
                        return null;
                    }
                    fastMatchBits = dis.GetBits();
                    if (fastMatchBits == null)
                    {
                        throw new System.ArgumentException("fastMatchFilter does not implement DocIdSet.bits");
                    }
                }
                else
                {
                    fastMatchBits = null;
                }

                return new DocIdSetAnonymousInnerClassHelper(this, acceptDocs, values, maxDoc, fastMatchBits);
            }

            private class DocIdSetAnonymousInnerClassHelper : DocIdSet
            {
                private readonly FilterAnonymousInnerClassHelper outerInstance;

                private Bits acceptDocs;
                private FunctionValues values;
                private int maxDoc;
                private Bits fastMatchBits;

                public DocIdSetAnonymousInnerClassHelper(FilterAnonymousInnerClassHelper outerInstance, Bits acceptDocs, FunctionValues values, int maxDoc, Bits fastMatchBits)
                {
                    this.outerInstance = outerInstance;
                    this.acceptDocs = acceptDocs;
                    this.values = values;
                    this.maxDoc = maxDoc;
                    this.fastMatchBits = fastMatchBits;
                }


                public override Bits GetBits()
                {
                    return new BitsAnonymousInnerClassHelper(this);
                }

                private class BitsAnonymousInnerClassHelper : Bits
                {
                    private readonly DocIdSetAnonymousInnerClassHelper outerInstance;

                    public BitsAnonymousInnerClassHelper(DocIdSetAnonymousInnerClassHelper outerInstance)
                    {
                        this.outerInstance = outerInstance;
                    }

                    public virtual bool Get(int docID)
                    {
                        if (outerInstance.acceptDocs != null && outerInstance.acceptDocs.Get(docID) == false)
                        {
                            return false;
                        }
                        if (outerInstance.fastMatchBits != null && outerInstance.fastMatchBits.Get(docID) == false)
                        {
                            return false;
                        }
                        return outerInstance.outerInstance.outerInstance.accept(outerInstance.values.DoubleVal(docID));
                    }

                    public virtual int Length()
                    {
                        return outerInstance.maxDoc;
                    }
                }

                public override DocIdSetIterator GetIterator()
                {
                    throw new System.NotSupportedException("this filter can only be accessed via bits()");
                }
            }
        }
    }


}