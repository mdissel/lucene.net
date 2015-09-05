﻿/*
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

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search.Function;

namespace Lucene.Net.Spatial.Util
{
    public class CachingDoubleValueSource : ValueSource
    {
        protected readonly Dictionary<int, double> cache;
        protected readonly ValueSource source;

        public CachingDoubleValueSource(ValueSource source)
        {
            this.source = source;
            cache = new Dictionary<int, double>();
        }

        public override string Description
        {
            get { return "Cached[" + source.Description + "]"; }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            int @base = readerContext.docBase;
            FunctionValues vals = source.GetValues(context, readerContext);
            return new CachingDoubleFunctionValue(@base, vals, cache);
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;

            var that = o as CachingDoubleValueSource;

            if (that == null) return false;
            if (source != null ? !source.Equals(that.source) : that.source != null) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return source != null ? source.GetHashCode() : 0;
        }

        #region Nested type: CachingDoubleFunctionValue

        public class CachingDoubleFunctionValue : FunctionValues
        {
            private readonly Dictionary<int, double> cache;
            private readonly int docBase;
            private readonly FunctionValues values;

            public CachingDoubleFunctionValue(int docBase, FunctionValues vals, Dictionary<int, double> cache)
            {
                this.docBase = docBase;
                values = vals;
                this.cache = cache;
            }

            public override double DoubleVal(int doc)
            {
                int key = docBase + doc;
                double v;
                if (!cache.TryGetValue(key, out v))
                {
                    v = values.DoubleVal(doc);
                    cache[key] = v;
                }
                return v;
            }

            public override float FloatVal(int doc)
            {
                return (float)DoubleVal(doc);
            }

            public override string ToString(int doc)
            {
                return DoubleVal(doc) + string.Empty;
            }
        }

        #endregion
    }
}