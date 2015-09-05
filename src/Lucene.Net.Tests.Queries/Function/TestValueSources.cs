﻿using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries.Function
{
    // TODO: add separate docvalues test
    /// <summary>
    /// barebones tests for function queries.
    /// </summary>
    public class TestValueSources : LuceneTestCase
    {
        internal static Directory dir;
        internal static IndexReader reader;
        internal static IndexSearcher searcher;

        internal static readonly IList<string[]> documents = new[]
        {
            /*      id,  byte, double, float, int,  long,   short, string, text */
            new[] { "0", "5",  "3.63", "5.2", "35", "4343", "945", "test", "this is a test test test" },
            new[] { "1", "12", "5.65", "9.3", "54", "1954", "123", "bar", "second test" }
        };

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            dir = NewDirectory();
            IndexWriterConfig iwConfig = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwConfig.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwConfig);
            Document document = new Document();
            Field idField = new StringField("id", "", Field.Store.NO);
            document.Add(idField);
            Field byteField = new StringField("byte", "", Field.Store.NO);
            document.Add(byteField);
            Field doubleField = new StringField("double", "", Field.Store.NO);
            document.Add(doubleField);
            Field floatField = new StringField("float", "", Field.Store.NO);
            document.Add(floatField);
            Field intField = new StringField("int", "", Field.Store.NO);
            document.Add(intField);
            Field longField = new StringField("long", "", Field.Store.NO);
            document.Add(longField);
            Field shortField = new StringField("short", "", Field.Store.NO);
            document.Add(shortField);
            Field stringField = new StringField("string", "", Field.Store.NO);
            document.Add(stringField);
            Field textField = new TextField("text", "", Field.Store.NO);
            document.Add(textField);

            foreach (string[] doc in documents)
            {
                idField.StringValue = doc[0];
                byteField.StringValue = doc[1];
                doubleField.StringValue = doc[2];
                floatField.StringValue = doc[3];
                intField.StringValue = doc[4];
                longField.StringValue = doc[5];
                shortField.StringValue = doc[6];
                stringField.StringValue = doc[7];
                textField.StringValue = doc[8];
                iw.AddDocument(document);
            }

            reader = iw.Reader;
            searcher = NewSearcher(reader);
            iw.Dispose();
        }
        
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            searcher = null;
            reader.Dispose();
            reader = null;
            dir.Dispose();
            dir = null;
        }
        
        [Test]
        public void TestByte()
        {
            AssertHits(new FunctionQuery(new ByteFieldSource("byte")), new[] { 5f, 12f });
        }

        [Test]
        public void TestConst()
        {
            AssertHits(new FunctionQuery(new ConstValueSource(0.3f)), new[] { 0.3f, 0.3f });
        }

        [Test]
        public void TestDiv()
        {
            AssertHits(new FunctionQuery(new DivFloatFunction(new ConstValueSource(10f), new ConstValueSource(5f))), new[] { 2f, 2f });
        }

        [Test]
        public void TestDocFreq()
        {
            AssertHits(new FunctionQuery(new DocFreqValueSource("bogus", "bogus", "text", new BytesRef("test"))), new[] { 2f, 2f });
        }

        [Test]
        public void TestDoubleConst()
        {
            AssertHits(new FunctionQuery(new DoubleConstValueSource(0.3d)), new[] { 0.3f, 0.3f });
        }

        [Test]
        public void TestDouble()
        {
            AssertHits(new FunctionQuery(new DoubleFieldSource("double")), new[] { 3.63f, 5.65f });
        }

        [Test]
        public void TestFloat()
        {
            AssertHits(new FunctionQuery(new FloatFieldSource("float")), new[] { 5.2f, 9.3f });
        }

        [Test]
        public void TestIDF()
        {
            Similarity saved = searcher.Similarity;
            try
            {
                searcher.Similarity = new DefaultSimilarity();
                AssertHits(new FunctionQuery(new IDFValueSource("bogus", "bogus", "text", new BytesRef("test"))), new[] { 0.5945349f, 0.5945349f });
            }
            finally
            {
                searcher.Similarity = saved;
            }
        }

        [Test]
        public void TestIf()
        {
            AssertHits(new FunctionQuery(new IfFunction(new BytesRefFieldSource("id"), new ConstValueSource(1.0f), new ConstValueSource(2.0f)
               )), new[] { 1f, 1f });
            // true just if a value exists...
            AssertHits(new FunctionQuery(new IfFunction(new LiteralValueSource("false"), new ConstValueSource(1.0f), new ConstValueSource(2.0f)
               )), new[] { 1f, 1f });
        }

        [Test]
        public void TestInt()
        {
            AssertHits(new FunctionQuery(new IntFieldSource("int")), new[] { 35f, 54f });
        }

        [Test]
        public void TestJoinDocFreq()
        {
            AssertHits(new FunctionQuery(new JoinDocFreqValueSource("string", "text")), new[] { 2f, 0f });
        }

        [Test]
        public void TestLinearFloat()
        {
            AssertHits(new FunctionQuery(new LinearFloatFunction(new ConstValueSource(2.0f), 3, 1)), new[] { 7f, 7f });
        }

        [Test]
        public void TestLong()
        {
            AssertHits(new FunctionQuery(new LongFieldSource("long")), new[] { 4343f, 1954f });
        }

        [Test]
        public void TestMaxDoc()
        {
            AssertHits(new FunctionQuery(new MaxDocValueSource()), new[] { 2f, 2f });
        }

        [Test]
        public void TestMaxFloat()
        {
            AssertHits(new FunctionQuery(new MaxFloatFunction(new ValueSource[] { new ConstValueSource(1f), new ConstValueSource(2f) })), new[] { 2f, 2f });
        }

        [Test]
        public void TestMinFloat()
        {
            AssertHits(new FunctionQuery(new MinFloatFunction(new ValueSource[] { new ConstValueSource(1f), new ConstValueSource(2f) })), new[] { 1f, 1f });
        }

        [Test]
        public void TestNorm()
        {
            Similarity saved = searcher.Similarity;
            try
            {
                // no norm field (so agnostic to indexed similarity)
                searcher.Similarity = new DefaultSimilarity();
                AssertHits(new FunctionQuery(new NormValueSource("byte")), new[] { 0f, 0f });
            }
            finally
            {
                searcher.Similarity = saved;
            }
        }

        [Test]
        public void TestNumDocs()
        {
            AssertHits(new FunctionQuery(new NumDocsValueSource()), new[] { 2f, 2f });
        }

        [Test]
        public void TestPow()
        {
            AssertHits(new FunctionQuery(new PowFloatFunction(new ConstValueSource(2f), new ConstValueSource(3f))), new[] { 8f, 8f });
        }

        [Test]
        public void TestProduct()
        {
            AssertHits(new FunctionQuery(new ProductFloatFunction(new ValueSource[] { new ConstValueSource(2f), new ConstValueSource(3f) })), new[] { 6f, 6f });
        }

        [Test]
        public void TestQuery()
        {
            AssertHits(new FunctionQuery(new QueryValueSource(new FunctionQuery(new ConstValueSource(2f)), 0f)), new[] { 2f, 2f });
        }

        [Test]
        public void TestRangeMap()
        {
            AssertHits(new FunctionQuery(new RangeMapFloatFunction(new FloatFieldSource("float"), 5, 6, 1, 0f)), new[] { 1f, 0f });
            AssertHits(new FunctionQuery(new RangeMapFloatFunction(new FloatFieldSource("float"), 5, 6, new SumFloatFunction(new ValueSource[] { new ConstValueSource(1f), new ConstValueSource(2f) }), new ConstValueSource(11f))), new[] { 3f, 11f });
        }

        [Test]
        public void TestReciprocal()
        {
            AssertHits(new FunctionQuery(new ReciprocalFloatFunction(new ConstValueSource(2f), 3, 1, 4)), new[] { 0.1f, 0.1f });
        }

        [Test]
        public void TestScale()
        {
            AssertHits(new FunctionQuery(new ScaleFloatFunction(new IntFieldSource("int"), 0, 1)), new[] { 0.0f, 1.0f });
        }

        [Test]
        public void TestShort()
        {
            AssertHits(new FunctionQuery(new ShortFieldSource("short")), new[] { 945f, 123f });
        }

        [Test]
        public void TestSumFloat()
        {
            AssertHits(new FunctionQuery(new SumFloatFunction(new ValueSource[] { new ConstValueSource(1f), new ConstValueSource(2f) })), new[] { 3f, 3f });
        }

        [Test]
        public void TestSumTotalTermFreq()
        {
            if (Codec.Default.Name.Equals("Lucene3x"))
            {
                AssertHits(new FunctionQuery(new SumTotalTermFreqValueSource("text")), new[] { -1f, -1f });
            }
            else
            {
                AssertHits(new FunctionQuery(new SumTotalTermFreqValueSource("text")), new[] { 8f, 8f });
            }
        }

        [Test]
        public void TestTermFreq()
        {
            AssertHits(new FunctionQuery(new TermFreqValueSource("bogus", "bogus", "text", new BytesRef("test"))), new[] { 3f, 1f });
            AssertHits(new FunctionQuery(new TermFreqValueSource("bogus", "bogus", "string", new BytesRef("bar"))), new[] { 0f, 1f });
        }

        [Test]
        public void TestTF()
        {
            Similarity saved = searcher.Similarity;
            try
            {
                // no norm field (so agnostic to indexed similarity)
                searcher.Similarity = new DefaultSimilarity();
                AssertHits(new FunctionQuery(new TFValueSource("bogus", "bogus", "text", new BytesRef("test"))), new[] { (float)Math.Sqrt(3d), (float)Math.Sqrt(1d) });
                AssertHits(new FunctionQuery(new TFValueSource("bogus", "bogus", "string", new BytesRef("bar"))), new[] { 0f, 1f });
            }
            finally
            {
                searcher.Similarity = saved;
            }
        }

        [Test]
        public void TestTotalTermFreq()
        {
            if (Codec.Default.Name.Equals("Lucene3x"))
            {
                AssertHits(new FunctionQuery(new TotalTermFreqValueSource("bogus", "bogus", "text", new BytesRef("test"))), new[] { -1f, -1f });
            }
            else
            {
                AssertHits(new FunctionQuery(new TotalTermFreqValueSource("bogus", "bogus", "text", new BytesRef("test"))), new[] { 4f, 4f });
            }
        }

        private static void AssertHits(Query q, float[] scores)
        {
            ScoreDoc[] expected = new ScoreDoc[scores.Length];
            int[] expectedDocs = new int[scores.Length];
            for (int i = 0; i < expected.Length; i++)
            {
                expectedDocs[i] = i;
                expected[i] = new ScoreDoc(i, scores[i]);
            }
            TopDocs docs = searcher.Search(q, null, documents.Count, new Sort(new SortField("id", SortField.Type_e.STRING)), true, false);
            CheckHits.DoCheckHits(Random(), q, "", searcher, expectedDocs);
            CheckHits.CheckHitsQuery(q, expected, docs.ScoreDocs, expectedDocs);
            CheckHits.CheckExplanations(q, "", searcher);
        }
    }
}