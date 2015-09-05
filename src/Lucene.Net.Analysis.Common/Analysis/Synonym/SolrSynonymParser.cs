﻿using System;
using System.Collections.Generic;
using System.Text;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.Synonym
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
	/// Parser for the Solr synonyms format.
	/// <ol>
	///   <li> Blank lines and lines starting with '#' are comments.
	///   <li> Explicit mappings match any token sequence on the LHS of "=>"
	///        and replace with all alternatives on the RHS.  These types of mappings
	///        ignore the expand parameter in the constructor.
	///        Example:
	///        <blockquote>i-pod, i pod => ipod</blockquote>
	///   <li> Equivalent synonyms may be separated with commas and give
	///        no explicit mapping.  In this case the mapping behavior will
	///        be taken from the expand parameter in the constructor.  This allows
	///        the same synonym file to be used in different synonym handling strategies.
	///        Example:
	///        <blockquote>ipod, i-pod, i pod</blockquote>
	/// 
	///   <li> Multiple synonym mapping entries are merged.
	///        Example:
	///        <blockquote>
	///         foo => foo bar<br>
	///         foo => baz<br><br>
	///         is equivalent to<br><br>
	///         foo => foo bar, baz
	///        </blockquote>
	///  </ol>
	/// @lucene.experimental
	/// </summary>
	public class SolrSynonymParser : SynonymMap.Parser
	{
	  private readonly bool expand;

	  public SolrSynonymParser(bool dedup, bool expand, Analyzer analyzer) : base(dedup, analyzer)
	  {
		this.expand = expand;
	  }

	  public override void Parse(Reader @in)
	  {
		LineNumberReader br = new LineNumberReader(@in);
		try
		{
		  addInternal(br);
		}
		catch (System.ArgumentException e)
		{
		  ParseException ex = new ParseException("Invalid synonym rule at line " + br.LineNumber, 0);
		  ex.initCause(e);
		  throw ex;
		}
		finally
		{
		  br.close();
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void addInternal(java.io.BufferedReader in) throws java.io.IOException
	  private void addInternal(BufferedReader @in)
	  {
		string line = null;
		while ((line = @in.readLine()) != null)
		{
		  if (line.Length == 0 || line[0] == '#')
		  {
			continue; // ignore empty lines and comments
		  }

		  CharsRef[] inputs;
		  CharsRef[] outputs;

		  // TODO: we could process this more efficiently.
		  string[] sides = Split(line, "=>");
		  if (sides.Length > 1) // explicit mapping
		  {
			if (sides.Length != 2)
			{
			  throw new System.ArgumentException("more than one explicit mapping specified on the same line");
			}
			string[] inputStrings = Split(sides[0], ",");
			inputs = new CharsRef[inputStrings.Length];
			for (int i = 0; i < inputs.Length; i++)
			{
			  inputs[i] = Analyze(unescape(inputStrings[i]).Trim(), new CharsRef());
			}

			string[] outputStrings = Split(sides[1], ",");
			outputs = new CharsRef[outputStrings.Length];
			for (int i = 0; i < outputs.Length; i++)
			{
			  outputs[i] = Analyze(unescape(outputStrings[i]).Trim(), new CharsRef());
			}
		  }
		  else
		  {
			string[] inputStrings = Split(line, ",");
			inputs = new CharsRef[inputStrings.Length];
			for (int i = 0; i < inputs.Length; i++)
			{
			  inputs[i] = Analyze(unescape(inputStrings[i]).Trim(), new CharsRef());
			}
			if (expand)
			{
			  outputs = inputs;
			}
			else
			{
			  outputs = new CharsRef[1];
			  outputs[0] = inputs[0];
			}
		  }

		  // currently we include the term itself in the map,
		  // and use includeOrig = false always.
		  // this is how the existing filter does it, but its actually a bug,
		  // especially if combined with ignoreCase = true
		  for (int i = 0; i < inputs.Length; i++)
		  {
			for (int j = 0; j < outputs.Length; j++)
			{
			  Add(inputs[i], outputs[j], false);
			}
		  }
		}
	  }

	  private static string[] Split(string s, string separator)
	  {
		List<string> list = new List<string>(2);
		StringBuilder sb = new StringBuilder();
		int pos = 0, end = s.Length;
		while (pos < end)
		{
		  if (s.StartsWith(separator,pos))
		  {
			if (sb.Length > 0)
			{
			  list.Add(sb.ToString());
			  sb = new StringBuilder();
			}
			pos += separator.Length;
			continue;
		  }

		  char ch = s[pos++];
		  if (ch == '\\')
		  {
			sb.Append(ch);
			if (pos >= end) // ERROR, or let it go?
			{
				break;
			}
			ch = s[pos++];
		  }

		  sb.Append(ch);
		}

		if (sb.Length > 0)
		{
		  list.Add(sb.ToString());
		}

		return list.ToArray();
	  }

	  private string unescape(string s)
	  {
		if (s.IndexOf("\\", StringComparison.Ordinal) >= 0)
		{
		  StringBuilder sb = new StringBuilder();
		  for (int i = 0; i < s.Length; i++)
		  {
			char ch = s[i];
			if (ch == '\\' && i < s.Length - 1)
			{
			  sb.Append(s[++i]);
			}
			else
			{
			  sb.Append(ch);
			}
		  }
		  return sb.ToString();
		}
		return s;
	  }
	}

}