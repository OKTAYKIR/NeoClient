using Neo4j.Driver.V1;
using System;

namespace NeoClient.Tests
{
    public abstract class IntegrationTestBase : IDisposable
    {
        #region Variables
        internal const string URL = "bolt://localhost:7687";
        internal const string USER = "neo4j";
        internal const string PASSWORD = "changeme";
        internal static readonly Config CONFIG = Config.Builder
              .WithEncryptionLevel(EncryptionLevel.None)
              .ToConfig();
        #endregion

        public virtual void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            using (var client = new NeoClient(URL, USER, PASSWORD, CONFIG))
            {
                client.Connect();
                client.RunCustomQuery("MATCH (n) DETACH DELETE n");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
