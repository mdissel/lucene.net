﻿using System;
using System.Text;

namespace org.apache.lucene.analysis.standard
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
	using ClasspathResourceLoader = org.apache.lucene.analysis.util.ClasspathResourceLoader;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// A few tests based on org.apache.lucene.analysis.TestUAX29URLEmailTokenizer
	/// </summary>

	public class TestUAX29URLEmailTokenizerFactory : BaseTokenStreamFactoryTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testUAX29URLEmailTokenizer() throws Exception
	  public virtual void testUAX29URLEmailTokenizer()
	  {
		Reader reader = new StringReader("Wha\u0301t's this thing do?");
		TokenStream stream = tokenizerFactory("UAX29URLEmail").create(reader);
		assertTokenStreamContents(stream, new string[] {"Wha\u0301t's", "this", "thing", "do"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testArabic() throws Exception
	  public virtual void testArabic()
	  {
		Reader reader = new StringReader("الفيلم الوثائقي الأول عن ويكيبيديا يسمى \"الحقيقة بالأرقام: قصة ويكيبيديا\" (بالإنجليزية: Truth in Numbers: The Wikipedia Story)، سيتم إطلاقه في 2008.");
		TokenStream stream = tokenizerFactory("UAX29URLEmail").create(reader);
		assertTokenStreamContents(stream, new string[] {"الفيلم", "الوثائقي", "الأول", "عن", "ويكيبيديا", "يسمى", "الحقيقة", "بالأرقام", "قصة", "ويكيبيديا", "بالإنجليزية", "Truth", "in", "Numbers", "The", "Wikipedia", "Story", "سيتم", "إطلاقه", "في", "2008"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testChinese() throws Exception
	  public virtual void testChinese()
	  {
		Reader reader = new StringReader("我是中国人。 １２３４ Ｔｅｓｔｓ ");
		TokenStream stream = tokenizerFactory("UAX29URLEmail").create(reader);
		assertTokenStreamContents(stream, new string[] {"我", "是", "中", "国", "人", "１２３４", "Ｔｅｓｔｓ"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKorean() throws Exception
	  public virtual void testKorean()
	  {
		Reader reader = new StringReader("안녕하세요 한글입니다");
		TokenStream stream = tokenizerFactory("UAX29URLEmail").create(reader);
		assertTokenStreamContents(stream, new string[] {"안녕하세요", "한글입니다"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testHyphen() throws Exception
	  public virtual void testHyphen()
	  {
		Reader reader = new StringReader("some-dashed-phrase");
		TokenStream stream = tokenizerFactory("UAX29URLEmail").create(reader);
		assertTokenStreamContents(stream, new string[] {"some", "dashed", "phrase"});
	  }

	  // Test with some URLs from TestUAX29URLEmailTokenizer's 
	  // urls.from.random.text.with.urls.txt
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testURLs() throws Exception
	  public virtual void testURLs()
	  {
		string textWithURLs = "http://johno.jsmf.net/knowhow/ngrams/index.php?table=en-dickens-word-2gram&paragraphs=50&length=200&no-ads=on\n" + " some extra\nWords thrown in here. " + "http://c5-3486.bisynxu.FR/aI.YnNms/" + " samba Halta gamba " + "ftp://119.220.152.185/JgJgdZ/31aW5c/viWlfQSTs5/1c8U5T/ih5rXx/YfUJ/xBW1uHrQo6.R\n" + "M19nq.0URV4A.Me.CC/mj0kgt6hue/dRXv8YVLOw9v/CIOqb\n" + "Https://yu7v33rbt.vC6U3.XN--KPRW13D/y%4fMSzkGFlm/wbDF4m" + " inter Locutio " + "[c2d4::]/%471j5l/j3KFN%AAAn/Fip-NisKH/\n" + "file:///aXvSZS34is/eIgM8s~U5dU4Ifd%c7" + " blah Sirrah woof " + "http://[a42:a7b6::]/qSmxSUU4z/%52qVl4\n";
		Reader reader = new StringReader(textWithURLs);
		TokenStream stream = tokenizerFactory("UAX29URLEmail").create(reader);
		assertTokenStreamContents(stream, new string[] {"http://johno.jsmf.net/knowhow/ngrams/index.php?table=en-dickens-word-2gram&paragraphs=50&length=200&no-ads=on", "some", "extra", "Words", "thrown", "in", "here", "http://c5-3486.bisynxu.FR/aI.YnNms/", "samba", "Halta", "gamba", "ftp://119.220.152.185/JgJgdZ/31aW5c/viWlfQSTs5/1c8U5T/ih5rXx/YfUJ/xBW1uHrQo6.R", "M19nq.0URV4A.Me.CC/mj0kgt6hue/dRXv8YVLOw9v/CIOqb", "Https://yu7v33rbt.vC6U3.XN--KPRW13D/y%4fMSzkGFlm/wbDF4m", "inter", "Locutio", "[c2d4::]/%471j5l/j3KFN%AAAn/Fip-NisKH/", "file:///aXvSZS34is/eIgM8s~U5dU4Ifd%c7", "blah", "Sirrah", "woof", "http://[a42:a7b6::]/qSmxSUU4z/%52qVl4"});
	  }

	  // Test with some emails from TestUAX29URLEmailTokenizer's 
	  // email.addresses.from.random.text.with.email.addresses.txt
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmails() throws Exception
	  public virtual void testEmails()
	  {
		string textWithEmails = " some extra\nWords thrown in here. " + "dJ8ngFi@avz13m.CC\n" + "kU-l6DS@[082.015.228.189]\n" + "\"%U\u0012@?\\B\"@Fl2d.md" + " samba Halta gamba " + "Bvd#@tupjv.sn\n" + "SBMm0Nm.oyk70.rMNdd8k.#ru3LI.gMMLBI.0dZRD4d.RVK2nY@au58t.B13albgy4u.mt\n" + "~+Kdz@3mousnl.SE\n" + " inter Locutio " + "C'ts`@Vh4zk.uoafcft-dr753x4odt04q.UY\n" + "}0tzWYDBuy@cSRQAABB9B.7c8xawf75-cyo.PM" + " blah Sirrah woof " + "lMahAA.j/5.RqUjS745.DtkcYdi@d2-4gb-l6.ae\n" + "lv'p@tqk.vj5s0tgl.0dlu7su3iyiaz.dqso.494.3hb76.XN--MGBAAM7A8H\n";
		Reader reader = new StringReader(textWithEmails);
		TokenStream stream = tokenizerFactory("UAX29URLEmail").create(reader);
		assertTokenStreamContents(stream, new string[] {"some", "extra", "Words", "thrown", "in", "here", "dJ8ngFi@avz13m.CC", "kU-l6DS@[082.015.228.189]", "\"%U\u0012@?\\B\"@Fl2d.md", "samba", "Halta", "gamba", "Bvd#@tupjv.sn", "SBMm0Nm.oyk70.rMNdd8k.#ru3LI.gMMLBI.0dZRD4d.RVK2nY@au58t.B13albgy4u.mt", "~+Kdz@3mousnl.SE", "inter", "Locutio", "C'ts`@Vh4zk.uoafcft-dr753x4odt04q.UY", "}0tzWYDBuy@cSRQAABB9B.7c8xawf75-cyo.PM", "blah", "Sirrah", "woof", "lMahAA.j/5.RqUjS745.DtkcYdi@d2-4gb-l6.ae", "lv'p@tqk.vj5s0tgl.0dlu7su3iyiaz.dqso.494.3hb76.XN--MGBAAM7A8H"});
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testMaxTokenLength() throws Exception
	  public virtual void testMaxTokenLength()
	  {
		StringBuilder builder = new StringBuilder();
		for (int i = 0 ; i < 100 ; ++i)
		{
		  builder.Append("abcdefg"); // 7 * 100 = 700 char "word"
		}
		string longWord = builder.ToString();
		string content = "one two three " + longWord + " four five six";
		Reader reader = new StringReader(content);
		TokenStream stream = tokenizerFactory("UAX29URLEmail", "maxTokenLength", "1000").create(reader);
		assertTokenStreamContents(stream, new string[] {"one", "two", "three", longWord, "four", "five", "six"});
	  }

	  /// @deprecated nuke this test in lucene 5.0 
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Deprecated("nuke this test in lucene 5.0") public void testMatchVersion() throws Exception
	  [Obsolete("nuke this test in lucene 5.0")]
	  public virtual void testMatchVersion()
	  {
		Reader reader = new StringReader("ざ");
		TokenStream stream = tokenizerFactory("UAX29URLEmail").create(reader);
		assertTokenStreamContents(stream, new string[] {"ざ"});

		reader = new StringReader("ざ");
		stream = tokenizerFactory("UAX29URLEmail", Version.LUCENE_31, new ClasspathResourceLoader(this.GetType())).create(reader);
		assertTokenStreamContents(stream, new string[] {"さ"}); // old broken behavior
	  }

	  /// <summary>
	  /// Test that bogus arguments result in exception </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testBogusArguments() throws Exception
	  public virtual void testBogusArguments()
	  {
		try
		{
		  tokenizerFactory("UAX29URLEmail", "bogusArg", "bogusValue");
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("Unknown parameters"));
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testIllegalArguments() throws Exception
	 public virtual void testIllegalArguments()
	 {
		try
		{
		  tokenizerFactory("UAX29URLEmail", "maxTokenLength", "-1").create(new StringReader("hello"));
		  fail();
		}
		catch (System.ArgumentException expected)
		{
		  assertTrue(expected.Message.contains("maxTokenLength must be greater than zero"));
		}
	 }
	}

}