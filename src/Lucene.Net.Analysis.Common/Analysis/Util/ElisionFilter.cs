﻿using Lucene.Net.Analysis.Tokenattributes;

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
    /// Removes elisions from a <seealso cref="TokenStream"/>. For example, "l'avion" (the plane) will be
    /// tokenized as "avion" (plane).
    /// </summary>
    /// <seealso cref= <a href="http://fr.wikipedia.org/wiki/%C3%89lision">Elision in Wikipedia</a> </seealso>
    public sealed class ElisionFilter : TokenFilter
    {
        private readonly CharArraySet articles;
        private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));

        /// <summary>
        /// Constructs an elision filter with a Set of stop words </summary>
        /// <param name="input"> the source <seealso cref="TokenStream"/> </param>
        /// <param name="articles"> a set of stopword articles </param>
        public ElisionFilter(TokenStream input, CharArraySet articles)
            : base(input)
        {
            this.articles = articles;
        }

        /// <summary>
        /// Increments the <seealso cref="TokenStream"/> with a <seealso cref="CharTermAttribute"/> without elisioned start
        /// </summary>
        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                char[] termBuffer = termAtt.Buffer();
                int termLength = termAtt.Length;

                int index = -1;
                for (int i = 0; i < termLength; i++)
                {
                    char ch = termBuffer[i];
                    if (ch == '\'' || ch == '\u2019')
                    {
                        index = i;
                        break;
                    }
                }

                // An apostrophe has been found. If the prefix is an article strip it off.
                if (index >= 0 && articles.Contains(termBuffer, 0, index))
                {
                    termAtt.CopyBuffer(termBuffer, index + 1, termLength - (index + 1));
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}