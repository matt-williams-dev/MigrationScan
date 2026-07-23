using System;
using System.Web.UI;

namespace LegacyWebForms
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Message.Text = "Hello from " + Server.MachineName;
        }
    }
}
