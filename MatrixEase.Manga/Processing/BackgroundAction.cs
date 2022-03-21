using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Processing
{
    public abstract class BackgroundAction : MangaJobFactory
    {
        private static volatile object _lockFilters = new object();
        private static Dictionary<Guid, BackgroundAction> _activeFilters = new Dictionary<Guid, BackgroundAction>();
        private static Dictionary<Guid, object> _pickupFilters = new Dictionary<Guid, object>();
        private static Dictionary<Guid, Tuple<string, Guid>> _pickupMatrixIds = new Dictionary<Guid, Tuple<string, Guid>>();

        private Guid _pickupKey;
        private Tuple<string, Guid> _mxesId;

        public Tuple<string, Guid> MatrixId { get => _mxesId; }
        public Guid PickupKey { get => _pickupKey; }

        protected BackgroundAction(Tuple<string, Guid> mxesId)
        {
            _pickupKey = Guid.NewGuid();
            _mxesId = mxesId;

            lock (_lockFilters)
            {
                _activeFilters.Add(_pickupKey, this);
            }
        }

        private object GetStatus(Tuple<string, Guid> mxesId, Guid pickupKey)
        {
            if (mxesId.Item1 == _mxesId.Item1 && mxesId.Item2 == _mxesId.Item2 && _pickupKey == pickupKey)
            {
                return new { Success = true, Complete = false, StatusData = Status };
            }
            return new { Success = false };
        }

        public static object GetPickupJob(Tuple<string, Guid> mxesId, Guid pickupKey)
        {
            lock(_lockFilters)
            {
                if (_pickupMatrixIds.ContainsKey(pickupKey))
                {
                    var matchMatrixId = _pickupMatrixIds[pickupKey];
                    _pickupMatrixIds.Remove(pickupKey);
                    var results = _pickupFilters[pickupKey];
                    _pickupFilters.Remove(pickupKey);

                    if (_activeFilters.ContainsKey(pickupKey))
                        _activeFilters.Remove(pickupKey);

                    if ( mxesId.Item1 == matchMatrixId.Item1 && mxesId.Item2 == matchMatrixId.Item2)
                    {
                        return new { Success = true, Complete = true, Results = results };
                    }
                }
                else if (_activeFilters.ContainsKey(pickupKey))
                {
                    return _activeFilters[pickupKey].GetStatus(mxesId, pickupKey);

                }
            }

            return new { Success = false };
        }

        protected void AddPickup(object pickup)
        {
            lock (_lockFilters)
            {
                _pickupFilters.Add(_pickupKey, pickup);
                _pickupMatrixIds.Add(_pickupKey, _mxesId);
            }
        }

        protected override void DisposeManaged()
        {
            lock (_lockFilters)
            {
                if (_activeFilters.ContainsKey(_pickupKey))
                    _activeFilters.Remove(_pickupKey);
            }
        }

        protected override void DisposeUnManaged()
        {
        }
    }
}
