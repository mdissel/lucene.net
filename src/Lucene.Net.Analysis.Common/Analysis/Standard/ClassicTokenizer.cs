﻿/*
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

using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using org.apache.lucene.analysis.standard;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.Standard
{
    /// <summary>
    /// A grammar-based tokenizer constructed with JFlex
    /// 
    /// <para> This should be a good tokenizer for most European-language documents:
    /// 
    /// <ul>
    ///   <li>Splits words at punctuation characters, removing punctuation. However, a 
    ///     dot that's not followed by whitespace is considered part of a token.
    ///   <li>Splits words at hyphens, unless there's a number in the token, in which case
    ///     the whole token is interpreted as a product number and is not split.
    ///   <li>Recognizes email addresses and internet hostnames as one token.
    /// </ul>
    /// 
    /// </para>
    /// <para>Many applications have specific tokenizer needs.  If this tokenizer does
    /// not suit your application, please consider copying this source code
    /// directory to your project and maintaining your own grammar-based tokenizer.
    /// 
    /// ClassicTokenizer was named StandardTokenizer in Lucene versions prior to 3.1.
    /// As of 3.1, <seealso cref="StandardTokenizer"/> implements Unicode text segmentation,
    /// as specified by UAX#29.
    /// </para>
    /// </summary>

    public sealed class ClassicTokenizer : Tokenizer
    {
        /// <summary>
        /// A private instance of the JFlex-constructed scanner </summary>
        private StandardTokenizerInterface scanner;

        public const int ALPHANUM = 0;
        public const int APOSTROPHE = 1;
        public const int ACRONYM = 2;
        public const int COMPANY = 3;
        public const int EMAIL = 4;
        public const int HOST = 5;
        public const int NUM = 6;
        public const int CJ = 7;

        public const int ACRONYM_DEP = 8;

        /// <summary>
        /// String token types that correspond to token type int constants </summary>
        public static readonly string[] TOKEN_TYPES = new string[] { "<ALPHANUM>", "<APOSTROPHE>", "<ACRONYM>", "<COMPANY>", "<EMAIL>", "<HOST>", "<NUM>", "<CJ>", "<ACRONYM_DEP>" };

        private int skippedPositions;

        private int maxTokenLength = StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH;

        /// <summary>
        /// Set the max allowed token length.  Any token longer
        ///  than this is skipped. 
        /// </summary>
        public int MaxTokenLength
        {
            set
            {
                if (value < 1)
                {
                    throw new System.ArgumentException("maxTokenLength must be greater than zero");
                }
                this.maxTokenLength = value;
            }
            get
            {
                return maxTokenLength;
            }
        }


        /// <summary>
        /// Creates a new instance of the <seealso cref="ClassicTokenizer"/>.  Attaches
        /// the <code>input</code> to the newly created JFlex scanner.
        /// </summary>
        /// <param name="input"> The input reader
        /// 
        /// See http://issues.apache.org/jira/browse/LUCENE-1068 </param>
        public ClassicTokenizer(LuceneVersion matchVersion, Reader input)
            : base(input)
        {
            Init(matchVersion);
        }

        /// <summary>
        /// Creates a new ClassicTokenizer with a given <seealso cref="org.apache.lucene.util.AttributeSource.AttributeFactory"/> 
        /// </summary>
        public ClassicTokenizer(LuceneVersion matchVersion, AttributeFactory factory, Reader input)
            : base(factory, input)
        {
            Init(matchVersion);
        }

        private void Init(LuceneVersion matchVersion)
        {
            this.scanner = new ClassicTokenizerImpl(input);
        }

        // this tokenizer generates three attributes:
        // term offset, positionIncrement and type
        private readonly CharTermAttribute termAtt;
        private readonly OffsetAttribute offsetAtt;
        private readonly PositionIncrementAttribute posIncrAtt;
        private readonly TypeAttribute typeAtt;
        /*
         * (non-Javadoc)
         *
         * @see org.apache.lucene.analysis.TokenStream#next()
         */
        public override bool IncrementToken()
        {
            ClearAttributes();
            skippedPositions = 0;

            while (true)
            {
                int tokenType = scanner.NextToken;

                if (tokenType == StandardTokenizerInterface_Fields.YYEOF)
                {
                    return false;
                }

                if (scanner.yylength() <= maxTokenLength)
                {
                    posIncrAtt.PositionIncrement = skippedPositions + 1;
                    scanner.getText(termAtt);

                    int start = scanner.yychar();
                    offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + termAtt.Length));

                    if (tokenType == ClassicTokenizer.ACRONYM_DEP)
                    {
                        typeAtt.Type = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.HOST];
                        termAtt.Length = termAtt.Length - 1; // remove extra '.'
                    }
                    else
                    {
                        typeAtt.Type = ClassicTokenizer.TOKEN_TYPES[tokenType];
                    }
                    return true;
                }
                else
                // When we skip a too-long term, we still increment the
                // position increment
                {
                    skippedPositions++;
                }
            }
        }

        public override void End()
        {
            base.End();
            // set final offset
            int finalOffset = CorrectOffset(scanner.yychar() + scanner.yylength());
            offsetAtt.SetOffset(finalOffset, finalOffset);
            // adjust any skipped tokens
            posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
        }

        public override void Dispose()
        {
            base.Dispose();
            scanner.yyreset(input);
        }

        public override void Reset()
        {
            base.Reset();
            scanner.yyreset(input);
            skippedPositions = 0;
        }
    }

}