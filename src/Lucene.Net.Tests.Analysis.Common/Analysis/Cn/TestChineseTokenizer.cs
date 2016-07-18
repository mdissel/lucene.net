﻿using System;

namespace org.apache.lucene.analysis.cn
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


	using org.apache.lucene.analysis;
	using WhitespaceTokenizer = org.apache.lucene.analysis.core.WhitespaceTokenizer;
	using OffsetAttribute = org.apache.lucene.analysis.tokenattributes.OffsetAttribute;
	using Version = org.apache.lucene.util.Version;


	/// @deprecated Remove this test when ChineseAnalyzer is removed. 
	[Obsolete("Remove this test when ChineseAnalyzer is removed.")]
	public class TestChineseTokenizer : BaseTokenStreamTestCase
	{
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testOtherLetterOffset() throws java.io.IOException
		public virtual void testOtherLetterOffset()
		{
			string s = "a天b";
			ChineseTokenizer tokenizer = new ChineseTokenizer(new StringReader(s));

			int correctStartOffset = 0;
			int correctEndOffset = 1;
			OffsetAttribute offsetAtt = tokenizer.getAttribute(typeof(OffsetAttribute));
			tokenizer.reset();
			while (tokenizer.incrementToken())
			{
			  assertEquals(correctStartOffset, offsetAtt.startOffset());
			  assertEquals(correctEndOffset, offsetAtt.endOffset());
			  correctStartOffset++;
			  correctEndOffset++;
			}
			tokenizer.end();
			tokenizer.close();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testReusableTokenStream() throws Exception
		public virtual void testReusableTokenStream()
		{
		  Analyzer a = new ChineseAnalyzer();
		  assertAnalyzesTo(a, "中华人民共和国", new string[] {"中", "华", "人", "民", "共", "和", "国"}, new int[] {0, 1, 2, 3, 4, 5, 6}, new int[] {1, 2, 3, 4, 5, 6, 7});
		  assertAnalyzesTo(a, "北京市", new string[] {"北", "京", "市"}, new int[] {0, 1, 2}, new int[] {1, 2, 3});
		}

		/*
		 * Analyzer that just uses ChineseTokenizer, not ChineseFilter.
		 * convenience to show the behavior of the tokenizer
		 */
		private class JustChineseTokenizerAnalyzer : Analyzer
		{
			private readonly TestChineseTokenizer outerInstance;

			public JustChineseTokenizerAnalyzer(TestChineseTokenizer outerInstance)
			{
				this.outerInstance = outerInstance;
			}

		  public override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			return new TokenStreamComponents(new ChineseTokenizer(reader));
		  }
		}

		/*
		 * Analyzer that just uses ChineseFilter, not ChineseTokenizer.
		 * convenience to show the behavior of the filter.
		 */
		private class JustChineseFilterAnalyzer : Analyzer
		{
			private readonly TestChineseTokenizer outerInstance;

			public JustChineseFilterAnalyzer(TestChineseTokenizer outerInstance)
			{
				this.outerInstance = outerInstance;
			}

		  public override TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new WhitespaceTokenizer(Version.LUCENE_CURRENT, reader);
			return new TokenStreamComponents(tokenizer, new ChineseFilter(tokenizer));
		  }
		}

		/*
		 * ChineseTokenizer tokenizes numbers as one token, but they are filtered by ChineseFilter
		 */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testNumerics() throws Exception
		public virtual void testNumerics()
		{
		  Analyzer justTokenizer = new JustChineseTokenizerAnalyzer(this);
		  assertAnalyzesTo(justTokenizer, "中1234", new string[] {"中", "1234"});

		  // in this case the ChineseAnalyzer (which applies ChineseFilter) will remove the numeric token.
		  Analyzer a = new ChineseAnalyzer();
		  assertAnalyzesTo(a, "中1234", new string[] {"中"});
		}

		/*
		 * ChineseTokenizer tokenizes english similar to SimpleAnalyzer.
		 * it will lowercase terms automatically.
		 * 
		 * ChineseFilter has an english stopword list, it also removes any single character tokens.
		 * the stopword list is case-sensitive.
		 */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEnglish() throws Exception
		public virtual void testEnglish()
		{
		  Analyzer chinese = new ChineseAnalyzer();
		  assertAnalyzesTo(chinese, "This is a Test. b c d", new string[] {"test"});

		  Analyzer justTokenizer = new JustChineseTokenizerAnalyzer(this);
		  assertAnalyzesTo(justTokenizer, "This is a Test. b c d", new string[] {"this", "is", "a", "test", "b", "c", "d"});

		  Analyzer justFilter = new JustChineseFilterAnalyzer(this);
		  assertAnalyzesTo(justFilter, "This is a Test. b c d", new string[] {"This", "Test."});
		}

		/// <summary>
		/// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
		public virtual void testRandomStrings()
		{
		  checkRandomData(random(), new ChineseAnalyzer(), 10000 * RANDOM_MULTIPLIER);
		}

	}

}