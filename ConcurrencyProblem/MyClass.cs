using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ConcurrencyProblem
{
    public class MyClass
    {
        public enum LockType
        {
            None,
            PgAdvisoryLock,
            ExclusiveLock,
            ForUpdate,
            SerializableTransaction,
            Optimistic
        }


        private MyEntity _myEntity;

        private List<string> _possibleValues;

        public MyClass(MyDbContext db, string valueColA, string valueColB, List<string> possibleValues)
        {
            _myEntity = db.MyTable.Add(new MyEntity() { ColA = valueColA, ColB = valueColB, TimeStamp = DateTime.UtcNow }).Entity;
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

            if (lockType == LockType.ForUpdate)
            {
                updateSql = "update mytable " +
                                $"set myseq = (SELECT CASE WHEN EXISTS (SELECT 1 FROM mytable WHERE cola = '{_myEntity.ColA}' and colb = '{_myEntity.ColB}' and myseq is not null limit 1) THEN (Select suma from (SELECT myseq + 1 as suma FROM mytable WHERE id <> {_myEntity.Id} AND cola = '{_myEntity.ColA}' and colb = '{_myEntity.ColB}' order by myseq is null asc, myseq desc for update) as subc limit 1) ELSE 1 END) " +
                                $"where id = {_myEntity.Id}; ";
            }

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

                if (lockType == LockType.Optimistic)
                {
                    SetMySeqValue(db);
                }
                else
                {
                    db.Database.ExecuteSqlRaw(updateSql);
                }
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

            Console.WriteLine($"Updated MySeq: {this} Thread: {Thread.CurrentThread.ManagedThreadId}");
        }

        private void SetMySeqValue(MyDbContext db, int depth = 0)
        {
            //if(depth > 10)
            //{
            //    Debug.WriteLine(this);
            //}
            try
            {
                var siguienteMySeq = db.MyTable
                    .Where(e => e.ColA == _myEntity.ColA && e.ColB == _myEntity.ColB && e.MySeq.HasValue)
                    .OrderByDescending(e => e.MySeq)
                    .Take(1).Select(e => e.MySeq).FirstOrDefault();

                _myEntity.MySeq = siguienteMySeq.HasValue ? siguienteMySeq.Value + 1 : 1;
                
                db.SaveChanges();
            }
            //catch (Exception ex) when (ex is DbUpdateConcurrencyException || ex is DbUpdateException)
            //{
            //    SetMySeqValue(db);//infine loop?
            //}
            catch (Exception ex)
            {
                if (ex is DbUpdateConcurrencyException || ex is DbUpdateException)
                {
                    Debug.WriteLine($"Error updating: {this} Thread: {Thread.CurrentThread.ManagedThreadId}");
                    Thread.Sleep(300);
                    db.Entry(_myEntity).Reload();
                    SetMySeqValue(db, ++depth);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                    throw;
                }
            }
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
