﻿namespace org.apache.lucene.analysis.miscellaneous
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

	using BaseTokenStreamFactoryTestCase = org.apache.lucene.analysis.util.BaseTokenStreamFactoryTestCase;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using ClasspathResourceLoader = org.apache.lucene.analysis.util.ClasspathResourceLoader;
	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;

	public class TestKeepFilterFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testInform() throws Exception
	  public virtual void testInform()
	  {
		ResourceLoader loader = new ClasspathResourceLoader(this.GetType());
		assertTrue("loader is null and it shouldn't be", loader != null);
		KeepWordFilterFactory factory = (KeepWordFilterFactory) tokenFilterFactory("KeepWord", "words", "keep-1.txt", "ignoreCase", "true");
		CharArraySet words = factory.Words;
		assertTrue("words is null and it shouldn't be", words != null);
		assertTrue("words Size: " + words.size() + " is not: " + 2, words.size() == 2);

		factory = (KeepWordFilterFactory) tokenFilterFactory("KeepWord", "words", "keep-1.txt, keep-2.txt", "ignoreCase", "true");
		words = factory.Words;
		assertTrue("words is null and it shouldn't be", words != null);
		assertTrue("words Size: " + words.size() + " is not: " + 4, words.size() == 4);
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenFilterFactory("KeepWord", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }
	}
}