using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoClient.Extensions
{
    public static class StatementResultExtensions
    {
        public static T Map<T>(this IStatementResult statementResult) where T : new()
        {
            var recordValue = statementResult?.FirstOrDefault()?[0];

            if (recordValue == null)
                return default;

            var properties = recordValue.As<INode>().Properties;

            return properties == null ? default : properties.Map<T>();
        }

        public static IList<object> GetValues(this IStatementResult source)
        {
            if (source == null)
                return null;

            var entites = new Lazy<List<object>>();

            foreach (IRecord record in source)
            {
                var node = record.Values.As<Dictionary<string, object>>();

                entites.Value.Add(node);
            }

            return entites.Value;
        }
    }
}
