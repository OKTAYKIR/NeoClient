using NeoClient.Attributes;
using NeoClient.TransactionManager;
using System.Collections.Generic;

namespace NeoClient
{
    public interface INeoClient
    {
        IList<T> GetByProperty<T>(
            string propertyName, 
            object propertValue) where T : EntityBase, new();
        IList<T> GetByProperties<T>(Dictionary<string, object> entity) where T : EntityBase, new();
        T Add<T>(T entity) where T : EntityBase, new();
        T Update<T>(
            T entity, 
            string id, 
            bool fetchResult = false) where T : EntityBase, new();
        T PartialUpdate<T>(
            T entity, 
            string id, 
            bool fetchResult = false) where T : EntityBase, new();
        T Delete<T>(string uuid) where T : EntityBase, new();
        T GetByUuidWithRelatedNodes<T>(string uuid) where T : EntityBase, new();
        IList<T> GetAll<T>() where T : EntityBase, new();
        bool CreateRelationship(
            string uuidFrom,
            string uuidTo,
            RelationshipAttribute relationshipAttribute,
            Dictionary<string, object> props = null);
        T Merge<T>(
            T entityOnCreate, 
            T entityOnUpdate, 
            string where) where T : EntityBase, new();
        bool MergeRelationship(
            string uuidFrom,
            string uuidTo,
            RelationshipAttribute relationshipAttribute);
        bool Drop<T>(string uuid) where T : EntityBase, new();
        bool DropRelationshipBetweenTwoNodes(
            string uuidIncoming,
            string uuidOutgoing,
            RelationshipAttribute relationshipAttribute);
        IList<T> RunCustomQuery<T>(
            string query, 
            Dictionary<string, object> parameters) where T : EntityBase, new();
        IList<object> RunCustomQuery(
            string query, 
            Dictionary<string, object> parameters);
        //TODO: will be removed after isDeleted refactor
        bool DropByProperties<T>(Dictionary<string, object> props) where T : EntityBase, new();
        bool AddLabel(string uuid, string newLabelName);
        void Connect();
        ITransaction BeginTransaction();
        bool Ping();
    }
}