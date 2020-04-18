using System;
using Neo4j.Driver.V1;

namespace NeoClient.TransactionManager
{
    public class Transaction : ITransaction, IInternalTransaction, IDisposable
    {
        public Transaction(IDriver driver)
        {
            _driver = driver;
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();

            _currentTransaction = null;
        }

        #region Private variables
        private IDriver _driver;
        private Neo4j.Driver.V1.ITransaction _currentTransaction { get; set; }
        #endregion

        #region Public variables
        public bool InTransaction { get; set; }
        #endregion

        #region Public methods
        public void Commit()
        {
            _currentTransaction?.Success();

            this.Dispose();
            //_currentTransaction?.Dispose();

            //_currentTransaction = null;

            //InTransaction = false;
        }

        public void Rollback()
        {
            _currentTransaction?.Failure();

            this.Dispose();

            //_currentTransaction?.Dispose();

            //_currentTransaction = null;

            //InTransaction = false;
        }

        public void BeginTransaction()
        {
            _currentTransaction = _driver.Session().BeginTransaction();

            InTransaction = true;
        }
        #endregion

        Neo4j.Driver.V1.ITransaction IInternalTransaction.CurrentTransaction { get => _currentTransaction; }
    }
}
