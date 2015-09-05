﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


namespace Lucene.Net.Analysis.Compound.Hyphenation
{

	/// <summary>
	/// This class implements a simple byte vector with access to the underlying
	/// array.
	/// This class has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified. 
	/// </summary>
	public class ByteVector
	{

	  /// <summary>
	  /// Capacity increment size
	  /// </summary>
	  private const int DEFAULT_BLOCK_SIZE = 2048;

	  private int blockSize;

	  /// <summary>
	  /// The encapsulated array
	  /// </summary>
	  private sbyte[] array;

	  /// <summary>
	  /// Points to next free item
	  /// </summary>
	  private int n;

	  public ByteVector() : this(DEFAULT_BLOCK_SIZE)
	  {
	  }

	  public ByteVector(int capacity)
	  {
		if (capacity_Renamed > 0)
		{
		  blockSize = capacity_Renamed;
		}
		else
		{
		  blockSize = DEFAULT_BLOCK_SIZE;
		}
		array = new sbyte[blockSize];
		n = 0;
	  }

	  public ByteVector(sbyte[] a)
	  {
		blockSize = DEFAULT_BLOCK_SIZE;
		array = a;
		n = 0;
	  }

	  public ByteVector(sbyte[] a, int capacity)
	  {
		if (capacity_Renamed > 0)
		{
		  blockSize = capacity_Renamed;
		}
		else
		{
		  blockSize = DEFAULT_BLOCK_SIZE;
		}
		array = a;
		n = 0;
	  }

	  public virtual sbyte[] Array
	  {
		  get
		  {
			return array;
		  }
	  }

	  /// <summary>
	  /// return number of items in array
	  /// </summary>
	  public virtual int length()
	  {
		return n;
	  }

	  /// <summary>
	  /// returns current capacity of array
	  /// </summary>
	  public virtual int capacity()
	  {
		return array.Length;
	  }

	  public virtual void put(int index, sbyte val)
	  {
		array[index] = val;
	  }

	  public virtual sbyte get(int index)
	  {
		return array[index];
	  }

	  /// <summary>
	  /// This is to implement memory allocation in the array. Like malloc().
	  /// </summary>
	  public virtual int alloc(int size)
	  {
		int index = n;
		int len = array.Length;
		if (n + size >= len)
		{
		  sbyte[] aux = new sbyte[len + blockSize];
		  Array.Copy(array, 0, aux, 0, len);
		  array = aux;
		}
		n += size;
		return index;
	  }

	  public virtual void trimToSize()
	  {
		if (n < array.Length)
		{
		  sbyte[] aux = new sbyte[n];
		  Array.Copy(array, 0, aux, 0, n);
		  array = aux;
		}
	  }

	}

}