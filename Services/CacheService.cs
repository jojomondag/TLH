using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TLH.Services
{
    public class CacheService<T>
    {
        private Dictionary<string, T> _cache = new Dictionary<string, T>();

        public T Get(string id)
        {
            if (_cache.ContainsKey(id))
            {
                return _cache[id];
            }

            return default(T);
        }

        public void Add(string id, T item)
        {
            if (!_cache.ContainsKey(id))
            {
                _cache[id] = item;
            }
        }
    }
}
