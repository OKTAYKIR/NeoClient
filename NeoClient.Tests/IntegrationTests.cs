using FluentAssertions;
using Neo4j.Driver.V1;
using NeoClient.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NeoClient.Tests
{
    //public abstract class BaseExample : IDisposable
    //{
    //    protected IDriver Driver { set; get; }
    //    protected const string Uri = Neo4jDefaultInstallation.BoltUri;
    //    protected const string User = Neo4jDefaultInstallation.User;
    //    protected const string Password = Neo4jDefaultInstallation.Password;

    //    protected BaseExample(ITestOutputHelper output, StandAloneIntegrationTestFixture fixture)
    //    {
    //        Output = output;
    //        Driver = fixture.StandAlone.Driver;
    //    }

    //    protected virtual void Dispose(bool isDisposing)
    //    {
    //        if (!isDisposing)
    //            return;

    //        using (var session = Driver.Session())
    //        {
    //            session.Run("MATCH (n) DETACH DELETE n").Consume();
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //    }
    //}

    public class User : EntityBase
    {
        public User() : base(label: "User") { }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class IntegrationTests
    {
        #region Variables
        const string URL = "bolt://localhost:7687";
        const string USER = "neo4j";
        const string PASSWORD = "changeme";
        static readonly Config CONFIG = Config.Builder
              .WithEncryptionLevel(EncryptionLevel.None)
              .ToConfig();
        #endregion

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

                bool result = client.DropByProperties<User>(properties);

                result.Should().BeTrue();
            }
        }
    }
}
