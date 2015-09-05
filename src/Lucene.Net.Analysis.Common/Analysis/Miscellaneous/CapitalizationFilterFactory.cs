﻿using System.Collections.Generic;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;

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
    /// Factory for <seealso cref="CapitalizationFilter"/>.
    /// <p/>
    /// The factory takes parameters:<br/>
    /// "onlyFirstWord" - should each word be capitalized or all of the words?<br/>
    /// "keep" - a keep word list.  Each word that should be kept separated by whitespace.<br/>
    /// "keepIgnoreCase - true or false.  If true, the keep list will be considered case-insensitive.<br/>
    /// "forceFirstLetter" - Force the first letter to be capitalized even if it is in the keep list<br/>
    /// "okPrefix" - do not change word capitalization if a word begins with something in this list.
    /// for example if "McK" is on the okPrefix list, the word "McKinley" should not be changed to
    /// "Mckinley"<br/>
    /// "minWordLength" - how long the word needs to be to get capitalization applied.  If the
    /// minWordLength is 3, "and" > "And" but "or" stays "or"<br/>
    /// "maxWordCount" - if the token contains more then maxWordCount words, the capitalization is
    /// assumed to be correct.<br/>
    /// 
    /// <pre class="prettyprint">
    /// &lt;fieldType name="text_cptlztn" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.CapitalizationFilterFactory" onlyFirstWord="true"
    ///           keep="java solr lucene" keepIgnoreCase="false"
    ///           okPrefix="McK McD McA"/&gt;   
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// 
    /// @since solr 1.3
    /// </summary>
    public class CapitalizationFilterFactory : TokenFilterFactory
    {
        public const string KEEP = "keep";
        public const string KEEP_IGNORE_CASE = "keepIgnoreCase";
        public const string OK_PREFIX = "okPrefix";
        public const string MIN_WORD_LENGTH = "minWordLength";
        public const string MAX_WORD_COUNT = "maxWordCount";
        public const string MAX_TOKEN_LENGTH = "maxTokenLength";
        public const string ONLY_FIRST_WORD = "onlyFirstWord";
        public const string FORCE_FIRST_LETTER = "forceFirstLetter";

        internal CharArraySet keep;

        internal ICollection<char[]> okPrefix = Collections.EmptyList<char[]>(); // for Example: McK

        internal readonly int minWordLength; // don't modify capitalization for words shorter then this
        internal readonly int maxWordCount;
        internal readonly int maxTokenLength;
        internal readonly bool onlyFirstWord;
        internal readonly bool forceFirstLetter; // make sure the first letter is capital even if it is in the keep list

        /// <summary>
        /// Creates a new CapitalizationFilterFactory </summary>
        public CapitalizationFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            assureMatchVersion();
            bool ignoreCase = getBoolean(args, KEEP_IGNORE_CASE, false);
            HashSet<string> k = getSet(args, KEEP);
            if (k != null)
            {
                keep = new CharArraySet(luceneMatchVersion, 10, ignoreCase);
                keep.AddAll(k);
            }

            k = getSet(args, OK_PREFIX);
            if (k != null)
            {
                okPrefix = new List<char[]>();
                foreach (string item in k)
                {
                    okPrefix.Add(item.ToCharArray());
                }
            }

            minWordLength = getInt(args, MIN_WORD_LENGTH, 0);
            maxWordCount = getInt(args, MAX_WORD_COUNT, CapitalizationFilter.DEFAULT_MAX_WORD_COUNT);
            maxTokenLength = getInt(args, MAX_TOKEN_LENGTH, CapitalizationFilter.DEFAULT_MAX_TOKEN_LENGTH);
            onlyFirstWord = getBoolean(args, ONLY_FIRST_WORD, true);
            forceFirstLetter = getBoolean(args, FORCE_FIRST_LETTER, true);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new CapitalizationFilter(input, onlyFirstWord, keep, forceFirstLetter, okPrefix, minWordLength, maxWordCount, maxTokenLength);
        }
    }
}