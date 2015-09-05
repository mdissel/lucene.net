﻿using NUnit.Framework;

namespace Lucene.Net.Facet.Taxonomy
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

    using Lucene.Net.Facet.Taxonomy;
    [TestFixture]
    public class TestLRUHashMap : FacetTestCase
    {
        // testLRU() tests that the specified size limit is indeed honored, and
        // the remaining objects in the map are indeed those that have been most
        // recently used
        [Test]
        public virtual void TestLru()
        {
            LRUHashMap<string, string> lru = new LRUHashMap<string, string>(3,1);
            Assert.AreEqual(0, lru.Size());
            lru.Put("one", "Hello world");
            Assert.AreEqual(1, lru.Size());
            lru.Put("two", "Hi man");
            Assert.AreEqual(2, lru.Size());
            lru.Put("three", "Bonjour");
            Assert.AreEqual(3, lru.Size());
            lru.Put("four", "Shalom");
            Assert.AreEqual(3, lru.Size());
            Assert.NotNull(lru.Get("three"));
            Assert.NotNull(lru.Get("two"));
            Assert.NotNull(lru.Get("four"));
            Assert.Null(lru.Get("one"));
            lru.Put("five", "Yo!");
            Assert.AreEqual(3, lru.Size());
            Assert.Null(lru.Get("three")); // three was last used, so it got removed
            Assert.NotNull(lru.Get("five"));
            lru.Get("four");
            lru.Put("six", "hi");
            lru.Put("seven", "hey dude");
            Assert.AreEqual(3, lru.Size());
            Assert.Null(lru.Get("one"));
            Assert.Null(lru.Get("two"));
            Assert.Null(lru.Get("three"));
            Assert.NotNull(lru.Get("four"));
            Assert.Null(lru.Get("five"));
            Assert.NotNull(lru.Get("six"));
            Assert.NotNull(lru.Get("seven"));
        }
    }

}