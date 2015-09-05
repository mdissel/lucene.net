﻿using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Util;

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
    /// Abstract parent class for analysis factories that create <seealso cref="Tokenizer"/>
    /// instances.
    /// </summary>
    public abstract class TokenizerFactory : AbstractAnalysisFactory
    {

        private static readonly AnalysisSPILoader<TokenizerFactory> loader = new AnalysisSPILoader<TokenizerFactory>(typeof(TokenizerFactory));

        /// <summary>
        /// looks up a tokenizer by name from context classpath </summary>
        public static TokenizerFactory ForName(string name, IDictionary<string, string> args)
        {
            return loader.newInstance(name, args);
        }

        /// <summary>
        /// looks up a tokenizer class by name from context classpath </summary>
        public static Type LookupClass(string name)
        {
            return loader.lookupClass(name);
        }

        /// <summary>
        /// returns a list of all available tokenizer names from context classpath </summary>
        public static HashSet<string> AvailableTokenizers()
        {
            return loader.availableServices();
        }

        /// <summary>
        /// Reloads the factory list from the given <seealso cref="ClassLoader"/>.
        /// Changes to the factories are visible after the method ends, all
        /// iterators (<seealso cref="#availableTokenizers()"/>,...) stay consistent. 
        /// 
        /// <para><b>NOTE:</b> Only new factories are added, existing ones are
        /// never removed or replaced.
        /// 
        /// </para>
        /// <para><em>This method is expensive and should only be called for discovery
        /// of new factories on the given classpath/classloader!</em>
        /// </para>
        /// </summary>
        public static void ReloadTokenizers(ClassLoader classloader)
        {
            loader.reload(classloader);
        }

        /// <summary>
        /// Initialize this factory via a set of key-value pairs.
        /// </summary>
        protected internal TokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
        }

        /// <summary>
        /// Creates a TokenStream of the specified input using the default attribute factory. </summary>
        public Tokenizer Create(TextReader input)
        {
            return Create(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input);
        }

        /// <summary>
        /// Creates a TokenStream of the specified input using the given AttributeFactory </summary>
        public abstract Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input);
    }
}