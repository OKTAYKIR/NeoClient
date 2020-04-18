using System.Collections.Generic;
using System.Linq;

namespace NeoClient.Utilities
{
    public class StringFormatter
    {
        public string Str { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        public StringFormatter(string p_str)
        {
            Str = p_str;
            Parameters = new Dictionary<string, object>();
        }

        public void Add(string key, object val)
        {
            Parameters.Add(key, val);
        }

        public bool Remove(string key)
        {
            return Parameters.Remove(key);
        }

        public override string ToString()
        {
            return Parameters.Aggregate(Str, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
        }
    }
}
