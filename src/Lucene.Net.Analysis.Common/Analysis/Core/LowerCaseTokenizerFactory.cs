﻿using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Util;
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
    /// Factory for <seealso cref="LowerCaseTokenizer"/>. 
    /// <pre class="prettyprint">
    /// &lt;fieldType name="text_lwrcase" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.LowerCaseTokenizerFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// </summary>
    public class LowerCaseTokenizerFactory : TokenizerFactory, MultiTermAwareComponent
    {

        /// <summary>
        /// Creates a new LowerCaseTokenizerFactory </summary>
        public LowerCaseTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            assureMatchVersion();
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
            return new LowerCaseTokenizer(luceneMatchVersion, factory, input);
        }

        public virtual AbstractAnalysisFactory MultiTermComponent
        {
            get
            {
                return new LowerCaseFilterFactory(new Dictionary<string, string>(OriginalArgs));
            }
        }
    }
}