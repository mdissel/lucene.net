﻿using System.Collections.Generic;
using Lucene.Net.Analysis.Util;

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
    /// Factory for <seealso cref="KeepWordFilter"/>. 
    /// <pre class="prettyprint">
    /// &lt;fieldType name="text_keepword" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.KeepWordFilterFactory" words="keepwords.txt" ignoreCase="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// </summary>
    public class KeepWordFilterFactory : TokenFilterFactory, ResourceLoaderAware
    {
        private readonly bool ignoreCase;
        private readonly bool enablePositionIncrements;
        private readonly string wordFiles;
        private CharArraySet words;

        /// <summary>
        /// Creates a new KeepWordFilterFactory </summary>
        public KeepWordFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            assureMatchVersion();
            wordFiles = get(args, "words");
            ignoreCase = getBoolean(args, "ignoreCase", false);
            enablePositionIncrements = getBoolean(args, "enablePositionIncrements", true);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public virtual void Inform(ResourceLoader loader)
        {
            if (wordFiles != null)
            {
                words = GetWordSet(loader, wordFiles, ignoreCase);
            }
        }

        public virtual bool EnablePositionIncrements
        {
            get
            {
                return enablePositionIncrements;
            }
        }

        public virtual bool IgnoreCase
        {
            get
            {
                return ignoreCase;
            }
        }

        public virtual CharArraySet Words
        {
            get
            {
                return words;
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            // if the set is null, it means it was empty
            if (words == null)
            {
                return input;
            }
            else
            {
                TokenStream filter = new KeepWordFilter(luceneMatchVersion, enablePositionIncrements, input, words);
                return filter;
            }
        }
    }
}