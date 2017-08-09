using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RequireJsNet.EntryPointResolver
{
    /// <summary>
    /// EntryPoint Resolver for area-based mvc structures
    /// </summary>
    public class AreaEntryPointResolver : IEntryPointResolver
    {
        private readonly IHostingEnvironment hostingEnvironment;

        public AreaEntryPointResolver(IHostingEnvironment hostingEnvironment)
        {
            this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        /// <inheritdoc />
        public virtual string Resolve(ViewContext viewContext, string entryPointRoot)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            var area = viewContext.RouteData.Values["area"] == null ? null : viewContext.RouteData.Values["area"].ToString();
            var viewName = viewContext.View.Path.Substring(1).Replace(".cshtml", string.Empty);

            var pathBuilder = new StringBuilder();

            if (string.IsNullOrEmpty(area))
            {
                pathBuilder.Append("Site/");
            }

            pathBuilder.Append(viewName);

            var localPathBuilder = new StringBuilder();
            localPathBuilder.Append(this.hostingEnvironment.WebRootPath);
            localPathBuilder.Append("/");
            localPathBuilder.Append(entryPointRoot);
            localPathBuilder.Append(pathBuilder.ToString());
            localPathBuilder.Append(".js");

            if (!File.Exists(localPathBuilder.ToString()))
            {
                return null;
            }

            return pathBuilder.ToString();
        }
    }
}
