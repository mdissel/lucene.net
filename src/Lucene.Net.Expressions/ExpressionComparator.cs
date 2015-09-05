using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Support;

namespace Lucene.Net.Expressions
{
    /// <summary>A custom comparator for sorting documents by an expression</summary>
    internal class ExpressionComparator : FieldComparator<double>
    {
        private readonly double[] values;

        private double bottom;

        private double topValue;

        private ValueSource source;

        private FunctionValues scores;

        private AtomicReaderContext readerContext;

        public ExpressionComparator(ValueSource source, int numHits)
        {
            values = new double[numHits];
            this.source = source;
        }

        // TODO: change FieldComparator.setScorer to throw IOException and remove this try-catch
        public override Scorer Scorer
        {
            set
            {
                base.Scorer = value;
                // TODO: might be cleaner to lazy-init 'source' and set scorer after?

                Debug.Assert(readerContext != null);
                var context = new Dictionary<string, object>();
                Debug.Assert(value != null);
                context["scorer"] = value;
                scores = source.GetValues(context, readerContext);
            }
        }

        public override int Compare(int slot1, int slot2)
        {
            return values[slot1].CompareTo(values[slot2]);
        }

        public override int Bottom
        {
            set { bottom = values[value]; }
        }

        public override object TopValue
        {
            set { topValue = (double)value; }
        }


        public override int CompareBottom(int doc)
        {
            return bottom.CompareTo(scores.DoubleVal(doc));
        }


        public override void Copy(int slot, int doc)
        {
            values[slot] = scores.DoubleVal(doc);
        }


        public override FieldComparator SetNextReader(AtomicReaderContext context)
        {
            this.readerContext = context;
            return this;
        }

        public override IComparable Value(int slot)
        {
            return (values[slot]);
        }


        public override int CompareTop(int doc)
        {
            return topValue.CompareTo(scores.DoubleVal(doc));
        }
    }
}
