﻿using System.Collections.Generic;
using org.apache.lucene.analysis.reverse;

namespace Lucene.Net.Analysis.Reverse
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

	using TokenFilterFactory = TokenFilterFactory;

	/// <summary>
	/// Factory for <seealso cref="ReverseStringFilter"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_rvsstr" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.ReverseStringFilterFactory"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// 
	/// @since solr 1.4
	/// </summary>
	public class ReverseStringFilterFactory : TokenFilterFactory
	{

	  /// <summary>
	  /// Creates a new ReverseStringFilterFactory </summary>
	  public ReverseStringFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		assureMatchVersion();
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public override ReverseStringFilter create(TokenStream @in)
	  {
		return new ReverseStringFilter(luceneMatchVersion,@in);
	  }
	}


}