﻿using System;
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
            Debug.WriteLine($"Row created: [{_myEntity.Id}|{_myEntity.ColA}|{_myEntity.ColB}");
        }

        public void SetMySeqValue(MyDbContext db)
        {
            var updateSql =
                "update temporal.mytable " +
                "set myseq = ((select myseq + 1 as newval " +
                                "from temporal.mytable " +
                                $"where {nameof(MyEntity.ColA)} = '{_myEntity.ColA}' and {nameof(MyEntity.ColB)} = '{_myEntity.ColB}' and {nameof(MyEntity.MySeq)} is not null " +
                                "order by " +
                                "myseq desc " +
                                "limit 1) " +
                               "union all(select 1 as newval " +
                                "where not exists(select 1 " +
                                "from temporal.mytable " +
                                $"where {nameof(MyEntity.ColA)} = '{_myEntity.ColA}' and {nameof(MyEntity.ColB)} = '{_myEntity.ColB}' and {nameof(MyEntity.MySeq)} is not null)) " +
                                "order by newval desc " +
                                "limit 1) " +
                $"where id = {_myEntity.Id};";

            db.Database.ExecuteSqlRaw(updateSql);
            db.SaveChanges();
        }
    }
}
