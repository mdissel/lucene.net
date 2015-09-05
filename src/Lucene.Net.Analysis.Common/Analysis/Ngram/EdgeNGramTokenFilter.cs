﻿using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using org.apache.lucene.analysis.reverse;
using org.apache.lucene.analysis.util;

namespace Lucene.Net.Analysis.Ngram
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
	/// Tokenizes the given token into n-grams of given size(s).
	/// <para>
	/// This <seealso cref="TokenFilter"/> create n-grams from the beginning edge or ending edge of a input token.
	/// </para>
	/// <para><a name="version"/>As of Lucene 4.4, this filter does not support
	/// <seealso cref="Side#BACK"/> (you can use <seealso cref="ReverseStringFilter"/> up-front and
	/// afterward to get the same behavior), handles supplementary characters
	/// correctly and does not update offsets anymore.
	/// </para>
	/// </summary>
	public sealed class EdgeNGramTokenFilter : TokenFilter
	{
	  public const Side DEFAULT_SIDE = Side.FRONT;
	  public const int DEFAULT_MAX_GRAM_SIZE = 1;
	  public const int DEFAULT_MIN_GRAM_SIZE = 1;

	    /// <summary>
	    /// Specifies which side of the input the n-gram should be generated from </summary>
	    public enum Side
	    {

	        /// <summary>
	        /// Get the n-gram from the front of the input </summary>
	        FRONT,

	        /// <summary>
	        /// Get the n-gram from the end of the input </summary>
	        [System.Obsolete] BACK,
	    }

	    // Get the appropriate Side from a string
		public static Side getSide(String sideName)
		{
//JAVA TO C# CONVERTER TODO TASK: The following line could not be converted:
		  if (FRONT.getLabel().equals(sideName))
		  {
			return FRONT;
		  }
//JAVA TO C# CONVERTER TODO TASK: The following line could not be converted:
		  if (BACK.getLabel().equals(sideName))
		  {
			return BACK;
		  }
		  return null;
		}

	  private readonly LuceneVersion version;
	  private readonly CharacterUtils charUtils;
	  private readonly int minGram;
	  private readonly int maxGram;
	  private Side side;
	  private char[] curTermBuffer;
	  private int curTermLength;
	  private int curCodePointCount;
	  private int curGramSize;
	  private int tokStart;
	  private int tokEnd; // only used if the length changed before this filter
	  private bool updateOffsets; // never if the length changed before this filter
	  private int savePosIncr;
	  private int savePosLen;

	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));
	  private readonly PositionIncrementAttribute posIncrAtt = addAttribute(typeof(PositionIncrementAttribute));
	  private readonly PositionLengthAttribute posLenAtt = addAttribute(typeof(PositionLengthAttribute));

	  /// <summary>
	  /// Creates EdgeNGramTokenFilter that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="input"> <seealso cref="TokenStream"/> holding the input to be tokenized </param>
	  /// <param name="side"> the <seealso cref="Side"/> from which to chop off an n-gram </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  [Obsolete]
	  public EdgeNGramTokenFilter(LuceneVersion version, TokenStream input, Side side, int minGram, int maxGram) : base(input)
	  {

		if (version == null)
		{
		  throw new System.ArgumentException("version must not be null");
		}

		if (version.OnOrAfter(LuceneVersion.LUCENE_44) && side == Side.BACK)
		{
		  throw new System.ArgumentException("Side.BACK is not supported anymore as of Lucene 4.4, use ReverseStringFilter up-front and afterward");
		}

		if (side == null)
		{
		  throw new System.ArgumentException("sideLabel must be either front or back");
		}

		if (minGram < 1)
		{
		  throw new System.ArgumentException("minGram must be greater than zero");
		}

		if (minGram > maxGram)
		{
		  throw new System.ArgumentException("minGram must not be greater than maxGram");
		}

		this.version = version;
		this.charUtils = version.onOrAfter(LuceneVersion.LUCENE_44) ? CharacterUtils.getInstance(version) : CharacterUtils.Java4Instance;
		this.minGram = minGram;
		this.maxGram = maxGram;
		this.side = side;
	  }

	  /// <summary>
	  /// Creates EdgeNGramTokenFilter that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="input"> <seealso cref="TokenStream"/> holding the input to be tokenized </param>
	  /// <param name="sideLabel"> the name of the <seealso cref="Side"/> from which to chop off an n-gram </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  [Obsolete]
	  public EdgeNGramTokenFilter(LuceneVersion version, TokenStream input, string sideLabel, int minGram, int maxGram) : this(version, input, Side.getSide(sideLabel), minGram, maxGram)
	  {
	  }

	  /// <summary>
	  /// Creates EdgeNGramTokenFilter that can generate n-grams in the sizes of the given range
	  /// </summary>
	  /// <param name="version"> the <a href="#version">Lucene match version</a> </param>
	  /// <param name="input"> <seealso cref="TokenStream"/> holding the input to be tokenized </param>
	  /// <param name="minGram"> the smallest n-gram to generate </param>
	  /// <param name="maxGram"> the largest n-gram to generate </param>
	  public EdgeNGramTokenFilter(LuceneVersion version, TokenStream input, int minGram, int maxGram) : this(version, input, Side.FRONT, minGram, maxGram)
	  {
	  }

	  public override bool IncrementToken()
	  {
		while (true)
		{
		  if (curTermBuffer == null)
		  {
			if (!input.IncrementToken())
			{
			  return false;
			}
			else
			{
			  curTermBuffer = termAtt.Buffer().Clone();
			  curTermLength = termAtt.Length();
			  curCodePointCount = charUtils.codePointCount(termAtt);
			  curGramSize = minGram;
			  tokStart = offsetAtt.StartOffset();
			  tokEnd = offsetAtt.EndOffset();
			  if (version.OnOrAfter(LuceneVersion.LUCENE_44))
			  {
				// Never update offsets
				updateOffsets = false;
			  }
			  else
			  {
				// if length by start + end offsets doesn't match the term text then assume
				// this is a synonym and don't adjust the offsets.
				updateOffsets = (tokStart + curTermLength) == tokEnd;
			  }
			  savePosIncr += posIncrAtt.PositionIncrement;
			  savePosLen = posLenAtt.PositionLength;
			}
		  }
		  if (curGramSize <= maxGram) // if we have hit the end of our n-gram size range, quit
		  {
			if (curGramSize <= curCodePointCount) // if the remaining input is too short, we can't generate any n-grams
			{
			  // grab gramSize chars from front or back
			  int start = side == Side.FRONT ? 0 : charUtils.offsetByCodePoints(curTermBuffer, 0, curTermLength, curTermLength, -curGramSize);
			  int end = charUtils.offsetByCodePoints(curTermBuffer, 0, curTermLength, start, curGramSize);
			  ClearAttributes();
			  if (updateOffsets)
			  {
				offsetAtt.SetOffset(tokStart + start, tokStart + end);
			  }
			  else
			  {
				offsetAtt.SetOffset(tokStart, tokEnd);
			  }
			  // first ngram gets increment, others don't
			  if (curGramSize == minGram)
			  {
				posIncrAtt.PositionIncrement = savePosIncr;
				savePosIncr = 0;
			  }
			  else
			  {
				posIncrAtt.PositionIncrement = 0;
			  }
			  posLenAtt.PositionLength = savePosLen;
			  termAtt.CopyBuffer(curTermBuffer, start, end - start);
			  curGramSize++;
			  return true;
			}
		  }
		  curTermBuffer = null;
		}
	  }

	  public override void Reset()
	  {
		base.Reset();
		curTermBuffer = null;
		savePosIncr = 0;
	  }
}
}