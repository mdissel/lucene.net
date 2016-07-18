﻿using System;
using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Analysis.Common.Analysis.Util
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
    /// TestCase for the <seealso cref="CharacterUtils"/> class.
    /// </summary>
    [TestFixture]
    public class TestCharacterUtils : LuceneTestCase
    {
        [Test]
        public virtual void TestCodePointAtCharSequenceInt()
        {
            var java4 = CharacterUtils.GetInstance(LuceneVersion.LUCENE_30);
            var cpAt3 = "Abc\ud801\udc1c";
            var highSurrogateAt3 = "Abc\ud801";
            assertEquals((int)'A', java4.CodePointAt(cpAt3, 0));
            assertEquals((int)'\ud801', java4.CodePointAt(cpAt3, 3));
            assertEquals((int)'\ud801', java4.CodePointAt(highSurrogateAt3, 3));
            try
            {
                java4.CodePointAt(highSurrogateAt3, 4);
                fail("string index out of bounds");
            }
            catch (IndexOutOfRangeException)
            {
            }

            var java5 = CharacterUtils.GetInstance(TEST_VERSION_CURRENT);
            assertEquals((int)'A', java5.CodePointAt(cpAt3, 0));
            assertEquals(Character.CodePointAt('\ud801', '\udc1c'), java5.CodePointAt(cpAt3, 3));
            assertEquals((int)'\ud801', java5.CodePointAt(highSurrogateAt3, 3));
            try
            {
                java5.CodePointAt(highSurrogateAt3, 4);
                fail("string index out of bounds");
            }
            catch (System.IndexOutOfRangeException)
            {
            }

        }

        [Test]
        public virtual void TestCodePointAtCharArrayIntInt()
        {
            var java4 = CharacterUtils.GetInstance(LuceneVersion.LUCENE_30);
            var cpAt3 = "Abc\ud801\udc1c".ToCharArray();
            var highSurrogateAt3 = "Abc\ud801".ToCharArray();
            assertEquals((int)'A', java4.CodePointAt(cpAt3, 0, 2));
            assertEquals((int)'\ud801', java4.CodePointAt(cpAt3, 3, 5));
            assertEquals((int)'\ud801', java4.CodePointAt(highSurrogateAt3, 3, 4));

            var java5 = CharacterUtils.GetInstance(TEST_VERSION_CURRENT);
            assertEquals((int)'A', java5.CodePointAt(cpAt3, 0, 2));
            assertEquals(Character.ToCodePoint('\ud801', '\udc1c'), java5.CodePointAt(cpAt3, 3, 5));
            assertEquals((int)'\ud801', java5.CodePointAt(highSurrogateAt3, 3, 4));
        }

        [Test]
        public virtual void TestCodePointCount()
        {
            var java4 = CharacterUtils.Java4Instance;
            var java5 = CharacterUtils.GetInstance(TEST_VERSION_CURRENT);
            
            var s = TestUtil.RandomUnicodeString(Random());
            assertEquals(s.Length, java4.CodePointCount(s));
            assertEquals(Character.CodePointCount(s, 0, s.Length), java5.CodePointCount(s));
        }

        [Test]
        public virtual void TestOffsetByCodePoint()
        {
            var java4 = CharacterUtils.Java4Instance;
            var java5 = CharacterUtils.GetInstance(TEST_VERSION_CURRENT);
            for (int i = 0; i < 10; ++i)
            {
                var s = TestUtil.RandomUnicodeString(Random()).toCharArray();
                var index = TestUtil.NextInt(Random(), 0, s.Length);
                var offset = Random().Next(7) - 3;
                try
                {
                    var to = java4.OffsetByCodePoints(s, 0, s.Length, index, offset);
                    assertEquals(to, index + offset);
                }
                catch (System.IndexOutOfRangeException)
                {
                    assertTrue((index + offset) < 0 || (index + offset) > s.Length);
                }

                int o;
                try
                {
                    o = java5.OffsetByCodePoints(s, 0, s.Length, index, offset);
                }
                catch (System.IndexOutOfRangeException)
                {
                    try
                    {
                        Character.OffsetByCodePoints(s, 0, s.Length, index, offset);
                        fail();
                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        // OK
                    }
                    o = -1;
                }
                if (o >= 0)
                {
                    assertEquals(Character.OffsetByCodePoints(s, 0, s.Length, index, offset), o);
                }
            }
        }

        [Test]
        public virtual void TestConversions()
        {
            var java4 = CharacterUtils.Java4Instance;
            var java5 = CharacterUtils.GetInstance(TEST_VERSION_CURRENT);
            TestConversions(java4);
            TestConversions(java5);
        }

        private void TestConversions(CharacterUtils charUtils)
        {
            var orig = TestUtil.RandomUnicodeString(Random(), 100).toCharArray();
            
            var buf = new int[orig.Length];
            
            var restored = new char[buf.Length];
            
            var o1 = TestUtil.NextInt(Random(), 0, Math.Min(5, orig.Length));
            var o2 = TestUtil.NextInt(Random(), 0, o1);
            var o3 = TestUtil.NextInt(Random(), 0, o1);
            var codePointCount = charUtils.toCodePoints(orig, o1, orig.Length - o1, buf, o2);
            var charCount = charUtils.toChars(buf, o2, codePointCount, restored, o3);
            assertEquals(orig.Length - o1, charCount);
            assertArrayEquals(Arrays.CopyOfRange(orig, o1, o1 + charCount), Arrays.CopyOfRange(restored, o3, o3 + charCount));
        }

        [Test]
        public virtual void TestNewCharacterBuffer()
        {
            var newCharacterBuffer = CharacterUtils.NewCharacterBuffer(1024);
            assertEquals(1024, newCharacterBuffer.Buffer.Length);
            assertEquals(0, newCharacterBuffer.Offset);
            assertEquals(0, newCharacterBuffer.Length);

            newCharacterBuffer = CharacterUtils.NewCharacterBuffer(2);
            assertEquals(2, newCharacterBuffer.Buffer.Length);
            assertEquals(0, newCharacterBuffer.Offset);
            assertEquals(0, newCharacterBuffer.Length);

            try
            {
                CharacterUtils.NewCharacterBuffer(1);
                fail("length must be >= 2");
            }
            catch (System.ArgumentException)
            {
            }
        }

        [Test]
        public virtual void TestFillNoHighSurrogate()
        {
            var versions = new LuceneVersion[] { LuceneVersion.LUCENE_30, TEST_VERSION_CURRENT };
            foreach (var version in versions)
            {
                var instance = CharacterUtils.GetInstance(version);
                var reader = new StringReader("helloworld");
                var buffer = CharacterUtils.NewCharacterBuffer(6);
                assertTrue(instance.Fill(buffer, reader));
                assertEquals(0, buffer.Offset);
                assertEquals(6, buffer.Length);
                assertEquals("hellow", new string(buffer.Buffer));
                assertFalse(instance.Fill(buffer, reader));
                assertEquals(4, buffer.Length);
                assertEquals(0, buffer.Offset);

                assertEquals("orld", new string(buffer.Buffer, buffer.Offset, buffer.Length));
                assertFalse(instance.Fill(buffer, reader));
            }
        }

        [Test]
        public virtual void TestFillJava15()
        {
            const string input = "1234\ud801\udc1c789123\ud801\ud801\udc1c\ud801";
            var instance = CharacterUtils.GetInstance(TEST_VERSION_CURRENT);
            var reader = new StringReader(input);
            var buffer = CharacterUtils.NewCharacterBuffer(5);
            assertTrue(instance.Fill(buffer, reader));
            assertEquals(4, buffer.Length);
            assertEquals("1234", new string(buffer.Buffer, buffer.Offset, buffer.Length));
            assertTrue(instance.Fill(buffer, reader));
            assertEquals(5, buffer.Length);
            assertEquals("\ud801\udc1c789", new string(buffer.Buffer));
            assertTrue(instance.Fill(buffer, reader));
            assertEquals(4, buffer.Length);
            assertEquals("123\ud801", new string(buffer.Buffer, buffer.Offset, buffer.Length));
            assertFalse(instance.Fill(buffer, reader));
            assertEquals(3, buffer.Length);
            assertEquals("\ud801\udc1c\ud801", new string(buffer.Buffer, buffer.Offset, buffer.Length));
            assertFalse(instance.Fill(buffer, reader));
            assertEquals(0, buffer.Length);
        }

        [Test]
        public virtual void TestFillJava14()
        {
            var input = "1234\ud801\udc1c789123\ud801\ud801\udc1c\ud801";
            var instance = CharacterUtils.GetInstance(LuceneVersion.LUCENE_30);
            var reader = new StringReader(input);
            var buffer = CharacterUtils.NewCharacterBuffer(5);
            assertTrue(instance.Fill(buffer, reader));
            assertEquals(5, buffer.Length);
            assertEquals("1234\ud801", new string(buffer.Buffer, buffer.Offset, buffer.Length));
            assertTrue(instance.Fill(buffer, reader));
            assertEquals(5, buffer.Length);
            assertEquals("\udc1c7891", new string(buffer.Buffer));
            buffer = CharacterUtils.NewCharacterBuffer(6);
            assertTrue(instance.Fill(buffer, reader));
            assertEquals(6, buffer.Length);
            assertEquals("23\ud801\ud801\udc1c\ud801", new string(buffer.Buffer, buffer.Offset, buffer.Length));
            assertFalse(instance.Fill(buffer, reader));
        }

    }

}