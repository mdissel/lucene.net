﻿using System;
using System.Collections.Generic;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using org.apache.lucene.analysis.miscellaneous;

namespace Lucene.Net.Analysis.Miscellaneous
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
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.miscellaneous.WordDelimiterFilter.*;

	/// <summary>
	/// Factory for <seealso cref="WordDelimiterFilter"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_wd" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.WordDelimiterFilterFactory" protected="protectedword.txt"
	///             preserveOriginal="0" splitOnNumerics="1" splitOnCaseChange="1"
	///             catenateWords="0" catenateNumbers="0" catenateAll="0"
	///             generateWordParts="1" generateNumberParts="1" stemEnglishPossessive="1"
	///             types="wdfftypes.txt" /&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class WordDelimiterFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  public const string PROTECTED_TOKENS = "protected";
	  public const string TYPES = "types";

	  private readonly string wordFiles;
	  private readonly string types;
	  private readonly int flags;
	  internal sbyte[] typeTable = null;
	  private CharArraySet protectedWords = null;

	  /// <summary>
	  /// Creates a new WordDelimiterFilterFactory </summary>
	  public WordDelimiterFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		assureMatchVersion();
		int flags = 0;
		if (getInt(args, "generateWordParts", 1) != 0)
		{
		  flags |= WordDelimiterFilter.GENERATE_WORD_PARTS;
		}
		if (getInt(args, "generateNumberParts", 1) != 0)
		{
		  flags |= WordDelimiterFilter.GENERATE_NUMBER_PARTS;
		}
		if (getInt(args, "catenateWords", 0) != 0)
		{
		  flags |= WordDelimiterFilter.CATENATE_WORDS;
		}
		if (getInt(args, "catenateNumbers", 0) != 0)
		{
		  flags |= WordDelimiterFilter.CATENATE_NUMBERS;
		}
		if (getInt(args, "catenateAll", 0) != 0)
		{
		  flags |= WordDelimiterFilter.CATENATE_ALL;
		}
		if (getInt(args, "splitOnCaseChange", 1) != 0)
		{
		  flags |= WordDelimiterFilter.SPLIT_ON_CASE_CHANGE;
		}
		if (getInt(args, "splitOnNumerics", 1) != 0)
		{
		  flags |= WordDelimiterFilter.SPLIT_ON_NUMERICS;
		}
		if (getInt(args, "preserveOriginal", 0) != 0)
		{
		  flags |= WordDelimiterFilter.PRESERVE_ORIGINAL;
		}
		if (getInt(args, "stemEnglishPossessive", 1) != 0)
		{
		  flags |= WordDelimiterFilter.STEM_ENGLISH_POSSESSIVE;
		}
		wordFiles = get(args, PROTECTED_TOKENS);
		types = get(args, TYPES);
		this.flags = flags;
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public virtual void Inform(ResourceLoader loader)
	  {
		if (wordFiles != null)
		{
		  protectedWords = GetWordSet(loader, wordFiles, false);
		}
		if (types != null)
		{
		  IList<string> files = splitFileNames(types);
		  IList<string> wlist = new List<string>();
		  foreach (string file in files)
		  {
			IList<string> lines = getLines(loader, file.Trim());
			wlist.AddRange(lines);
		  }
		  typeTable = parseTypes(wlist);
		}
	  }

	  public override TokenStream Create(TokenStream input)
	  {
		if (luceneMatchVersion.OnOrAfter(LuceneVersion.LUCENE_48))
		{
		  return new WordDelimiterFilter(luceneMatchVersion, input, typeTable == null ? WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE : typeTable, flags, protectedWords);
		}
		else
		{
		  return new Lucene47WordDelimiterFilter(input, typeTable ?? WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE, flags, protectedWords);
		}
	  }

	  // source => type
	  private static Pattern typePattern = Pattern.compile("(.*)\\s*=>\\s*(.*)\\s*$");

	  // parses a list of MappingCharFilter style rules into a custom byte[] type table
	  private sbyte[] parseTypes(IList<string> rules)
	  {
		SortedMap<char?, sbyte?> typeMap = new SortedDictionary<char?, sbyte?>();
		foreach (string rule in rules)
		{
		  Matcher m = typePattern.matcher(rule);
		  if (!m.find())
		  {
			throw new System.ArgumentException("Invalid Mapping Rule : [" + rule + "]");
		  }
		  string lhs = parseString(m.group(1).Trim());
		  sbyte? rhs = parseType(m.group(2).Trim());
		  if (lhs.Length != 1)
		  {
			throw new System.ArgumentException("Invalid Mapping Rule : [" + rule + "]. Only a single character is allowed.");
		  }
		  if (rhs == null)
		  {
			throw new System.ArgumentException("Invalid Mapping Rule : [" + rule + "]. Illegal type.");
		  }
		  typeMap.put(lhs[0], rhs);
		}

		// ensure the table is always at least as big as DEFAULT_WORD_DELIM_TABLE for performance
		sbyte[] types = new sbyte[Math.Max(typeMap.LastKey() + 1, WordDelimiterIterator.DEFAULT_WORD_DELIM_TABLE.Length)];
		for (int i = 0; i < types.Length; i++)
		{
		  types[i] = WordDelimiterIterator.getType(i);
		}
		foreach (KeyValuePair<char?, sbyte?> mapping in typeMap.EntrySet())
		{
		  types[mapping.Key] = mapping.Value;
		}
		return types;
	  }

	  private sbyte? ParseType(string s)
	  {
		if (s.Equals("LOWER"))
		{
		  return WordDelimiterFilter.LOWER;
		}
		else if (s.Equals("UPPER"))
		{
		  return WordDelimiterFilter.UPPER;
		}
		else if (s.Equals("ALPHA"))
		{
		  return WordDelimiterFilter.ALPHA;
		}
		else if (s.Equals("DIGIT"))
		{
		  return WordDelimiterFilter.DIGIT;
		}
		else if (s.Equals("ALPHANUM"))
		{
		  return WordDelimiterFilter.ALPHANUM;
		}
		else if (s.Equals("SUBWORD_DELIM"))
		{
		  return WordDelimiterFilter.SUBWORD_DELIM;
		}
		else
		{
		  return null;
		}
	  }

	  internal char[] @out = new char[256];

	  private string parseString(string s)
	  {
		int readPos = 0;
		int len = s.Length;
		int writePos = 0;
		while (readPos < len)
		{
		  char c = s[readPos++];
		  if (c == '\\')
		  {
			if (readPos >= len)
			{
			  throw new System.ArgumentException("Invalid escaped char in [" + s + "]");
			}
			c = s[readPos++];
			switch (c)
			{
			  case '\\' :
				  c = '\\';
				  break;
			  case 'n' :
				  c = '\n';
				  break;
			  case 't' :
				  c = '\t';
				  break;
			  case 'r' :
				  c = '\r';
				  break;
			  case 'b' :
				  c = '\b';
				  break;
			  case 'f' :
				  c = '\f';
				  break;
			  case 'u' :
				if (readPos + 3 >= len)
				{
				  throw new System.ArgumentException("Invalid escaped char in [" + s + "]");
				}
				c = (char)int.Parse(s.Substring(readPos, 4), 16);
				readPos += 4;
				break;
			}
		  }
		  @out[writePos++] = c;
		}
		return new string(@out, 0, writePos);
	  }
	}
}