using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Processing
{
    public abstract class BackgroundAction : MangaJobFactory
    {
        private static volatile object _lockFilters = new object();
        private static Dictionary<Guid, BackgroundAction> _activeFilters = new Dictionary<Guid, BackgroundAction>();
        private static Dictionary<Guid, object> _pickupFilters = new Dictionary<Guid, object>();
        private static Dictionary<Guid, Tuple<string, Guid>> _pickupVisIds = new Dictionary<Guid, Tuple<string, Guid>>();

        private Guid _pickupKey;
        private Tuple<string, Guid> _visId;

        public Tuple<string, Guid> VisId { get => _visId; }
        public Guid PickupKey { get => _pickupKey; }

        protected BackgroundAction(Tuple<string, Guid> visId)
        {
            _pickupKey = Guid.NewGuid();
            _visId = visId;

            lock (_lockFilters)
            {
                _activeFilters.Add(_pickupKey, this);
            }
        }

        private object GetStatus(Tuple<string, Guid> visId, Guid pickupKey)
        {
            if (visId.Item1 == _visId.Item1 && visId.Item2 == _visId.Item2 && _pickupKey == pickupKey)
            {
                return new { Success = true, Complete = false, StatusData = Status };
            }
            return new { Success = false };
        }

        public static object GetPickupJob(Tuple<string, Guid> visId, Guid pickupKey)
        {
            lock(_lockFilters)
            {
                if (_pickupVisIds.ContainsKey(pickupKey))
                {
                    var matchVisId = _pickupVisIds[pickupKey];
                    _pickupVisIds.Remove(pickupKey);
                    var results = _pickupFilters[pickupKey];
                    _pickupFilters.Remove(pickupKey);

                    if (_activeFilters.ContainsKey(pickupKey))
                        _activeFilters.Remove(pickupKey);

                    if ( visId.Item1 == matchVisId.Item1 && visId.Item2 == matchVisId.Item2)
                    {
                        return new { Success = true, Complete = true, Results = results };
                    }
                }
                else if (_activeFilters.ContainsKey(pickupKey))
                {
                    return _activeFilters[pickupKey].GetStatus(visId, pickupKey);

                }
            }

            return new { Success = false };
        }

        protected void AddPickup(object pickup)
        {
            lock (_lockFilters)
            {
                _pickupFilters.Add(_pickupKey, pickup);
                _pickupVisIds.Add(_pickupKey, _visId);
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
