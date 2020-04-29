using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.DB.Cte;

namespace S138124 {
    class Program {
        static void Main(string[] args) {
            DevExpress.Xpo.Logger.LogManager.SetTransport(new ConsoleLogger());
            XpoDefault.ConnectionString = MSSqlConnectionProviderWithCte.GetConnectionString(@"(local)", "CTE_Test");

            //fill structure and data
            using(UnitOfWork uow = new UnitOfWork()) {
                uow.ClearDatabase();
                new BaseTable(uow) { Str = "Aa" };
                new BaseTable(uow) { Str = "bB" };
                new BaseTable(uow) { Str = "CCccc" };
                uow.CommitChanges();
            }
            //working with cte
            using(UnitOfWork uow = new UnitOfWork()) {
                uow.RegisterCte("CteStructure_1", "(ID, String, Math) AS (select OID, Str, sin(OID) from BaseTable)");
                try {
                    Console.WriteLine("=== Count for sine CTE ===");
                    Console.WriteLine("Count: {0}", uow.Evaluate<CteStructure_1>(new AggregateOperand(null, null, Aggregate.Count, null), null));
                    Console.WriteLine("=== Filtered count for sine CTE ===");
                    Console.WriteLine("Count: {0}", uow.Evaluate<CteStructure_1>(new AggregateOperand(null, null, Aggregate.Count, null), new OperandProperty("ID") > 1));
                    Console.WriteLine("=== Select all with sine CTE ===");
                    foreach(var cteS in new XPCollection<CteStructure_1>(uow))
                        Console.WriteLine(cteS.ToString());
                } finally {
                    uow.UnregisterCte("CteStructure_1");
                }
            }
            using(UnitOfWork uow = new UnitOfWork()) {
                uow.RegisterCte("CteStructure_1", "AS (select OID as ID, 'MyString: ' + coalesce(Str, '') as String, cos(OID) as Math from BaseTable where len(Str) < 5)");
                try {
                    Console.WriteLine("=== Count for filtered CTE ===");
                    Console.WriteLine("Count: {0}", uow.Evaluate<CteStructure_1>(new AggregateOperand(null, null, Aggregate.Count, null), null));
                    Console.WriteLine("=== Filtered count for filtered CTE ===");
                    Console.WriteLine("Count: {0}", uow.Evaluate<CteStructure_1>(new AggregateOperand(null, null, Aggregate.Count, null), new OperandProperty("ID") > 1));
                    Console.WriteLine("=== Select all with filtered CTE ===");
                    foreach(var cteS in new XPCollection<CteStructure_1>(uow))
                        Console.WriteLine(cteS.ToString());
                    Console.WriteLine("=== Linq2XPO ===");
                    foreach(var cteS in uow.Query<CteStructure_1>().Where(q => q.Math >= 0))
                        Console.WriteLine(cteS.ToString());
                } finally {
                    uow.UnregisterCte("CteStructure_1");
                }
            }
            Console.ReadLine();
        }
    }
    public class BaseTable: XPObject {
        public BaseTable(Session session) : base(session) { }
        string _Str;
        public string Str { get { return _Str; } set { SetPropertyValue("Str", ref _Str, value); } }
    }
    public class CteStructure_1: XPLiteObject {
        public CteStructure_1(Session session) : base(session) { }
        [Key]
        public int ID;
        public string String;
        public double? Math;
        public override string ToString() {
            return string.Format("<'{0}', '{1}', '{2}'>", ID, String, Math);
        }
    }
}