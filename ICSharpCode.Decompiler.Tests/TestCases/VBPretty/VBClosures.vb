Imports System
Imports System.Collections.Generic

Public Class VBClosures
	Public Shared Function PerIteration(ByVal items As IEnumerable(Of Integer), ByVal factor As Integer) As List(Of Func(Of Integer))
		Dim result As New List(Of Func(Of Integer))()
		For Each item As Integer In items
			Dim local As Integer = item * factor
			result.Add(Function() local + factor)
		Next
		Return result
	End Function

	Public Shared Function SingleCapture(ByVal factor As Integer) As Func(Of Integer, Integer)
		Return Function(n) n * factor
	End Function
End Class
