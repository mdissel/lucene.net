﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using Lucene.Net.Support;

namespace Lucene.Net.Facet.Taxonomy
{


    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;

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
    /// TaxonomyReader is the read-only interface with which the faceted-search
    /// library uses the taxonomy during search time.
    /// <P>
    /// A TaxonomyReader holds a list of categories. Each category has a serial
    /// number which we call an "ordinal", and a hierarchical "path" name:
    /// <UL>
    /// <LI>
    /// The ordinal is an integer that starts at 0 for the first category (which is
    /// always the root category), and grows contiguously as more categories are
    /// added; Note that once a category is added, it can never be deleted.
    /// <LI>
    /// The path is a CategoryPath object specifying the category's position in the
    /// hierarchy.
    /// </UL>
    /// <B>Notes about concurrent access to the taxonomy:</B>
    /// <P>
    /// An implementation must allow multiple readers to be active concurrently
    /// with a single writer. Readers follow so-called "point in time" semantics,
    /// i.e., a TaxonomyReader object will only see taxonomy entries which were
    /// available at the time it was created. What the writer writes is only
    /// available to (new) readers after the writer's commit() is called.
    /// <P>
    /// In faceted search, two separate indices are used: the main Lucene index,
    /// and the taxonomy. Because the main index refers to the categories listed
    /// in the taxonomy, it is important to open the taxonomy *after* opening the
    /// main index, and it is also necessary to reopen() the taxonomy after
    /// reopen()ing the main index.
    /// <P>
    /// This order is important, otherwise it would be possible for the main index
    /// to refer to a category which is not yet visible in the old snapshot of
    /// the taxonomy. Note that it is indeed fine for the the taxonomy to be opened
    /// after the main index - even a long time after. The reason is that once
    /// a category is added to the taxonomy, it can never be changed or deleted,
    /// so there is no danger that a "too new" taxonomy not being consistent with
    /// an older index.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class TaxonomyReader
    {

        /// <summary>
        /// An iterator over a category's children. </summary>
        public class ChildrenIterator
        {

            internal readonly int[] siblings;
            internal int child;

            internal ChildrenIterator(int child, int[] siblings)
            {
                this.siblings = siblings;
                this.child = child;
            }

            /// <summary>
            /// Return the next child ordinal, or <seealso cref="TaxonomyReader#INVALID_ORDINAL"/>
            /// if no more children.
            /// </summary>
            public virtual int Next()
            {
                int res = child;
                if (child != TaxonomyReader.INVALID_ORDINAL)
                {
                    child = siblings[child];
                }
                return res;
            }

        }

        /// <summary>
        /// Sole constructor. </summary>
        public TaxonomyReader()
        {
        }

        /// <summary>
        /// The root category (the category with the empty path) always has the ordinal
        /// 0, to which we give a name ROOT_ORDINAL. <seealso cref="#getOrdinal(FacetLabel)"/>
        /// of an empty path will always return {@code ROOT_ORDINAL}, and
        /// <seealso cref="#getPath(int)"/> with {@code ROOT_ORDINAL} will return the empty path.
        /// </summary>
        public const int ROOT_ORDINAL = 0;

        /// <summary>
        /// Ordinals are always non-negative, so a negative ordinal can be used to
        /// signify an error. Methods here return INVALID_ORDINAL (-1) in this case.
        /// </summary>
        public const int INVALID_ORDINAL = -1;

        /// <summary>
        /// If the taxonomy has changed since the provided reader was opened, open and
        /// return a new <seealso cref="TaxonomyReader"/>; else, return {@code null}. The new
        /// reader, if not {@code null}, will be the same type of reader as the one
        /// given to this method.
        /// 
        /// <para>
        /// This method is typically far less costly than opening a fully new
        /// <seealso cref="TaxonomyReader"/> as it shares resources with the provided
        /// <seealso cref="TaxonomyReader"/>, when possible.
        /// </para>
        /// </summary>
        public static T OpenIfChanged<T>(T oldTaxoReader) where T : TaxonomyReader
        {
            T newTaxoReader = (T)oldTaxoReader.DoOpenIfChanged();
            Debug.Assert(newTaxoReader != oldTaxoReader);
            return newTaxoReader;
        }

        private volatile bool Closed = false;

        // set refCount to 1 at start
        private readonly AtomicInteger refCount = new AtomicInteger(1);

        /// <summary>
        /// performs the actual task of closing the resources that are used by the
        /// taxonomy reader.
        /// </summary>
        protected internal abstract void DoClose();

        /// <summary>
        /// Implements the actual opening of a new <seealso cref="TaxonomyReader"/> instance if
        /// the taxonomy has changed.
        /// </summary>
        /// <seealso cref= #openIfChanged(TaxonomyReader) </seealso>
        protected abstract TaxonomyReader DoOpenIfChanged();

        /// <summary>
        /// Throws <seealso cref="AlreadyClosedException"/> if this IndexReader is closed
        /// </summary>
        protected void EnsureOpen()
        {
            if (RefCount <= 0)
            {
                throw new AlreadyClosedException("this TaxonomyReader is closed");
            }
        }

        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    if (!Closed)
                    {
                        DecRef();
                        Closed = true;
                    }
                }
            }
        }


        /// <summary>
        /// Expert: decreases the refCount of this TaxonomyReader instance. If the
        /// refCount drops to 0 this taxonomy reader is closed.
        /// </summary>
        public void DecRef()
        {
            EnsureOpen();
            int rc = refCount.DecrementAndGet();
            if (rc == 0)
            {
                bool success = false;
                try
                {
                    DoClose();
                    Closed = true;
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Put reference back on failure
                        refCount.IncrementAndGet();
                    }
                }
            }
            else if (rc < 0)
            {
                throw new ThreadStateException("too many decRef calls: refCount is " + rc + " after decrement");
            }
        }

        /// <summary>
        /// Returns a <seealso cref="ParallelTaxonomyArrays"/> object which can be used to
        /// efficiently traverse the taxonomy tree.
        /// </summary>
        public abstract ParallelTaxonomyArrays ParallelTaxonomyArrays { get; }

        /// <summary>
        /// Returns an iterator over the children of the given ordinal. </summary>
        public virtual ChildrenIterator GetChildren(int ordinal)
        {
            ParallelTaxonomyArrays arrays = ParallelTaxonomyArrays;
            int child = ordinal >= 0 ? arrays.Children()[ordinal] : INVALID_ORDINAL;
            return new ChildrenIterator(child, arrays.Siblings());
        }

        /// <summary>
        /// Retrieve user committed data.
        /// </summary>
        /// <seealso cref= TaxonomyWriter#setCommitData(Map) </seealso>
        public abstract IDictionary<string, string> CommitUserData { get; }

        /// <summary>
        /// Returns the ordinal of the category given as a path. The ordinal is the
        /// category's serial number, an integer which starts with 0 and grows as more
        /// categories are added (note that once a category is added, it can never be
        /// deleted).
        /// </summary>
        /// <returns> the category's ordinal or <seealso cref="#INVALID_ORDINAL"/> if the category
        ///         wasn't foun. </returns>
        public abstract int GetOrdinal(FacetLabel categoryPath);

        /// <summary>
        /// Returns ordinal for the dim + path. </summary>
        public virtual int GetOrdinal(string dim, string[] path)
        {
            string[] fullPath = new string[path.Length + 1];
            fullPath[0] = dim;
            Array.Copy(path, 0, fullPath, 1, path.Length);
            return GetOrdinal(new FacetLabel(fullPath));
        }

        /// <summary>
        /// Returns the path name of the category with the given ordinal. </summary>
        public abstract FacetLabel GetPath(int ordinal);

        /// <summary>
        /// Returns the current refCount for this taxonomy reader. </summary>
        public int RefCount
        {
            get
            {
                return refCount.Get();
            }
        }

        /// <summary>
        /// Returns the number of categories in the taxonomy. Note that the number of
        /// categories returned is often slightly higher than the number of categories
        /// inserted into the taxonomy; This is because when a category is added to the
        /// taxonomy, its ancestors are also added automatically (including the root,
        /// which always get ordinal 0).
        /// </summary>
        public abstract int Size { get; }

        /// <summary>
        /// Expert: increments the refCount of this TaxonomyReader instance. RefCounts
        /// can be used to determine when a taxonomy reader can be closed safely, i.e.
        /// as soon as there are no more references. Be sure to always call a
        /// corresponding decRef(), in a finally clause; otherwise the reader may never
        /// be closed.
        /// </summary>
        public void IncRef()
        {
            EnsureOpen();
            refCount.IncrementAndGet();
        }

        /// <summary>
        /// Expert: increments the refCount of this TaxonomyReader
        ///  instance only if it has not been closed yet.  Returns
        ///  true on success. 
        /// </summary>
        public bool TryIncRef()
        {
            int count;
            while ((count = refCount.Get()) > 0)
            {
                if (refCount.CompareAndSet(count, count + 1))
                {
                    return true;
                }
            }
            return false;
        }


    }

}