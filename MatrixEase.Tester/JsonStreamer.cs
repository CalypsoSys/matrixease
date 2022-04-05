using MatrixEase.Manga.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Tester
{
    class JsonStreamer : MyDisposable
    {
        private StreamWriter _output;
        private bool _firstOut = true;
        public JsonStreamer(string file)
        {
            _output = new StreamWriter(file);
            _output.Write("{");
#if DEBUG
            _output.WriteLine();
#endif
        }

        public void WriteNode(string name, object data)
        {
            if (_firstOut)
                _firstOut = false;
            else
            {
                _output.Write(",");
#if DEBUG
                _output.WriteLine();
#endif
            }

            _output.Write("\"{0}\":{1}", name, JsonConvert.SerializeObject(data));
        }

        protected override void DisposeManaged()
        {
            if (_output != null)
            {
                _output.Write("}");
#if DEBUG
                _output.WriteLine();
#endif
                MiscHelpers.SafeDispose(_output);
            }
        }

        protected override void DisposeUnManaged()
        {
        }
    }
}
