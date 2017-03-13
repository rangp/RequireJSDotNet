using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RequireJsNet.Models;
using Newtonsoft.Json.Converters;

namespace RequireJsNet.Configuration
{
	public class JsonWriter : IConfigWriter
	{
		private readonly ConfigLoaderOptions options;

		public JsonWriter(string path, ConfigLoaderOptions options)
		{
			this.options = options;
			Path = path;
		}

		public string Path { get; private set; }

		public void WriteConfig(ConfigurationCollection conf)
		{
			dynamic obj = new ExpandoObject();
			if (conf.Paths != null && conf.Paths.PathList != null && conf.Paths.PathList.Any())
			{
				obj.paths = GetPaths(conf.Paths.PathList);
			}

			if (conf.Shim != null && conf.Shim.ShimEntries != null && conf.Shim.ShimEntries.Any())
			{
				obj.shim = GetShim(conf.Shim.ShimEntries);
			}

			if (conf.Bundles != null && conf.Bundles.Any())
			{
				obj.bundles = conf.Bundles;
			}

			File.WriteAllText(
				Path, 
				JsonConvert.SerializeObject(
							obj,
							Formatting.Indented,
							new KeyValuePairConverter()
							));
		}

		public dynamic GetPaths(List<RequirePath> pathList)
		{
			return pathList.ToDictionary(
				r => r.Key,
				r =>
					{
						if (string.IsNullOrEmpty(r.DefaultBundle))
						{
							return (object)r.Value;
						}

						return new { path = r.Value, defaultBundle = r.DefaultBundle };
					});
		}

		public dynamic GetShim(List<ShimEntry> shimEntries)
		{
			return shimEntries.ToDictionary(
				r => r.For,
				r =>
					{
						dynamic obj = new ExpandoObject();
						if (r.Dependencies != null && r.Dependencies.Any())
						{
							obj.deps = r.Dependencies.Select(x => x.Dependency);
						}

						if (!string.IsNullOrEmpty(r.Exports))
						{
							obj.exports = r.Exports;
						}

						return obj;
					});
		}
	}
}
