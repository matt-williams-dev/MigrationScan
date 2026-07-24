using System.Web.Services;

namespace LegacyAsmxService
{
    [WebService(Namespace = "http://contoso.example/")]
    public class Service : WebService
    {
        [WebMethod]
        public string Ping() => "pong";
    }
}
