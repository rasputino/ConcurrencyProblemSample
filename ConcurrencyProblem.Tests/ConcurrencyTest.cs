using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrencyProblem.Tests
{
    [TestClass]
    public class ConcurrencyTest
    {
        [TestMethod]
        public void TestMethod()
        {
            var possibleValues = new[] { "a", "b" };
            var rnd = new Random();
            var db = new MyDbContext();
            db.Database.EnsureCreated();

            Parallel.For(1, 1000, (index, state) =>
            {
                using (var db = new MyDbContext())
                {
                    for (int i = 0; i < 1000; i++)
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


                        insertedRow.SetMySeqValue(db);

                        transaction.Commit();
                    }
                }
            });

            db.Database.EnsureDeleted();

        }


    }
}