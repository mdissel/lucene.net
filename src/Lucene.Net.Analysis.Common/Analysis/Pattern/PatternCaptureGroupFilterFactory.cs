﻿using System.Collections.Generic;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Pattern
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
	/// Factory for <seealso cref="PatternCaptureGroupTokenFilter"/>. 
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_ptncapturegroup" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
	///     &lt;filter class="solr.PatternCaptureGroupFilterFactory" pattern="([^a-z])" preserve_original="true"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	/// <seealso cref= PatternCaptureGroupTokenFilter </seealso>
	public class PatternCaptureGroupFilterFactory : TokenFilterFactory
	{
	  private Pattern pattern;
	  private bool preserveOriginal = true;

	  public PatternCaptureGroupFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		pattern = getPattern(args, "pattern");
		preserveOriginal = args.ContainsKey("preserve_original") ? bool.Parse(args["preserve_original"]) : true;
	  }
	  public override TokenStream Create(TokenStream input)
	  {
		return new PatternCaptureGroupTokenFilter(input, preserveOriginal, pattern);
	  }
	}

}