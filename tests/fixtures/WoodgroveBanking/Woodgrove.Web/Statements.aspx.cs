using System;
using System.Web.UI;
using Woodgrove.Domain;

namespace Woodgrove.Web
{
    public partial class Statements : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                StatementGrid.DataSource = StatementArchive.LoadRecent(AccountPortalConfig.DefaultAccount);
                StatementGrid.DataBind();
            }
        }

        protected void ExportButton_Click(object sender, EventArgs e)
        {
            Response.ContentType = "application/octet-stream";
            Response.AddHeader("Content-Disposition", "attachment; filename=statements.bin");
            Response.BinaryWrite(StatementArchive.Serialize(AccountPortalConfig.DefaultAccount));
            Response.End();
        }
    }
}
