﻿using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Core
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
    /// A LetterTokenizer is a tokenizer that divides text at non-letters. That's to
    /// say, it defines tokens as maximal strings of adjacent letters, as defined by
    /// java.lang.Character.isLetter() predicate.
    /// <para>
    /// Note: this does a decent job for most European languages, but does a terrible
    /// job for some Asian languages, where words are not separated by spaces.
    /// </para>
    /// <para>
    /// <a name="version"/>
    /// You must specify the required <seealso cref="LuceneVersion"/> compatibility when creating
    /// <seealso cref="LetterTokenizer"/>:
    /// <ul>
    /// <li>As of 3.1, <seealso cref="CharTokenizer"/> uses an int based API to normalize and
    /// detect token characters. See <seealso cref="CharTokenizer#isTokenChar(int)"/> and
    /// <seealso cref="CharTokenizer#normalize(int)"/> for details.</li>
    /// </ul>
    /// </para>
    /// </summary>

    public class LetterTokenizer : CharTokenizer
    {

        /// <summary>
        /// Construct a new LetterTokenizer.
        /// </summary>
        /// <param name="matchVersion">
        ///          Lucene version to match See <seealso cref="<a href="#version">above</a>"/> </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public LetterTokenizer(LuceneVersion matchVersion, TextReader @in)
            : base(matchVersion, @in)
        {
        }

        /// <summary>
        /// Construct a new LetterTokenizer using a given
        /// <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/>.
        /// </summary>
        /// <param name="matchVersion">
        ///          Lucene version to match See <seealso cref="<a href="#version">above</a>"/> </param>
        /// <param name="factory">
        ///          the attribute factory to use for this <seealso cref="Tokenizer"/> </param>
        /// <param name="in">
        ///          the input to split up into tokens </param>
        public LetterTokenizer(LuceneVersion matchVersion, AttributeSource.AttributeFactory factory, TextReader @in)
            : base(matchVersion, factory, @in)
        {
        }

        /// <summary>
        /// Collects only characters which satisfy
        /// <seealso cref="Character#isLetter(int)"/>.
        /// </summary>	  
        protected override bool IsTokenChar(char c)
        {
            return char.IsLetter(c);
        }
    }
}