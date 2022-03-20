using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public abstract class MyDisposable : IDisposable
    {
        protected abstract void DisposeManaged();
        protected abstract void DisposeUnManaged();

        // Flag: Has Dispose already been called? 
        bool _disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch
            {
            }
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    // Free any other managed objects here. 
                    DisposeManaged();
                }
                catch
                {
                }
            }

            try
            {
                // Free any unmanaged objects here. 
                DisposeUnManaged();
            }
            catch
            {
            }

            _disposed = true;
        }

        ~MyDisposable()
        {
            try
            {
                Dispose(false);
            }
            catch
            {
            }
        }
    }
}
