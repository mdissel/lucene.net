﻿using System;
using Lucene.Net.Search;

namespace Lucene.Net.Grouping
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
    /// Represents result returned by a grouping search.
    /// 
    /// @lucene.experimental 
    /// </summary>
    public class TopGroups<TGroupValueType>
    {
        /// <summary>
        /// Number of documents matching the search </summary>
        public readonly int TotalHitCount;

        /// <summary>
        /// Number of documents grouped into the topN groups </summary>
        public readonly int TotalGroupedHitCount;

        /// <summary>
        /// The total number of unique groups. If <code>null</code> this value is not computed. </summary>
        public readonly int? TotalGroupCount;

        /// <summary>
        /// Group results in groupSort order </summary>
        public readonly GroupDocs<TGroupValueType>[] Groups;

        /// <summary>
        /// How groups are sorted against each other </summary>
        public readonly SortField[] GroupSort;

        /// <summary>
        /// How docs are sorted within each group </summary>
        public readonly SortField[] WithinGroupSort;

        /// <summary>
        /// Highest score across all hits, or
        ///  <code>Float.NaN</code> if scores were not computed. 
        /// </summary>
        public readonly float MaxScore;

        public TopGroups(SortField[] groupSort, SortField[] withinGroupSort, int totalHitCount, int totalGroupedHitCount, GroupDocs<TGroupValueType>[] groups, float maxScore)
        {
            GroupSort = groupSort;
            WithinGroupSort = withinGroupSort;
            TotalHitCount = totalHitCount;
            TotalGroupedHitCount = totalGroupedHitCount;
            Groups = groups;
            TotalGroupCount = null;
            MaxScore = maxScore;
        }

        public TopGroups(TopGroups<TGroupValueType> oldTopGroups, int? totalGroupCount)
        {
            GroupSort = oldTopGroups.GroupSort;
            WithinGroupSort = oldTopGroups.WithinGroupSort;
            TotalHitCount = oldTopGroups.TotalHitCount;
            TotalGroupedHitCount = oldTopGroups.TotalGroupedHitCount;
            Groups = oldTopGroups.Groups;
            MaxScore = oldTopGroups.MaxScore;
            TotalGroupCount = totalGroupCount;
        }

        /// <summary>
        /// How the GroupDocs score (if any) should be merged. </summary>
        public enum ScoreMergeMode
        {
            /// <summary>
            /// Set score to Float.NaN
            /// </summary>
            None,

            /// <summary>
            /// Sum score across all shards for this group.
            /// </summary>
            Total,

            /// <summary>
            /// Avg score across all shards for this group.
            /// </summary>
            Avg,
        }

        /// <summary>
        /// Merges an array of TopGroups, for example obtained from the second-pass 
        /// collector across multiple shards. Each TopGroups must have been sorted by the
        /// same groupSort and docSort, and the top groups passed to all second-pass 
        /// collectors must be the same.
        /// 
        /// <b>NOTE</b>: We can't always compute an exact totalGroupCount.
        /// Documents belonging to a group may occur on more than
        /// one shard and thus the merged totalGroupCount can be
        /// higher than the actual totalGroupCount. In this case the
        /// totalGroupCount represents a upper bound. If the documents
        /// of one group do only reside in one shard then the
        /// totalGroupCount is exact.
        /// 
        /// <b>NOTE</b>: the topDocs in each GroupDocs is actually
        /// an instance of TopDocsAndShards
        /// </summary>
        public static TopGroups<T> Merge<T>(TopGroups<T>[] shardGroups, Sort groupSort, Sort docSort, int docOffset, int docTopN, ScoreMergeMode scoreMergeMode)
        {
            //System.out.println("TopGroups.merge");

            if (shardGroups.Length == 0)
            {
                return null;
            }

            int totalHitCount = 0;
            int totalGroupedHitCount = 0;
            // Optionally merge the totalGroupCount.
            int? totalGroupCount = null;

            int numGroups = shardGroups[0].Groups.Length;
            foreach (var shard in shardGroups)
            {
                if (numGroups != shard.Groups.Length)
                {
                    throw new ArgumentException("number of groups differs across shards; you must pass same top groups to all shards' second-pass collector");
                }
                totalHitCount += shard.TotalHitCount;
                totalGroupedHitCount += shard.TotalGroupedHitCount;
                if (shard.TotalGroupCount != null)
                {
                    if (totalGroupCount == null)
                    {
                        totalGroupCount = 0;
                    }

                    totalGroupCount += shard.TotalGroupCount;
                }
            }
            
            var mergedGroupDocs = new GroupDocs<T>[numGroups];

            TopDocs[] shardTopDocs = new TopDocs[shardGroups.Length];
            float totalMaxScore = float.MinValue;

            for (int groupIDX = 0; groupIDX < numGroups; groupIDX++)
            {
                T groupValue = shardGroups[0].Groups[groupIDX].GroupValue;
                //System.out.println("  merge groupValue=" + groupValue + " sortValues=" + Arrays.toString(shardGroups[0].groups[groupIDX].groupSortValues));
                float maxScore = float.MinValue;
                int totalHits = 0;
                double scoreSum = 0.0;
                for (int shardIdx = 0; shardIdx < shardGroups.Length; shardIdx++)
                {
                    //System.out.println("    shard=" + shardIDX);
                    TopGroups<T> shard = shardGroups[shardIdx];
                    var shardGroupDocs = shard.Groups[groupIDX];
                    if (groupValue == null)
                    {
                        if (shardGroupDocs.GroupValue != null)
                        {
                            throw new ArgumentException("group values differ across shards; you must pass same top groups to all shards' second-pass collector");
                        }
                    }
                    else if (!groupValue.Equals(shardGroupDocs.GroupValue))
                    {
                        throw new ArgumentException("group values differ across shards; you must pass same top groups to all shards' second-pass collector");
                    }

                    /*
                    for(ScoreDoc sd : shardGroupDocs.scoreDocs) {
                      System.out.println("      doc=" + sd.doc);
                    }
                    */

                    shardTopDocs[shardIdx] = new TopDocs(shardGroupDocs.TotalHits, shardGroupDocs.ScoreDocs, shardGroupDocs.MaxScore);
                    maxScore = Math.Max(maxScore, shardGroupDocs.MaxScore);
                    totalHits += shardGroupDocs.TotalHits;
                    scoreSum += shardGroupDocs.Score;
                }

                TopDocs mergedTopDocs = TopDocs.Merge(docSort, docOffset + docTopN, shardTopDocs);

                // Slice;
                ScoreDoc[] mergedScoreDocs;
                if (docOffset == 0)
                {
                    mergedScoreDocs = mergedTopDocs.ScoreDocs;
                }
                else if (docOffset >= mergedTopDocs.ScoreDocs.Length)
                {
                    mergedScoreDocs = new ScoreDoc[0];
                }
                else
                {
                    mergedScoreDocs = new ScoreDoc[mergedTopDocs.ScoreDocs.Length - docOffset];
                    Array.Copy(mergedTopDocs.ScoreDocs, docOffset, mergedScoreDocs, 0, mergedTopDocs.ScoreDocs.Length - docOffset);
                }

                float groupScore;
                switch (scoreMergeMode)
                {
                    case ScoreMergeMode.None:
                        groupScore = float.NaN;
                        break;
                    case ScoreMergeMode.Avg:
                        if (totalHits > 0)
                        {
                            groupScore = (float)(scoreSum / totalHits);
                        }
                        else
                        {
                            groupScore = float.NaN;
                        }
                        break;
                    case ScoreMergeMode.Total:
                        groupScore = (float)scoreSum;
                        break;
                    default:
                        throw new ArgumentException("can't handle ScoreMergeMode " + scoreMergeMode);
                }

                //System.out.println("SHARDS=" + Arrays.toString(mergedTopDocs.shardIndex));
                mergedGroupDocs[groupIDX] = new GroupDocs<T>(groupScore, maxScore, totalHits, mergedScoreDocs, groupValue, shardGroups[0].Groups[groupIDX].GroupSortValues);
                totalMaxScore = Math.Max(totalMaxScore, maxScore);
            }

            if (totalGroupCount != null)
            {
                var result = new TopGroups<T>(groupSort.GetSort(), docSort == null ? null : docSort.GetSort(), totalHitCount, totalGroupedHitCount, mergedGroupDocs, totalMaxScore);
                return new TopGroups<T>(result, totalGroupCount);
            }

            return new TopGroups<T>(groupSort.GetSort(), docSort == null ? null : docSort.GetSort(), totalHitCount, totalGroupedHitCount, mergedGroupDocs, totalMaxScore);
        }
    }
}