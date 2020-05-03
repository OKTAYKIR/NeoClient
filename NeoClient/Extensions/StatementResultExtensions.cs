using Neo4j.Driver.V1;
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
    }
}
