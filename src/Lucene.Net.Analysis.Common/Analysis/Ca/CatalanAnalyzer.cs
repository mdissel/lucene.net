﻿using System;

namespace org.apache.lucene.analysis.ca
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


	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using StopFilter = org.apache.lucene.analysis.core.StopFilter;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using SnowballFilter = org.apache.lucene.analysis.snowball.SnowballFilter;
	using StandardFilter = org.apache.lucene.analysis.standard.StandardFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using ElisionFilter = org.apache.lucene.analysis.util.ElisionFilter;
	using StopwordAnalyzerBase = org.apache.lucene.analysis.util.StopwordAnalyzerBase;
	using Version = org.apache.lucene.util.Version;
	using CatalanStemmer = org.tartarus.snowball.ext.CatalanStemmer;

	/// <summary>
	/// <seealso cref="Analyzer"/> for Catalan.
	/// <para>
	/// <a name="version"/>
	/// </para>
	/// <para>You must specify the required <seealso cref="Version"/>
	/// compatibility when creating CatalanAnalyzer:
	/// <ul>
	///   <li> As of 3.6, ElisionFilter with a set of Catalan 
	///        contractions is used by default.
	/// </ul>
	/// </para>
	/// </summary>
	public sealed class CatalanAnalyzer : StopwordAnalyzerBase
	{
	  private readonly CharArraySet stemExclusionSet;

	  /// <summary>
	  /// File containing default Catalan stopwords. </summary>
	  public const string DEFAULT_STOPWORD_FILE = "stopwords.txt";

	  private static readonly CharArraySet DEFAULT_ARTICLES = CharArraySet.unmodifiableSet(new CharArraySet(Version.LUCENE_CURRENT, Arrays.asList("d", "l", "m", "n", "s", "t"), true));

	  /// <summary>
	  /// Returns an unmodifiable instance of the default stop words set. </summary>
	  /// <returns> default stop words set. </returns>
	  public static CharArraySet DefaultStopSet
	  {
		  get
		  {
			return DefaultSetHolder.DEFAULT_STOP_SET;
		  }
	  }

	  /// <summary>
	  /// Atomically loads the DEFAULT_STOP_SET in a lazy fashion once the outer class 
	  /// accesses the static final set the first time.;
	  /// </summary>
	  private class DefaultSetHolder
	  {
		internal static readonly CharArraySet DEFAULT_STOP_SET;

		static DefaultSetHolder()
		{
		  try
		  {
			DEFAULT_STOP_SET = loadStopwordSet(false, typeof(CatalanAnalyzer), DEFAULT_STOPWORD_FILE, "#");
		  }
		  catch (IOException)
		  {
			// default set should always be present as it is part of the
			// distribution (JAR)
			throw new Exception("Unable to load default stopword set");
		  }
		}
	  }

	  /// <summary>
	  /// Builds an analyzer with the default stop words: <seealso cref="#DEFAULT_STOPWORD_FILE"/>.
	  /// </summary>
	  public CatalanAnalyzer(Version matchVersion) : this(matchVersion, DefaultSetHolder.DEFAULT_STOP_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words.
	  /// </summary>
	  /// <param name="matchVersion"> lucene compatibility version </param>
	  /// <param name="stopwords"> a stopword set </param>
	  public CatalanAnalyzer(Version matchVersion, CharArraySet stopwords) : this(matchVersion, stopwords, CharArraySet.EMPTY_SET)
	  {
	  }

	  /// <summary>
	  /// Builds an analyzer with the given stop words. If a non-empty stem exclusion set is
	  /// provided this analyzer will add a <seealso cref="SetKeywordMarkerFilter"/> before
	  /// stemming.
	  /// </summary>
	  /// <param name="matchVersion"> lucene compatibility version </param>
	  /// <param name="stopwords"> a stopword set </param>
	  /// <param name="stemExclusionSet"> a set of terms not to be stemmed </param>
	  public CatalanAnalyzer(Version matchVersion, CharArraySet stopwords, CharArraySet stemExclusionSet) : base(matchVersion, stopwords)
	  {
		this.stemExclusionSet = CharArraySet.unmodifiableSet(CharArraySet.copy(matchVersion, stemExclusionSet));
	  }

	  /// <summary>
	  /// Creates a
	  /// <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  /// which tokenizes all the text in the provided <seealso cref="Reader"/>.
	  /// </summary>
	  /// <returns> A
	  ///         <seealso cref="org.apache.lucene.analysis.Analyzer.TokenStreamComponents"/>
	  ///         built from an <seealso cref="StandardTokenizer"/> filtered with
	  ///         <seealso cref="StandardFilter"/>, <seealso cref="ElisionFilter"/>, <seealso cref="LowerCaseFilter"/>, 
	  ///         <seealso cref="StopFilter"/>, <seealso cref="SetKeywordMarkerFilter"/> if a stem exclusion set is
	  ///         provided and <seealso cref="SnowballFilter"/>. </returns>
	  protected internal override TokenStreamComponents createComponents(string fieldName, Reader reader)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.Tokenizer source = new org.apache.lucene.analysis.standard.StandardTokenizer(matchVersion, reader);
		Tokenizer source = new StandardTokenizer(matchVersion, reader);
		TokenStream result = new StandardFilter(matchVersion, source);
		if (matchVersion.onOrAfter(Version.LUCENE_36))
		{
		  result = new ElisionFilter(result, DEFAULT_ARTICLES);
		}
		result = new LowerCaseFilter(matchVersion, result);
		result = new StopFilter(matchVersion, result, stopwords);
		if (!stemExclusionSet.Empty)
		{
		  result = new SetKeywordMarkerFilter(result, stemExclusionSet);
		}
		result = new SnowballFilter(result, new CatalanStemmer());
		return new TokenStreamComponents(source, result);
	  }
	}

}