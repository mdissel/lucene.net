﻿using System.IO;
using Lucene.Net.Search;

namespace Lucene.Net.Join
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
	/// Utility for query time joining using TermsQuery and TermsCollector.
	/// 
	/// @lucene.experimental
	/// </summary>
    public sealed class JoinUtil
    {
        // No instances allowed
        private JoinUtil()
        {
        }

        /// <summary>
        /// Method for query time joining.
        /// <p/>
        /// Execute the returned query with a <seealso cref="IndexSearcher"/> to retrieve all documents that have the same terms in the
        /// to field that match with documents matching the specified fromQuery and have the same terms in the from field.
        /// <p/>
        /// In the case a single document relates to more than one document the <code>multipleValuesPerDocument</code> option
        /// should be set to true. When the <code>multipleValuesPerDocument</code> is set to <code>true</code> only the
        /// the score from the first encountered join value originating from the 'from' side is mapped into the 'to' side.
        /// Even in the case when a second join value related to a specific document yields a higher score. Obviously this
        /// doesn't apply in the case that <seealso cref="ScoreMode.None"/> is used, since no scores are computed at all.
        /// </p>
        /// Memory considerations: During joining all unique join values are kept in memory. On top of that when the scoreMode
        /// isn't set to <seealso cref="ScoreMode.None"/> a float value per unique join value is kept in memory for computing scores.
        /// When scoreMode is set to <seealso cref="ScoreMode.Avg"/> also an additional integer value is kept in memory per unique
        /// join value.
        /// </summary>
        /// <param name="fromField">                 The from field to join from </param>
        /// <param name="multipleValuesPerDocument"> Whether the from field has multiple terms per document </param>
        /// <param name="toField">                   The to field to join to </param>
        /// <param name="fromQuery">                 The query to match documents on the from side </param>
        /// <param name="fromSearcher">              The searcher that executed the specified fromQuery </param>
        /// <param name="scoreMode">                 Instructs how scores from the fromQuery are mapped to the returned query </param>
        /// <returns>A <see cref="Query"/> instance that can be used to join documents based on the terms in the from and to field</returns>
        /// <exception cref="IOException"> If I/O related errors occur </exception>
        public static Query CreateJoinQuery(string fromField, bool multipleValuesPerDocument, string toField, Query fromQuery, IndexSearcher fromSearcher, ScoreMode scoreMode)
        {
            switch (scoreMode)
            {
                case ScoreMode.None:
                    TermsCollector termsCollector = TermsCollector.Create(fromField, multipleValuesPerDocument);
                    fromSearcher.Search(fromQuery, termsCollector);
                    return new TermsQuery(toField, fromQuery, termsCollector.CollectorTerms);
                case ScoreMode.Total:
                case ScoreMode.Max:
                case ScoreMode.Avg:
                    TermsWithScoreCollector termsWithScoreCollector = TermsWithScoreCollector.Create(fromField, multipleValuesPerDocument, scoreMode);
                    fromSearcher.Search(fromQuery, termsWithScoreCollector);
                    return new TermsIncludingScoreQuery(toField, multipleValuesPerDocument, termsWithScoreCollector.CollectedTerms, termsWithScoreCollector.ScoresPerTerm, fromQuery);
                default:
                    throw new System.ArgumentException(string.Format("Score mode {0} isn't supported.", scoreMode));
            }
        }
    }
}