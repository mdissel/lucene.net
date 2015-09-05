﻿using System;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Analysis.Tokenattributes;
using org.apache.lucene.analysis.util;
using Reader = System.IO.TextReader;
using Version = Lucene.Net.Util.LuceneVersion;

namespace Lucene.Net.Analysis.Util
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
	/// Breaks text into sentences with a <seealso cref="BreakIterator"/> and
	/// allows subclasses to decompose these sentences into words.
	/// <para>
	/// This can be used by subclasses that need sentence context 
	/// for tokenization purposes, such as CJK segmenters.
	/// </para>
	/// <para>
	/// Additionally it can be used by subclasses that want to mark
	/// sentence boundaries (with a custom attribute, extra token, position
	/// increment, etc) for downstream processing.
	/// 
	/// @lucene.experimental
	/// </para>
	/// </summary>
	public abstract class SegmentingTokenizerBase : Tokenizer
	{
	  protected internal const int BUFFERMAX = 1024;
	  protected internal readonly char[] buffer = new char[BUFFERMAX];
	  /// <summary>
	  /// true length of text in the buffer </summary>
	  private int length = 0;
	  /// <summary>
	  /// length in buffer that can be evaluated safely, up to a safe end point </summary>
	  private int usableLength = 0;
	  /// <summary>
	  /// accumulated offset of previous buffers for this reader, for offsetAtt </summary>
	  protected internal int offset = 0;

	  private readonly BreakIterator iterator;
	  private readonly CharArrayIterator wrapper = CharArrayIterator.newSentenceInstance();

	  private readonly OffsetAttribute offsetAtt = addAttribute(typeof(OffsetAttribute));

	  /// <summary>
	  /// Construct a new SegmenterBase, using
	  /// the provided BreakIterator for sentence segmentation.
	  /// <para>
	  /// Note that you should never share BreakIterators across different
	  /// TokenStreams, instead a newly created or cloned one should always
	  /// be provided to this constructor.
	  /// </para>
	  /// </summary>
	  public SegmentingTokenizerBase(Reader reader, BreakIterator iterator) : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, reader, iterator)
	  {
	  }

	  /// <summary>
	  /// Construct a new SegmenterBase, also supplying the AttributeFactory
	  /// </summary>
	  public SegmentingTokenizerBase(AttributeFactory factory, Reader reader, BreakIterator iterator) : base(factory, reader)
	  {
		this.iterator = iterator;
	  }

	  public override bool IncrementToken()
	  {
		if (length == 0 || !IncrementWord())
		{
		  while (!IncrementSentence())
		  {
			Refill();
			if (length <= 0) // no more bytes to read;
			{
			  return false;
			}
		  }
		}

		return true;
	  }

	  public override void Reset()
	  {
		base.Reset();
		wrapper.setText(buffer, 0, 0);
		iterator.Text = wrapper;
		length = usableLength = offset = 0;
	  }

	  public override void End()
	  {
		base.End();
		int finalOffset = CorrectOffset(length < 0 ? offset : offset + length);
		offsetAtt.SetOffset(finalOffset, finalOffset);
	  }

	  /// <summary>
	  /// Returns the last unambiguous break position in the text. </summary>
	  private int FindSafeEnd()
	  {
		for (int i = length - 1; i >= 0; i--)
		{
		  if (IsSafeEnd(buffer[i]))
		  {
			return i + 1;
		  }
		}
		return -1;
	  }

	  /// <summary>
	  /// For sentence tokenization, these are the unambiguous break positions. </summary>
	  protected internal virtual bool IsSafeEnd(char ch)
	  {
		switch ((int)ch)
		{
		  case 0x000D:
		  case 0x000A:
		  case 0x0085:
		  case 0x2028:
		  case 0x2029:
			return true;
		  default:
			return false;
		}
	  }

	    /// <summary>
	    /// Refill the buffer, accumulating the offset and setting usableLength to the
	    /// last unambiguous break position
	    /// </summary>
	    private void Refill()
	    {
	        offset += usableLength;
	        int leftover = length - usableLength;
	        Array.Copy(buffer, usableLength, buffer, 0, leftover);
	        int requested = buffer.Length - leftover;
	        int returned = Read(input, buffer, leftover, requested);
	        length = returned < 0 ? leftover : returned + leftover;
	        if (returned < requested) // reader has been emptied, process the rest
	        {
	            usableLength = length;
	        }
	        else // still more data to be read, find a safe-stopping place
	        {
	            usableLength = FindSafeEnd();
	            if (usableLength < 0)
	            {
	                usableLength = length; /*
		  }
	                                * more than IOBUFFER of text without breaks,
	                                * gonna possibly truncate tokens
	                                */
	            }

	            wrapper.SetText(buffer, 0, Math.Max(0, usableLength));
	            iterator.Text = wrapper;
	        }
	    }

	    // TODO: refactor to a shared readFully somewhere
	  // (NGramTokenizer does this too):
	  /// <summary>
	  /// commons-io's readFully, but without bugs if offset != 0 </summary>
	  private static int Read(TextReader input, char[] buffer, int offset, int length)
	  {
		Debug.Assert(length >= 0, "length must not be negative: " + length);

		int remaining = length;
		while (remaining > 0)
		{
		  int location = length - remaining;
		  int count = input.read(buffer, offset + location, remaining);
		  if (-1 == count) // EOF
		  {
			break;
		  }
		  remaining -= count;
		}
		return length - remaining;
	  }

	  /// <summary>
	  /// return true if there is a token from the buffer, or null if it is
	  /// exhausted.
	  /// </summary>
	  private bool IncrementSentence()
	  {
		if (length == 0) // we must refill the buffer
		{
		  return false;
		}

		while (true)
		{
		  int start = iterator.Current();

		  if (start == BreakIterator.DONE)
		  {
			return false; // BreakIterator exhausted
		  }

		  // find the next set of boundaries
		  int end_Renamed = iterator.next();

		  if (end_Renamed == BreakIterator.DONE)
		  {
			return false; // BreakIterator exhausted
		  }

		  setNextSentence(start, end_Renamed);
		  if (incrementWord())
		  {
			return true;
		  }
		}
	  }

	  /// <summary>
	  /// Provides the next input sentence for analysis </summary>
	  protected internal abstract void SetNextSentence(int sentenceStart, int sentenceEnd);

	  /// <summary>
	  /// Returns true if another word is available </summary>
	  protected internal abstract bool IncrementWord();
	}
}