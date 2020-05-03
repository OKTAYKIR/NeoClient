using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;

namespace NeoClient.Externsions
{
    public static class IStatementResultExtensions
    {
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
