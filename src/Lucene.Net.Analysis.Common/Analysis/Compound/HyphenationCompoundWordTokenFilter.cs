﻿using System.IO;
using Lucene.Net.Analysis.Compound.Hyphenation;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using org.apache.lucene.analysis.compound;

namespace Lucene.Net.Analysis.Compound
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
    /// A <seealso cref="TokenFilter"/> that decomposes compound words found in many Germanic languages.
    /// <para>
    /// "Donaudampfschiff" becomes Donau, dampf, schiff so that you can find
    /// "Donaudampfschiff" even when you only enter "schiff". It uses a hyphenation
    /// grammar and a word dictionary to achieve this.
    /// </para>
    /// <para>
    /// You must specify the required <seealso cref="LuceneVersion"/> compatibility when creating
    /// CompoundWordTokenFilterBase:
    /// <ul>
    /// <li>As of 3.1, CompoundWordTokenFilterBase correctly handles Unicode 4.0
    /// supplementary characters in strings and char arrays provided as compound word
    /// dictionaries.
    /// </ul>
    /// </para>
    /// </summary>
    public class HyphenationCompoundWordTokenFilter : CompoundWordTokenFilterBase
    {
        private readonly HyphenationTree hyphenator;

        /// <summary>
        /// Creates a new <seealso cref="HyphenationCompoundWordTokenFilter"/> instance. 
        /// </summary>
        /// <param name="matchVersion">
        ///          Lucene version to enable correct Unicode 4.0 behavior in the
        ///          dictionaries if Version > 3.0. See <a
        ///          href="CompoundWordTokenFilterBase.html#version"
        ///          >CompoundWordTokenFilterBase</a> for details. </param>
        /// <param name="input">
        ///          the <seealso cref="TokenStream"/> to process </param>
        /// <param name="hyphenator">
        ///          the hyphenation pattern tree to use for hyphenation </param>
        /// <param name="dictionary">
        ///          the word dictionary to match against. </param>
        public HyphenationCompoundWordTokenFilter(LuceneVersion matchVersion, TokenStream input, HyphenationTree hyphenator, CharArraySet dictionary)
            : this(matchVersion, input, hyphenator, dictionary, DEFAULT_MIN_WORD_SIZE, DEFAULT_MIN_SUBWORD_SIZE, DEFAULT_MAX_SUBWORD_SIZE, false)
        {
        }

        /// <summary>
        /// Creates a new <seealso cref="HyphenationCompoundWordTokenFilter"/> instance.
        /// </summary>
        /// <param name="matchVersion">
        ///          Lucene version to enable correct Unicode 4.0 behavior in the
        ///          dictionaries if Version > 3.0. See <a
        ///          href="CompoundWordTokenFilterBase.html#version"
        ///          >CompoundWordTokenFilterBase</a> for details. </param>
        /// <param name="input">
        ///          the <seealso cref="TokenStream"/> to process </param>
        /// <param name="hyphenator">
        ///          the hyphenation pattern tree to use for hyphenation </param>
        /// <param name="dictionary">
        ///          the word dictionary to match against. </param>
        /// <param name="minWordSize">
        ///          only words longer than this get processed </param>
        /// <param name="minSubwordSize">
        ///          only subwords longer than this get to the output stream </param>
        /// <param name="maxSubwordSize">
        ///          only subwords shorter than this get to the output stream </param>
        /// <param name="onlyLongestMatch">
        ///          Add only the longest matching subword to the stream </param>
        public HyphenationCompoundWordTokenFilter(LuceneVersion matchVersion, TokenStream input, HyphenationTree hyphenator, CharArraySet dictionary, int minWordSize, int minSubwordSize, int maxSubwordSize, bool onlyLongestMatch)
            : base(matchVersion, input, dictionary, minWordSize, minSubwordSize, maxSubwordSize, onlyLongestMatch)
        {

            this.hyphenator = hyphenator;
        }

        /// <summary>
        /// Create a HyphenationCompoundWordTokenFilter with no dictionary.
        /// <para>
        /// Calls {@link #HyphenationCompoundWordTokenFilter(Version, TokenStream, HyphenationTree, CharArraySet, int, int, int, boolean)
        /// HyphenationCompoundWordTokenFilter(matchVersion, input, hyphenator,
        /// null, minWordSize, minSubwordSize, maxSubwordSize }
        /// </para>
        /// </summary>
        public HyphenationCompoundWordTokenFilter(LuceneVersion matchVersion, TokenStream input, HyphenationTree hyphenator, int minWordSize, int minSubwordSize, int maxSubwordSize)
            : this(matchVersion, input, hyphenator, null, minWordSize, minSubwordSize, maxSubwordSize, false)
        {
        }

        /// <summary>
        /// Create a HyphenationCompoundWordTokenFilter with no dictionary.
        /// <para>
        /// Calls {@link #HyphenationCompoundWordTokenFilter(Version, TokenStream, HyphenationTree, int, int, int) 
        /// HyphenationCompoundWordTokenFilter(matchVersion, input, hyphenator, 
        /// DEFAULT_MIN_WORD_SIZE, DEFAULT_MIN_SUBWORD_SIZE, DEFAULT_MAX_SUBWORD_SIZE }
        /// </para>
        /// </summary>
        public HyphenationCompoundWordTokenFilter(LuceneVersion matchVersion, TokenStream input, HyphenationTree hyphenator)
            : this(matchVersion, input, hyphenator, DEFAULT_MIN_WORD_SIZE, DEFAULT_MIN_SUBWORD_SIZE, DEFAULT_MAX_SUBWORD_SIZE)
        {
        }

        /// <summary>
        /// Create a hyphenator tree
        /// </summary>
        /// <param name="hyphenationFilename"> the filename of the XML grammar to load </param>
        /// <returns> An object representing the hyphenation patterns </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public static HyphenationTree GetHyphenationTree(string hyphenationFilename)
        {
            return getHyphenationTree(new InputSource(hyphenationFilename));
        }

        /// <summary>
        /// Create a hyphenator tree
        /// </summary>
        /// <param name="hyphenationFile"> the file of the XML grammar to load </param>
        /// <returns> An object representing the hyphenation patterns </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public static HyphenationTree GetHyphenationTree(File hyphenationFile)
        {
            return getHyphenationTree(new InputSource(hyphenationFile.ToURI().toASCIIString()));
        }

        /// <summary>
        /// Create a hyphenator tree
        /// </summary>
        /// <param name="hyphenationSource"> the InputSource pointing to the XML grammar </param>
        /// <returns> An object representing the hyphenation patterns </returns>
        /// <exception cref="IOException"> If there is a low-level I/O error. </exception>
        public static HyphenationTree getHyphenationTree(InputSource hyphenationSource)
        {
            var tree = new HyphenationTree();
            tree.loadPatterns(hyphenationSource);
            return tree;
        }

        protected internal override void decompose()
        {
            // get the hyphenation points
            Hyphenation hyphens = hyphenator.hyphenate(termAtt.Buffer(), 0, termAtt.Length(), 1, 1);
            // No hyphen points found -> exit
            if (hyphens == null)
            {
                return;
            }

            int[] hyp = hyphens.HyphenationPoints;

            for (int i = 0; i < hyp.Length; ++i)
            {
                int remaining = hyp.Length - i;
                int start = hyp[i];
                CompoundToken longestMatchToken = null;
                for (int j = 1; j < remaining; j++)
                {
                    int partLength = hyp[i + j] - start;

                    // if the part is longer than maxSubwordSize we
                    // are done with this round
                    if (partLength > this.maxSubwordSize)
                    {
                        break;
                    }

                    // we only put subwords to the token stream
                    // that are longer than minPartSize
                    if (partLength < this.minSubwordSize)
                    {
                        // BOGUS/BROKEN/FUNKY/WACKO: somehow we have negative 'parts' according to the 
                        // calculation above, and we rely upon minSubwordSize being >=0 to filter them out...
                        continue;
                    }

                    // check the dictionary
                    if (dictionary == null || dictionary.Contains(termAtt.Buffer(), start, partLength))
                    {
                        if (this.onlyLongestMatch)
                        {
                            if (longestMatchToken != null)
                            {
                                if (longestMatchToken.txt.Length() < partLength)
                                {
                                    longestMatchToken = new CompoundToken(this, start, partLength);
                                }
                            }
                            else
                            {
                                longestMatchToken = new CompoundToken(this, start, partLength);
                            }
                        }
                        else
                        {
                            tokens.AddLast(new CompoundToken(this, start, partLength));
                        }
                    }
                    else if (dictionary.contains(termAtt.buffer(), start, partLength - 1))
                    {
                        // check the dictionary again with a word that is one character
                        // shorter
                        // to avoid problems with genitive 's characters and other binding
                        // characters
                        if (this.onlyLongestMatch)
                        {
                            if (longestMatchToken != null)
                            {
                                if (longestMatchToken.txt.Length() < partLength - 1)
                                {
                                    longestMatchToken = new CompoundToken(this, start, partLength - 1);
                                }
                            }
                            else
                            {
                                longestMatchToken = new CompoundToken(this, start, partLength - 1);
                            }
                        }
                        else
                        {
                            tokens.AddLast(new CompoundToken(this, start, partLength - 1));
                        }
                    }
                }
                if (this.onlyLongestMatch && longestMatchToken != null)
                {
                    tokens.AddLast(longestMatchToken);
                }
            }
        }
    }
}