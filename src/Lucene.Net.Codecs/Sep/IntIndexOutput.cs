﻿/*
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

namespace Lucene.Net.Codecs.Sep
{
    using System;
    using Store;

    /// <summary>
    /// Defines basic API for writing ints to an IndexOutput.
    /// IntBlockCodec interacts with this API. @see IntBlockReader.
    /// 
    /// NOTE: block sizes could be variable
    /// 
    /// @lucene.experimental 
    /// </summary>
    /// <remarks>
    /// TODO: We may want tighter integration w/IndexOutput
    /// may give better performance
    /// </remarks>
    public abstract class IntIndexOutput : IDisposable
    {
        protected internal IndexOutput OUTPUT;
        protected internal int _upto;

        /// <summary>
        /// Write an int to the primary file.  The value must be
        /// >= 0.  
        /// </summary>
        public abstract void Write(int v);

        /// <summary>
        /// If you are indexing the primary output file, call
        ///  this and interact with the returned IndexWriter. 
        /// </summary>
        public abstract IntIndexOutputIndex Index();

        public abstract void Dispose();
    }


    /// <summary>Records a single skip-point in the IndexOutput. </summary>
    public abstract class IntIndexOutputIndex
    {

        /// <summary>Internally records the current location </summary>
        public abstract void Mark();

        /// <summary>Copies index from other </summary>
        public abstract void CopyFrom(IntIndexOutputIndex other, bool copyLast);

        /// <summary>
        /// Writes "location" of current output pointer of primary
        ///  output to different output (out) 
        /// </summary>
        public abstract void Write(DataOutput indexOut, bool absolute);
    }

}