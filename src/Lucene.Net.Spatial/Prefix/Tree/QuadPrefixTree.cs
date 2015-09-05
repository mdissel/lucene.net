/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix.Tree
{
    /// <summary>
    /// A
    /// <see cref="SpatialPrefixTree">SpatialPrefixTree</see>
    /// which uses a
    /// <a href="http://en.wikipedia.org/wiki/Quadtree">quad tree</a> in which an
    /// indexed term will be generated for each cell, 'A', 'B', 'C', 'D'.
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public class QuadPrefixTree : SpatialPrefixTree
    {
        public const int MaxLevelsPossible = 50;

        public const int DefaultMaxLevels = 12;

        public readonly double gridH;
        private readonly double gridW;

        internal readonly double[] levelH;

        internal readonly int[] levelN;
        internal readonly int[] levelS;
        internal readonly double[] levelW;
        private readonly double xmax;
        private readonly double xmid;
        private readonly double xmin;
        private readonly double ymax;
        private readonly double ymid;
        private readonly double ymin;

        public QuadPrefixTree(SpatialContext ctx, Rectangle bounds, int maxLevels)
            : base(ctx, maxLevels)
        {
            //not really sure how big this should be
            // side
            // number
            xmin = bounds.GetMinX();
            xmax = bounds.GetMaxX();
            ymin = bounds.GetMinY();
            ymax = bounds.GetMaxY();
            levelW = new double[maxLevels];
            levelH = new double[maxLevels];
            levelS = new int[maxLevels];
            levelN = new int[maxLevels];
            gridW = xmax - xmin;
            gridH = ymax - ymin;
            xmid = xmin + gridW / 2.0;
            ymid = ymin + gridH / 2.0;
            levelW[0] = gridW / 2.0;
            levelH[0] = gridH / 2.0;
            levelS[0] = 2;
            levelN[0] = 4;
            for (int i = 1; i < levelW.Length; i++)
            {
                levelW[i] = levelW[i - 1] / 2.0;
                levelH[i] = levelH[i - 1] / 2.0;
                levelS[i] = levelS[i - 1] * 2;
                levelN[i] = levelN[i - 1] * 4;
            }
        }

        public QuadPrefixTree(SpatialContext ctx)
            : this(ctx, DefaultMaxLevels)
        {
        }

        public QuadPrefixTree(SpatialContext ctx, int maxLevels)
            : this(ctx, ctx.GetWorldBounds(), maxLevels)
        {
        }

        public virtual void PrintInfo(TextWriter @out)
        {
            // Format the number to min 3 integer digits and exactly 5 fraction digits
            const string FORMAT_STR = @"000.00000";
            /*NumberFormat nf = NumberFormat.GetNumberInstance(CultureInfo.Root);
			nf.SetMaximumFractionDigits(5);
			nf.SetMinimumFractionDigits(5);
			nf.SetMinimumIntegerDigits(3);*/
            for (int i = 0; i < maxLevels; i++)
            {
                @out.WriteLine(i + "]\t" + levelW[i].ToString(FORMAT_STR) + "\t" + levelH[i].ToString(FORMAT_STR) + "\t" +
                               levelS[i] + "\t" + (levelS[i] * levelS[i]));
            }
        }

        public override int GetLevelForDistance(double dist)
        {
            if (dist == 0)
            {
                //short circuit
                return maxLevels;
            }
            for (int i = 0; i < maxLevels - 1; i++)
            {
                //note: level[i] is actually a lookup for level i+1
                if (dist > levelW[i] && dist > levelH[i])
                {
                    return i + 1;
                }
            }
            return maxLevels;
        }

        protected internal override Cell GetCell(Point p, int level)
        {
            IList<Cell> cells = new List<Cell>(1);
            Build(xmid, ymid, 0, cells, new StringBuilder(), ctx.MakePoint(p.GetX(), p.GetY()), level);
            return cells[0];
        }

        //note cells could be longer if p on edge
        public override Cell GetCell(string token)
        {
            return new QuadCell(this, token);
        }

        public override Cell GetCell(byte[] bytes, int offset, int len)
        {
            return new QuadCell(this, bytes, offset, len);
        }

        private void Build(double x, double y, int level, IList<Cell> matches, StringBuilder
                                                                                   str, Shape shape, int maxLevel)
        {
            Debug.Assert(str.Length == level);
            double w = levelW[level] / 2;
            double h = levelH[level] / 2;
            // Z-Order
            // http://en.wikipedia.org/wiki/Z-order_%28curve%29
            CheckBattenberg('A', x - w, y + h, level, matches, str, shape, maxLevel);
            CheckBattenberg('B', x + w, y + h, level, matches, str, shape, maxLevel);
            CheckBattenberg('C', x - w, y - h, level, matches, str, shape, maxLevel);
            CheckBattenberg('D', x + w, y - h, level, matches, str, shape, maxLevel);
        }

        // possibly consider hilbert curve
        // http://en.wikipedia.org/wiki/Hilbert_curve
        // http://blog.notdot.net/2009/11/Damn-Cool-Algorithms-Spatial-indexing-with-Quadtrees-and-Hilbert-Curves
        // if we actually use the range property in the query, this could be useful
        private void CheckBattenberg(char c, double cx, double cy, int level, IList<Cell>
                                                                                  matches, StringBuilder str,
                                     Shape shape, int maxLevel)
        {
            Debug.Assert(str.Length == level);
            double w = levelW[level] / 2;
            double h = levelH[level] / 2;
            int strlen = str.Length;
            Rectangle rectangle = ctx.MakeRectangle(cx - w, cx + w, cy - h, cy + h);
            SpatialRelation v = shape.Relate(rectangle);
            if (SpatialRelation.CONTAINS == v)
            {
                str.Append(c);
                //str.append(SpatialPrefixGrid.COVER);
                matches.Add(new QuadCell(this, str.ToString(), v.Transpose()));
            }
            else
            {
                if (SpatialRelation.DISJOINT == v)
                {
                }
                else
                {
                    // nothing
                    // SpatialRelation.WITHIN, SpatialRelation.INTERSECTS
                    str.Append(c);
                    int nextLevel = level + 1;
                    if (nextLevel >= maxLevel)
                    {
                        //str.append(SpatialPrefixGrid.INTERSECTS);
                        matches.Add(new QuadCell(this, str.ToString(), v.Transpose()));
                    }
                    else
                    {
                        Build(cx, cy, nextLevel, matches, str, shape, maxLevel);
                    }
                }
            }
            str.Length = strlen;
        }

        #region Nested type: Factory

        /// <summary>
        /// Factory for creating
        /// <see cref="QuadPrefixTree">QuadPrefixTree</see>
        /// instances with useful defaults
        /// </summary>
        public class Factory : SpatialPrefixTreeFactory
        {
            protected internal override int GetLevelForDistance(double degrees)
            {
                var grid = new QuadPrefixTree(ctx, MaxLevelsPossible);
                return grid.GetLevelForDistance(degrees);
            }

            protected internal override SpatialPrefixTree NewSPT()
            {
                return new QuadPrefixTree(ctx, maxLevels.HasValue ? maxLevels.Value : MaxLevelsPossible);
            }
        }

        #endregion

        #region Nested type: QuadCell

        internal class QuadCell : Cell
        {
            private readonly QuadPrefixTree _enclosing;
            private Shape shape;

            public QuadCell(QuadPrefixTree _enclosing, string token)
                : base(token)
            {
                this._enclosing = _enclosing;
            }

            public QuadCell(QuadPrefixTree _enclosing, string token, SpatialRelation shapeRel
                )
                : base(token)
            {
                this._enclosing = _enclosing;
                this.shapeRel = shapeRel;
            }

            internal QuadCell(QuadPrefixTree _enclosing, byte[] bytes, int off, int len)
                : base(bytes, off, len)
            {
                this._enclosing = _enclosing;
            }

            public override void Reset(byte[] bytes, int off, int len)
            {
                base.Reset(bytes, off, len);
                shape = null;
            }

            protected internal override ICollection<Cell> GetSubCells()
            {
                IList<Cell> cells = new List<Cell>(4);
                cells.Add(new QuadCell(_enclosing, TokenString + "A"));
                cells.Add(new QuadCell(_enclosing, TokenString + "B"));
                cells.Add(new QuadCell(_enclosing, TokenString + "C"));
                cells.Add(new QuadCell(_enclosing, TokenString + "D"));
                return cells;
            }

            public override int GetSubCellsSize()
            {
                return 4;
            }

            public override Cell GetSubCell(Point p)
            {
                return _enclosing.GetCell(p, Level + 1);
            }

            //not performant!
            //cache
            public override Shape GetShape()
            {
                if (shape == null)
                {
                    shape = MakeShape();
                }
                return shape;
            }

            private Rectangle MakeShape()
            {
                string token = TokenString;
                double xmin = _enclosing.xmin;
                double ymin = _enclosing.ymin;
                for (int i = 0; i < token.Length; i++)
                {
                    char c = token[i];
                    if ('A' == c || 'a' == c)
                    {
                        ymin += _enclosing.levelH[i];
                    }
                    else
                    {
                        if ('B' == c || 'b' == c)
                        {
                            xmin += _enclosing.levelW[i];
                            ymin += _enclosing.levelH[i];
                        }
                        else
                        {
                            if ('C' == c || 'c' == c)
                            {
                            }
                            else
                            {
                                // nothing really
                                if ('D' == c || 'd' == c)
                                {
                                    xmin += _enclosing.levelW[i];
                                }
                                else
                                {
                                    throw new Exception("unexpected char: " + c);
                                }
                            }
                        }
                    }
                }
                int len = token.Length;
                double width;
                double height;
                if (len > 0)
                {
                    width = _enclosing.levelW[len - 1];
                    height = _enclosing.levelH[len - 1];
                }
                else
                {
                    width = _enclosing.gridW;
                    height = _enclosing.gridH;
                }
                return _enclosing.ctx.MakeRectangle(xmin, xmin + width, ymin, ymin + height);
            }

            //QuadCell
        }

        #endregion
    }
}