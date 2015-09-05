﻿using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

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
     
    internal class TermsIncludingScoreQuery : Query
    {
        private readonly string _field;
        private readonly bool _multipleValuesPerDocument;
        private readonly BytesRefHash _terms;
        private readonly float[] _scores;
        private readonly int[] _ords;
        private readonly Query _originalQuery;
        private readonly Query _unwrittenOriginalQuery;

        internal TermsIncludingScoreQuery(string field, bool multipleValuesPerDocument, BytesRefHash terms,
            float[] scores, Query originalQuery)
        {
            _field = field;
            _multipleValuesPerDocument = multipleValuesPerDocument;
            _terms = terms;
            _scores = scores;
            _originalQuery = originalQuery;
            _ords = terms.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
            _unwrittenOriginalQuery = originalQuery;
        }

        private TermsIncludingScoreQuery(string field, bool multipleValuesPerDocument, BytesRefHash terms,
            float[] scores, int[] ords, Query originalQuery, Query unwrittenOriginalQuery)
        {
            _field = field;
            _multipleValuesPerDocument = multipleValuesPerDocument;
            _terms = terms;
            _scores = scores;
            _originalQuery = originalQuery;
            _ords = ords;
            _unwrittenOriginalQuery = unwrittenOriginalQuery;
        }

        public override string ToString(string @string)
        {
            return string.Format("TermsIncludingScoreQuery{{field={0};originalQuery={1}}}", _field,
                _unwrittenOriginalQuery);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            _originalQuery.ExtractTerms(terms);
        }

        public override Query Rewrite(IndexReader reader)
        {
            Query originalQueryRewrite = _originalQuery.Rewrite(reader);
            if (originalQueryRewrite != _originalQuery)
            {
                Query rewritten = new TermsIncludingScoreQuery(_field, _multipleValuesPerDocument, _terms, _scores,
                    _ords, originalQueryRewrite, _originalQuery);
                rewritten.Boost = Boost;
                return rewritten;
            }

            return this;
        }

        protected bool Equals(TermsIncludingScoreQuery other)
        {
            return base.Equals(other) && string.Equals(_field, other._field) &&
                   Equals(_unwrittenOriginalQuery, other._unwrittenOriginalQuery);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TermsIncludingScoreQuery) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode*397) ^ (_field != null ? _field.GetHashCode() : 0);
                hashCode = (hashCode*397) ^
                           (_unwrittenOriginalQuery != null ? _unwrittenOriginalQuery.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            Weight originalWeight = _originalQuery.CreateWeight(searcher);
            return new WeightAnonymousInnerClassHelper(this, originalWeight);
        }

        private class WeightAnonymousInnerClassHelper : Weight
        {
            private readonly TermsIncludingScoreQuery outerInstance;

            private Weight originalWeight;

            public WeightAnonymousInnerClassHelper(TermsIncludingScoreQuery outerInstance, Weight originalWeight)
            {
                this.outerInstance = outerInstance;
                this.originalWeight = originalWeight;
            }


            private TermsEnum segmentTermsEnum;
            
            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                SVInnerScorer scorer = (SVInnerScorer) BulkScorer(context, false, null);
                if (scorer != null)
                {
                    return scorer.Explain(doc);
                }
                return new ComplexExplanation(false, 0.0f, "Not a match");
            }

            public override bool ScoresDocsOutOfOrder()
            {
                // We have optimized impls below if we are allowed
                // to score out-of-order:
                return true;
            }

            public override Query Query
            {
                get { return outerInstance; }
            }
            
            public override float ValueForNormalization
            {
                get { return originalWeight.ValueForNormalization*outerInstance.Boost*outerInstance.Boost; }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                originalWeight.Normalize(norm, topLevelBoost*outerInstance.Boost);
            }
            
            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                Terms terms = context.AtomicReader.Terms(outerInstance._field);
                if (terms == null)
                {
                    return null;
                }

                // what is the runtime...seems ok?
                long cost = context.AtomicReader.MaxDoc * terms.Size();

                segmentTermsEnum = terms.Iterator(segmentTermsEnum);
                if (outerInstance._multipleValuesPerDocument)
                {
                    return new MVInOrderScorer(outerInstance, this, acceptDocs, segmentTermsEnum, context.AtomicReader.MaxDoc, cost);
                }

                return new SVInOrderScorer(outerInstance, this, acceptDocs, segmentTermsEnum, context.AtomicReader.MaxDoc, cost);
            }
            
            public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, Bits acceptDocs)
            {
                if (scoreDocsInOrder)
                {
                    return base.BulkScorer(context, scoreDocsInOrder, acceptDocs);
                }

                Terms terms = context.AtomicReader.Terms(outerInstance._field);
                if (terms == null)
                {
                    return null;
                }
                // what is the runtime...seems ok?
                long cost = context.AtomicReader.MaxDoc * terms.Size();

                segmentTermsEnum = terms.Iterator(segmentTermsEnum);
                // Optimized impls that take advantage of docs
                // being allowed to be out of order:
                if (outerInstance._multipleValuesPerDocument)
                {
                    return new MVInnerScorer(outerInstance, this, acceptDocs, segmentTermsEnum, context.AtomicReader.MaxDoc, cost);
                }

                return new SVInnerScorer(outerInstance, this, acceptDocs, segmentTermsEnum, cost);
            }
        }

        // This impl assumes that the 'join' values are used uniquely per doc per field. Used for one to many relations.
        internal class SVInnerScorer : BulkScorer
        {
            private readonly TermsIncludingScoreQuery outerInstance;

            private readonly BytesRef _spare = new BytesRef();
            private readonly Bits _acceptDocs;
            private readonly TermsEnum _termsEnum;
            private readonly long _cost;

            private int _upto;
            internal DocsEnum DocsEnum;
            private DocsEnum _reuse;
            private int _scoreUpto;
            private int _doc;

            internal SVInnerScorer(TermsIncludingScoreQuery outerInstance, Weight weight, Bits acceptDocs, TermsEnum termsEnum, long cost)
            {
                this.outerInstance = outerInstance;
                _acceptDocs = acceptDocs;
                _termsEnum = termsEnum;
                _cost = cost;
                _doc = -1;
            }
            
            public override bool Score(Collector collector, int max)
            {
                FakeScorer fakeScorer = new FakeScorer();
                collector.Scorer = fakeScorer;
                if (_doc == -1)
                {
                    _doc = NextDocOutOfOrder();
                }
                while (_doc < max)
                {
                    fakeScorer.doc = _doc;
                    fakeScorer._score = outerInstance._scores[outerInstance._ords[_scoreUpto]];
                    collector.Collect(_doc);
                    _doc = NextDocOutOfOrder();
                }

                return _doc != DocIdSetIterator.NO_MORE_DOCS;
            }

            private int NextDocOutOfOrder()
            {
                while (true)
                {
                    if (DocsEnum != null)
                    {
                        int docId = DocsEnumNextDoc();
                        if (docId == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            DocsEnum = null;
                        }
                        else
                        {
                            return _doc = docId;
                        }
                    }

                    if (_upto == outerInstance._terms.Size())
                    {
                        return _doc = DocIdSetIterator.NO_MORE_DOCS;
                    }

                    _scoreUpto = _upto;
                    if (_termsEnum.SeekExact(outerInstance._terms.Get(outerInstance._ords[_upto++], _spare)))
                    {
                        DocsEnum = _reuse = _termsEnum.Docs(_acceptDocs, _reuse, DocsEnum.FLAG_NONE);
                    }
                }
            }
            
            protected virtual int DocsEnumNextDoc()
            {
                return DocsEnum.NextDoc();
            }
            
            internal Explanation Explain(int target)
            {
                int docId;
                do
                {
                    docId = NextDocOutOfOrder();
                    if (docId < target)
                    {
                        int tempDocId = DocsEnum.Advance(target);
                        if (tempDocId == target)
                        {
                            docId = tempDocId;
                            break;
                        }
                    }
                    else if (docId == target)
                    {
                        break;
                    }
                    DocsEnum = null; // goto the next ord.
                } while (docId != DocIdSetIterator.NO_MORE_DOCS);

                return new ComplexExplanation(true, outerInstance._scores[outerInstance._ords[_scoreUpto]],
                    "Score based on join value " + _termsEnum.Term().Utf8ToString());
            }
        }

        // This impl that tracks whether a docid has already been emitted. This check makes sure that docs aren't emitted
        // twice for different join values. This means that the first encountered join value determines the score of a document
        // even if other join values yield a higher score.
        internal class MVInnerScorer : SVInnerScorer
        {
            private readonly TermsIncludingScoreQuery outerInstance;


            internal readonly FixedBitSet alreadyEmittedDocs;

            internal MVInnerScorer(TermsIncludingScoreQuery outerInstance, Weight weight, Bits acceptDocs,
                TermsEnum termsEnum, int maxDoc, long cost) : base(outerInstance, weight, acceptDocs, termsEnum, cost)
            {
                this.outerInstance = outerInstance;
                alreadyEmittedDocs = new FixedBitSet(maxDoc);
            }
            
            protected override int DocsEnumNextDoc()
            {
                while (true)
                {
                    int docId = DocsEnum.NextDoc();
                    if (docId == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        return docId;
                    }
                    if (!alreadyEmittedDocs.GetAndSet(docId))
                    {
                        return docId; //if it wasn't previously set, return it
                    }
                }
            }
        }

        internal class SVInOrderScorer : Scorer
        {
            private readonly TermsIncludingScoreQuery outerInstance;


            internal readonly DocIdSetIterator matchingDocsIterator;
            internal readonly float[] scores;
            internal readonly long cost_Renamed;

            internal int currentDoc = -1;
            
            internal SVInOrderScorer(TermsIncludingScoreQuery outerInstance, Weight weight, Bits acceptDocs,
                TermsEnum termsEnum, int maxDoc, long cost) : base(weight)
            {
                this.outerInstance = outerInstance;
                FixedBitSet matchingDocs = new FixedBitSet(maxDoc);
                scores = new float[maxDoc];
                FillDocsAndScores(matchingDocs, acceptDocs, termsEnum);
                matchingDocsIterator = matchingDocs.GetIterator();
                cost_Renamed = cost;
            }
            
            protected virtual void FillDocsAndScores(FixedBitSet matchingDocs, Bits acceptDocs,
                TermsEnum termsEnum)
            {
                BytesRef spare = new BytesRef();
                DocsEnum docsEnum = null;
                for (int i = 0; i < outerInstance._terms.Size(); i++)
                {
                    if (termsEnum.SeekExact(outerInstance._terms.Get(outerInstance._ords[i], spare)))
                    {
                        docsEnum = termsEnum.Docs(acceptDocs, docsEnum, FLAG_NONE);
                        float score = outerInstance._scores[outerInstance._ords[i]];
                        for (int doc = docsEnum.NextDoc();
                            doc != NO_MORE_DOCS;
                            doc = docsEnum.NextDoc())
                        {
                            matchingDocs.Set(doc);
                            // In the case the same doc is also related to a another doc, a score might be overwritten. I think this
                            // can only happen in a many-to-many relation
                            scores[doc] = score;
                        }
                    }
                }
            }
            
            public override float Score()
            {
                return scores[currentDoc];
            }
            
            public override int Freq()
            {
                return 1;
            }

            public override int DocID()
            {
                return currentDoc;
            }
            
            public override int NextDoc()
            {
                return currentDoc = matchingDocsIterator.NextDoc();
            }
            
            public override int Advance(int target)
            {
                return currentDoc = matchingDocsIterator.Advance(target);
            }

            public override long Cost()
            {
                return cost_Renamed;
            }
        }

        // This scorer deals with the fact that a document can have more than one score from multiple related documents.
        internal class MVInOrderScorer : SVInOrderScorer
        {
            private readonly TermsIncludingScoreQuery outerInstance;

            
            internal MVInOrderScorer(TermsIncludingScoreQuery outerInstance, Weight weight, Bits acceptDocs,
                TermsEnum termsEnum, int maxDoc, long cost)
                : base(outerInstance, weight, acceptDocs, termsEnum, maxDoc, cost)
            {
                this.outerInstance = outerInstance;
            }
            
            protected override void FillDocsAndScores(FixedBitSet matchingDocs, Bits acceptDocs,
                TermsEnum termsEnum)
            {
                BytesRef spare = new BytesRef();
                DocsEnum docsEnum = null;
                for (int i = 0; i < outerInstance._terms.Size(); i++)
                {
                    if (termsEnum.SeekExact(outerInstance._terms.Get(outerInstance._ords[i], spare)))
                    {
                        docsEnum = termsEnum.Docs(acceptDocs, docsEnum, FLAG_NONE);
                        float score = outerInstance._scores[outerInstance._ords[i]];
                        for (int doc = docsEnum.NextDoc();
                            doc != NO_MORE_DOCS;
                            doc = docsEnum.NextDoc())
                        {
                            // I prefer this:
                            /*if (scores[doc] < score) {
                              scores[doc] = score;
                              matchingDocs.set(doc);
                            }*/
                            // But this behaves the same as MVInnerScorer and only then the tests will pass:
                            if (!matchingDocs.Get(doc))
                            {
                                scores[doc] = score;
                                matchingDocs.Set(doc);
                            }
                        }
                    }
                }
            }
        }
    }
}