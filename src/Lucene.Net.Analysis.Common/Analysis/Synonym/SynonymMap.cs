﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
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
	/// A map of synonyms, keys and values are phrases.
	/// @lucene.experimental
	/// </summary>
	public class SynonymMap
	{
	  /// <summary>
	  /// for multiword support, you must separate words with this separator </summary>
	  public const char WORD_SEPARATOR = (char)0;
	  /// <summary>
	  /// map&lt;input word, list&lt;ord&gt;&gt; </summary>
	  public readonly FST<BytesRef> fst;
	  /// <summary>
	  /// map&lt;ord, outputword&gt; </summary>
	  public readonly BytesRefHash words;
	  /// <summary>
	  /// maxHorizontalContext: maximum context we need on the tokenstream </summary>
	  public readonly int maxHorizontalContext;

	  public SynonymMap(FST<BytesRef> fst, BytesRefHash words, int maxHorizontalContext)
	  {
		this.fst = fst;
		this.words = words;
		this.maxHorizontalContext = maxHorizontalContext;
	  }

	  /// <summary>
	  /// Builds an FSTSynonymMap.
	  /// <para>
	  /// Call add() until you have added all the mappings, then call build() to get an FSTSynonymMap
	  /// @lucene.experimental
	  /// </para>
	  /// </summary>
	  public class Builder
	  {
		internal readonly Dictionary<CharsRef, MapEntry> workingSet = new Dictionary<CharsRef, MapEntry>();
		internal readonly BytesRefHash words = new BytesRefHash();
		internal readonly BytesRef utf8Scratch = new BytesRef(8);
		internal int maxHorizontalContext;
		internal readonly bool dedup;

		/// <summary>
		/// If dedup is true then identical rules (same input,
		///  same output) will be added only once. 
		/// </summary>
		public Builder(bool dedup)
		{
		  this.dedup = dedup;
		}

		internal class MapEntry
		{
		  internal bool includeOrig;
		  // we could sort for better sharing ultimately, but it could confuse people
		  internal List<int?> ords = new List<int?>();
		}

		/// <summary>
		/// Sugar: just joins the provided terms with {@link
		///  SynonymMap#WORD_SEPARATOR}.  reuse and its chars
		///  must not be null. 
		/// </summary>
		public static CharsRef join(string[] words, CharsRef reuse)
		{
		  int upto = 0;
		  char[] buffer = reuse.Chars;
		  foreach (string word in words)
		  {
			int wordLen = word.Length;
			int needed = (0 == upto ? wordLen : 1 + upto + wordLen); // Add 1 for WORD_SEPARATOR
			if (needed > buffer.Length)
			{
			  reuse.Grow(needed);
			  buffer = reuse.Chars;
			}
			if (upto > 0)
			{
			  buffer[upto++] = SynonymMap.WORD_SEPARATOR;
			}

			word.CopyTo(0, buffer, upto, wordLen - 0);
			upto += wordLen;
		  }
		  reuse.Length = upto;
		  return reuse;
		}



		/// <summary>
		/// only used for asserting!
		/// </summary>
		internal virtual bool HasHoles(CharsRef chars)
		{
		  int end = chars.Offset + chars.Length;
		  for (int idx = chars.Offset + 1;idx < end;idx++)
		  {
			if (chars.Chars[idx] == SynonymMap.WORD_SEPARATOR && chars.Chars[idx - 1] == SynonymMap.WORD_SEPARATOR)
			{
			  return true;
			}
		  }
		  if (chars.Chars[chars.Offset] == '\u0000')
		  {
			return true;
		  }
		  if (chars.Chars[chars.Offset + chars.Length - 1] == '\u0000')
		  {
			return true;
		  }

		  return false;
		}

		// NOTE: while it's tempting to make this public, since
		// caller's parser likely knows the
		// numInput/numOutputWords, sneaky exceptions, much later
		// on, will result if these values are wrong; so we always
		// recompute ourselves to be safe:
		internal virtual void Add(CharsRef input, int numInputWords, CharsRef output, int numOutputWords, bool includeOrig)
		{
		  // first convert to UTF-8
		  if (numInputWords <= 0)
		  {
			throw new System.ArgumentException("numInputWords must be > 0 (got " + numInputWords + ")");
		  }
		  if (input.Length <= 0)
		  {
			throw new System.ArgumentException("input.length must be > 0 (got " + input.Length + ")");
		  }
		  if (numOutputWords <= 0)
		  {
			throw new System.ArgumentException("numOutputWords must be > 0 (got " + numOutputWords + ")");
		  }
		  if (output.Length <= 0)
		  {
			throw new System.ArgumentException("output.length must be > 0 (got " + output.Length + ")");
		  }

		  Debug.Assert(!HasHoles(input), "input has holes: " + input);
		  Debug.Assert(!HasHoles(output), "output has holes: " + output);

		  //System.out.println("fmap.add input=" + input + " numInputWords=" + numInputWords + " output=" + output + " numOutputWords=" + numOutputWords);
		  UnicodeUtil.UTF16toUTF8(output.Chars, output.Offset, output.Length, utf8Scratch);
		  // lookup in hash
		  int ord = words.Add(utf8Scratch);
		  if (ord < 0)
		  {
			// already exists in our hash
			ord = (-ord) - 1;
			//System.out.println("  output=" + output + " old ord=" + ord);
		  }
		  else
		  {
			//System.out.println("  output=" + output + " new ord=" + ord);
		  }

		  MapEntry e = workingSet[input];
		  if (e == null)
		  {
			e = new MapEntry();
			workingSet[CharsRef.DeepCopyOf(input)] = e; // make a copy, since we will keep around in our map
		  }

		  e.ords.Add(ord);
		  e.includeOrig |= includeOrig;
		  maxHorizontalContext = Math.Max(maxHorizontalContext, numInputWords);
		  maxHorizontalContext = Math.Max(maxHorizontalContext, numOutputWords);
		}

		internal virtual int countWords(CharsRef chars)
		{
		  int wordCount = 1;
		  int upto = chars.Offset;
		  int limit = chars.Offset + chars.Length;
		  while (upto < limit)
		  {
			if (chars.Chars[upto++] == SynonymMap.WORD_SEPARATOR)
			{
			  wordCount++;
			}
		  }
		  return wordCount;
		}

		/// <summary>
		/// Add a phrase->phrase synonym mapping.
		/// Phrases are character sequences where words are
		/// separated with character zero (U+0000).  Empty words
		/// (two U+0000s in a row) are not allowed in the input nor
		/// the output!
		/// </summary>
		/// <param name="input"> input phrase </param>
		/// <param name="output"> output phrase </param>
		/// <param name="includeOrig"> true if the original should be included </param>
		public virtual void Add(CharsRef input, CharsRef output, bool includeOrig)
		{
		  Add(input, countWords(input), output, countWords(output), includeOrig);
		}

		/// <summary>
		/// Builds an <seealso cref="SynonymMap"/> and returns it.
		/// </summary>
		public virtual SynonymMap Build()
		{
		  ByteSequenceOutputs outputs = ByteSequenceOutputs.Singleton;
		  // TODO: are we using the best sharing options?
		  var builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE4, outputs);

		  BytesRef scratch = new BytesRef(64);
		  ByteArrayDataOutput scratchOutput = new ByteArrayDataOutput();

		  HashSet<int?> dedupSet;

		  if (dedup)
		  {
			dedupSet = new HashSet<int?>();
		  }
		  else
		  {
			dedupSet = null;
		  }

		  
            var spare = new sbyte[5];

		  Dictionary<CharsRef, MapEntry>.KeyCollection keys = workingSet.Keys;
		  CharsRef[] sortedKeys = keys.ToArray();
		  Arrays.Sort(sortedKeys, CharsRef.UTF16SortedAsUTF8Comparator);

		  IntsRef scratchIntsRef = new IntsRef();

		  //System.out.println("fmap.build");
		  for (int keyIdx = 0; keyIdx < sortedKeys.Length; keyIdx++)
		  {
			CharsRef input = sortedKeys[keyIdx];
			MapEntry output = workingSet[input];

			int numEntries = output.ords.Count;
			// output size, assume the worst case
			int estimatedSize = 5 + numEntries * 5; // numEntries + one ord for each entry

			scratch.Grow(estimatedSize);
			scratchOutput.Reset(scratch.Bytes, scratch.Offset, scratch.Bytes.Length);
			Debug.Assert(scratch.Offset == 0);

			// now write our output data:
			int count = 0;
			for (int i = 0; i < numEntries; i++)
			{
			  if (dedupSet != null)
			  {
				// box once
				int? ent = output.ords[i];
				if (dedupSet.Contains(ent))
				{
				  continue;
				}
				dedupSet.Add(ent);
			  }
			  scratchOutput.WriteVInt(output.ords[i]);
			  count++;
			}

			int pos = scratchOutput.Position;
			scratchOutput.WriteVInt(count << 1 | (output.includeOrig ? 0 : 1));
			int pos2 = scratchOutput.Position;
			int vIntLen = pos2 - pos;

			// Move the count + includeOrig to the front of the byte[]:
			Array.Copy(scratch.Bytes, pos, spare, 0, vIntLen);
			Array.Copy(scratch.Bytes, 0, scratch.Bytes, vIntLen, pos);
			Array.Copy(spare, 0, scratch.Bytes, 0, vIntLen);

			if (dedupSet != null)
			{
			  dedupSet.Clear();
			}

			scratch.Length = scratchOutput.Position - scratch.Offset;
			//System.out.println("  add input=" + input + " output=" + scratch + " offset=" + scratch.offset + " length=" + scratch.length + " count=" + count);
			builder.Add(Util.ToUTF32(input, scratchIntsRef), BytesRef.DeepCopyOf(scratch));
		  }

		  FST<BytesRef> fst = builder.Finish();
		  return new SynonymMap(fst, words, maxHorizontalContext);
		}
	  }

	  /// <summary>
	  /// Abstraction for parsing synonym files.
	  /// 
	  /// @lucene.experimental
	  /// </summary>
	  public abstract class Parser : Builder
	  {

		internal readonly Analyzer analyzer;

		public Parser(bool dedup, Analyzer analyzer) : base(dedup)
		{
		  this.analyzer = analyzer;
		}

		/// <summary>
		/// Parse the given input, adding synonyms to the inherited <seealso cref="Builder"/>. </summary>
		/// <param name="in"> The input to parse </param>
		public abstract void Parse(Reader @in);

		/// <summary>
		/// Sugar: analyzes the text with the analyzer and
		///  separates by <seealso cref="SynonymMap#WORD_SEPARATOR"/>.
		///  reuse and its chars must not be null. 
		/// </summary>
		public virtual CharsRef Analyze(string text, CharsRef reuse)
		{
		  IOException priorException = null;
		  TokenStream ts = analyzer.TokenStream("", text);
		  try
		  {
              var termAtt = ts.AddAttribute < ICharTermAttribute>();
              var posIncAtt = ts.AddAttribute < IPositionIncrementAttribute>();
			ts.Reset();
			reuse.Length = 0;
			while (ts.IncrementToken())
			{
			  int length = termAtt.Length;
			  if (length == 0)
			  {
				throw new System.ArgumentException("term: " + text + " analyzed to a zero-length token");
			  }
			  if (posIncAtt.PositionIncrement != 1)
			  {
				throw new System.ArgumentException("term: " + text + " analyzed to a token with posinc != 1");
			  }
			  reuse.Grow(reuse.Length + length + 1); // current + word + separator
			  int end = reuse.Offset + reuse.Length;
			  if (reuse.Length > 0)
			  {
				reuse.Chars[end++] = SynonymMap.WORD_SEPARATOR;
				reuse.Length++;
			  }
			  Array.Copy(termAtt.Buffer(), 0, reuse.Chars, end, length);
			  reuse.Length += length;
			}
			ts.End();
		  }
		  catch (IOException e)
		  {
			priorException = e;
		  }
		  finally
		  {
			IOUtils.CloseWhileHandlingException(priorException, ts);
		  }
		  if (reuse.Length == 0)
		  {
			throw new System.ArgumentException("term: " + text + " was completely eliminated by analyzer");
		  }
		  return reuse;
		}
	  }

	}

}