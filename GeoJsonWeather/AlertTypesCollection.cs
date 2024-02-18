using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoJsonWeather
{
    public class AlertTypesCollection<T> : IEnumerable<T> where T : IAlert
    {
        private IList<T> _types = new List<T>();

        public IEnumerator<T> GetEnumerator()
        {
            return _types.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_types).GetEnumerator();
        }
    }
}
