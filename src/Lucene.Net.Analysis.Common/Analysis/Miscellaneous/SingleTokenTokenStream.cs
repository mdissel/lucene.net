﻿using System.Diagnostics;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Miscellaneous
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
	/// A <seealso cref="TokenStream"/> containing a single token.
	/// </summary>
	public sealed class SingleTokenTokenStream : TokenStream
	{

	  private bool exhausted = false;

	  // The token needs to be immutable, so work with clones!
	  private Token singleToken;
	  private readonly AttributeImpl tokenAtt;

	  public SingleTokenTokenStream(Token token) : base(Token.TOKEN_ATTRIBUTE_FACTORY)
	  {

		Debug.Assert(token != null);
		this.singleToken = token.Clone();

        tokenAtt = AddAttribute <ICharTermAttribute>();
		Debug.Assert(tokenAtt is Token);
	  }

	  public override bool IncrementToken()
	  {
		if (exhausted)
		{
		  return false;
		}
		else
		{
		  ClearAttributes();
		  singleToken.CopyTo(tokenAtt);
		  exhausted = true;
		  return true;
		}
	  }

	  public override void Reset()
	  {
		exhausted = false;
	  }

        public Token Token
        {
            get { return (Token) singleToken.Clone(); }
            set { this.singleToken = (Token) value.Clone(); }
        }
	}
}