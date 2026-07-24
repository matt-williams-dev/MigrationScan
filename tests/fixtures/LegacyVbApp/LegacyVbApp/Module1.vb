Imports System
Imports System.Configuration
Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Text
Imports Microsoft.Win32

' A legacy VB console tool exercising several runtime-failure APIs. These are now detected
' by the same Tier 2 rules that scan C# — the syntax queries are language-neutral.
Module Module1

    Sub Main()
        ' MIG5001 — ConfigurationManager.AppSettings
        Dim environment As String = ConfigurationManager.AppSettings("Environment")

        ' MIG4002 — Windows Registry
        Dim key As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Contoso")

        ' MIG8002 — Encoding.Default (ANSI on Framework, UTF-8 on modern .NET)
        Dim bytes() As Byte = Encoding.Default.GetBytes(environment)

        ' MIG8003 — code page encoding without provider registration
        Dim ansi As Encoding = Encoding.GetEncoding(1252)

        ' MIG6001 — BinaryFormatter (removed in .NET 9)
        Dim formatter As New BinaryFormatter()
        Using stream As New MemoryStream()
            formatter.Serialize(stream, environment)
        End Using

        Console.WriteLine("Legacy VB console app: " & environment)
    End Sub

End Module
