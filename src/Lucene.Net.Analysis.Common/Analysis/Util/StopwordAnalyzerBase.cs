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
using System;
using System.IO;
using Lucene.Net.Util;
using Reader = System.IO.TextReader;
using Version = Lucene.Net.Util.LuceneVersion;

namespace Lucene.Net.Analysis.Util
{
    /// <summary>
	/// Base class for Analyzers that need to make use of stopword sets. 
	/// 
	/// </summary>
	public abstract class StopwordAnalyzerBase : Analyzer
	{

	  /// <summary>
	  /// An immutable stopword set
	  /// </summary>
	  protected internal readonly CharArraySet stopwords;

	  protected internal readonly Version matchVersion;

	  /// <summary>
	  /// Returns the analyzer's stopword set or an empty set if the analyzer has no
	  /// stopwords
	  /// </summary>
	  /// <returns> the analyzer's stopword set or an empty set if the analyzer has no
	  ///         stopwords </returns>
	  public virtual CharArraySet StopwordSet
	  {
		  get
		  {
			return stopwords;
		  }
	  }

	  /// <summary>
	  /// Creates a new instance initialized with the given stopword set
	  /// </summary>
	  /// <param name="version">
	  ///          the Lucene version for cross version compatibility </param>
	  /// <param name="stopwords">
	  ///          the analyzer's stopword set </param>
	  protected internal StopwordAnalyzerBase(Version version, CharArraySet stopwords)
	  {
		matchVersion = version;
		// analyzers should use char array set for stopwords!
		this.stopwords = stopwords == null ? CharArraySet.EMPTY_SET : CharArraySet.UnmodifiableSet(CharArraySet.Copy(version, stopwords));
	  }

	  /// <summary>
	  /// Creates a new Analyzer with an empty stopword set
	  /// </summary>
	  /// <param name="version">
	  ///          the Lucene version for cross version compatibility </param>
	  protected internal StopwordAnalyzerBase(Version version) : this(version, null)
	  {
	  }

	  /// <summary>
	  /// Creates a CharArraySet from a file resource associated with a class. (See
	  /// <seealso cref="Class#getResourceAsStream(String)"/>).
	  /// </summary>
	  /// <param name="ignoreCase">
	  ///          <code>true</code> if the set should ignore the case of the
	  ///          stopwords, otherwise <code>false</code> </param>
	  /// <param name="aClass">
	  ///          a class that is associated with the given stopwordResource </param>
	  /// <param name="resource">
	  ///          name of the resource file associated with the given class </param>
	  /// <param name="comment">
	  ///          comment string to ignore in the stopword file </param>
	  /// <returns> a CharArraySet containing the distinct stopwords from the given
	  ///         file </returns>
	  /// <exception cref="IOException">
	  ///           if loading the stopwords throws an <seealso cref="IOException"/> </exception>
	  protected internal static CharArraySet LoadStopwordSet(bool ignoreCase, Type aClass, string resource, string comment)
	  {
		TextReader reader = null;
		try
		{
		  reader = IOUtils.GetDecodingReader(aClass.GetResourceAsStream(resource), StandardCharsets.UTF_8);
		  return WordlistLoader.GetWordSet(reader, comment, new CharArraySet(Version.LUCENE_CURRENT, 16, ignoreCase));
		}
		finally
		{
		  IOUtils.Close(reader);
		}
	  }

	  /// <summary>
	  /// Creates a CharArraySet from a file.
	  /// </summary>
	  /// <param name="stopwords">
	  ///          the stopwords file to load
	  /// </param>
	  /// <param name="matchVersion">
	  ///          the Lucene version for cross version compatibility </param>
	  /// <returns> a CharArraySet containing the distinct stopwords from the given
	  ///         file </returns>
	  /// <exception cref="IOException">
	  ///           if loading the stopwords throws an <seealso cref="IOException"/> </exception>
	  protected internal static CharArraySet LoadStopwordSet(File stopwords, Version matchVersion)
	  {
		Reader reader = null;
		try
		{
		  reader = IOUtils.GetDecodingReader(stopwords, StandardCharsets.UTF_8);
		  return WordlistLoader.GetWordSet(reader, matchVersion);
		}
		finally
		{
		  IOUtils.Close(reader);
		}
	  }

	  /// <summary>
	  /// Creates a CharArraySet from a file.
	  /// </summary>
	  /// <param name="stopwords">
	  ///          the stopwords reader to load
	  /// </param>
	  /// <param name="matchVersion">
	  ///          the Lucene version for cross version compatibility </param>
	  /// <returns> a CharArraySet containing the distinct stopwords from the given
	  ///         reader </returns>
	  /// <exception cref="IOException">
	  ///           if loading the stopwords throws an <seealso cref="IOException"/> </exception>
	  protected internal static CharArraySet loadStopwordSet(Reader stopwords, Version matchVersion)
	  {
		try
		{
		  return WordlistLoader.GetWordSet(stopwords, matchVersion);
		}
		finally
		{
		  IOUtils.Close(stopwords);
		}
	  }
	}

}