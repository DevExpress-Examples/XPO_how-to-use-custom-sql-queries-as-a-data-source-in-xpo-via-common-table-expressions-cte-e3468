Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Linq
Imports DevExpress.Data.Filtering
Imports DevExpress.Xpo
Imports DevExpress.Xpo.DB
Imports DevExpress.Xpo.DB.Cte

Namespace S138124
	Friend Class Program
		Shared Sub Main(ByVal args() As String)
			DevExpress.Xpo.Logger.LogManager.SetTransport(New ConsoleLogger())
			XpoDefault.ConnectionString = MSSqlConnectionProviderWithCte.GetConnectionString("(local)", "CTE_Test")

			'fill structure and data
			Using uow As New UnitOfWork()
				uow.ClearDatabase()
				Dim obj As BaseTable
				obj = New BaseTable(uow)
				obj.Str = "Aa"
				obj = New BaseTable(uow)
				obj.Str = "bB"
				obj = New BaseTable(uow)
				obj.Str = "CCccc"
				uow.CommitChanges()
			End Using
			'working with cte
			Using uow As New UnitOfWork()
				uow.RegisterCte("CteStructure_1", "(ID, String, Math) AS (select OID, Str, sin(OID) from BaseTable)")
				Try
					Console.WriteLine("=== Count for sine CTE ===")
					Console.WriteLine("Count: {0}", uow.Evaluate(Of CteStructure_1)(New AggregateOperand(Nothing, Nothing, Aggregate.Count, Nothing), Nothing))
					Console.WriteLine("=== Filtered count for sine CTE ===")
					Console.WriteLine("Count: {0}", uow.Evaluate(Of CteStructure_1)(New AggregateOperand(Nothing, Nothing, Aggregate.Count, Nothing), New OperandProperty("ID") > 1))
					Console.WriteLine("=== Select all with sine CTE ===")
					For Each cteS In New XPCollection(Of CteStructure_1)(uow)
						Console.WriteLine(cteS.ToString())
					Next cteS
				Finally
					uow.UnregisterCte("CteStructure_1")
				End Try
			End Using
			Using uow As New UnitOfWork()
				uow.RegisterCte("CteStructure_1", "AS (select OID as ID, 'MyString: ' + coalesce(Str, '') as String, cos(OID) as Math from BaseTable where len(Str) < 5)")
				Try
					Console.WriteLine("=== Count for filtered CTE ===")
					Console.WriteLine("Count: {0}", uow.Evaluate(Of CteStructure_1)(New AggregateOperand(Nothing, Nothing, Aggregate.Count, Nothing), Nothing))
					Console.WriteLine("=== Filtered count for filtered CTE ===")
					Console.WriteLine("Count: {0}", uow.Evaluate(Of CteStructure_1)(New AggregateOperand(Nothing, Nothing, Aggregate.Count, Nothing), New OperandProperty("ID") > 1))
					Console.WriteLine("=== Select all with filtered CTE ===")
					For Each cteS In New XPCollection(Of CteStructure_1)(uow)
						Console.WriteLine(cteS.ToString())
					Next cteS
					Console.WriteLine("=== Linq2XPO ===")
					For Each cteS In uow.Query(Of CteStructure_1)().Where(Function(q) q.Math.HasValue AndAlso q.Math.Value >= 0)
						Console.WriteLine(cteS.ToString())
					Next cteS
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
		Public Property Str() As String
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
		<Key> _
		Public ID As Integer
		Public [String] As String
		Public Math? As Double
		Public Overrides Function ToString() As String
			Return String.Format("<'{0}', '{1}', '{2}'>", ID, [String], Math)
		End Function
	End Class
End Namespace
