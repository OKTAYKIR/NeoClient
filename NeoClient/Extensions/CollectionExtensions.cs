using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace NeoClient.Extensions
{
    public static class CollectionExtensions
    {
        internal static dynamic AsQueryClause(this Dictionary<string, object> source)
        {
            dynamic properties = new ExpandoObject();
            bool firstNode = true;
            StringBuilder clause = new StringBuilder();
            var parameters = new Dictionary<string, object>();

            foreach (var item in source)
            {
                object value = item.Value;

                parameters[item.Key] = item.Value;

                if (value.GetType() == typeof(string))
                {
                    string sValue = value as string;
                    sValue = sValue.Replace("\"", string.Empty);
                    parameters[item.Key] = value as string;
                }
                else
                {
                    parameters[item.Key] = value;
                }

                if (firstNode)
                    firstNode = false;
                else
                    clause.Append(",");

                clause.Append(string.Format("{0}:${0}", item.Key));
            }

            properties.parameters = parameters;
            properties.clause = clause;

            return properties;
        }
    }
}
