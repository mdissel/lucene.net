using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Support.Compatibility;

namespace Lucene.Net.Util
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
    /// On-disk sorting of byte arrays. Each byte array (entry) is a composed of the following
    /// fields:
    /// <ul>
    ///   <li>(two bytes) length of the following byte array,
    ///   <li>exactly the above count of bytes for the sequence to be sorted.
    /// </ul>
    /// </summary>
    public sealed class OfflineSorter
    {
        // LUCENENET TODO: keep this code as it will be used by subprojects once ported
        //        private bool InstanceFieldsInitialized = false;
        //
        //        private void InitializeInstanceFields()
        //        {
        //            Buffer = new BytesRefArray(BufferBytesUsed);
        //        }
        //
        //        /// <summary>
        //        /// Convenience constant for megabytes </summary>
        //        public const long MB = 1024 * 1024;
        //        /// <summary>
        //        /// Convenience constant for gigabytes </summary>
        //        public static readonly long GB = MB * 1024;
        //
        //        /// <summary>
        //        /// Minimum recommended buffer size for sorting.
        //        /// </summary>
        //        public const long MIN_BUFFER_SIZE_MB = 32;
        //
        //        /// <summary>
        //        /// Absolute minimum required buffer size for sorting.
        //        /// </summary>
        //        public static readonly long ABSOLUTE_MIN_SORT_BUFFER_SIZE = MB / 2;
        //        private const string MIN_BUFFER_SIZE_MSG = "At least 0.5MB RAM buffer is needed";
        //
        //        /// <summary>
        //        /// Maximum number of temporary files before doing an intermediate merge.
        //        /// </summary>
        //        public const int MAX_TEMPFILES = 128;
        //
        //        /// <summary>
        //        /// A bit more descriptive unit for constructors.
        //        /// </summary>
        //        /// <seealso cref= #automatic() </seealso>
        //        /// <seealso cref= #megabytes(long) </seealso>
        //        public sealed class BufferSize
        //        {
        //            internal readonly int Bytes;
        //
        //            internal BufferSize(long bytes)
        //            {
        //                if (bytes > int.MaxValue)
        //                {
        //                    throw new System.ArgumentException("Buffer too large for Java (" + (int.MaxValue / MB) + "mb max): " + bytes);
        //                }
        //
        //                if (bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
        //                {
        //                    throw new System.ArgumentException(MIN_BUFFER_SIZE_MSG + ": " + bytes);
        //                }
        //
        //                this.Bytes = (int)bytes;
        //            }
        //
        //            /// <summary>
        //            /// Creates a <seealso cref="BufferSize"/> in MB. The given
        //            /// values must be &gt; 0 and &lt; 2048.
        //            /// </summary>
        //            public static BufferSize Megabytes(long mb)
        //            {
        //                return new BufferSize(mb * MB);
        //            }
        //
        //            /// <summary>
        //            /// Approximately half of the currently available free heap, but no less
        //            /// than <seealso cref="#ABSOLUTE_MIN_SORT_BUFFER_SIZE"/>. However if current heap allocation
        //            /// is insufficient or if there is a large portion of unallocated heap-space available
        //            /// for sorting consult with max allowed heap size.
        //            /// </summary>
        //            public static BufferSize Automatic()
        //            {
        //                var proc = Process.GetCurrentProcess();
        //
        //                // take sizes in "conservative" order
        //                long max = proc.PeakVirtualMemorySize64; // max allocated; java has it as Runtime.maxMemory();
        //                long total = proc.VirtualMemorySize64; // currently allocated; java has it as Runtime.totalMemory();
        //                long free = rt.freeMemory(); // unused portion of currently allocated
        //                long totalAvailableBytes = max - total + free;
        //
        //                // by free mem (attempting to not grow the heap for this)
        //                long sortBufferByteSize = free / 2;
        //                const long minBufferSizeBytes = MIN_BUFFER_SIZE_MB * MB;
        //                if (sortBufferByteSize < minBufferSizeBytes || totalAvailableBytes > 10 * minBufferSizeBytes) // lets see if we need/should to grow the heap
        //                {
        //                    if (totalAvailableBytes / 2 > minBufferSizeBytes) // there is enough mem for a reasonable buffer
        //                    {
        //                        sortBufferByteSize = totalAvailableBytes / 2; // grow the heap
        //                    }
        //                    else
        //                    {
        //                        //heap seems smallish lets be conservative fall back to the free/2
        //                        sortBufferByteSize = Math.Max(ABSOLUTE_MIN_SORT_BUFFER_SIZE, sortBufferByteSize);
        //                    }
        //                }
        //                return new BufferSize(Math.Min((long)int.MaxValue, sortBufferByteSize));
        //            }
        //        }
        //
        //        /// <summary>
        //        /// Sort info (debugging mostly).
        //        /// </summary>
        //        public class SortInfo
        //        {
        //            internal bool InstanceFieldsInitialized = false;
        //
        //            internal virtual void InitializeInstanceFields()
        //            {
        //                BufferSize = OuterInstance.RamBufferSize.Bytes;
        //            }
        //
        //            private readonly OfflineSorter OuterInstance;
        //
        //            /// <summary>
        //            /// number of temporary files created when merging partitions </summary>
        //            public int TempMergeFiles;
        //            /// <summary>
        //            /// number of partition merges </summary>
        //            public int MergeRounds;
        //            /// <summary>
        //            /// number of lines of data read </summary>
        //            public int Lines;
        //            /// <summary>
        //            /// time spent merging sorted partitions (in milliseconds) </summary>
        //            public long MergeTime;
        //            /// <summary>
        //            /// time spent sorting data (in milliseconds) </summary>
        //            public long SortTime;
        //            /// <summary>
        //            /// total time spent (in milliseconds) </summary>
        //            public long TotalTime;
        //            /// <summary>
        //            /// time spent in i/o read (in milliseconds) </summary>
        //            public long ReadTime;
        //            /// <summary>
        //            /// read buffer size (in bytes) </summary>
        //            public long BufferSize;
        //
        //            /// <summary>
        //            /// create a new SortInfo (with empty statistics) for debugging </summary>
        //            public SortInfo(OfflineSorter outerInstance)
        //            {
        //                this.OuterInstance = outerInstance;
        //
        //                if (!InstanceFieldsInitialized)
        //                {
        //                    InitializeInstanceFields();
        //                    InstanceFieldsInitialized = true;
        //                }
        //            }
        //
        //            public override string ToString()
        //            {
        //                return string.Format("time=%.2f sec. total (%.2f reading, %.2f sorting, %.2f merging), lines=%d, temp files=%d, merges=%d, soft ram limit=%.2f MB", TotalTime / 1000.0d, ReadTime / 1000.0d, SortTime / 1000.0d, MergeTime / 1000.0d, Lines, TempMergeFiles, MergeRounds, (double)BufferSize / MB);
        //            }
        //        }
        //
        //        private readonly BufferSize RamBufferSize;
        //
        //        private readonly Counter BufferBytesUsed = Counter.NewCounter();
        //        private BytesRefArray Buffer;
        //        private SortInfo sortInfo;
        //        private readonly int MaxTempFiles;
        //        private readonly IComparer<BytesRef> comparator;
        //
        //        /// <summary>
        //        /// Default comparator: sorts in binary (codepoint) order </summary>
        //        public static readonly IComparer<BytesRef> DEFAULT_COMPARATOR = BytesRef.UTF8SortedAsUnicodeComparator.Instance;
        //
        //        /// <summary>
        //        /// Defaults constructor.
        //        /// </summary>
        //        /// <seealso cref= #defaultTempDir() </seealso>
        //        /// <seealso cref= BufferSize#automatic() </seealso>
        //        public OfflineSorter()
        //            : this(DEFAULT_COMPARATOR, BufferSize.Automatic(), DefaultTempDir(), MAX_TEMPFILES)
        //        {
        //            if (!InstanceFieldsInitialized)
        //            {
        //                InitializeInstanceFields();
        //                InstanceFieldsInitialized = true;
        //            }
        //        }
        //
        //        /// <summary>
        //        /// Defaults constructor with a custom comparator.
        //        /// </summary>
        //        /// <seealso cref= #defaultTempDir() </seealso>
        //        /// <seealso cref= BufferSize#automatic() </seealso>
        //        public OfflineSorter(IComparer<BytesRef> comparator)
        //            : this(comparator, BufferSize.Automatic(), DefaultTempDir(), MAX_TEMPFILES)
        //        {
        //            if (!InstanceFieldsInitialized)
        //            {
        //                InitializeInstanceFields();
        //                InstanceFieldsInitialized = true;
        //            }
        //        }
        //
        //        /// <summary>
        //        /// All-details constructor.
        //        /// </summary>
        //        public OfflineSorter(IComparer<BytesRef> comparator, BufferSize ramBufferSize, /*DirectoryInfo tempDirectory,*/ int maxTempfiles)
        //        {
        //            if (!InstanceFieldsInitialized)
        //            {
        //                InitializeInstanceFields();
        //                InstanceFieldsInitialized = true;
        //            }
        //            if (ramBufferSize.Bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
        //            {
        //                throw new System.ArgumentException(MIN_BUFFER_SIZE_MSG + ": " + ramBufferSize.Bytes);
        //            }
        //
        //            if (maxTempfiles < 2)
        //            {
        //                throw new System.ArgumentException("maxTempFiles must be >= 2");
        //            }
        //
        //            this.RamBufferSize = ramBufferSize;
        //            this.MaxTempFiles = maxTempfiles;
        //            this.comparator = comparator;
        //        }
        //
        //        /// <summary>
        //        /// Sort input to output, explicit hint for the buffer size. The amount of allocated
        //        /// memory may deviate from the hint (may be smaller or larger).
        //        /// </summary>
        //        public SortInfo Sort(FileInfo input, FileInfo output)
        //        {
        //            sortInfo = new SortInfo(this) {TotalTime = DateTime.Now.Millisecond};
        //
        //            output.Delete();
        //
        //            var merges = new List<FileInfo>();
        //            bool success2 = false;
        //            try
        //            {
        //                var inputStream = new ByteSequencesReader(input);
        //                bool success = false;
        //                try
        //                {
        //                    int lines = 0;
        //                    while ((lines = ReadPartition(inputStream)) > 0)
        //                    {
        //                        merges.Add(SortPartition(lines));
        //                        sortInfo.TempMergeFiles++;
        //                        sortInfo.Lines += lines;
        //
        //                        // Handle intermediate merges.
        //                        if (merges.Count == MaxTempFiles)
        //                        {
        //                            var intermediate = new FileInfo(Path.GetTempFileName());
        //                            try
        //                            {
        //                                MergePartitions(merges, intermediate);
        //                            }
        //                            finally
        //                            {
        //                                foreach (var file in merges)
        //                                {
        //                                    file.Delete();
        //                                }
        //                                merges.Clear();
        //                                merges.Add(intermediate);
        //                            }
        //                            sortInfo.TempMergeFiles++;
        //                        }
        //                    }
        //                    success = true;
        //                }
        //                finally
        //                {
        //                    if (success)
        //                    {
        //                        IOUtils.Close(inputStream);
        //                    }
        //                    else
        //                    {
        //                        IOUtils.CloseWhileHandlingException(inputStream);
        //                    }
        //                }
        //
        //                // One partition, try to rename or copy if unsuccessful.
        //                if (merges.Count == 1)
        //                {
        //                    FileInfo single = merges[0];
        //                    Copy(single, output);
        //                    try
        //                    {
        //                        File.Delete(single.FullName);
        //                    }
        //                    catch (Exception)
        //                    {
        //                        // ignored
        //                    }
        //                }
        //                else
        //                {
        //                    // otherwise merge the partitions with a priority queue.
        //                    MergePartitions(merges, output);
        //                }
        //                success2 = true;
        //            }
        //            finally
        //            {
        //                foreach (FileInfo file in merges)
        //                {
        //                    file.Delete();
        //                }
        //                if (!success2)
        //                {
        //                    output.Delete();
        //                }
        //            }
        //
        //            sortInfo.TotalTime = (DateTime.Now.Millisecond - sortInfo.TotalTime);
        //            return sortInfo;
        //        }
        //
        //        /// <summary>
        //        /// Returns the default temporary directory. By default, the System's temp folder. If not accessible
        //        /// or not available, an IOException is thrown
        //        /// </summary>
        //        public static DirectoryInfo DefaultTempDir()
        //        {
        //            return new DirectoryInfo(Path.GetTempPath());
        //        }
        //
        //        /// <summary>
        //        /// Copies one file to another.
        //        /// </summary>
        //        private static void Copy(FileInfo file, FileInfo output)
        //        {
        //            File.Copy(file.FullName, output.FullName);
        //        }
        //
        //        /// <summary>
        //        /// Sort a single partition in-memory. </summary>
        //        internal FileInfo SortPartition(int len)
        //        {
        //            var data = this.Buffer;
        //            var tempFile = new FileInfo(Path.GetTempFileName());
        //            //var tempFile1 = File.Create(new ());
        //            //FileInfo tempFile = FileInfo.createTempFile("sort", "partition", TempDirectory);
        //
        //            long start = DateTime.Now.Millisecond;
        //            sortInfo.SortTime += (DateTime.Now.Millisecond - start);
        //
        //            var @out = new ByteSequencesWriter(tempFile);
        //            BytesRef spare;
        //            try
        //            {
        //                BytesRefIterator iter = Buffer.Iterator(comparator);
        //                while ((spare = iter.Next()) != null)
        //                {
        //                    Debug.Assert(spare.Length <= short.MaxValue);
        //                    @out.Write(spare);
        //                }
        //
        //                @out.Dispose();
        //
        //                // Clean up the buffer for the next partition.
        //                data.Clear();
        //                return tempFile;
        //            }
        //            finally
        //            {
        //                IOUtils.Close(@out);
        //            }
        //        }
        //
        //        /// <summary>
        //        /// Merge a list of sorted temporary files (partitions) into an output file </summary>
        //        internal void MergePartitions(IList<FileInfo> merges, FileInfo outputFile)
        //        {
        //            long start = DateTime.Now.Millisecond;
        //
        //            var @out = new ByteSequencesWriter(outputFile);
        //
        //            PriorityQueue<FileAndTop> queue = new PriorityQueueAnonymousInnerClassHelper(this, merges.Count);
        //
        //            var streams = new ByteSequencesReader[merges.Count];
        //            try
        //            {
        //                // Open streams and read the top for each file
        //                for (int i = 0; i < merges.Count; i++)
        //                {
        //                    streams[i] = new ByteSequencesReader(merges[i]);
        //                    sbyte[] line = streams[i].Read();
        //                    if (line != null)
        //                    {
        //                        queue.InsertWithOverflow(new FileAndTop(i, line));
        //                    }
        //                }
        //
        //                // Unix utility sort() uses ordered array of files to pick the next line from, updating
        //                // it as it reads new lines. The PQ used here is a more elegant solution and has
        //                // a nicer theoretical complexity bound :) The entire sorting process is I/O bound anyway
        //                // so it shouldn't make much of a difference (didn't check).
        //                FileAndTop top;
        //                while ((top = queue.Top()) != null)
        //                {
        //                    @out.Write(top.Current);
        //                    if (!streams[top.Fd].Read(top.Current))
        //                    {
        //                        queue.Pop();
        //                    }
        //                    else
        //                    {
        //                        queue.UpdateTop();
        //                    }
        //                }
        //
        //                SortInfo.MergeTime += DateTime.UtcNow.Ticks - start;
        //                SortInfo.MergeRounds++;
        //            }
        //            finally
        //            {
        //                // The logic below is: if an exception occurs in closing out, it has a priority over exceptions
        //                // happening in closing streams.
        //                try
        //                {
        //                    IOUtils.Close(streams);
        //                }
        //                finally
        //                {
        //                    IOUtils.Close(@out);
        //                }
        //            }
        //        }
        //
        //        private class PriorityQueueAnonymousInnerClassHelper : PriorityQueue<FileAndTop>
        //        {
        //            private readonly OfflineSorter OuterInstance;
        //
        //            public PriorityQueueAnonymousInnerClassHelper(OfflineSorter outerInstance, int size)
        //                : base(size)
        //            {
        //                this.OuterInstance = outerInstance;
        //            }
        //
        //            public override bool LessThan(FileAndTop a, FileAndTop b)
        //            {
        //                return OuterInstance.comparator.Compare(a.Current, b.Current) < 0;
        //            }
        //        }
        //
        //        /// <summary>
        //        /// Read in a single partition of data </summary>
        //        internal int ReadPartition(ByteSequencesReader reader)
        //        {
        //            long start = DateTime.Now.Millisecond;
        //            var scratch = new BytesRef();
        //            while ((scratch.Bytes = reader.Read()) != null)
        //            {
        //                scratch.Length = scratch.Bytes.Length;
        //                Buffer.Append(scratch);
        //                // Account for the created objects.
        //                // (buffer slots do not account to buffer size.)
        //                if (RamBufferSize.Bytes < BufferBytesUsed.Get())
        //                {
        //                    break;
        //                }
        //            }
        //            sortInfo.ReadTime += (DateTime.Now.Millisecond - start);
        //            return Buffer.Size();
        //        }
        //
        //        internal class FileAndTop
        //        {
        //            internal readonly int Fd;
        //            internal readonly BytesRef Current;
        //
        //            internal FileAndTop(int fd, sbyte[] firstLine)
        //            {
        //                this.Fd = fd;
        //                this.Current = new BytesRef(firstLine);
        //            }
        //        }
        //

        /// <summary>
        /// Utility class to emit length-prefixed byte[] entries to an output stream for sorting.
        /// Complementary to <seealso cref="ByteSequencesReader"/>.
        /// </summary>
        public class ByteSequencesWriter : IDisposable
        {
            internal readonly DataOutput Os;

            /// <summary>
            /// Constructs a ByteSequencesWriter to the provided File </summary>
            public ByteSequencesWriter(string filePath)
                : this(new BinaryWriterDataOutput(new BinaryWriter(new FileStream(filePath, FileMode.Open))))
            {
            }

            /// <summary>
            /// Constructs a ByteSequencesWriter to the provided DataOutput </summary>
            public ByteSequencesWriter(DataOutput os)
            {
                this.Os = os;
            }

            /// <summary>
            /// Writes a BytesRef. </summary>
            /// <seealso cref= #write(byte[], int, int) </seealso>
            public virtual void Write(BytesRef @ref)
            {
                Debug.Assert(@ref != null);
                Write(@ref.Bytes, @ref.Offset, @ref.Length);
            }

            /// <summary>
            /// Writes a byte array. </summary>
            /// <seealso cref= #write(byte[], int, int) </seealso>
            public virtual void Write(byte[] bytes)
            {
                Write(bytes, 0, bytes.Length);
            }

            /// <summary>
            /// Writes a byte array.
            /// <p>
            /// The length is written as a <code>short</code>, followed
            /// by the bytes.
            /// </summary>
            public virtual void Write(byte[] bytes, int off, int len)
            {
                Debug.Assert(bytes != null);
                Debug.Assert(off >= 0 && off + len <= bytes.Length);
                Debug.Assert(len >= 0);
                Os.WriteShort((short)len);
                Os.Write(bytes, off, len);
            }

            /// <summary>
            /// Closes the provided <seealso cref="DataOutput"/> if it is <seealso cref="IDisposable"/>.
            /// </summary>
            public void Dispose()
            {
                var os = Os as IDisposable;
                if (os != null)
                {
                    os.Dispose();
                }
            }
        }
//
//        /// <summary>
//        /// Utility class to read length-prefixed byte[] entries from an input.
//        /// Complementary to <seealso cref="ByteSequencesWriter"/>.
//        /// </summary>
//        public class ByteSequencesReader : IDisposable
//        {
//            internal readonly DataInput inputStream;
//
//            /// <summary>
//            /// Constructs a ByteSequencesReader from the provided File </summary>
//            public ByteSequencesReader(FileInfo file)
//                : this(new DataInputStream(new BufferedInputStream(new FileInputStream(file))))
//            {
//            }
//
//            /// <summary>
//            /// Constructs a ByteSequencesReader from the provided DataInput </summary>
//            public ByteSequencesReader(DataInput inputStream)
//            {
//                this.inputStream = inputStream;
//            }
//
//            /// <summary>
//            /// Reads the next entry into the provided <seealso cref="BytesRef"/>. The internal
//            /// storage is resized if needed.
//            /// </summary>
//            /// <returns> Returns <code>false</code> if EOF occurred when trying to read
//            /// the header of the next sequence. Returns <code>true</code> otherwise. </returns>
//            /// <exception cref="EOFException"> if the file ends before the full sequence is read. </exception>
//            public virtual bool Read(BytesRef @ref)
//            {
//                short length;
//                try
//                {
//                    length = inputStream.ReadShort();
//                }
//                catch (EOFException)
//                {
//                    return false;
//                }
//
//                @ref.Grow(length);
//                @ref.Offset = 0;
//                @ref.Length = length;
//                inputStream.ReadFully(@ref.Bytes, 0, length);
//                return true;
//            }
//
//            /// <summary>
//            /// Reads the next entry and returns it if successful.
//            /// </summary>
//            /// <seealso cref= #read(BytesRef)
//            /// </seealso>
//            /// <returns> Returns <code>null</code> if EOF occurred before the next entry
//            /// could be read. </returns>
//            /// <exception cref="EOFException"> if the file ends before the full sequence is read. </exception>
//            public virtual sbyte[] Read()
//            {
//                short length;
//                try
//                {
//                    length = inputStream.ReadShort();
//                }
//                catch (EOFException e)
//                {
//                    return null;
//                }
//
//                Debug.Assert(length >= 0, "Sanity: sequence length < 0: " + length);
//                sbyte[] result = new sbyte[length];
//                inputStream.ReadFully(result);
//                return result;
//            }
//
//            /// <summary>
//            /// Closes the provided <seealso cref="DataInput"/> if it is <seealso cref="IDisposable"/>.
//            /// </summary>
//            public void Dispose()
//            {
//                var @is = inputStream as IDisposable;
//                if (@is != null)
//                {
//                    @is.Dispose();
//                }
//            }
//        }
//
//        /// <summary>
//        /// Returns the comparator in use to sort entries </summary>
//        public IComparer<BytesRef> Comparator
//        {
//            get
//            {
//                return comparator;
//            }
//        }
    }
}