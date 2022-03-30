using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Utility
{
    public class MyPerformance
    {
        private string _name;
        private object _value;
        private string _description;
        private bool _canSetValue = false;

        public MyPerformance(string name, string description) : base()
        {
            Init(name, null, description);
            _canSetValue = true;
        }
        
        public MyPerformance(string name, object value, string description )
        {
            Init(name, value, description);
        }

        private void Init(string name, object value, string description)
        {
            _name = name;
            _value = value;
            _description = description;
        }

        public void SetValue(object value)
        {
            if (_canSetValue)
            {
                _value = value;
            }
#if DEBUG
            else
            {
                throw new Exception("Cannot set a value when set in constructor");
            }
#endif
        }

        public void AppendDescription(string additional)
        {
            _description = string.Format("{0} - {1}", _description, additional);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", _name, _value, _description);
        }
    }
}
