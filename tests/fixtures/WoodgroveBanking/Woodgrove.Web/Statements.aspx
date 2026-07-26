<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Statements.aspx.cs" Inherits="Woodgrove.Web.Statements" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Woodgrove Bank — Statements</title>
</head>
<body>
    <form id="statementForm" runat="server">
        <div>
            <asp:Label ID="AccountLabel" runat="server" Text="Account" />
            <asp:GridView ID="StatementGrid" runat="server" AutoGenerateColumns="true" />
            <asp:Button ID="ExportButton" runat="server" Text="Export" OnClick="ExportButton_Click" />
        </div>
    </form>
</body>
</html>
