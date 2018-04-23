Imports Microsoft.VisualBasic
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports DevExpress.Xpo
Imports DevExpress.Xpo.Helpers
Imports DevExpress.Xpo.DB
Imports DevExpress.Xpo.DB.Helpers

Namespace DevExpress.Xpo.DB.Cte
	Public Class MSSqlConnectionProviderWithCte
		Inherits MSSqlConnectionProvider
		#Region "infrastructure"
		Public Sub New(ByVal connection As IDbConnection, ByVal autoCreateOption As AutoCreateOption)
			MyBase.New(connection, autoCreateOption)
		End Sub
		Public Shadows Shared Function CreateProviderFromString(ByVal connectionString As String, ByVal autoCreateOption As AutoCreateOption, <System.Runtime.InteropServices.Out()> ByRef objectsToDisposeOnDisconnect() As IDisposable) As IDataStore
			Dim connection As IDbConnection = New SqlConnection(connectionString)
			objectsToDisposeOnDisconnect = New IDisposable() { connection }
			Return CreateProviderFromConnection(connection, autoCreateOption)
		End Function
		Public Shadows Shared Function CreateProviderFromConnection(ByVal connection As IDbConnection, ByVal autoCreateOption As AutoCreateOption) As IDataStore
			Return New MSSqlConnectionProviderWithCte(connection, autoCreateOption)
		End Function
		Shared Sub New()
			RegisterDataStoreProvider(XpoProviderTypeString, New DataStoreCreationFromStringDelegate(AddressOf CreateProviderFromString))
			'RegisterDataStoreProvider("System.Data.SqlClient.SqlConnection", new DataStoreCreationFromConnectionDelegate(CreateProviderFromConnection));
			RegisterFactory(New MSSqlWithCteProviderFactory())
		End Sub
		Public Shadows Shared Sub Register()
		End Sub
		Public Shadows Const XpoProviderTypeString As String = "MSSqlServerWithCte"
		Public Shadows Shared Function GetConnectionString(ByVal server As String, ByVal userid As String, ByVal password As String, ByVal database As String) As String
			Return GetConnectionStringForType(XpoProviderTypeString, server, userid, password, database)
		End Function
		Public Shadows Shared Function GetConnectionString(ByVal server As String, ByVal database As String) As String
			Return GetConnectionStringForType(XpoProviderTypeString, server, database)
		End Function
		Public Shadows Shared Function GetConnectionStringWithAttach(ByVal server As String, ByVal userid As String, ByVal password As String, ByVal attachDbFilename As String, ByVal userInstance As Boolean) As String
			Return GetConnectionStringForTypeWithAttach(XpoProviderTypeString, server, userid, password, attachDbFilename, userInstance)
		End Function
		Public Shadows Shared Function GetConnectionStringWithAttach(ByVal server As String, ByVal attachDbFilename As String, ByVal userInstance As Boolean) As String
			Return GetConnectionStringForTypeWithAttach(XpoProviderTypeString, server, attachDbFilename, userInstance)
		End Function
		#End Region

		Private ReadOnly registeredCte As New Dictionary(Of String, String)()
		Public Sub RegisterCte(ByVal cteName As String, ByVal cteBody As String)
			SyncLock SyncRoot
				registeredCte(cteName) = cteBody
			End SyncLock
		End Sub
		Public Sub UnregisterCte(ByVal cteName As String)
			SyncLock SyncRoot
				registeredCte.Remove(cteName)
			End SyncLock
		End Sub
		Private usedCtes As Dictionary(Of String, String)
		Private Function ProcessPossibleCte(ByVal maybeCteName As String) As String
			Dim cteBody As String = Nothing
			If (Not registeredCte.TryGetValue(maybeCteName, cteBody)) Then
				Return Nothing
			End If
			If usedCtes Is Nothing Then
				usedCtes = New Dictionary(Of String, String)()
			End If
			usedCtes(maybeCteName) = cteBody
			Return maybeCteName
		End Function
		Public Overrides Function FormatTable(ByVal schema As String, ByVal tableName As String) As String
			Return If(ProcessPossibleCte(tableName), MyBase.FormatTable(schema, tableName))
		End Function
		Public Overrides Function FormatTable(ByVal schema As String, ByVal tableName As String, ByVal tableAlias As String) As String
			Dim possibleCte As String = ProcessPossibleCte(tableName)
			If possibleCte IsNot Nothing Then
				Return possibleCte & " "c & tableAlias
			Else
				Return MyBase.FormatTable(schema, tableName, tableAlias)
			End If
		End Function
		Public Overrides Function FormatSelect(ByVal selectedPropertiesSql As String, ByVal fromSql As String, ByVal whereSql As String, ByVal orderBySql As String, ByVal groupBySql As String, ByVal havingSql As String, ByVal skipSelectedRecords As Integer, ByVal topSelectedRecords As Integer) As String
			Dim selectBody As String = MyBase.FormatSelect(selectedPropertiesSql, fromSql, whereSql, orderBySql, groupBySql, havingSql, skipSelectedRecords, topSelectedRecords)
			If usedCtes Is Nothing OrElse usedCtes.Count = 0 Then
				Return selectBody
			End If
			Dim ctes As New List(Of String)(usedCtes.Count)
			For Each cte As KeyValuePair(Of String, String) In usedCtes
				ctes.Add(cte.Key & " "c + cte.Value)
			Next cte
			usedCtes = Nothing
			Return String.Format("with" & Constants.vbLf & "{0}" & Constants.vbLf & "{1}", String.Join("," & Constants.vbLf, ctes.ToArray()), selectBody)
		End Function
		Protected Overrides Function DoInternal(ByVal command As String, ByVal args As Object) As Object
			Select Case command
				Case CteExtensions.CommandRegisterCteName
						If args Is Nothing Then
							Throw New ArgumentNullException("args")
						End If
						Dim cteExpression As String = args.ToString()
						Dim spacePos As Integer = cteExpression.IndexOf(" "c)
						If spacePos < 1 Then
							Throw New ArgumentException("malformed args")
						End If
						Dim cteName As String = cteExpression.Substring(0, spacePos)
						Dim cteBody As String = cteExpression.Substring(spacePos + 1)
						Me.RegisterCte(cteName, cteBody)
						Return Nothing
				Case CteExtensions.CommandUnregisterCteName
						If args Is Nothing Then
							Throw New ArgumentNullException("args")
						End If
						Me.UnregisterCte(args.ToString())
						Return Nothing
			End Select
			Return MyBase.DoInternal(command, args)
		End Function
		Protected Overrides Function ProcessUpdateSchema(ByVal skipIfFirstTableNotExists As Boolean, ParamArray ByVal tables() As DBTable) As UpdateSchemaResult
			Dim realTables As New List(Of DBTable)(tables.Length)
			For Each t As DBTable In tables
				If t.Name.StartsWith("CteStructure_") Then
					If realTables.Count = 0 Then
						skipIfFirstTableNotExists = False
					End If
				Else
					realTables.Add(t)
				End If
			Next t
			If realTables.Count = 0 Then
				Return UpdateSchemaResult.SchemaExists
			End If
			Return MyBase.ProcessUpdateSchema(skipIfFirstTableNotExists, realTables.ToArray())
		End Function
	End Class
	Public Module CteExtensions
		Public Const CommandRegisterCteName As String = "RegisterCte"
		Public Const CommandUnregisterCteName As String = "UnregisterCte"
		<System.Runtime.CompilerServices.Extension> _
		Public Sub RegisterCte(ByVal channel As ICommandChannel, ByVal cteName As String, ByVal cteBody As String)
			channel.Do(CommandRegisterCteName, cteName & " "c & cteBody)
		End Sub
		<System.Runtime.CompilerServices.Extension> _
		Public Sub UnregisterCte(ByVal channel As ICommandChannel, ByVal cteName As String)
			channel.Do(CommandUnregisterCteName, cteName)
		End Sub
	End Module
	#Region "infastructure"
	Public Class MSSqlWithCteProviderFactory
		Inherits MSSqlProviderFactory
		Public Overrides Function CreateProviderFromConnection(ByVal connection As IDbConnection, ByVal autoCreateOption As AutoCreateOption) As IDataStore
			Return MSSqlConnectionProviderWithCte.CreateProviderFromConnection(connection, autoCreateOption)
		End Function
		Public Overrides Function CreateProviderFromString(ByVal connectionString As String, ByVal autoCreateOption As AutoCreateOption, <System.Runtime.InteropServices.Out()> ByRef objectsToDisposeOnDisconnect() As IDisposable) As IDataStore
			Return MSSqlConnectionProviderWithCte.CreateProviderFromString(connectionString, autoCreateOption, objectsToDisposeOnDisconnect)
		End Function
		Public Overrides Function GetConnectionString(ByVal parameters As Dictionary(Of String, String)) As String
			Dim connectionString As String
			Dim useIntegratedSecurity As Boolean = False
			If parameters.ContainsKey(UseIntegratedSecurityParamID) Then
				useIntegratedSecurity = Convert.ToBoolean(parameters(UseIntegratedSecurityParamID))
			End If
			If useIntegratedSecurity Then
				If (Not parameters.ContainsKey(ServerParamID)) OrElse (Not parameters.ContainsKey(DatabaseParamID)) Then
					Return Nothing
				End If
				connectionString = MSSqlConnectionProviderWithCte.GetConnectionString(parameters(ServerParamID), parameters(DatabaseParamID))
			Else
				If (Not parameters.ContainsKey(ServerParamID)) OrElse (Not parameters.ContainsKey(DatabaseParamID)) OrElse (Not parameters.ContainsKey(UserIDParamID)) OrElse (Not parameters.ContainsKey(PasswordParamID)) Then
					Return Nothing
				End If
				connectionString = MSSqlConnectionProviderWithCte.GetConnectionString(parameters(ServerParamID), parameters(UserIDParamID), parameters(PasswordParamID), parameters(DatabaseParamID))
			End If
			Return connectionString
		End Function
		Public Overrides ReadOnly Property ProviderKey() As String
			Get
				Return MSSqlConnectionProviderWithCte.XpoProviderTypeString
			End Get
		End Property
	End Class
	#End Region ' infastructure
End Namespace