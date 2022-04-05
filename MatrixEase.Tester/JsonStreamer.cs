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
        private bool _inSubNode = false;
        private bool _nodeFirstOut = true;
        public JsonStreamer(string file)
        {
            _output = new StreamWriter(file);
            _output.Write("{");
#if DEBUG
            _output.WriteLine();
#endif
        }

        private void WriteComma()
        {
            if (_firstOut || (_inSubNode && _nodeFirstOut))
            {
                if (_inSubNode)
                {
                    _nodeFirstOut = false;
                }
                else
                {
                    _firstOut = false;
                }
            }
            else
            {
                _output.Write(",");
#if DEBUG
                _output.WriteLine();
#endif
            }
        }

        public void WriteNode(string name, object data)
        {
            WriteComma();
            _output.Write("\"{0}\":{1}", name, JsonConvert.SerializeObject(data));
        }

        public void StartNode(string name)
        {
            WriteComma();
            _inSubNode = true;
            _nodeFirstOut = true;
            _output.Write("\"{0}\":{{", name);
        }

        public void EndNode()
        {
            _inSubNode = false;
            _output.Write("}");
#if DEBUG
            _output.WriteLine();
#endif
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
