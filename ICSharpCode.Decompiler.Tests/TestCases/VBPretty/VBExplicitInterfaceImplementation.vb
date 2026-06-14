Imports System

Public Interface IHasNameCountChanged
	ReadOnly Property Name As String
	Property Count As Integer
	Event Changed As EventHandler
End Interface

Public Class VBExplicitInterfaceImplementation
	Implements IHasNameCountChanged

	Public ReadOnly Property TheName As String Implements IHasNameCountChanged.Name
		Get
			Return Nothing
		End Get
	End Property

	Public Property TheCount As Integer Implements IHasNameCountChanged.Count

	Public Event SomethingChanged As EventHandler Implements IHasNameCountChanged.Changed
End Class
