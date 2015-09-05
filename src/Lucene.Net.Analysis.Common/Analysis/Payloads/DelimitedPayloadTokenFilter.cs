﻿using Lucene.Net.Analysis.Tokenattributes;
using org.apache.lucene.analysis.payloads;

namespace Lucene.Net.Analysis.Payloads
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
    /// Characters before the delimiter are the "token", those after are the payload.
    /// <p/>
    /// For example, if the delimiter is '|', then for the string "foo|bar", foo is the token
    /// and "bar" is a payload.
    /// <p/>
    /// Note, you can also include a <seealso cref="org.apache.lucene.analysis.payloads.PayloadEncoder"/> to convert the payload in an appropriate way (from characters to bytes).
    /// <p/>
    /// Note make sure your Tokenizer doesn't split on the delimiter, or this won't work
    /// </summary>
    /// <seealso cref= PayloadEncoder </seealso>
    public sealed class DelimitedPayloadTokenFilter : TokenFilter
    {
        public const char DEFAULT_DELIMITER = '|';
        private readonly char delimiter;
        private readonly ICharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
        private readonly IPayloadAttribute payAtt = addAttribute(typeof(PayloadAttribute));
        private readonly PayloadEncoder encoder;


        public DelimitedPayloadTokenFilter(TokenStream input, char delimiter, PayloadEncoder encoder)
            : base(input)
        {
            this.delimiter = delimiter;
            this.encoder = encoder;
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                char[] buffer = termAtt.Buffer();
                int length = termAtt.Length;
                for (int i = 0; i < length; i++)
                {
                    if (buffer[i] == delimiter)
                    {
                        payAtt.Payload = encoder.encode(buffer, i + 1, (length - (i + 1)));
                        termAtt.Length = i; // simply set a new length
                        return true;
                    }
                }
                // we have not seen the delimiter
                payAtt.Payload = null;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

}