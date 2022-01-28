using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;


namespace ConcurrencyProblem.Tests
{
    [TestClass]
    public class ConcurrencyTest
    {
        [TestMethod]
        public void TestMethod()
        {
            TestMethod(false);
        }

        [TestMethod]
        public void TestMethodAdvisoryLocks()
        {
            TestMethod(true);
        }


        private async void TestMethod(bool useAdvisoryLocks)
        {
            var counter = 100;
            var possibleValues = new[] { "a", "b" };
            var rnd = new Random();
            using (var dbMain = new MyDbContext())
            {
                dbMain.Database.EnsureCreated();

                Parallel.For(1, counter, (index, state) =>
                {
                    using (var db = new MyDbContext())
                    {
                        for (int i = 0; i < counter; i++)
                        {
                            var insertBeforeTransaction = rnd.Next() % 2 == 0;
                            var valueColA = possibleValues[rnd.Next(0, 2)];
                            var valueColB = possibleValues[rnd.Next(0, 2)];
                            MyClass insertedRow = null;
                            if (insertBeforeTransaction)
                            {
                                insertedRow = new MyClass(db, valueColA, valueColB);
                            }

                            var transaction = db.Database.BeginTransaction();

                            if (!insertBeforeTransaction)
                            {
                                insertedRow = new MyClass(db, valueColA, valueColB);
                            }

                            System.Threading.Thread.Sleep(rnd.Next(1000));


                            insertedRow.SetMySeqValue(db, useAdvisoryLocks);

                            System.Threading.Thread.Sleep(rnd.Next(1000));

                            transaction.Commit();
                        }
                    }
                });

                var count = await dbMain.MyTable.CountAsync();
                
                Assert.IsTrue(count == counter * counter);

                count = await dbMain.MyTable.CountAsync(x => x.MySeq.HasValue);

                Assert.IsTrue(count == counter * counter);

                dbMain.Database.EnsureDeleted();
            }
        }


    }
}