using System;

namespace NeoClient.TransactionManager
{
    internal interface IInternalTransaction : IDisposable
    {
        Neo4j.Driver.V1.ITransaction CurrentTransaction { get; }
    }
}
