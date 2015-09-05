﻿namespace Lucene.Net.Facet
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

    using Lucene.Net.Util;

	/// <summary>
	/// Keeps highest results, first by largest float value,
	///  then tie break by smallest ord. 
	/// </summary>
	public class TopOrdAndFloatQueue : PriorityQueue<TopOrdAndFloatQueue.OrdAndValue>
	{

	  /// <summary>
	  /// Holds a single entry. </summary>
	  public sealed class OrdAndValue
	  {

		/// <summary>
		/// Ordinal of the entry. </summary>
		public int ord;

		/// <summary>
		/// Value associated with the ordinal. </summary>
		public float value;

		/// <summary>
		/// Default constructor. </summary>
		public OrdAndValue()
		{
		}
	  }

	  /// <summary>
	  /// Sole constructor. </summary>
	  public TopOrdAndFloatQueue(int topN) : base(topN, false)
	  {
	  }

	  public override bool LessThan(OrdAndValue a, OrdAndValue b)
	  {
		if (a.value < b.value)
		{
		  return true;
		}
		else if (a.value > b.value)
		{
		  return false;
		}
		else
		{
		  return a.ord > b.ord;
		}
	  }
	}

}