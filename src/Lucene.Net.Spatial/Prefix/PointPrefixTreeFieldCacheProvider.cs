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
using Lucene.Net.Index;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
    /// <summary>
    /// Implementation of
    /// <see cref="Lucene.Net.Spatial.Util.ShapeFieldCacheProvider{T}">Lucene.Net.Spatial.Util.ShapeFieldCacheProvider&lt;T&gt;
    /// 	</see>
    /// designed for
    /// <see cref="PrefixTreeStrategy">PrefixTreeStrategy</see>
    /// s.
    /// Note, due to the fragmented representation of Shapes in these Strategies, this implementation
    /// can only retrieve the central
    /// <see cref="Point">Point</see>
    /// of the original Shapes.
    /// </summary>
    /// <lucene.internal></lucene.internal>
    public class PointPrefixTreeFieldCacheProvider : ShapeFieldCacheProvider<Point>
    {
        internal readonly SpatialPrefixTree grid;

        public PointPrefixTreeFieldCacheProvider(SpatialPrefixTree grid, string shapeField
            , int defaultSize)
            : base(shapeField, defaultSize)
        {
            //
            this.grid = grid;
        }

        private Cell scanCell = null;

        //re-used in readShape to save GC
        protected internal override Point ReadShape(BytesRef term)
        {
            scanCell = grid.GetCell(term.bytes.ToByteArray(), term.offset, term.length, scanCell);
            if (scanCell.Level == grid.MaxLevels && !scanCell.IsLeaf())
            {
                return scanCell.GetCenter();
            }
            return null;
        }
    }
}
