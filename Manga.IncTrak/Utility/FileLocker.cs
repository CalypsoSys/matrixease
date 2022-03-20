using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public class FileLocker : MyDisposable
    {
        private Mutex _mutex = null;

        public FileLocker(string lockName)
        {
            _mutex = new Mutex(false, new string(lockName.Where(c => char.IsLetterOrDigit(c)).ToArray()));
            _mutex.WaitOne();
        }

        protected override void DisposeManaged()
        {
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex = null;
            }
        }

        protected override void DisposeUnManaged()
        {
        }
    }
}
