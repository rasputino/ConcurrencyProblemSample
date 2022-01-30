using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace ConcurrencyProblem.Tests
{
    [TestClass]
    public class ConcurrencyTest
    {
        [TestMethod]
        public void TestMethod()
        {
            //using (var dbMain = new MyDbContext())
            //{
            //    dbMain.Database.EnsureDeleted();
            //    dbMain.Database.EnsureCreated();
            //}
            TestMethod(MyClass.LockType.None, new List<string>() { "a", "b" });
        }

        [TestMethod]
        public void TestMethodAdvisoryLocks()
        {
            TestMethod(MyClass.LockType.PgAdvisoryLock, new List<string>() { "c", "d" });
        }

        [TestMethod]
        public void TestMethodExclusiveLock()
        {
            TestMethod(MyClass.LockType.ExclusiveLock, new List<string>() { "e", "f" });
        }

        [TestMethod]
        public void TestMethodForUpdate()
        {
            TestMethod(MyClass.LockType.ForUpdate, new List<string>() { "g", "h" });
        }

        [TestMethod]
        public void TestMethodSerializableTransaction()
        {
            TestMethod(MyClass.LockType.SerializableTransaction, new List<string>() { "i", "k" });
        }

        [TestMethod]
        public void TestMethodOptimistic()
        {
            TestMethod(MyClass.LockType.Optimistic, new List<string>() { "j", "l" });
        }


        private void TestMethod(MyClass.LockType lockType, List<string> possibleValues)
        {
            var possibleValuesCount = possibleValues.Count;
            var counter = 100;
            var rnd = new Random();
            using (var dbMain = new MyDbContext())
            {
                Debug.WriteLine(dbMain.Database.GetDbConnection().ConnectionString);

                dbMain.Database.EnsureDeleted();
                dbMain.Database.EnsureCreated();

                Parallel.For(1, counter, (index, state) =>
                {
                    using (var db = new MyDbContext())
                    {
                        for (int i = 0; i < counter; i++)
                        {
                            var insertBeforeTransaction = rnd.Next() % 2 == 0;
                            var valueColA = possibleValues[rnd.Next(0, possibleValuesCount)];
                            var valueColB = possibleValues[rnd.Next(0, possibleValuesCount)];
                            MyClass insertedRow = null;
                            if (insertBeforeTransaction)
                            {
                                insertedRow = new MyClass(db, valueColA, valueColB, possibleValues);
                            }

                            var transaction = db.Database.BeginTransaction();

                            if(lockType == MyClass.LockType.SerializableTransaction)
                            {
                                db.Database.ExecuteSqlRaw("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;");
                            }

                            if (!insertBeforeTransaction)
                            {
                                insertedRow = new MyClass(db, valueColA, valueColB, possibleValues);
                            }

                            System.Threading.Thread.Sleep(rnd.Next(100));

                            insertedRow.SetMySeqValue(db, lockType);

                            System.Threading.Thread.Sleep(rnd.Next(100));

                            transaction.Commit();
                        }
                    }
                });

                var count = dbMain.MyTable.Count(x => x.MySeq.HasValue && possibleValues.Contains(x.ColA) && possibleValues.Contains(x.ColB));
                
                Assert.IsTrue(count == counter * counter);

                //dbMain.Database.EnsureDeleted();
            }
        }


    }
}