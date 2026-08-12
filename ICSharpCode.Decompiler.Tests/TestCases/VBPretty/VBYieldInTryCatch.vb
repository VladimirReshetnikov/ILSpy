Imports System
Imports System.Collections.Generic

Public Class VBYieldInTryCatch
	Private NotInheritable Class ThrowingSequence
		Private ReadOnly label As String
		Private ReadOnly throwBeforeFirstValue As Boolean
		Private ReadOnly throwAfterFirstValue As Boolean

		Public Sub New(label As String, throwBeforeFirstValue As Boolean, throwAfterFirstValue As Boolean)
			Me.label = label
			Me.throwBeforeFirstValue = throwBeforeFirstValue
			Me.throwAfterFirstValue = throwAfterFirstValue
		End Sub

		Public Function GetEnumerator() As ThrowingEnumerator
			Console.WriteLine(label & ":get-enumerator")
			If throwBeforeFirstValue Then
				Throw New InvalidOperationException("before")
			End If
			Return New ThrowingEnumerator(label, throwAfterFirstValue)
		End Function
	End Class

	Private Structure ThrowingEnumerator
		Private ReadOnly label As String
		Private ReadOnly throwAfterFirstValue As Boolean
		Private position As Integer

		Public Sub New(label As String, throwAfterFirstValue As Boolean)
			Me.label = label
			Me.throwAfterFirstValue = throwAfterFirstValue
			position = 0
		End Sub

		Public ReadOnly Property Current As Integer
			Get
				Return position
			End Get
		End Property

		Public Function MoveNext() As Boolean
			position += 1
			Console.WriteLine(label & ":move-next:" & position)
			If throwAfterFirstValue AndAlso position = 2 Then
				Throw New InvalidOperationException("after")
			End If
			Return position <= 2
		End Function
	End Structure

	Private Shared Iterator Function Enumerate(label As String, sequence As ThrowingSequence) As IEnumerable(Of Integer)
		Try
			For Each item In sequence
				Yield TraceValue(label, item)
			Next
		Catch ex As InvalidOperationException
			Console.WriteLine(label & ":catch:" & ex.Message)
		End Try
	End Function

	Private Shared Function TraceValue(label As String, value As Integer) As Integer
		Console.WriteLine(label & ":yield:" & value)
		Return value
	End Function

	Private Shared Sub Run(label As String, sequence As ThrowingSequence)
		Console.WriteLine(label & ":start")
		For Each value In Enumerate(label, sequence)
			Console.WriteLine(label & ":consumer:" & value)
		Next
		Console.WriteLine(label & ":end")
	End Sub

	Public Shared Sub Main()
		Run("before", New ThrowingSequence("before", True, False))
		Run("after", New ThrowingSequence("after", False, True))
		Run("complete", New ThrowingSequence("complete", False, False))
	End Sub
End Class
