Imports System
Imports System.Linq
Imports DevExpress.Data.Filtering
Imports DevExpress.Xpo
Imports DevExpress.Xpo.DB.Cte

Namespace S138124

    Friend Class Program

        Shared Sub Main(ByVal args As String())
            Logger.LogManager.SetTransport(New ConsoleLogger())
            XpoDefault.ConnectionString = MSSqlConnectionProviderWithCte.GetConnectionString("(local)", "CTE_Test")
            'fill structure and data
            Using uow As UnitOfWork = New UnitOfWork()
                uow.ClearDatabase()
                Dim tmp_BaseTable = New BaseTable(uow) With {.Str = "Aa"}
                Dim tmp_BaseTable = New BaseTable(uow) With {.Str = "bB"}
                Dim tmp_BaseTable = New BaseTable(uow) With {.Str = "CCccc"}
                uow.CommitChanges()
            End Using

            'working with cte
            Using uow As UnitOfWork = New UnitOfWork()
                uow.RegisterCte("CteStructure_1", "(ID, String, Math) AS (select OID, Str, sin(OID) from BaseTable)")
                Try
                    Console.WriteLine("=== Count for sine CTE ===")
                    Console.WriteLine("Count: {0}", uow.Evaluate(Of CteStructure_1)(New AggregateOperand(CType(Nothing, String), CType(Nothing, String), Aggregate.Count, CType(Nothing, CriteriaOperator)), Nothing))
                    Console.WriteLine("=== Filtered count for sine CTE ===")
                    Console.WriteLine("Count: {0}", uow.Evaluate(Of CteStructure_1)(New AggregateOperand(CType(Nothing, String), CType(Nothing, String), Aggregate.Count, CType(Nothing, CriteriaOperator)), New OperandProperty("ID") > 1))
                    Console.WriteLine("=== Select all with sine CTE ===")
                    For Each cteS In New XPCollection(Of CteStructure_1)(uow)
                        Console.WriteLine(cteS.ToString())
                    Next
                Finally
                    uow.UnregisterCte("CteStructure_1")
                End Try
            End Using

            Using uow As UnitOfWork = New UnitOfWork()
                uow.RegisterCte("CteStructure_1", "AS (select OID as ID, 'MyString: ' + coalesce(Str, '') as String, cos(OID) as Math from BaseTable where len(Str) < 5)")
                Try
                    Console.WriteLine("=== Count for filtered CTE ===")
                    Console.WriteLine("Count: {0}", uow.Evaluate(Of CteStructure_1)(New AggregateOperand(CType(Nothing, String), CType(Nothing, String), Aggregate.Count, CType(Nothing, CriteriaOperator)), Nothing))
                    Console.WriteLine("=== Filtered count for filtered CTE ===")
                    Console.WriteLine("Count: {0}", uow.Evaluate(Of CteStructure_1)(New AggregateOperand(CType(Nothing, String), CType(Nothing, String), Aggregate.Count, CType(Nothing, CriteriaOperator)), New OperandProperty("ID") > 1))
                    Console.WriteLine("=== Select all with filtered CTE ===")
                    For Each cteS In New XPCollection(Of CteStructure_1)(uow)
                        Console.WriteLine(cteS.ToString())
                    Next

                    Console.WriteLine("=== Linq2XPO ===")
                    For Each cteS In uow.Query(Of CteStructure_1)().Where(Function(q) q.Math >= 0)
                        Console.WriteLine(cteS.ToString())
                    Next
                Finally
                    uow.UnregisterCte("CteStructure_1")
                End Try
            End Using

            Console.ReadLine()
        End Sub
    End Class

    Public Class BaseTable
        Inherits XPObject

        Public Sub New(ByVal session As Session)
            MyBase.New(session)
        End Sub

        Private _Str As String

        Public Property Str As String
            Get
                Return _Str
            End Get

            Set(ByVal value As String)
                SetPropertyValue("Str", _Str, value)
            End Set
        End Property
    End Class

    Public Class CteStructure_1
        Inherits XPLiteObject

        Public Sub New(ByVal session As Session)
            MyBase.New(session)
        End Sub

        <Key>
        Public ID As Integer

        Public [String] As String

        Public Math As Double?

        Public Overrides Function ToString() As String
            Return String.Format("<'{0}', '{1}', '{2}'>", ID, [String], Math)
        End Function
    End Class
End Namespace
