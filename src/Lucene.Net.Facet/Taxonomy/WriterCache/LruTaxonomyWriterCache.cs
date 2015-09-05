﻿namespace Lucene.Net.Facet.Taxonomy.WriterCache
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
    /// LRU <seealso cref="TaxonomyWriterCache"/> - good choice for huge taxonomies.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class LruTaxonomyWriterCache : TaxonomyWriterCache
    {

        /// <summary>
        /// Determines cache type.
        /// For guaranteed correctness - not relying on no-collisions in the hash
        /// function, LRU_STRING should be used.
        /// </summary>
        public enum LRUType
        {
            /// <summary>
            /// Use the label's hash as the key; this can lead to
            ///  silent conflicts! 
            /// </summary>
            LRU_HASHED,

            /// <summary>
            /// Use the label as the hash key; this is always
            ///  correct but will usually use more RAM. 
            /// </summary>
            LRU_STRING
        }

        private NameIntCacheLRU cache;

        /// <summary>
        /// Creates this with <seealso cref="LRUType#LRU_HASHED"/> method. </summary>
        public LruTaxonomyWriterCache(int cacheSize)
            : this(cacheSize, LRUType.LRU_HASHED)
        {
            // TODO (Facet): choose between NameHashIntCacheLRU and NameIntCacheLRU.
            // For guaranteed correctness - not relying on no-collisions in the hash
            // function, NameIntCacheLRU should be used:
            // On the other hand, NameHashIntCacheLRU takes less RAM but if there
            // are collisions (which we never found) two different paths would be
            // mapped to the same ordinal...
        }

        /// <summary>
        /// Creates this with the specified method. </summary>
        public LruTaxonomyWriterCache(int cacheSize, LRUType lruType)
        {
            // TODO (Facet): choose between NameHashIntCacheLRU and NameIntCacheLRU.
            // For guaranteed correctness - not relying on no-collisions in the hash
            // function, NameIntCacheLRU should be used:
            // On the other hand, NameHashIntCacheLRU takes less RAM but if there
            // are collisions (which we never found) two different paths would be
            // mapped to the same ordinal...
            if (lruType == LRUType.LRU_HASHED)
            {
                this.cache = new NameHashIntCacheLRU(cacheSize);
            }
            else
            {
                this.cache = new NameIntCacheLRU(cacheSize);
            }
        }

        public virtual bool Full
        {
            get
            {
                lock (this)
                {
                    return cache.Size == cache.MaxSize;
                }
            }
        }

        public virtual void Clear()
        {
            lock (this)
            {
                cache.Clear();
            }
        }

        public virtual void Close()
        {
            lock (this)
            {
                cache.Clear();
                cache = null;
            }
        }

        public virtual int Get(FacetLabel categoryPath)
        {
            lock (this)
            {
                int? res = cache.Get(categoryPath);
                if (res == null)
                {
                    return -1;
                }

                return (int)res;
            }
        }

        public virtual bool Put(FacetLabel categoryPath, int ordinal)
        {
            lock (this)
            {
                bool ret = cache.Put(categoryPath, new int?(ordinal));
                // If the cache is full, we need to clear one or more old entries
                // from the cache. However, if we delete from the cache a recent
                // addition that isn't yet in our reader, for this entry to be
                // visible to us we need to make sure that the changes have been
                // committed and we reopen the reader. Because this is a slow
                // operation, we don't delete entries one-by-one but rather in bulk
                // (put() removes the 2/3rd oldest entries).
                if (ret)
                {
                    cache.MakeRoomLRU();
                }
                return ret;
            }
        }

    }

}