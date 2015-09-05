﻿using System.Collections.Generic;
using Lucene.Net.Analysis.Util;

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
    /// Factory for <seealso cref="UpperCaseFilter"/>. 
    /// <pre class="prettyprint">
    /// &lt;fieldType name="text_uppercase" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.UpperCaseFilterFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// 
    /// <para><b>NOTE:</b> In Unicode, this transformation may lose information when the
    /// upper case character represents more than one lower case character. Use this filter
    /// when you require uppercase tokens.  Use the <seealso cref="LowerCaseFilterFactory"/> for 
    /// general search matching
    /// </para>
    /// </summary>
    public class UpperCaseFilterFactory : TokenFilterFactory, MultiTermAwareComponent
    {

        /// <summary>
        /// Creates a new UpperCaseFilterFactory </summary>
        public UpperCaseFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            assureMatchVersion();
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new UpperCaseFilter(luceneMatchVersion, input);
        }

        public virtual AbstractAnalysisFactory MultiTermComponent
        {
            get
            {
                return this;
            }
        }
    }
}