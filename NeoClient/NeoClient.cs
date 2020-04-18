using AutoMapper;
using Neo4j.Driver.V1;
using NeoClient.Attributes;
using NeoClient.Extensions;
using NeoClient.Externsions;
using NeoClient.Templates;
using NeoClient.TransactionManager;
using NeoClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NeoClient
{
    public class NeoClient : INeoClient, IDisposable
    {
        #region Private variables
        private static readonly string PREFIX_QUERY_RESPONSE_KEY = "n";
        private static readonly string BIND_MARKER = "|";

        private readonly string URI;
        private readonly string UserName;
        private readonly string Password;
        private readonly bool StripHyphens;
        private IDriver Driver;
        private TransactionManager.ITransaction Transaction = null;
        #endregion

        #region Public variables
        bool IsConnected
        {
            get
            {
                return Driver != null;
            }
        }
        #endregion

        static NeoClient() => Mapper.Initialize(mapper => { });

        public NeoClient(
            string uri,
            string userName = null,
            string password = null,
            bool strip_hyphens = false)
        {
            this.URI = uri;
            this.UserName = userName;
            this.Password = password;
            this.StripHyphens = strip_hyphens;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Transaction?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public void Connect()
        {
            if (IsConnected)
                return;

            Config config = Config.Builder
                .WithEncryptionLevel(EncryptionLevel.Encrypted)
                .WithConnectionTimeout(TimeSpan.FromSeconds(30))
                .ToConfig();

            Driver = GraphDatabase.Driver(URI, (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password)) ? null :
                                                                                                                    AuthTokens.Basic(UserName, Password), config);
        }

        public TransactionManager.ITransaction BeginTransaction()
        {
            if (Driver == null)
                return null;

            Transaction = new Transaction(Driver);

            Transaction.BeginTransaction();

            return Transaction;
        }

        private IStatementResult ExecuteQuery(
            string query, 
            object parameters = null)
        {
            if (Transaction == null)
            {
                using (var session = Driver.Session())
                {
                    return parameters == null ? session.Run(query) :
                                                session.Run(query, parameters);
                }
            }

            var currentTransaction = ((IInternalTransaction)Transaction).CurrentTransaction;

            if (currentTransaction == null)
                throw new NullReferenceException("Transaction");

            return parameters == null ? currentTransaction.Run(query.ToString()) :
                                        currentTransaction.Run(query.ToString(), parameters);
        }

        private IStatementResult ExecuteQuery(
            string query, 
            IDictionary<string, object> parameters)
        {
            if (Transaction == null)
            {
                using (var session = Driver.Session())
                {
                    return parameters == null ? session.Run(query) :
                                                session.Run(query, parameters);
                }
            }

            var currentTransaction = ((IInternalTransaction)Transaction).CurrentTransaction;

            if (currentTransaction == null)
                throw new NullReferenceException("Transaction");

            return parameters == null ? currentTransaction.Run(query.ToString()) :
                                        currentTransaction.Run(query.ToString(), parameters);
        }

        private IDictionary<string, object> FetchRelatedNode<T>(string uuid) 
            where T : EntityBase, new()
        {
            var query = new StringFormatter(QueryTemplates.TEMPLATE_GET_BY_PROPERTIES);

            query.Add("@label", new T().GetLabelName());
            query.Add("@clause", "uuid:$uuid");
            query.Add("@result", PREFIX_QUERY_RESPONSE_KEY);
            query.Add("@relatedNode", string.Empty);
            query.Add("@relationship", string.Empty);

            var nodes = new Lazy<Dictionary<string, object>>();

            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute(typeof(NotMappedAttribute), true) != null)
                    continue;

                RelationshipAttribute relationshipAttribute = (RelationshipAttribute)prop.GetCustomAttributes(typeof(RelationshipAttribute), true).FirstOrDefault();

                if (relationshipAttribute != null)
                {
                    string labelName;
                    if (prop.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
                    {
                        labelName = (Activator.CreateInstance(prop.PropertyType.GetGenericArguments()[0], null) as EntityBase).GetLabelName();
                    }
                    else
                    {
                        labelName = (Activator.CreateInstance(prop.PropertyType, null) as EntityBase).GetLabelName();
                    }

                    string parameterRelatedNode = string.Format(@"(rNode:{0}{{isDeleted:false}})", labelName);

                    string parameterRelationship = string.Format(
                        @"{0}[r:{1}]{2}",
                        relationshipAttribute.Direction == DIRECTION.INCOMING ? "<-" : "-",
                        relationshipAttribute.Name,
                        relationshipAttribute.Direction == DIRECTION.INCOMING ? "-" : "->");

                    query.Remove("@result");
                    query.Remove("@relatedNode");
                    query.Remove("@relationship");

                    query.Add("@relatedNode", parameterRelatedNode);
                    query.Add("@relationship", parameterRelationship);
                    query.Add("@result", "rNode");

                    IStatementResult resultRelatedNode = ExecuteQuery(query.ToString(), new { uuid });

                    if (prop.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
                    {
                        var relatedNodes = new Lazy<List<IReadOnlyDictionary<string, object>>>();

                        foreach (IRecord record in resultRelatedNode)
                        {
                            IReadOnlyDictionary<string, object> node = record[0].As<INode>().Properties;

                            relatedNodes.Value.Add(node);
                        }

                        if (relatedNodes.IsValueCreated)
                        {
                            nodes.Value.Add(prop.Name, relatedNodes.Value);
                        }
                    }
                    else
                    {
                        IReadOnlyDictionary<string, object> relatedNode = resultRelatedNode.FirstOrDefault()?[0].As<INode>().Properties;

                        if (relatedNode != null)
                        {
                            nodes.Value.Add(prop.Name, relatedNode);
                        }
                    }
                }
            }

            return nodes.Value;
        }

        public bool CreateRelationship(
            string uuidFrom,
            string uuidTo,
            RelationshipAttribute relationshipAttribute,
            Dictionary<string, object> props = null)
        {
            if (relationshipAttribute == null)
                return false;

            var query = new StringFormatter(QueryTemplates.TEMPLATE_CREATE_RELATIONSHIP);
            query.Add("@uuidFrom", uuidFrom);
            query.Add("@uuidTo", uuidTo);
            query.Add("@fromPartDirection", relationshipAttribute.Direction == DIRECTION.INCOMING ? "<-" : "-");
            query.Add("@toPartDirection", relationshipAttribute.Direction == DIRECTION.INCOMING ? "-" : "->");
            query.Add("@relationshipName", relationshipAttribute.Name);

            IStatementResult result;

            if (props != null)
            {
                dynamic properties = props.AsQueryClause();
                dynamic clause = properties.clause;
                
                IDictionary<string, object> parameters = properties.parameters;
                query.Add("@clause", $"{{{clause}}}");
                result = ExecuteQuery(query.ToString(), parameters);
            }
            else
            {
                query.Add("@clause", string.Empty);
                result = ExecuteQuery(query.ToString());
            }

            return result.Any();
        }

        public bool DropRelationshipBetweenTwoNodes(
            string uuidIncoming,
            string uuidOutgoing,
            RelationshipAttribute relationshipAttribute)
        {
            if (relationshipAttribute == null)
                return false;

            var query = new StringFormatter(QueryTemplates.TEMPLATE_DROP_RELATIONSHIPBETWEENTWONODES);
            query.Add("@uuidIncoming", uuidIncoming);
            query.Add("@uuidOutgoing", uuidOutgoing);
            query.Add("@fromPartDirection", relationshipAttribute.Direction == DIRECTION.INCOMING ? "<-" : "-");
            query.Add("@toPartDirection", relationshipAttribute.Direction == DIRECTION.INCOMING ? "-" : "->");
            query.Add("@relationshipName", relationshipAttribute.Name);

            IStatementResult result = ExecuteQuery(query.ToString());

            var affectedNodes = result.First()[0].As<long>();

            return affectedNodes == 1;
        }

        public bool MergeRelationship(
            string uuidFrom,
            string uuidTo,
            RelationshipAttribute relationshipAttribute)
        {
            if (relationshipAttribute == null)
                return false;

            var query = new StringFormatter(QueryTemplates.TEMPLATE_MERGE_RELATIONSHIP);
            query.Add("@uuidFrom", uuidFrom);
            query.Add("@uuidTo", uuidTo);
            query.Add("@fromPartDirection", relationshipAttribute.Direction == DIRECTION.INCOMING ? "<-" : "-");
            query.Add("@toPartDirection", relationshipAttribute.Direction == DIRECTION.INCOMING ? "-" : "->");
            query.Add("@relationshipName", relationshipAttribute.Name);

            IStatementResult result = ExecuteQuery(query.ToString());

            return result.Any();
        }

        public T Add<T>(T entity) where T : EntityBase, new()
        {
            if (entity == null)
                throw new ArgumentException("entity");

            StringFormatter match = null;
            string clause = null;
            bool firstNode = true;
            bool hasRelationship = false;

            var parameters = new Lazy<Dictionary<string, object>>();

            var conditions = new Lazy<StringBuilder>();

            foreach (PropertyInfo prop in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute(typeof(NotMappedAttribute), true) != null ||
                    prop.GetCustomAttribute(typeof(RelationshipAttribute), true) != null)
                    continue;

                if (prop.Name.Equals("uuid", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                parameters.Value[prop.Name] = prop.GetValue(entity, null);

                if (firstNode)
                    firstNode = false;
                else
                    conditions.Value.Append(",");

                conditions.Value.Append(string.Format("{0}:${0}", prop.Name));
            }

            string uuid = StripHyphens ? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString();

            parameters.Value["uuid"] = uuid;
            conditions.Value.Append(firstNode ? "uuid:$uuid" : ",uuid:$uuid");

            var query = new StringFormatter(QueryTemplates.TEMPLATE_CREATE);
            query.Add("@match", hasRelationship ? match.ToString() : string.Empty);
            query.Add("@node", entity.GetLabelName());
            query.Add("@conditions", conditions.Value.ToString());
            query.Add("@clause", hasRelationship ? clause : string.Empty);

            IStatementResult result = ExecuteQuery(query.ToString(), parameters.Value);

            var retValue = Mapper.Map<IReadOnlyDictionary<string, object>, T>(result.SingleOrDefault()?[0].As<INode>().Properties);

            return retValue;
        }

        public T Merge<T>(
            T entityOnCreate, 
            T entityOnUpdate, 
            string where) where T : EntityBase, new()
        {
            if (entityOnCreate == null || entityOnUpdate == null)
                throw new ArgumentException("entity");

            bool firstNode = true;

            var setCaluseOnCreate = new Lazy<StringBuilder>();
            var setCaluseOnUpdate = new Lazy<StringBuilder>();

            var formattedSetClauseOnCreate = new StringFormatter("");
            var formattedSetClauseOnUpdate = new StringFormatter("");

            foreach (PropertyInfo prop in entityOnCreate.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute(typeof(NotMappedAttribute), true) != null ||
                    prop.GetCustomAttribute(typeof(RelationshipAttribute), true) != null)
                    continue;

                if (prop.Name.Equals("uuid", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                object value = prop.GetValue(entityOnCreate, null);

                string prefixAndPostfix = (value is string) ? "\"" : string.Empty;

                formattedSetClauseOnCreate.Add(BIND_MARKER + prop.Name + BIND_MARKER, prefixAndPostfix + (value ?? "null") + prefixAndPostfix);

                if (firstNode)
                {
                    firstNode = false;
                }
                else
                {
                    setCaluseOnCreate.Value.Append(",");
                }

                setCaluseOnCreate.Value.Append(string.Format("n.{0}={1}{0}{1}", prop.Name, BIND_MARKER));
            }

            firstNode = true;
            foreach (PropertyInfo prop in entityOnUpdate.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute(typeof(NotMappedAttribute), true) != null ||
                    prop.GetCustomAttribute(typeof(RelationshipAttribute), true) != null)
                    continue;

                if (prop.Name.Equals("uuid", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                object value = prop.GetValue(entityOnUpdate, null);

                string prefixAndPostfix = (value is string) ? "\"" : string.Empty;

                formattedSetClauseOnUpdate.Add(BIND_MARKER + prop.Name + BIND_MARKER, prefixAndPostfix + (value ?? "null") + prefixAndPostfix);

                if (firstNode)
                {
                    firstNode = false;
                }
                else
                {
                    setCaluseOnUpdate.Value.Append(",");
                }

                setCaluseOnUpdate.Value.Append(string.Format("n.{0}={1}{0}{1}", prop.Name, BIND_MARKER));
            }

            string uuid = StripHyphens ? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString();

            formattedSetClauseOnCreate.Add(string.Format("{0}uuid{0}", BIND_MARKER), "\"" + uuid + "\"");

            setCaluseOnCreate.Value.Append(firstNode ? string.Format("n.uuid={0}uuid{0}", BIND_MARKER) : string.Format(",n.uuid={0}uuid{0}", BIND_MARKER));

            formattedSetClauseOnCreate.Str = setCaluseOnCreate.Value.ToString();
            formattedSetClauseOnUpdate.Str = setCaluseOnUpdate.Value.ToString();

            var query = new StringFormatter(QueryTemplates.TEMPLATE_MERGE);
            query.Add("@node", entityOnCreate.GetLabelName());
            query.Add("@on_create_clause", string.Format("ON CREATE SET {0}", formattedSetClauseOnCreate.ToString()));
            query.Add("@on_match_clause", string.Format("ON MATCH SET {0}", formattedSetClauseOnUpdate.ToString()));
            query.Add("@conditions", where);

            IStatementResult result = ExecuteQuery(query.ToString());

            var retValue = Mapper.Map<IReadOnlyDictionary<string, object>, T>(result.SingleOrDefault()?[0].As<INode>().Properties);

            return retValue;
        }

        public T Update<T>(
            T entity, 
            string id, 
            bool fetchResult = false) where T : EntityBase, new()
        {
            if (entity == null)
                throw new ArgumentException("entity");

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("uuid");

            entity.uuid = id;

            dynamic properties = entity.AsUpdateClause(PREFIX_QUERY_RESPONSE_KEY);
            dynamic clause = properties.clause;
            IDictionary<string, object> parameters = properties.parameters;

            var query = new StringFormatter(QueryTemplates.TEMPLATE_UPDATE);
            query.Add("@label", entity.GetLabelName());
            query.Add("@uuid", entity.uuid);
            query.Add("@clause", clause);
            query.Add("@return", fetchResult ? string.Format("RETURN {0}", PREFIX_QUERY_RESPONSE_KEY) : string.Empty);

            IStatementResult result = ExecuteQuery(query.ToString(), parameters);

            T model = Mapper.Map<IReadOnlyDictionary<string, object>, T>(result.FirstOrDefault()?[0].As<INode>().Properties);

            return model;
        }

        public T PartialUpdate<T>(
            T entity, 
            string id, 
            bool fetchResult = false) where T : EntityBase, new()
        {
            if (entity == null)
                throw new ArgumentException("entity");

            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("uuid");

            entity.uuid = id;

            dynamic properties = entity.AsPartialUpdateClause(PREFIX_QUERY_RESPONSE_KEY);
            dynamic clause = properties.clause;
            IDictionary<string, object> parameters = properties.parameters;

            var query = new StringFormatter(QueryTemplates.TEMPLATE_UPDATE);
            query.Add("@label", entity.GetLabelName());
            query.Add("@uuid", entity.uuid);
            query.Add("@clause", clause);
            query.Add("@return", fetchResult ? string.Format("RETURN {0}", PREFIX_QUERY_RESPONSE_KEY) : string.Empty);

            IStatementResult result = ExecuteQuery(query.ToString(), parameters);

            T model = Mapper.Map<IReadOnlyDictionary<string, object>, T>(result.FirstOrDefault()?[0].As<INode>().Properties);

            return model;
        }

        public T Delete<T>(string uuid) where T : EntityBase, new()
        {
            if (string.IsNullOrWhiteSpace(uuid))
                throw new ArgumentException("uuid");

            T model = new T();

            var query = new StringFormatter(QueryTemplates.TEMPLATE_DELETE);
            query.Add("@label", model.GetLabelName());
            query.Add("@uuid", uuid);
            query.Add("@updatedAt", DateTime.UtcNow.ToTimeStamp());

            IStatementResult result = ExecuteQuery(query.ToString());

            model = Mapper.Map<IReadOnlyDictionary<string, object>, T>(result.FirstOrDefault()?[0].As<INode>().Properties);

            return model;
        }

        public bool Drop<T>(string uuid) where T : EntityBase, new()
        {
            if (string.IsNullOrWhiteSpace(uuid))
                throw new ArgumentException("uuid");

            var query = new StringFormatter(QueryTemplates.TEMPLATE_DROP);
            query.Add("@label", new T().GetLabelName());
            query.Add("@uuid", uuid);

            IStatementResult result = ExecuteQuery(query.ToString());

            var affectedNodes = result.First()[0].As<long>();

            return affectedNodes == 1;
        }

        public bool DropByProperties<T>(Dictionary<string, object> props) where T : EntityBase, new()
        {
            if (props == null)
                throw new ArgumentException("properties");

            dynamic properties = props.AsQueryClause();
            dynamic clause = properties.clause;
            Dictionary<string, object> parameters = properties.parameters;

            var query = new StringFormatter(QueryTemplates.TEMPLATE_DROP_BY_PROPERTIES);
            query.Add("@label", new T().GetLabelName());
            query.Add("@clause", clause);

            IStatementResult result = ExecuteQuery(query.ToString(), parameters);

            var affectedNodes = result.FirstOrDefault()?[0].As<long>();

            return affectedNodes == 1;
        }

        public IList<T> GetByProperty<T>(
            string propertyName, 
            object propertValue) where T : EntityBase, new()
        {
            if (string.IsNullOrWhiteSpace(propertyName) || propertValue == null)
                throw new ArgumentException("propertyName or properyValue");

            var entites = new Lazy<List<T>>();

            var query = new StringFormatter(QueryTemplates.TEMPLATE_GET_BY_PROPERTY);
            query.Add("@label", new T().GetLabelName());
            query.Add("@property", propertyName);
            query.Add("@result", PREFIX_QUERY_RESPONSE_KEY);
            query.Add("@relatedNode", string.Empty);
            query.Add("@relationship", string.Empty);

            IStatementResult result = ExecuteQuery(query.ToString(), new { value = propertValue });

            foreach (IRecord record in result)
            {
                IReadOnlyDictionary<string, object> node = record[0].As<INode>().Properties;

                IDictionary<string, object> relatedNodes = FetchRelatedNode<T>(node["uuid"].ToString());

                IDictionary<string, object> nodes = node.Concat(relatedNodes).ToDictionary(x => x.Key, x => x.Value);

                T nodeObject = Mapper.Map<IDictionary<string, object>, T>(nodes);

                entites.Value.Add(nodeObject);
            }

            return entites.Value;
        }

        public IList<T> GetByProperties<T>(Dictionary<string, object> entity) where T : EntityBase, new()
        {
            if (entity == null)
                throw new ArgumentException("properties");

            dynamic properties = entity.AsQueryClause();
            dynamic clause = properties.clause;
            Dictionary<string, object> parameters = properties.parameters;

            var entites = new Lazy<List<T>>();

            var query = new StringFormatter(QueryTemplates.TEMPLATE_GET_BY_PROPERTIES);
            query.Add("@label", new T().GetLabelName());
            query.Add("@clause", clause);
            query.Add("@result", PREFIX_QUERY_RESPONSE_KEY);
            query.Add("@relatedNode", string.Empty);
            query.Add("@relationship", string.Empty);

            IStatementResult result = ExecuteQuery(query.ToString(), parameters);

            foreach (IRecord record in result)
            {
                IReadOnlyDictionary<string, object> node = record[0].As<INode>().Properties;

                IDictionary<string, object> relatedNodes = FetchRelatedNode<T>(node["uuid"].ToString());

                IDictionary<string, object> nodes = node.Concat(relatedNodes).ToDictionary(x => x.Key, x => x.Value);

                T nodeObject = Mapper.Map<IDictionary<string, object>, T>(nodes);

                entites.Value.Add(nodeObject);
            }

            return entites.Value;
        }

        public T GetByUuidWithRelatedNodes<T>(string uuid) where T : EntityBase, new()
        {
            if (string.IsNullOrWhiteSpace(uuid))
                throw new ArgumentException("id");

            var query = new StringFormatter(QueryTemplates.TEMPLATE_GET_BY_PROPERTY);
            query.Add("@label", new T().GetLabelName());
            query.Add("@property", "uuid");
            query.Add("@result", PREFIX_QUERY_RESPONSE_KEY);
            query.Add("@relatedNode", string.Empty);
            query.Add("@relationship", string.Empty);

            IStatementResult result = ExecuteQuery(query.ToString(), new { value = uuid });

            IReadOnlyDictionary<string, object> node = result.FirstOrDefault()?[0].As<INode>().Properties;

            if (node == null)
                return null;

            IDictionary<string, object> nodes;

            IDictionary<string, object> relatedNodes = FetchRelatedNode<T>(uuid);

            if (relatedNodes == null || !relatedNodes.Any())
            {
                nodes = node.ToDictionary(x => x.Key, x => x.Value);
            }
            else
            {
                nodes = node.Concat(relatedNodes).ToDictionary(x => x.Key, x => x.Value);
            }

            T entity = Mapper.Map<IDictionary<string, object>, T>(nodes);

            return entity;
        }

        public IList<T> GetAll<T>() where T : EntityBase, new()
        {
            var entites = new Lazy<List<T>>();

            var query = new StringFormatter(QueryTemplates.TEMPLATE_GET_ALL);
            query.Add("@label", new T().GetLabelName());
            query.Add("@result", PREFIX_QUERY_RESPONSE_KEY);

            var queryRelated = new StringFormatter(QueryTemplates.TEMPLATE_GET_BY_PROPERTY);
            queryRelated.Add("@label", new T().GetLabelName());
            queryRelated.Add("@property", "uuid");

            IStatementResult result = ExecuteQuery(query.ToString());

            foreach (IRecord record in result)
            {
                IReadOnlyDictionary<string, object> node = record[0].As<INode>().Properties;

                string uuid = node["uuid"].ToString();

                IDictionary<string, object> relatedNodes = FetchRelatedNode<T>(uuid);

                IDictionary<string, object> nodes = node.Concat(relatedNodes).ToDictionary(x => x.Key, x => x.Value);

                T nodeObject = Mapper.Map<IDictionary<string, object>, T>(nodes);

                entites.Value.Add(nodeObject);
            }

            return entites.Value;
        }

        public IList<T> RunCustomQuery<T>(
            string query, 
            Dictionary<string, object> parameters) where T : EntityBase, new()
        {
            var entites = new Lazy<List<T>>();

            IStatementResult result = ExecuteQuery(query, parameters);

            foreach (IRecord record in result)
            {
                IReadOnlyDictionary<string, object> node = record[0].As<INode>().Properties;

                T nodeObject = Mapper.Map<IReadOnlyDictionary<string, object>, T>(node);

                entites.Value.Add(nodeObject);
            }

            return entites.Value;
        }

        public IList<object> RunCustomQuery(
            string query, 
            Dictionary<string, object> parameters)
        {
            var entites = new Lazy<List<object>>();

            IStatementResult result = ExecuteQuery(query, parameters);

            foreach (IRecord record in result)
            {
                var node = record.Values.As<Dictionary<string, object>>();

                entites.Value.Add(node);
            }

            return entites.Value;
        }

        public bool AddLabel(
            string uuid, 
            string labelName)
        {
            if (string.IsNullOrWhiteSpace(uuid) || string.IsNullOrWhiteSpace(labelName))
                return false;

            var query = new StringFormatter(QueryTemplates.TEMPLATE_ADD_LABEL);
            query.Add("@uuid", uuid);
            query.Add("@label", labelName);

            IStatementResult result = ExecuteQuery(query.ToString());

            return result.Any();
        }

        #region Commented Methods
        //public List<T2> GetRelatedNodesByID<T, T2>(int id) where T : EntityBase, new()
        //                                                   where T2 : EntityBase, new()
        //{
        //    if (id <= 0)
        //        throw new ArgumentException("id");

        //    var entities = new List<T2>();
        //    string labelName = new T().GetLabelName();
        //    string labelNameRelatedNode = new T2().GetLabelName();

        //    IStatementResult result = ExecuteQuery(string.Format(@"MATCH ({0})--({1}) WHERE ID({0}) = $id RETURN {1}", labelName, labelNameRelatedNode), new { id });

        //    foreach (IRecord record in result)
        //    {
        //        T2 nodeObject = Mapper.Map<IReadOnlyDictionary<string, object>, T2>(record[labelNameRelatedNode].As<INode>().Properties);

        //        entities.Add(nodeObject);
        //    }

        //    return entities;
        //}

        //public IDictionary<string, object> GetAllWithRelationship<T, T2>(T2 rel, string relName) where T : EntityBase, new()
        //                                                                                         where T2 : EntityBase, new()
        //{
        //    IDictionary<string, object> entites = new Dictionary<string, object>();

        //    IStatementResult result = ExecuteQuery(string.Format(@"MATCH (n:{0}) OPTIONAL MATCH (n)-[r:" + relName + "]->(r:{1}) RETURN n,r", new T().GetLabelName(), new T2().GetLabelName()));

        //    foreach (IRecord record in result)
        //    {
        //        IReadOnlyDictionary<string, object> node = record[PREFIX_QUERY_RESPONSE_KEY].As<INode>().Properties;

        //        entites.Add(node.ToObject<T>());
        //    }

        //    return entites;
        //}
        #endregion
    }
}