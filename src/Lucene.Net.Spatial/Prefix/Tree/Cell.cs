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
using System.Runtime.CompilerServices;
using System.Text;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix.Tree
{
    /// <summary>Represents a grid cell.</summary>
    /// <remarks>
    /// Represents a grid cell. These are not necessarily thread-safe, although new
    /// Cell("") (world cell) must be.
    /// </remarks>
    /// <lucene.experimental></lucene.experimental>
    public abstract class Cell : IComparable<Cell>
    {
        public const byte LEAF_BYTE = (byte)('+');//NOTE: must sort before letters & numbers

        /*
        Holds a byte[] and/or String representation of the cell. Both are lazy constructed from the other.
        Neither contains the trailing leaf byte.
        */
        private byte[] bytes;
        private int b_off;
        private int b_len;

        /// <summary>Always false for points.</summary>
        /// <remarks>
        /// Always false for points. Otherwise, indicate no further sub-cells are going
        /// to be provided because shapeRel is WITHIN or maxLevels or a detailLevel is
        /// hit.
        /// </remarks>
        protected internal bool leaf;

        /// <summary>
        /// When set via getSubCells(filter), it is the relationship between this cell
        /// and the given shape filter.
        /// </summary>
        /// <remarks>
        /// When set via getSubCells(filter), it is the relationship between this cell
        /// and the given shape filter.
        /// </remarks>
        protected internal SpatialRelation shapeRel = SpatialRelation.NULL_VALUE;//set in getSubCells(filter), and via setLeaf().

        private string token;//this is the only part of equality

        protected internal Cell(string token)
        {
            //NOTE: must sort before letters & numbers
            //this is the only part of equality
            this.token = token;
            if (token.Length > 0 && token[token.Length - 1] == (char)LEAF_BYTE)
            {
                this.token = token.Substring(0, token.Length - 1);
                SetLeaf();
            }
            if (Level == 0)
            {
                GetShape();//ensure any lazy instantiation completes to make this threadsafe
            }
        }

        protected internal Cell(byte[] bytes, int off, int len)
        {
            //ensure any lazy instantiation completes to make this threadsafe
            this.bytes = bytes;
            b_off = off;
            b_len = len;
            B_fixLeaf();
        }

        #region IComparable<Cell> Members

        public virtual int CompareTo(Cell o)
        {
            return string.CompareOrdinal(TokenString, o.TokenString);
        }

        #endregion

        public virtual void Reset(byte[] bytes, int off, int len)
        {
            Debug.Assert(Level != 0);
            token = null;
            shapeRel = SpatialRelation.NULL_VALUE;
            this.bytes = bytes;
            b_off = off;
            b_len = len;
            B_fixLeaf();
        }

        public virtual void Reset(string token)
        {
            Debug.Assert(Level != 0);
            this.token = token;
            shapeRel = SpatialRelation.NULL_VALUE;

            //converting string t0 byte[]
            //bytes = Encoding.UTF8.GetBytes(token);
            BytesRef utf8Result = new BytesRef(token.Length);
            UnicodeUtil.UTF16toUTF8(token.ToCharArray(), 0, token.Length, utf8Result);
            bytes = utf8Result.bytes.ToByteArray();

            b_off = 0;
            b_len = bytes.Length;
            B_fixLeaf();
        }

        private void B_fixLeaf()
        {
            //note that non-point shapes always have the maxLevels cell set with setLeaf
            if (bytes[b_off + b_len - 1] == LEAF_BYTE)
            {
                b_len--;
                SetLeaf();
            }
            else
            {
                leaf = false;
            }
        }

        public virtual SpatialRelation GetShapeRel()
        {
            return shapeRel;
        }

        /// <summary>For points, this is always false.</summary>
        /// <remarks>
        /// For points, this is always false.  Otherwise this is true if there are no
        /// further cells with this prefix for the shape (always true at maxLevels).
        /// </remarks>
        public virtual bool IsLeaf()
        {
            return leaf;
        }

        /// <summary>Note: not supported at level 0.</summary>
        /// <remarks>Note: not supported at level 0.</remarks>
        public virtual void SetLeaf()
        {
            Debug.Assert(Level != 0);
            leaf = true;
        }

        /*
         * Note: doesn't contain a trailing leaf byte.
         */
        public virtual String TokenString
        {
            get
            {
                if (token == null)
                    throw new InvalidOperationException("Somehow we got a null token");
                return token;
            }
        }

        /// <summary>Note: doesn't contain a trailing leaf byte.</summary>
        /// <remarks>Note: doesn't contain a trailing leaf byte.</remarks>
        public virtual byte[] GetTokenBytes()
        {
            if (bytes != null)
            {
                if (b_off != 0 || b_len != bytes.Length)
                {
                    throw new InvalidOperationException("Not supported if byte[] needs to be recreated.");
                }
            }
            else
            {
                //converting string t0 byte[]
                //bytes = Encoding.UTF8.GetBytes(token);
                BytesRef utf8Result = new BytesRef(token.Length);
                UnicodeUtil.UTF16toUTF8(token.ToCharArray(), 0, token.Length, utf8Result);
                bytes = utf8Result.bytes.ToByteArray();
                b_off = 0;
                b_len = bytes.Length;
            }
            return bytes;
        }

        public virtual int Level
        {
            get
            {
                return token.Length;
                //return token != null ? token.Length : b_len;
            }
        }

        //TODO add getParent() and update some algorithms to use this?
        //public Cell getParent();
        /// <summary>
        /// Like
        /// <see cref="GetSubCells()">GetSubCells()</see>
        /// but with the results filtered by a shape. If
        /// that shape is a
        /// <see cref="Point">Spatial4n.Core.Shapes.Point</see>
        /// then it must call
        /// <see cref="GetSubCell(Point)">GetSubCell(Spatial4n.Core.Shapes.Point)
        /// 	</see>
        /// . The returned cells
        /// should have
        /// <see cref="GetShapeRel()">GetShapeRel()</see>
        /// set to their relation with
        /// <code>shapeFilter</code>
        /// . In addition,
        /// <see cref="IsLeaf()">IsLeaf()</see>
        /// must be true when that relation is WITHIN.
        /// <p/>
        /// Precondition: Never called when getLevel() == maxLevel.
        /// </summary>
        /// <param name="shapeFilter">an optional filter for the returned cells.</param>
        /// <returns>A set of cells (no dups), sorted. Not Modifiable.</returns>
        public virtual ICollection<Cell> GetSubCells(Shape shapeFilter)
        {
            //Note: Higher-performing subclasses might override to consider the shape filter to generate fewer cells.
            if (shapeFilter is Point)
            {
                Cell subCell = GetSubCell((Point)shapeFilter);
                subCell.shapeRel = SpatialRelation.CONTAINS;
#if !NET35
                return new ReadOnlyCollectionBuilder<Cell>(new[] { subCell }).ToReadOnlyCollection();
#else
                return new List<Cell>(new[] { subCell }).AsReadOnly();
#endif
            }

            ICollection<Cell> cells = GetSubCells();
            if (shapeFilter == null)
            {
                return cells;
            }
            //TODO change API to return a filtering iterator
            IList<Cell> copy = new List<Cell>(cells.Count);
            foreach (Cell cell in cells)
            {
                SpatialRelation rel = cell.GetShape().Relate(shapeFilter);
                if (rel == SpatialRelation.DISJOINT)
                {
                    continue;
                }
                cell.shapeRel = rel;
                if (rel == SpatialRelation.WITHIN)
                {
                    cell.SetLeaf();
                }
                copy.Add(cell);
            }
            return copy;
        }

        /// <summary>
        /// Performant implementations are expected to implement this efficiently by
        /// considering the current cell's boundary.
        /// </summary>
        /// <remarks>
        /// Performant implementations are expected to implement this efficiently by
        /// considering the current cell's boundary. Precondition: Never called when
        /// getLevel() == maxLevel.
        /// <p/>
        /// Precondition: this.getShape().relate(p) != DISJOINT.
        /// </remarks>
        public abstract Cell GetSubCell(Point p);

        //TODO Cell getSubCell(byte b)
        /// <summary>Gets the cells at the next grid cell level that cover this cell.</summary>
        /// <remarks>
        /// Gets the cells at the next grid cell level that cover this cell.
        /// Precondition: Never called when getLevel() == maxLevel.
        /// </remarks>
        /// <returns>A set of cells (no dups), sorted, modifiable, not empty, not null.</returns>
        protected internal abstract ICollection<Cell> GetSubCells();

        /// <summary>
        /// <see cref="GetSubCells()">GetSubCells()</see>
        /// .size() -- usually a constant. Should be &gt;=2
        /// </summary>
        public abstract int GetSubCellsSize();

        public abstract Shape GetShape();

        public virtual Point GetCenter()
        {
            return GetShape().GetCenter();
        }

        #region Equality overrides

        public override bool Equals(object obj)
        {
            return !(obj == null || !(obj is Cell)) &&
                   TokenString.Equals(((Cell)obj).TokenString);
        }

        public override int GetHashCode()
        {
            return TokenString.GetHashCode();
        }

        public override string ToString()
        {
            return TokenString + (IsLeaf() ? ((char)LEAF_BYTE).ToString() : string.Empty);
        }

        #endregion

    }
}