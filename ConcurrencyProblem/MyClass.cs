using Microsoft.EntityFrameworkCore;

namespace ConcurrencyProblem
{
    public class MyClass
    {
        public enum LockType
        {
            None,
            PgAdvisoryLock,
            ExclusiveLock
        }


        private MyEntity _myEntity;

        private List<string> _possibleValues;

        public MyClass(MyDbContext db, string valueColA, string valueColB, List<string> possibleValues)
        {
            _myEntity = db.MyTable.Add(new MyEntity() { ColA = valueColA, ColB = valueColB }).Entity;
            db.SaveChanges();
            Console.WriteLine($"Row created: {this} Thread: {Thread.CurrentThread.ManagedThreadId}");
            _possibleValues = possibleValues;
        }

        public void SetMySeqValue(MyDbContext db, LockType lockType)
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

            switch (lockType)
            {
                case LockType.PgAdvisoryLock:
                    var sqlLockAdvisory = $"SELECT pg_advisory_lock({GetAdvisoryLockKey()});";
                    db.Database.ExecuteSqlRaw(sqlLockAdvisory);
                    break;
                case LockType.ExclusiveLock:
                    var sqlLockExclusive = "LOCK TABLE mytable IN ACCESS EXCLUSIVE MODE;";
                    db.Database.ExecuteSqlRaw(sqlLockExclusive);
                    break;
            }

            try
            {
                db.Database.ExecuteSqlRaw(updateSql);
            }
            finally
            {
                switch (lockType)
                {
                    case LockType.PgAdvisoryLock:
                        var sqlUnlock = $"SELECT pg_advisory_unlock({GetAdvisoryLockKey()});";
                        db.Database.ExecuteSqlRaw(sqlUnlock);
                        break;
                    case LockType.ExclusiveLock:
                        break;
                }
            }

            db.Entry<MyEntity>(_myEntity).Reload();

            Console.WriteLine($"Updated MySeq: {this}");
        }

        private int GetAdvisoryLockKey()
        {
            int sum = _possibleValues.IndexOf(_myEntity.ColA) + 1;
            sum += (_possibleValues.IndexOf(_myEntity.ColA) + 1) * 10;
            return sum;
        }

        public override string ToString()
        {
            return $"[{_myEntity.Id}|{_myEntity.ColA}|{_myEntity.ColB}|{_myEntity.MySeq}]";
        }
    }
}
