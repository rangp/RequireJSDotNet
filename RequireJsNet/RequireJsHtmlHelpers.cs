// RequireJS.NET
// Copyright VeriTech.io
// http://veritech.io
// Dual licensed under the MIT and GPL licenses:
// http://www.opensource.org/licenses/mit-license.php
// http://www.gnu.org/licenses/gpl.html

using System;
using System.Collections.Generic;
using System.Linq;

using RequireJsNet.Configuration;
using RequireJsNet.Helpers;
using RequireJsNet.Models;
using Microsoft.AspNetCore.Hosting;
using RequireJsNet.EntryPointResolver;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace RequireJsNet
{
    public class RequireJsHtml
	{
        private readonly PathHelper pathHelper;

        public RequireJsHtml(IHostingEnvironment hostingEnvironment)
        {
            this.pathHelper = new PathHelper(hostingEnvironment);
            
            if(RequireJsOptions.ResolverCollection.IsEmpty())
            {
                RequireJsOptions.ResolverCollection.Add(new DefaultEntryPointResolver(hostingEnvironment));
            }
        }

		/// <summary>
		/// Setup RequireJS to be used in layouts
		/// </summary>
		/// <param name="view">
		/// Html helper.
		/// </param>s
		/// <param name="config">
		/// Configuration object for various options.
		/// </param>
		/// <returns>
		/// The <see cref="MvcHtmlString"/>.
		/// </returns>
		public HtmlString RenderRequireJsSetup(
            ViewContext view,
			RequireRendererConfiguration config,
			string[] globalScriptCalls = null
			)
		{
			if (config == null)
			{
				throw new ArgumentNullException("config");
			}

			var entryPointPath = RequireJsEntryPoint(view, config.EntryPointRoot);
			var entryPointString = "~/";

			if (entryPointPath != null)
			{
				entryPointString = entryPointPath.ToString();
			}

			if (config.ConfigurationFiles == null || !config.ConfigurationFiles.Any())
			{
				throw new Exception("No config files to load.");
			}

			var processedConfigs = config.ConfigurationFiles.Select(r =>
			{
				var resultingPath = this.pathHelper.MapPath(r);
				PathHelper.VerifyFileExists(resultingPath);
				return resultingPath;
			}).ToList();

			var loader = new ConfigLoader(processedConfigs, config.Logger, new ConfigLoaderOptions { LoadOverrides = config.LoadOverrides });
			var resultingConfig = loader.Get();

			var locale = config.LocaleSelector(view);

            var paths = resultingConfig.Paths.PathList as IEnumerable<RequirePath>;

            if (config.LoadOverrides)
            {
                paths = paths.Concat(resultingConfig.BundlePaths.PathList);
            }

			var outputConfig = new JsonRequireOutput
			{
				BaseUrl = config.BaseUrl,
				Locale = locale,
				UrlArgs = config.UrlArgs,
				Paths = paths.ToDictionary(r => r.Key, r => r.Value),
				Shim = resultingConfig.Shim.ShimEntries.ToDictionary(
						r => r.For,
						r => new JsonRequireDeps
								 {
									 Dependencies = r.Dependencies.Select(x => x.Dependency).ToList(),
									 Exports = r.Exports
								 }),
                Bundles = resultingConfig.Bundles,
                WaitSeconds = config.WaitSeconds,
            };

			config.ProcessConfig(outputConfig);

			var options = new JsonRequireOptions
			{
				Locale = locale,
				PageOptions = RequireJsOptions.GetPageOptions(view.HttpContext),
				WebsiteOptions = RequireJsOptions.GetGlobalOptions(),
			};

			config.ProcessOptions(options);

			var configBuilder = new StringBuilder();
            configBuilder.Append("<script>");
            configBuilder.AppendLine();
            configBuilder.Append(JavaScriptHelpers.SerializeAsVariable(options, "requireConfig"));
            configBuilder.AppendLine();
			configBuilder.Append(JavaScriptHelpers.SerializeAsVariable(outputConfig, "require"));
            configBuilder.Append("</script>");
            configBuilder.AppendLine();

            var requireRootBuilder = new StringBuilder();
            requireRootBuilder.Append("<script src='");
            requireRootBuilder.Append(config.RequireJsUrl);
            requireRootBuilder.Append("'></script>");
            requireRootBuilder.AppendLine();

            StringBuilder requireEntryPointBuilder = null;

			if(entryPointPath != null)
			{
				requireEntryPointBuilder = new StringBuilder();
                requireEntryPointBuilder.Append("<script>");
                requireEntryPointBuilder.AppendLine();
                requireEntryPointBuilder.Append(
				JavaScriptHelpers.MethodCall(
				"require", 
				(object)new[] { entryPointString }));
                requireEntryPointBuilder.Append("</script>");
                requireEntryPointBuilder.AppendLine();
            }

			var result = 
				configBuilder.ToString() 
				+ Environment.NewLine
				+ requireRootBuilder.ToString()
                + Environment.NewLine;

			if (globalScriptCalls != null)
			{
				var globalScriptCallsBuilder = new StringBuilder();
                globalScriptCallsBuilder.Append("<script>");
                globalScriptCallsBuilder.AppendLine();
                globalScriptCallsBuilder.Append(
				JavaScriptHelpers.MethodCall(
					"require",
					(object)globalScriptCalls));
                globalScriptCallsBuilder.Append("</script>");
                globalScriptCallsBuilder.AppendLine();

                result += globalScriptCallsBuilder.ToString();
			}

			if(requireEntryPointBuilder != null)
			{
				result += requireEntryPointBuilder.ToString();
			}

			return new HtmlString(result);
		}

		/// <summary>
		/// Returns entry point script relative path
		/// </summary>
		/// <param name="view">
		/// The HtmlHelper instance.
		/// </param>
		/// <param name="root">
		/// Relative root path ex. ~/Scripts/
		/// </param>
		/// <returns>
		/// The <see cref="MvcHtmlString"/>.
		/// </returns>
		public static string RequireJsEntryPoint(ViewContext view, string root)
		{
			return RequireJsOptions.ResolverCollection.Resolve(view, root);
		}

		public static Dictionary<string, int> ToJsonDictionary<TEnum>()
		{
			var enumType = typeof(TEnum);
			return Enum.GetNames(enumType).ToDictionary(r => r, r => Convert.ToInt32(Enum.Parse(enumType, r)));
		}
	}
}