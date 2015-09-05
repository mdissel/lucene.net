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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Analysis.CharFilters
{
    // TODO: save/load?

	/// <summary>
	/// Holds a map of String input to String output, to be used
	/// with <seealso cref="Builder"/>.  Use the <seealso cref="MappingCharFilter"/>
	/// to create this.
	/// </summary>
	public class NormalizeCharMap
	{

	  internal readonly FST<CharsRef> map;
	  internal readonly IDictionary<char?, FST.Arc<CharsRef>> cachedRootArcs = new Dictionary<char?, FST.Arc<CharsRef>>();

	  // Use the builder to create:
	  private NormalizeCharMap(FST<CharsRef> map)
	  {
		this.map = map;
		if (map != null)
		{
		  try
		  {
			// Pre-cache root arcs:
			var scratchArc = new FST.Arc<CharsRef>();
			FST.BytesReader fstReader = map.BytesReader;
			map.GetFirstArc(scratchArc);
			if (FST<CharsRef>.TargetHasArcs(scratchArc))
			{
			  map.ReadFirstRealTargetArc(scratchArc.Target, scratchArc, fstReader);
			  while (true)
			  {
				Debug.Assert(scratchArc.Label != FST<CharsRef>.END_LABEL); // LUCENENET TODO END_LABEL shouldn't be under generic?
				cachedRootArcs[Convert.ToChar((char) scratchArc.Label)] = (new FST.Arc<CharsRef>()).CopyFrom(scratchArc);
				if (scratchArc.Last)
				{
				  break;
				}
				map.ReadNextRealArc(scratchArc, fstReader);
			  }
			}
			//System.out.println("cached " + cachedRootArcs.size() + " root arcs");
		  }
		  catch (IOException ioe)
		  {
			// Bogus FST IOExceptions!!  (will never happen)
			throw new Exception(ioe);
		  }
		}
	  }

	  /// <summary>
	  /// Builds an NormalizeCharMap.
	  /// <para>
	  /// Call add() until you have added all the mappings, then call build() to get a NormalizeCharMap
	  /// @lucene.experimental
	  /// </para>
	  /// </summary>
	  public class Builder
	  {

		internal readonly IDictionary<string, string> pendingPairs = new SortedDictionary<string, string>();

		/// <summary>
		/// Records a replacement to be applied to the input
		///  stream.  Whenever <code>singleMatch</code> occurs in
		///  the input, it will be replaced with
		///  <code>replacement</code>.
		/// </summary>
		/// <param name="match"> input String to be replaced </param>
		/// <param name="replacement"> output String </param>
		/// <exception cref="IllegalArgumentException"> if
		/// <code>match</code> is the empty string, or was
		/// already previously added </exception>
		public virtual void add(string match, string replacement)
		{
		  if (match.Length == 0)
		  {
			throw new System.ArgumentException("cannot match the empty string");
		  }
		  if (pendingPairs.ContainsKey(match))
		  {
			throw new System.ArgumentException("match \"" + match + "\" was already added");
		  }
		  pendingPairs[match] = replacement;
		}

		/// <summary>
		/// Builds the NormalizeCharMap; call this once you
		///  are done calling <seealso cref="#add"/>. 
		/// </summary>
		public virtual NormalizeCharMap build()
		{

		  FST<CharsRef> map;
		  try
		  {
			Outputs<CharsRef> outputs = CharSequenceOutputs.Singleton;
			Builder<CharsRef> builder = new Builder<CharsRef>(FST.INPUT_TYPE.BYTE2, outputs);
			IntsRef scratch = new IntsRef();
			foreach (KeyValuePair<string, string> ent in pendingPairs.SetOfKeyValuePairs())
			{
			  builder.Add(Util.ToUTF16(ent.Key, scratch), new CharsRef(ent.Value));
			}
			map = builder.Finish();
			pendingPairs.Clear();
		  }
		  catch (IOException ioe)
		  {
			// Bogus FST IOExceptions!!  (will never happen)
			throw new Exception(ioe);
		  }

		  return new NormalizeCharMap(map);
		}
	  }
	}

}