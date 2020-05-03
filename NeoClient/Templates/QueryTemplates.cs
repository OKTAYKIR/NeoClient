namespace NeoClient.Templates
{
    internal static class QueryTemplates
    {
        internal static string TEMPLATE_CREATE = @"@match CREATE(n:@node{@conditions}) @clause RETURN n";
        internal static string TEMPLATE_MERGE = @"MERGE (n:@node{@conditions}) @on_create_clause @on_match_clause RETURN n";
        internal static string TEMPLATE_GET_ALL = @"MATCH (n:@label{IsDeleted:false}) RETURN @result";
        internal static string TEMPLATE_GET_BY_PROPERTY = @"MATCH (n:@label{@property:$value,IsDeleted:false})@relationship@relatedNode RETURN @result";
        internal static string TEMPLATE_GET_BY_PROPERTIES = @"MATCH (n:@label{@clause,IsDeleted:false})@relationship@relatedNode RETURN @result";
        internal static string TEMPLATE_DELETE = @"MATCH(n:@label{Uuid:""@Uuid"",IsDeleted:false}) SET n.updatedAt=@updatedAt,n.IsDeleted=true RETURN n";
        internal static string TEMPLATE_UPDATE = @"MATCH(n:@label{Uuid:""@Uuid"",IsDeleted:false}) SET @clause @return";

        internal static string TEMPLATE_CREATE_RELATIONSHIP = @"MATCH(from{Uuid:""@uuidFrom""}),(to{Uuid:""@uuidTo""}) CREATE (from)@fromPartDirection[r:@relationshipName@clause]@toPartDirection(to) RETURN r";
        internal static string TEMPLATE_MERGE_RELATIONSHIP = @"MATCH(from{Uuid:""@uuidFrom""}),(to{Uuid:""@uuidTo""}) MERGE (from)@fromPartDirection[r:@relationshipName]@toPartDirection(to) RETURN r";
        internal static string TEMPLATE_DROP_RELATIONSHIPBETWEENTWONODES = @"MATCH({Uuid:""@uuidIncoming""})@fromPartDirection[r:@relationshipName]@toPartDirection({Uuid:""@uuidOutgoing""}) DELETE r";
        internal static string TEMPLATE_DROP = @"MATCH(n:@label{Uuid:""@Uuid""}) DETACH DELETE n";
        internal static string TEMPLATE_DROP_BY_PROPERTIES = @"MATCH(n:@label{@clause}) DETACH DELETE n";
        internal static string TEMPLATE_ADD_LABEL = @"MATCH (n{Uuid:""@Uuid""}) SET n:@label";
    }
}
