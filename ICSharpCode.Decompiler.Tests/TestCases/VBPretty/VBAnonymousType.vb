Imports System

Module Program
	Function MakeAnonymous(ByVal value As Integer, ByVal name As String) As String
		Dim obj = New With {.Value = value, .Name = name}
		Return obj.Name & obj.Value.ToString()
	End Function

	Function MakeKeyedAnonymous(ByVal id As Integer, ByVal text As String) As String
		Dim obj = New With {Key .Id = id, Key .Text = text}
		Return obj.Text & obj.Id.ToString()
	End Function
End Module
