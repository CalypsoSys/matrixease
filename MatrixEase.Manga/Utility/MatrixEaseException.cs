using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public class MatrixEaseException : ApplicationException
    {
        public MatrixEaseException(string message) : base(message)
        {
        }
    }
}