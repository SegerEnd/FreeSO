using FSO.Common.Utils;
using FSO.Server.Database.DA.Utils;
using Nancy;
using Nancy.Security;
using System.Xml;

namespace FSO.Server.Servers.Api
{
    public static class NancyExtensions
    {
        public static void DemandModerator(this NancyModule controller)
        {
            controller.RequiresAuthentication();
            var user = controller.Context.CurrentUser;
            user.Claims.Contains("moderator");
        }

        public static void DemandAdmin(this NancyModule controller)
        {
            controller.RequiresAuthentication();
            var user = controller.Context.CurrentUser;
            user.Claims.Contains("admin");
        }

        public static Response AsPagedList<T>(this IResponseFormatter formatter, PagedList<T> list)
        {
            return FormatterExtensions.AsJson<PagedList<T>>(formatter, list)
                        .WithHeader("X-Total-Count", list.Total.ToString())
                        .WithHeader("X-Offset", list.Offset.ToString());
        }

        public static Response AsXml(this IResponseFormatter formatter, IXMLEntity entity)
        {
            var doc = new XmlDocument();
            var firstChild = entity.Serialize(doc);
            doc.AppendChild(firstChild);

            return FormatterExtensions.AsText(formatter, doc.OuterXml).WithContentType("text/xml");
        }
    }
}
