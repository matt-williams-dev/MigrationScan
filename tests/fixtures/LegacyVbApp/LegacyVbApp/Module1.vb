Imports System
Imports System.Configuration

' VB source. Not yet analyzed at the syntax level (Tier 2 VB rules are a later step), so the
' ConfigurationManager.AppSettings call below does NOT raise MIG5001 the way the C# equivalent
' would — while the project-level (Tier 1) rules still apply to this project.
Module Module1
    Sub Main()
        Dim environment As String = ConfigurationManager.AppSettings("Environment")
        Console.WriteLine("Legacy VB console app: " & environment)
    End Sub
End Module
