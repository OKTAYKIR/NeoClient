# NeoClient
A Lightweight and simple .Net Core micro object graph mapper (OGM) for Neo4j which support BOLT protocol.

## Usage

### Creating Database Connection
Optional you can pass authentication credential via the constructor.
```c#
INeoClient client = new NeoClient(
    uri: "bolt://localhost:7687", 
    userName: "user",
    password: "password", 
    strip_hyphens: true //optional, default is false
);
client.Connect();
```

For example, if you were using any IoC container, you could register the client like so:
```c#
container.Register<INeoClient>((c, p) =>
{
    INeoClient client = new NeoClient(
        uri: "bolt://localhost:7687", 
        userName: "user",
        password: "password", 
        strip_hyphens: true //optional, default is false
        );
    client.Connect();
    return client;
});
```

### Creating a Node
```c#
TNode entity = client.Add(new TNode{Foo = "Foo"});
```

### Adding a Label to a Node
```c#
bool result = client.AddLabel(
    uuid: "01a68df3-cc35-4eb0-a199-0d924da86eab",
    labelName: @"LabelName"
);
```

### Retrieving a Node by Id
```c#
TNode node = client.GetByUuidWithRelatedNodes("01a68df3-cc35-4eb0-a199-0d924da86eab");
```

### Retrieving All Nodes
```c#
TNode node = client.GetAll();
```

### Retrieving Nodes by Single Property
```c#
IList<TNode> nodes = client.GetByProperty<TNode>("uuid", "01a68df3-cc35-4eb0-a199-0d924da86eab");
```

### Retrieving Nodes by Multiple Properties
```c#
IList<TNode> nodes = client.GetByProperties<TNode>(
  new Dictionary<string, object>() 
  {
      { "type", UserNode" },
      { "userID", "01a68df3-cc35-4eb0-a199-0d924da86eab"}
  };
);
```

### Updating a Node
```c#
TNode updatedNode = client.Update(
    entity: node,
    id: "01a68df3-cc35-4eb0-a199-0d924da86eab",
    fetchResult: true //optional
);
```

### Partial Updating a Node
```c#
TNode updatedNode = client.PartialUpdate(
    entity: node,
    id: "01a68df3-cc35-4eb0-a199-0d924da86eab",
    fetchResult: true
);
```

### Deleting a Node (Soft Delete)
```c#
client.Delete("01a68df3-cc35-4eb0-a199-0d924da86eab");
```

### Dropping a Node by Id
```c#
client.Drop("01a68df3-cc35-4eb0-a199-0d924da86eab");
```

### Drop Nodes by Properties
Creating a node with its properties on creation time. If the nodes had already been found, different multiple properties would have been set.
```c#
bool result = client.DropByProperties<TNode>(
    props: new Dictionary<string, object>()
    {
        {"name", "keanu"}
    }
);
```

### Create a Relationship Between Certain Two Nodes
```c#
client.CreateRelationship(
    uuidFrom: "2ac55031-3089-453a-a858-e1a9a8b68a16",
    uuidTo: "ac43523a-a15e-4d25-876e-e2a2cc4de125",
    relationshipAttribute: node.GetRelationshipAttributes(ep => ep.roles).FirstOrDefault(),
    props: new Dictionary<string, object>() //optional
        {
            {"createdAt", DateTime.UtcNow.ToTimeStamp()}
        }; 
);
```

### Dropping a Relationship Between Certain Two Nodes 
```c#
client.DropRelationshipBetweenTwoNodes(
    uuidFrom: "2ac55031-3089-453a-a858-e1a9a8b68a16",
    uuidTo: "ac43523a-a15e-4d25-876e-e2a2cc4de125",
    relationshipAttribute: node.GetRelationshipAttributes(ep => ep.roles).FirstOrDefault()
);
```

### Merge Multiple Nodes
Creating a node with its properties on creation time. If the nodes had already been found, different multiple properties would have been set.
```c#
TNode node = client.Merge(
    entityOnCreate: new TNode(){ name="keanu"; createdAt = DateTime.UtcNow.ToTimeStamp(); },
    entityOnUpdate: new TNode(){ name="keanu"; updatedAt = DateTime.UtcNow.ToTimeStamp(); },
    where: @"{name=""keanu""}" //optional
);
```

### Merge Relationships
Creating a node with its properties on creation time. If the nodes had already been found, different multiple properties would have been set.
```c#
bool result = client.MergeRelationship(
    uuidFrom: "2ac55031-3089-453a-a858-e1a9a8b68a16",
    uuidTo: "ac43523a-a15e-4d25-876e-e2a2cc4de125",
    relationshipAttribute: node.GetRelationshipAttributes(ep => ep.roles).FirstOrDefault()
);
```

### Running Custom Cypher Query
```c#
string cypherQuery = $@"MATCH (u:User {{userName:{{name}}}})
                               OPTIONAL MATCH (u)-[r:USES]-(n:Device)
                               OPTIONAL MATCH (m:Person)-[k:CREATED]-(u)
                               detach delete u
                               detach delete n
                               detach delete m";

var queryParameters = new Dictionary<string, object>
{
    {"userName", "keanu"},
};

IList<object> result = client.RunCustomQuery(
    query: cypherQuery,
    parameters: queryParameters
);

//or using generics

IList<TNode> result = client.RunCustomQuery<TNode>(
    query: cypherQuery,
    parameters: queryParameters
);
```

## Transactions

## To Do
* Nuget package 

## Contributing
1. Fork it ( https://github.com/OKTAYKIR/NeoClient/fork )
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Push to the branch (`git push origin my-new-feature`)
5. Create a new Pull Request

## Licence Information
MIT License
