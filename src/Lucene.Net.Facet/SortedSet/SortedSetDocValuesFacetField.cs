﻿namespace Lucene.Net.Facet.SortedSet
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

    using Field = Lucene.Net.Documents.Field;
    using FieldType = Lucene.Net.Documents.FieldType;

    /// <summary>
    /// Add an instance of this to your Document for every facet
    ///  label to be indexed via SortedSetDocValues. 
    /// </summary>
    public class SortedSetDocValuesFacetField : Field
    {

        /// <summary>
        /// Indexed <seealso cref="FieldType"/>. </summary>
        public static readonly FieldType TYPE = new FieldType();
        static SortedSetDocValuesFacetField()
        {
            TYPE.Indexed = true;
            TYPE.Freeze();
        }

        /// <summary>
        /// Dimension. </summary>
        public readonly string Dim;

        /// <summary>
        /// Label. </summary>
        public readonly string Label;

        /// <summary>
        /// Sole constructor. </summary>
        public SortedSetDocValuesFacetField(string dim, string label)
            : base("dummy", TYPE)
        {
            FacetField.VerifyLabel(label);
            FacetField.VerifyLabel(dim);
            this.Dim = dim;
            this.Label = label;
        }

        public override string ToString()
        {
            return "SortedSetDocValuesFacetField(dim=" + Dim + " label=" + Label + ")";
        }
    }

}