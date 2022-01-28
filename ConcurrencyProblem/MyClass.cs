using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ConcurrencyProblem
{
    public class MyClass
    {
        private MyEntity _myEntity;

        public MyClass(MyDbContext db, string valueColA, string valueColB)
        {
            _myEntity = db.MyTable.Add(new MyEntity() { ColA = valueColA, ColB = valueColB }).Entity;
            db.SaveChanges();
            Console.WriteLine($"Row created: [{_myEntity.Id}|{_myEntity.ColA}|{_myEntity.ColB}] Thread: {Thread.CurrentThread.ManagedThreadId}");
        }

        public void SetMySeqValue(MyDbContext db, bool useAdvisoryLocks)
        {
            var updateSql =
                "update mytable " +
                "set myseq = ((select myseq + 1 as newval " +
                                "from mytable " +
                                $"where {nameof(MyEntity.ColA)} = '{_myEntity.ColA}' and {nameof(MyEntity.ColB)} = '{_myEntity.ColB}' and {nameof(MyEntity.MySeq)} is not null " +
                                "order by " +
                                "myseq desc " +
                                "limit 1) " +
                               "union all(select 1 as newval " +
                                "where not exists(select 1 " +
                                "from mytable " +
                                $"where {nameof(MyEntity.ColA)} = '{_myEntity.ColA}' and {nameof(MyEntity.ColB)} = '{_myEntity.ColB}' and {nameof(MyEntity.MySeq)} is not null)) " +
                                "order by newval desc " +
                                "limit 1) " +
                $"where {nameof(MyEntity.Id)} = {_myEntity.Id};";

            if (useAdvisoryLocks)
            {
                var sqlLock = $"SELECT pg_advisory_lock({GetAdvisoryLockKey()});";
                db.Database.ExecuteSqlRaw(sqlLock);
            }
            try
            {
                db.Database.ExecuteSqlRaw(updateSql);
            }
            finally
            {
                if (useAdvisoryLocks)
                {
                    var sqlUnlock = $"SELECT pg_advisory_unlock({GetAdvisoryLockKey()});";
                    db.Database.ExecuteSqlRaw(sqlUnlock);
                }
            }
        }

        private int GetAdvisoryLockKey()
        {
            int sum = _myEntity.ColA == "a" ? 1 : 2;
            sum += _myEntity.ColB == "a" ? 0 : 10;
            return sum;
        }
    }
}
