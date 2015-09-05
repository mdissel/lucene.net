﻿namespace Lucene.Net.Analysis.Miscellaneous
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
	/// A BreakIterator-like API for iterating over subwords in text, according to WordDelimiterFilter rules.
	/// @lucene.internal
	/// </summary>
	public sealed class WordDelimiterIterator
	{

	  /// <summary>
	  /// Indicates the end of iteration </summary>
	  public const int DONE = -1;

	  public static readonly sbyte[] DEFAULT_WORD_DELIM_TABLE;

	  internal char[] text;
	  internal int length;

	  /// <summary>
	  /// start position of text, excluding leading delimiters </summary>
	  internal int startBounds;
	  /// <summary>
	  /// end position of text, excluding trailing delimiters </summary>
	  internal int endBounds;

	  /// <summary>
	  /// Beginning of subword </summary>
	  internal int current;
	  /// <summary>
	  /// End of subword </summary>
	  internal int end;

	  /* does this string end with a possessive such as 's */
	  private bool hasFinalPossessive = false;

	  /// <summary>
	  /// If false, causes case changes to be ignored (subwords will only be generated
	  /// given SUBWORD_DELIM tokens). (Defaults to true)
	  /// </summary>
	  internal readonly bool splitOnCaseChange;

	  /// <summary>
	  /// If false, causes numeric changes to be ignored (subwords will only be generated
	  /// given SUBWORD_DELIM tokens). (Defaults to true)
	  /// </summary>
	  internal readonly bool splitOnNumerics;

	  /// <summary>
	  /// If true, causes trailing "'s" to be removed for each subword. (Defaults to true)
	  /// <p/>
	  /// "O'Neil's" => "O", "Neil"
	  /// </summary>
	  internal readonly bool stemEnglishPossessive;

	  private readonly sbyte[] charTypeTable;

	  /// <summary>
	  /// if true, need to skip over a possessive found in the last call to next() </summary>
	  private bool skipPossessive = false;

	  // TODO: should there be a WORD_DELIM category for chars that only separate words (no catenation of subwords will be
	  // done if separated by these chars?) "," would be an obvious candidate...
	  static WordDelimiterIterator()
	  {
		var tab = new sbyte[256];
		for (int i = 0; i < 256; i++)
		{
		  sbyte code = 0;
		  if (char.IsLower((char)i))
		  {
			code |= (sbyte)WordDelimiterFilter.LOWER;
		  }
		  else if (char.IsUpper((char)i))
		  {
			code |= (sbyte)WordDelimiterFilter.UPPER;
		  }
		  else if (char.IsDigit((char)i))
		  {
			code |= (sbyte)WordDelimiterFilter.DIGIT;
		  }
		  if (code == 0)
		  {
			code = WordDelimiterFilter.SUBWORD_DELIM;
		  }
		  tab[i] = code;
		}
		DEFAULT_WORD_DELIM_TABLE = tab;
	  }

	  /// <summary>
	  /// Create a new WordDelimiterIterator operating with the supplied rules.
	  /// </summary>
	  /// <param name="charTypeTable"> table containing character types </param>
	  /// <param name="splitOnCaseChange"> if true, causes "PowerShot" to be two tokens; ("Power-Shot" remains two parts regards) </param>
	  /// <param name="splitOnNumerics"> if true, causes "j2se" to be three tokens; "j" "2" "se" </param>
	  /// <param name="stemEnglishPossessive"> if true, causes trailing "'s" to be removed for each subword: "O'Neil's" => "O", "Neil" </param>
	  internal WordDelimiterIterator(sbyte[] charTypeTable, bool splitOnCaseChange, bool splitOnNumerics, bool stemEnglishPossessive)
	  {
		this.charTypeTable = charTypeTable;
		this.splitOnCaseChange = splitOnCaseChange;
		this.splitOnNumerics = splitOnNumerics;
		this.stemEnglishPossessive = stemEnglishPossessive;
	  }

	  /// <summary>
	  /// Advance to the next subword in the string.
	  /// </summary>
	  /// <returns> index of the next subword, or <seealso cref="#DONE"/> if all subwords have been returned </returns>
	  internal int next()
	  {
		current = end;
		if (current == DONE)
		{
		  return DONE;
		}

		if (skipPossessive)
		{
		  current += 2;
		  skipPossessive = false;
		}

		int lastType = 0;

		while (current < endBounds && (WordDelimiterFilter.isSubwordDelim(lastType = charType(text[current]))))
		{
		  current++;
		}

		if (current >= endBounds)
		{
		  return end = DONE;
		}

		for (end = current + 1; end < endBounds; end++)
		{
		  int type_Renamed = charType(text[end]);
		  if (isBreak(lastType, type_Renamed))
		  {
			break;
		  }
		  lastType = type_Renamed;
		}

		if (end < endBounds - 1 && endsWithPossessive(end + 2))
		{
		  skipPossessive = true;
		}

		return end;
	  }


	  /// <summary>
	  /// Return the type of the current subword.
	  /// This currently uses the type of the first character in the subword.
	  /// </summary>
	  /// <returns> type of the current word </returns>
	  internal int type()
	  {
		if (end == DONE)
		{
		  return 0;
		}

		int type_Renamed = charType(text[current]);
		switch (type_Renamed)
		{
		  // return ALPHA word type for both lower and upper
		  case WordDelimiterFilter.LOWER:
		  case WordDelimiterFilter.UPPER:
			return WordDelimiterFilter.ALPHA;
		  default:
			return type_Renamed;
		}
	  }

	  /// <summary>
	  /// Reset the text to a new value, and reset all state
	  /// </summary>
	  /// <param name="text"> New text </param>
	  /// <param name="length"> length of the text </param>
	  internal void setText(char[] text, int length)
	  {
		this.text = text;
		this.length = this.endBounds = length;
		current = startBounds = end = 0;
		skipPossessive = hasFinalPossessive = false;
		setBounds();
	  }

	  // ================================================= Helper Methods ================================================

	  /// <summary>
	  /// Determines whether the transition from lastType to type indicates a break
	  /// </summary>
	  /// <param name="lastType"> Last subword type </param>
	  /// <param name="type"> Current subword type </param>
	  /// <returns> {@code true} if the transition indicates a break, {@code false} otherwise </returns>
	  private bool isBreak(int lastType, int type)
	  {
		if ((type & lastType) != 0)
		{
		  return false;
		}

		if (!splitOnCaseChange && WordDelimiterFilter.isAlpha(lastType) && WordDelimiterFilter.isAlpha(type))
		{
		  // ALPHA->ALPHA: always ignore if case isn't considered.
		  return false;
		}
		else if (WordDelimiterFilter.isUpper(lastType) && WordDelimiterFilter.isAlpha(type))
		{
		  // UPPER->letter: Don't split
		  return false;
		}
		else if (!splitOnNumerics && ((WordDelimiterFilter.isAlpha(lastType) && WordDelimiterFilter.isDigit(type)) || (WordDelimiterFilter.isDigit(lastType) && WordDelimiterFilter.isAlpha(type))))
		{
		  // ALPHA->NUMERIC, NUMERIC->ALPHA :Don't split
		  return false;
		}

		return true;
	  }

	  /// <summary>
	  /// Determines if the current word contains only one subword.  Note, it could be potentially surrounded by delimiters
	  /// </summary>
	  /// <returns> {@code true} if the current word contains only one subword, {@code false} otherwise </returns>
	  internal bool SingleWord
	  {
		  get
		  {
			if (hasFinalPossessive)
			{
			  return current == startBounds && end == endBounds - 2;
			}
			else
			{
			  return current == startBounds && end == endBounds;
			}
		  }
	  }

	  /// <summary>
	  /// Set the internal word bounds (remove leading and trailing delimiters). Note, if a possessive is found, don't remove
	  /// it yet, simply note it.
	  /// </summary>
	  private void setBounds()
	  {
		while (startBounds < length && (WordDelimiterFilter.isSubwordDelim(charType(text[startBounds]))))
		{
		  startBounds++;
		}

		while (endBounds > startBounds && (WordDelimiterFilter.isSubwordDelim(charType(text[endBounds - 1]))))
		{
		  endBounds--;
		}
		if (endsWithPossessive(endBounds))
		{
		  hasFinalPossessive = true;
		}
		current = startBounds;
	  }

	  /// <summary>
	  /// Determines if the text at the given position indicates an English possessive which should be removed
	  /// </summary>
	  /// <param name="pos"> Position in the text to check if it indicates an English possessive </param>
	  /// <returns> {@code true} if the text at the position indicates an English posessive, {@code false} otherwise </returns>
	  private bool endsWithPossessive(int pos)
	  {
		return (stemEnglishPossessive && pos > 2 && text[pos - 2] == '\'' && (text[pos - 1] == 's' || text[pos - 1] == 'S') && WordDelimiterFilter.isAlpha(charType(text[pos - 3])) && (pos == endBounds || WordDelimiterFilter.isSubwordDelim(charType(text[pos]))));
	  }

	  /// <summary>
	  /// Determines the type of the given character
	  /// </summary>
	  /// <param name="ch"> Character whose type is to be determined </param>
	  /// <returns> Type of the character </returns>
	  private int charType(int ch)
	  {
		if (ch < charTypeTable.Length)
		{
		  return charTypeTable[ch];
		}
		return getType(ch);
	  }

	  /// <summary>
	  /// Computes the type of the given character
	  /// </summary>
	  /// <param name="ch"> Character whose type is to be determined </param>
	  /// <returns> Type of the character </returns>
	  public static sbyte getType(int ch)
	  {
		switch (char.getType(ch))
		{
		  case char.UPPERCASE_LETTER:
			  return WordDelimiterFilter.UPPER;
		  case char.LOWERCASE_LETTER:
			  return WordDelimiterFilter.LOWER;

		  case char.TITLECASE_LETTER:
		  case char.MODIFIER_LETTER:
		  case char.OTHER_LETTER:
		  case char.NON_SPACING_MARK:
		  case char.ENCLOSING_MARK: // depends what it encloses?
		  case char.COMBINING_SPACING_MARK:
			return WordDelimiterFilter.ALPHA;

		  case char.DECIMAL_DIGIT_NUMBER:
		  case char.LETTER_NUMBER:
		  case char.OTHER_NUMBER:
			return WordDelimiterFilter.DIGIT;

		  // case Character.SPACE_SEPARATOR:
		  // case Character.LINE_SEPARATOR:
		  // case Character.PARAGRAPH_SEPARATOR:
		  // case Character.CONTROL:
		  // case Character.FORMAT:
		  // case Character.PRIVATE_USE:

		  case char.SURROGATE: // prevent splitting
			return WordDelimiterFilter.ALPHA | WordDelimiterFilter.DIGIT;

		  // case Character.DASH_PUNCTUATION:
		  // case Character.START_PUNCTUATION:
		  // case Character.END_PUNCTUATION:
		  // case Character.CONNECTOR_PUNCTUATION:
		  // case Character.OTHER_PUNCTUATION:
		  // case Character.MATH_SYMBOL:
		  // case Character.CURRENCY_SYMBOL:
		  // case Character.MODIFIER_SYMBOL:
		  // case Character.OTHER_SYMBOL:
		  // case Character.INITIAL_QUOTE_PUNCTUATION:
		  // case Character.FINAL_QUOTE_PUNCTUATION:

		  default:
			  return WordDelimiterFilter.SUBWORD_DELIM;
		}
	  }
	}
}