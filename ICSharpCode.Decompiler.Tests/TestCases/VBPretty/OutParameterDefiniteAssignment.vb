Imports System.Runtime.InteropServices

Public Class OutParameterDefiniteAssignment
	Public Function EarlyReturnLeavesUnassigned(text As String, <Out> ByRef value As Integer) As Boolean
		If text Is Nothing Then
			Return False
		End If
		value = text.Length
		Return True
	End Function

	Public Sub AssignedOnEveryPath(flag As Boolean, <Out> ByRef result As String)
		If flag Then
			result = "yes"
		Else
			result = "no"
		End If
	End Sub
End Class
