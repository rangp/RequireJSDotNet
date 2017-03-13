using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RequireJsNet.Models;

namespace RequireJsNet.Configuration
{
	public class JsonReader : IConfigReader
	{
		private readonly string path;

		private readonly ConfigLoaderOptions options;

		private readonly IFileReader fileReader;

		public JsonReader(string path, ConfigLoaderOptions options)
		{
			this.path = path;
			this.options = options;
		}

		public JsonReader(string path, ConfigLoaderOptions options, IFileReader reader)
		{
			this.path = path;
			this.options = options;
			this.fileReader = reader;
		}

		public string Path
		{
			get
			{
				return this.path;
			}
		}

		public ConfigurationCollection ReadConfig()
		{
			string text;
			if (fileReader == null)
			{
				text = File.ReadAllText(Path);    
			}
			else
			{
				text = fileReader.ReadFile(path);
			}

			var collection = new ConfigurationCollection();
			var deserialized = (JObject)JsonConvert.DeserializeObject(text);
			collection.FilePath = Path;
			collection.Paths = GetPaths(deserialized);
			collection.Shim = GetShim(deserialized);
			collection.BundlePaths = GetPaths(deserialized, "bundlePaths");

			collection.Bundles = GetBundles(deserialized);

			return collection;
		}

		private RequirePaths GetPaths(JObject document, string key = "paths")
		{
			var paths = new RequirePaths();
			paths.PathList = new List<RequirePath>();
			if (document != null && document[key] != null)
			{
                paths.PathList = document[key].Select(r => new RequirePath { Key = ((JProperty)r).Name, Value = ((JProperty)r).Value.Value<string>() }).ToList();
			}
			
			return paths;
		}

		private RequireShim GetShim(JObject document)
		{
			var shim = new RequireShim();
			shim.ShimEntries = new List<ShimEntry>();
			if (document != null && document["shim"] != null)
			{
				shim.ShimEntries = document["shim"].Select(
					r =>
						{
							var result = new ShimEntry();
							var prop = (JProperty)r;
							result.For = prop.Name;
							var shimObj = (JObject)prop.Value;
							result.Exports = shimObj["exports"] != null ? shimObj["exports"].ToString() : null;
							var depArr = (JArray)shimObj["deps"];
							result.Dependencies = new List<RequireDependency>();
							if (depArr != null)
							{
								result.Dependencies = depArr.Select(dep => new RequireDependency
																			   {
																				   Dependency = dep.ToString()
																			   })
															.ToList();
							}

							return result;
						})
						.ToList();                
			}

			return shim;
		}

		private RequireBundles GetBundles(JObject document)
		{
			var bundles = new RequireBundles();
			if (document != null && document["bundles"] != null && this.options.LoadOverrides)
			{
                bundles =  JsonConvert.DeserializeObject<RequireBundles>(document["bundles"].ToString());
			}

			return bundles;
		}
	}
}
