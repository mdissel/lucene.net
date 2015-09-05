﻿using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Miscellaneous;

namespace org.apache.lucene.analysis.miscellaneous
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
	/// Links two <seealso cref="PrefixAwareTokenFilter"/>.
	/// <p/>
	/// <b>NOTE:</b> This filter might not behave correctly if used with custom Attributes, i.e. Attributes other than
	/// the ones located in org.apache.lucene.analysis.tokenattributes. 
	/// </summary>
	public class PrefixAndSuffixAwareTokenFilter : TokenStream
	{

	  private readonly PrefixAwareTokenFilter suffix;

	  public PrefixAndSuffixAwareTokenFilter(TokenStream prefix, TokenStream input, TokenStream suffix) : base(suffix)
	  {
		prefix = new PrefixAwareTokenFilterAnonymousInnerClassHelper(this, prefix, input);
		this.suffix = new PrefixAwareTokenFilterAnonymousInnerClassHelper2(this, prefix, suffix);
	  }

	  private class PrefixAwareTokenFilterAnonymousInnerClassHelper : PrefixAwareTokenFilter
	  {
		  private readonly PrefixAndSuffixAwareTokenFilter outerInstance;

		  public PrefixAwareTokenFilterAnonymousInnerClassHelper(PrefixAndSuffixAwareTokenFilter outerInstance, TokenStream prefix, TokenStream input) : base(prefix, input)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override Token UpdateSuffixToken(Token suffixToken, Token lastInputToken)
		  {
			return outerInstance.updateInputToken(suffixToken, lastInputToken);
		  }
	  }

	  private class PrefixAwareTokenFilterAnonymousInnerClassHelper2 : PrefixAwareTokenFilter
	  {
		  private readonly PrefixAndSuffixAwareTokenFilter outerInstance;

		  public PrefixAwareTokenFilterAnonymousInnerClassHelper2(PrefixAndSuffixAwareTokenFilter outerInstance, TokenStream prefix, TokenStream suffix) : base(prefix, suffix)
		  {
			  this.outerInstance = outerInstance;
		  }

		  public override Token UpdateSuffixToken(Token suffixToken, Token lastInputToken)
		  {
			return outerInstance.updateSuffixToken(suffixToken, lastInputToken);
		  }
	  }

	  public virtual Token updateInputToken(Token inputToken, Token lastPrefixToken)
	  {
		inputToken.SetOffset(lastPrefixToken.endOffset() + inputToken.startOffset(), lastPrefixToken.endOffset() + inputToken.endOffset());
		return inputToken;
	  }

	  public virtual Token updateSuffixToken(Token suffixToken, Token lastInputToken)
	  {
		suffixToken.SetOffset(lastInputToken.endOffset() + suffixToken.startOffset(), lastInputToken.endOffset() + suffixToken.endOffset());
		return suffixToken;
	  }

	  public override bool IncrementToken()
	  {
		return suffix.incrementToken();
	  }

	  public override void Reset()
	  {
		suffix.Reset();
	  }


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void close() throws java.io.IOException
	  public override void close()
	  {
		suffix.close();
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void end() throws java.io.IOException
	  public override void end()
	  {
		suffix.end();
	  }
	}

}