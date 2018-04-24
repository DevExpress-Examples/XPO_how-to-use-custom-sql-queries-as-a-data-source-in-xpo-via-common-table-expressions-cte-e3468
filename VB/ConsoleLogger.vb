Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports DevExpress.Xpo.Logger

Namespace S138124
    Public Class ConsoleLogger
        Implements ILogger

        #Region "ILogger Members"

        Public Sub ClearLog()
        End Sub

        Public ReadOnly Property Count() As Integer
            Get
                Return 0
            End Get
        End Property

        Public Property Enabled() As Boolean
            Get
                Return True
            End Get
            Set(ByVal value As Boolean)
            End Set
        End Property

        Public ReadOnly Property LostMessageCount() As Integer
            Get
                Return 0
            End Get
        End Property

        Public ReadOnly Property IsServerActive() As Boolean
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Capacity() As Integer
            Get
                Return Int32.MaxValue
            End Get
        End Property

        Public Sub Log(ByVal message As LogMessage)
            If message.MessageType <> LogMessageType.DbCommand Then
                Return
            End If
            Console.WriteLine(message.MessageText)
        End Sub

        Public Sub Log(ByVal messages() As LogMessage)
            For Each message As LogMessage In messages
                Log(messages)
            Next message
        End Sub

        #End Region
    End Class
End Namespace