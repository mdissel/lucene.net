﻿using Lucene.Net.Analysis.Tokenattributes;

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
    /// This TokenFilter emits each incoming token twice once as keyword and once non-keyword, in other words once with
    /// <seealso cref="KeywordAttribute#setKeyword(boolean)"/> set to <code>true</code> and once set to <code>false</code>.
    /// This is useful if used with a stem filter that respects the <seealso cref="KeywordAttribute"/> to index the stemmed and the
    /// un-stemmed version of a term into the same field.
    /// </summary>
    public sealed class KeywordRepeatFilter : TokenFilter
    {

        private readonly IKeywordAttribute keywordAttribute;
        private readonly IPositionIncrementAttribute posIncAttr;
        private State state;

        /// <summary>
        /// Construct a token stream filtering the given input.
        /// </summary>
        public KeywordRepeatFilter(TokenStream input)
            : base(input)
        {
            keywordAttribute = AddAttribute<IKeywordAttribute>();
            posIncAttr = AddAttribute<IPositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            if (state != null)
            {
                RestoreState(state);
                posIncAttr.PositionIncrement = 0;
                keywordAttribute.Keyword = false;
                state = null;
                return true;
            }
            if (input.IncrementToken())
            {
                state = CaptureState();
                keywordAttribute.Keyword = true;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            state = null;
        }
    }

}