﻿using System;
using System.Threading;

namespace Lucene.Net.Util
{
    /// <summary>
    /// Extensions to help obtain/release from a ReaderWriterSlimLock.
    /// Taken from:
    /// http://stackoverflow.com/questions/170028/how-would-you-simplify-entering-and-exiting-a-readerwriterlock
    /// 
    /// LUCENENET specific
    /// </summary>
    internal static class ReaderWriterExtension
    {
        sealed class ReadLockToken : IDisposable
        {
            private ReaderWriterLockSlim _readerWriterLockSlim;

            public ReadLockToken(ReaderWriterLockSlim sync)
            {
                _readerWriterLockSlim = sync;
                sync.EnterReadLock();
            }

            public void Dispose()
            {
                if (_readerWriterLockSlim != null)
                {
                    _readerWriterLockSlim.ExitReadLock();
                    _readerWriterLockSlim = null;
                }
            }
        }

        sealed class WriteLockToken : IDisposable
        {
            private ReaderWriterLockSlim _readerWriterLockSlim;

            public WriteLockToken(ReaderWriterLockSlim sync)
            {
                _readerWriterLockSlim = sync;
                sync.EnterWriteLock();
            }

            public void Dispose()
            {
                if (_readerWriterLockSlim != null)
                {
                    _readerWriterLockSlim.ExitWriteLock();
                    _readerWriterLockSlim = null;
                }
            }
        }

        public static IDisposable Read(this ReaderWriterLockSlim obj)
        {
            return new ReadLockToken(obj);
        }

        public static IDisposable Write(this ReaderWriterLockSlim obj)
        {
            return new WriteLockToken(obj);
        }
    }
}
