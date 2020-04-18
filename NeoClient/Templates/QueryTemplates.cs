namespace NeoClient.Templates
{
    internal static class QueryTemplates
    {
        internal static string TEMPLATE_CREATE = @"@match CREATE(n:@node{@conditions}) @clause RETURN n";
        internal static string TEMPLATE_MERGE = @"MERGE (n:@node{@conditions}) @on_create_clause @on_match_clause RETURN n";
        internal static string TEMPLATE_GET_ALL = @"MATCH (n:@label{isDeleted:false}) RETURN @result";
        internal static string TEMPLATE_GET_BY_PROPERTY = @"MATCH (n:@label{@property:$value,isDeleted:false})@relationship@relatedNode RETURN @result";
        internal static string TEMPLATE_GET_BY_PROPERTIES = @"MATCH (n:@label{@clause,isDeleted:false})@relationship@relatedNode RETURN @result";
        internal static string TEMPLATE_DELETE = @"MATCH(n:@label{uuid:""@uuid"",isDeleted:false}) SET n.updatedAt=@updatedAt,n.isDeleted=true RETURN n";
        internal static string TEMPLATE_UPDATE = @"MATCH(n:@label{uuid:""@uuid"",isDeleted:false}) SET @clause @return";

        internal static string TEMPLATE_CREATE_RELATIONSHIP = @"MATCH(from{uuid:""@uuidFrom""}),(to{uuid:""@uuidTo""}) CREATE (from)@fromPartDirection[r:@relationshipName@clause]@toPartDirection(to) RETURN r";
        internal static string TEMPLATE_MERGE_RELATIONSHIP = @"MATCH(from{uuid:""@uuidFrom""}),(to{uuid:""@uuidTo""}) MERGE (from)@fromPartDirection[r:@relationshipName]@toPartDirection(to) RETURN r";
        internal static string TEMPLATE_DROP_RELATIONSHIPBETWEENTWONODES = @"MATCH({uuid:""@uuidIncoming""})@fromPartDirection[r:@relationshipName]@toPartDirection({uuid:""@uuidOutgoing""}) DELETE r RETURN count(*)";
        internal static string TEMPLATE_DROP = @"MATCH(n:@label{uuid:""@uuid""}) DETACH DELETE n RETURN count(*)";
        internal static string TEMPLATE_DROP_BY_PROPERTIES = @"MATCH(n:@label{@clause}) WITH n LIMIT 1 DETACH DELETE n RETURN count(*)";
        internal static string TEMPLATE_ADD_LABEL = @"MATCH (n{uuid:""@uuid""}) SET n:@label";
    }
}
