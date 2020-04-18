using System;

namespace NeoClient.TransactionManager
{
    public interface ITransaction : IDisposable
    {
        void BeginTransaction();
        void Commit();
        void Rollback();
        bool InTransaction { get; }
    }
}
