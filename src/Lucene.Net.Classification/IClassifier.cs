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

namespace Lucene.Net.Classification
{
    using Lucene.Net.Analysis;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;

    /// <summary>
    /// A classifier, see <code>http://en.wikipedia.org/wiki/Classifier_(mathematics)</code>, which assign classes of type
    /// <code>T</code>
    /// @lucene.experimental
    /// </summary>
    public interface IClassifier<T>
    {
        /// <summary>
        /// Assign a class (with score) to the given text String
        /// </summary>
        /// <param name="text">a String containing text to be classified</param>
        /// <returns>a {ClassificationResult} holding assigned class of type <code>T</code> and score</returns>
        ClassificationResult<T> AssignClass(String text);

        /// <summary>
        /// * Train the classifier using the underlying Lucene index
        /// </summary>
        /// <param name="analyzer"> the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="textFieldName">the name of the field used to compare documents</param>
        void Train(AtomicReader atomicReader, String textFieldName, String classFieldName, Analyzer analyzer);

        /// <summary>Train the classifier using the underlying Lucene index</summary>
        /// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="query">the query to filter which documents use for training</param>
        /// <param name="textFieldName">the name of the field used to compare documents</param>
        void Train(AtomicReader atomicReader, String textFieldName, String classFieldName, Analyzer analyzer, Query query);

        /// <summary>Train the classifier using the underlying Lucene index</summary>
        /// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="query">the query to filter which documents use for training</param>
        /// <param name="textFieldNames">the names of the fields to be used to compare documents</param>
        void Train(AtomicReader atomicReader, String[] textFieldNames, String classFieldName, Analyzer analyzer,
                   Query query);
    }
}