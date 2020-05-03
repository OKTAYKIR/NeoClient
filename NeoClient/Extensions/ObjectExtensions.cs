using NeoClient.Attributes;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NeoClient.Extensions
{
    public static class ObjectExtensions
    {
        internal static dynamic AsUpdateClause(this object source, string prefix)
        {
            dynamic properties = new ExpandoObject();
            bool firstNode = true;
            StringBuilder clause = new StringBuilder();
            var parameters = new Dictionary<string, object>();

            foreach (PropertyInfo prop in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => !x.Name.Equals("createdAt")))
            {
                if (prop.GetCustomAttribute(typeof(NotMappedAttribute), true) != null ||
                    prop.GetCustomAttribute(typeof(RelationshipAttribute), true) != null)
                    continue;

                parameters[prop.Name] = prop.GetValue(source, null);

                if (firstNode)
                    firstNode = false;
                else
                    clause.Append(",");

                clause.Append(string.Format("{0}.{1}=${1}", prefix, prop.Name));
            }

            properties.parameters = parameters;
            properties.clause = clause;

            return properties;
        }

        internal static IEnumerable<RelationshipAttribute> GetRelationshipAttributes<T, T2>(this T obj, Expression<Func<T, T2>> value)
        {
            MemberExpression memberExpression = value.Body as MemberExpression;

            return (IEnumerable<RelationshipAttribute>)memberExpression.Member.GetCustomAttributes(typeof(RelationshipAttribute), true);
        }
    }
}
