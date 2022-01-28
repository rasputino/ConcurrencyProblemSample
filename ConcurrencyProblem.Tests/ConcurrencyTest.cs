using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace ConcurrencyProblem.Tests
{
    [TestClass]
    public class ConcurrencyTest
    {
        [TestMethod]
        public void TestMethod()
        {
            TestMethod(false, new List<string>() { "a", "b" });
        }

        [TestMethod]
        public void TestMethodAdvisoryLocks()
        {
            TestMethod(true, new List<string>() { "c", "d" });
        }


        private void TestMethod(bool useAdvisoryLocks, List<string> possibleValues)
        {
            var possibleValuesCount = possibleValues.Count;
            var counter = 100;
            var rnd = new Random();
            using (var dbMain = new MyDbContext())
            {
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
                                insertedRow = new MyClass(db, valueColA, valueColB);
                            }

                            var transaction = db.Database.BeginTransaction();

                            if (!insertBeforeTransaction)
                            {
                                insertedRow = new MyClass(db, valueColA, valueColB);
                            }

                            System.Threading.Thread.Sleep(rnd.Next(1000));

                            insertedRow.SetMySeqValue(db, useAdvisoryLocks, possibleValues);

                            System.Threading.Thread.Sleep(rnd.Next(1000));

                            transaction.Commit();
                        }
                    }
                });

                var count = dbMain.MyTable.Count(x => x.MySeq.HasValue && possibleValues.Contains(x.ColA) && possibleValues.Contains(x.ColB));
                
                Assert.IsTrue(count == counter * counter);

                dbMain.Database.EnsureDeleted();
            }
        }


    }
}