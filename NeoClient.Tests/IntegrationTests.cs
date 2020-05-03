using FluentAssertions;
using Neo4j.Driver.V1;
using NeoClient.Attributes;
using NeoClient.Extensions;
using NeoClient.Tests.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace NeoClient.Tests
{
    public class IntegrationTests : IntegrationTestBase
    {
        [Fact]
        public void ServiceUnavailableExceptionError()
        {
            using (var client = new NeoClient("bolt://localhost:1111", USER, PASSWORD))
            {
                client.Connect();

                var exception = Record.Exception(() => client.Ping());

                exception.Should().BeOfType<ServiceUnavailableException>();
            }
        }

        [Fact]
        public void AuthenticationErrorIfWrongAuthToken()
        {
            using (var client = new NeoClient(URL, "fake", "fake", CONFIG))
            {
                client.Connect();

                var exception = Record.Exception(() => client.Ping());

                exception.Should().BeOfType<AuthenticationException>();
            }
        }

        [Fact]
        public void CreateDriverWithBasicAuthenticationAndConfiguration()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                client.IsConnected.Should().BeTrue();
            }
        }

        [Fact]
        public void ErrorToRunInvalidCypher()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var exception = Record.Exception(() => client.RunCustomQuery("Invalid Cypher"));

                exception
                    .Should()
                    .BeOfType<ClientException>();
            }
        }

        [Fact]
        public void ShouldReportWrongScheme()
        {
            using (var client = new NeoClient("http://localhost", USER, PASSWORD, CONFIG))
            {
                var exception = Record.Exception(() => client.Connect());

                exception.Should().BeOfType<NotSupportedException>();
            }
        }

        [Fact]
        public void ShouldConnectAndPing()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                bool response = client.Ping();

                response.Should().BeTrue();
            }
        }

        [Fact]
        public void CreateDriverWithConnectionPool()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, Config.Builder
                    .WithMaxConnectionLifetime(TimeSpan.FromMinutes(30))
                    .WithMaxConnectionPoolSize(100)
                    .WithConnectionAcquisitionTimeout(TimeSpan.FromMinutes(2))
                    .WithEncryptionLevel(EncryptionLevel.None)
                    .ToConfig()))
            {
                client.Connect();

                client.IsConnected.Should().BeTrue();
            }
        }

        [Fact]
        public void CreateNode()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" };

                client.Add(user);
                    
                client.IsConnected.Should().BeTrue();
            }
        }

        [Fact]
        public void CreateLabel()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = client.Add(new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" });

                bool result = client.AddLabel(user.Uuid, "Father");

                result.Should().BeTrue();
            }
        }

        [Fact]
        public void GetNodeByUuid()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = client.Add(new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" });

                User entity = client.GetByUuidWithRelatedNodes<User>(user.Uuid);

                entity.Should().NotBeNull();
                user.Should().BeEquivalentTo(entity);
            }
        }

        [Fact]
        public void GetAllNodes()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var userList = new List<User>();
                
                for (int i = 0; i < 10; i++)
                {
                    userList.Add(new User 
                    { 
                        Email = "kir.oktay@gmail.com", 
                        FirstName = $"FakeFirstName{i}", 
                        LastName = $"FakeLastName{i}" 
                    });
                }

                userList.ForEach((user) => client.Add(user));

                IList<User> entities = client.GetAll<User>();

                entities.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void GetNodesBySingleProperty()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = client.Add(new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" });

                IList<User> entities = client.GetByProperty<User>(nameof(User.Email), user.Email);

                entities.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void GetNodesByMultipleProperties()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = client.Add(new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" });

                var properties = new Dictionary<string, object>()
                {
                    { nameof(User.Email), user.Email },
                    { nameof(User.FirstName), user.FirstName },
                    { nameof(User.LastName), user.LastName }
                };

                IList<User> entities = client.GetByProperties<User>(properties);

                entities.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void UpdateNode()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = client.Add(new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" });

                user.FirstName = "UpdatedName";

                var updatedEntity = client.Update(user, user.Uuid);

                updatedEntity.Should().BeNull();
            }
        }

        [Fact]
        public void UpdateNodeAndFetchResult()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = client.Add(new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" });

                user.FirstName = "UpdatedName";

                var updatedEntity = client.Update(user, user.Uuid, fetchResult: true);

                updatedEntity.Should().NotBeNull();
            }
        }

        [Fact]
        public void DeleteNode()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = client.Add(new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" });

                var deletedEntity = client.Delete<User>(user.Uuid);

                var entity = client.GetByUuidWithRelatedNodes<User>(user.Uuid);

                deletedEntity.IsDeleted.Should().BeTrue();
                entity.Should().BeNull();
            }
        }

        [Fact]
        public void DropNodeById()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user = client.Add(new User { Email = "kir.oktay@gmail.com", FirstName = "Oktay", LastName = "Kýr" });

                bool result = client.Drop<User>(user.Uuid);

                result.Should().BeTrue();
            }
        }

        [Fact]
        public void DropNodesByProperties()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var userList = new List<User>();

                for (int i = 0; i < 10; i++)
                {
                    userList.Add(new User
                    {
                        Email = "kir.oktay@gmail.com",
                        FirstName = $"FakeFirstName{i}",
                        LastName = $"FakeLastName{i}"
                    });
                }

                var properties = new Dictionary<string, object>()
                {
                    { nameof(User.Email), "kir.oktay@gmail.com" },
                };

                userList.ForEach((user) => client.Add(user));

                int nodesDeleted = client.DropByProperties<User>(properties);

                nodesDeleted.Should().Be(10);
            }
        }

        [Fact]
        public void CreateIncomingRelationship()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user1 = client.Add(new User { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" });
                var user2 = client.Add(new User { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" });

                var isCreated = client.CreateRelationship(
                    user1.Uuid, 
                    user2.Uuid,
                    new RelationshipAttribute
                    {
                        Direction = DIRECTION.INCOMING,
                        Name = "BROTHER"
                    });

                isCreated.Should().BeTrue();
            }
        }

        [Fact]
        public void CreateOutgoingRelationship()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user1 = client.Add(new User { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" });
                var user2 = client.Add(new User { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" });

                var isCreated = client.CreateRelationship(
                    user1.Uuid,
                    user2.Uuid,
                    new RelationshipAttribute
                    {
                        Direction = DIRECTION.OUTGOING,
                        Name = "BROTHER"
                    });

                isCreated.Should().BeTrue();
            }
        }

        [Fact]
        public void CreateMultipleRelationship()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user1 = client.Add(new User { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" });
                var user2 = client.Add(new User { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" });

                var isCreated1 = client.CreateRelationship(
                    user1.Uuid,
                    user2.Uuid,
                    new RelationshipAttribute
                    {
                        Direction = DIRECTION.INCOMING,
                        Name = "BROTHER"
                    });

                var isCreated2 = client.CreateRelationship(
                   user1.Uuid,
                   user2.Uuid,
                   new RelationshipAttribute
                   {
                       Direction = DIRECTION.INCOMING,
                       Name = "FAMILY"
                   });

                isCreated1.Should().BeTrue();
                isCreated2.Should().BeTrue();
            }
        }

        [Fact]
        public void CreateRelationshipWithProperties()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user1 = client.Add(new User { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" });
                var user2 = client.Add(new User { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" });

                var isCreated1 = client.CreateRelationship(
                    user1.Uuid,
                    user2.Uuid,
                    new RelationshipAttribute
                    {
                        Direction = DIRECTION.INCOMING,
                        Name = "BROTHER"
                    },
                    new Dictionary<string, object>()
                    {
                        {"CreatedAt", DateTime.UtcNow},
                        {"Kinship_Level", 1},
                        {"Name", "FakeName"}
                    }
                );

                isCreated1.Should().BeTrue();
            }
        }

        [Fact]
        public void DropRelationship()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user1 = client.Add(new User { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" });
                var user2 = client.Add(new User { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" });

                var relationship = new RelationshipAttribute
                {
                    Direction = DIRECTION.INCOMING,
                    Name = "BROTHER"
                };

                client.CreateRelationship(
                   user1.Uuid,
                   user2.Uuid,
                   relationship);

                bool result = client.DropRelationshipBetweenTwoNodes(
                    user1.Uuid,
                    user2.Uuid,
                    relationship);

                result.Should().BeTrue();
            }
        }

        [Fact]
        public void MergeOnCreateNodes()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user1 = new User() { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" };
                var user2 = new User() { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" };

                User result = client.Merge(
                    entityOnCreate: user1,
                    entityOnUpdate: user2,
                    $"Email:\"{user1.Email}\""
                );

                result.Should().NotBeNull();
            }
        }

        [Fact]
        public void MergeOnUpdateNodes()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user1 = new User() { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" };
                var user2 = new User() { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" };

                var entity = client.Add(user1);
                
                User result = client.Merge(
                    entityOnCreate: user1,
                    entityOnUpdate: user2,
                    $"Uuid:\"{entity.Uuid}\""
                );

                result.Should().NotBeNull();
                result.Uuid.Should().Be(entity.Uuid);
            }
        }

        [Fact]
        public void MergeRelationshipOnCreate()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                User user1 = client.Add(new User { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" });
                User user2 = client.Add(new User { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" });

                var relationship = new RelationshipAttribute
                {
                    Direction = DIRECTION.INCOMING,
                    Name = "BROTHER"
                };

                bool result = client.MergeRelationship(
                    uuidFrom: user1.Uuid,
                    uuidTo: user2.Uuid,
                    relationshipAttribute: relationship
                );

                result.Should().BeTrue();
            }
        }

        [Fact]
        public void MatchRelationship()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                User user1 = client.Add(new User { Email = "FakeEmail1", FirstName = "FakeFirstName1", LastName = "FakeLastName1" });
                User user2 = client.Add(new User { Email = "FakeEmail2", FirstName = "FakeFirstName2", LastName = "FakeLastName2" });

                var relationship = new RelationshipAttribute
                {
                    Direction = DIRECTION.INCOMING,
                    Name = "BROTHER"
                };

                bool isCreatedRelationship = client.CreateRelationship(
                       user1.Uuid,
                       user2.Uuid,
                       relationship
                );

                bool isUpdatedRelatiionship = client.MergeRelationship(
                    uuidFrom: user1.Uuid,
                    uuidTo: user2.Uuid,
                    relationshipAttribute: relationship
                );

                isCreatedRelationship.Should().BeTrue();
                isUpdatedRelatiionship.Should().BeTrue();
            }
        }

        [Fact]
        public void RunCustomCypherQuery()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                string cypherCreateQuery = @"CREATE (Neo:Crew {name:'Neo'}), 
                (Morpheus:Crew {name: 'Morpheus'}), 
                (Trinity:Crew {name: 'Trinity'}), 
                (Cypher:Crew:Matrix {name: 'Cypher'}), 
                (Smith:Matrix {name: 'Agent Smith'}), 
                (Architect:Matrix {name:'The Architect'}),
                (Neo)-[:KNOWS]->(Morpheus), 
                (Neo)-[:LOVES]->(Trinity), 
                (Morpheus)-[:KNOWS]->(Trinity),
                (Morpheus)-[:KNOWS]->(Cypher), 
                (Cypher)-[:KNOWS]->(Smith), 
                (Smith)-[:CODED_BY]->(Architect)";

                IStatementResult createResult = client.RunCustomQuery(
                    query: cypherCreateQuery
                );

                createResult.Should().NotBeNull();
                createResult.Summary.Counters.LabelsAdded.Should().Be(7);
                createResult.Summary.Counters.NodesCreated.Should().Be(6);
                createResult.Summary.Counters.PropertiesSet.Should().Be(6);
                createResult.Summary.Counters.RelationshipsCreated.Should().Be(6);
            }
        }

        [Fact]
        public void RunCustomCypherWithReturnsQuery()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                string cypherCreateQuery = @"CREATE (Neo:Crew {name:'Neo'}), 
                (Morpheus:Crew {name: 'Morpheus'}), 
                (Trinity:Crew {name: 'Trinity'}), 
                (Cypher:Crew:Matrix {name: 'Cypher'}), 
                (Smith:Matrix {name: 'Agent Smith'}), 
                (Architect:Matrix {name:'The Architect'}),
                (Neo)-[:KNOWS]->(Morpheus), 
                (Neo)-[:LOVES]->(Trinity), 
                (Morpheus)-[:KNOWS]->(Trinity),
                (Morpheus)-[:KNOWS]->(Cypher), 
                (Cypher)-[:KNOWS]->(Smith), 
                (Smith)-[:CODED_BY]->(Architect)";

                IStatementResult createResult = client.RunCustomQuery(
                    query: cypherCreateQuery
                );

                string cypherQuery = @"match (n:Crew)-[r:KNOWS*]-(m) where n.name='Neo' return n as Neo,r,m";

                IStatementResult queryResult = client.RunCustomQuery(
                  query: cypherQuery
                );

                IList<object> result = queryResult.GetValues();

                createResult.Should().NotBeNull();
                createResult.Summary.Counters.LabelsAdded.Should().Be(7);
                createResult.Summary.Counters.NodesCreated.Should().Be(6);
                createResult.Summary.Counters.PropertiesSet.Should().Be(6);
                createResult.Summary.Counters.RelationshipsCreated.Should().Be(6);
                queryResult.Should().NotBeNull();
                result.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void RunCustomCypherQueryWithModelBinding()
        {
            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();

                var user1 = new User { Email = "kir.oktay@gmail.com", FirstName = "FakeFirstName1", LastName = "FakeLastName2" };
                var user2 = new User { Email = "kir.oktay@gmail.com", FirstName = "FakeFirstName2", LastName = "FakeLastName2" };

                client.Add(user1);
                client.Add(user2);

                string cypherQuery = @"MATCH (n:User) RETURN n";

                IList<User> result = client.RunCustomQuery<User>(
                    query: cypherQuery
                );

                result.Should().NotBeNullOrEmpty();
            }
        }
    }
}
