using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    class TermWeight
    {
        private string _name;
        private int _count = 0;
        private double _totalWeight = 0;

        public TermWeight(string name, double weight)
        {
            _name = name;
            _count = 1;
            _totalWeight = weight;
        }

        public void AddWeight(double weight)
        {
            _count++;
            _totalWeight += weight;
        }

        public double AverageWeigth
        {
            get
            {
                return _totalWeight / _count;
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
        }
    }
}
