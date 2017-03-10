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

            // Using the requirejs build.js file means cutting off the () around the configuration
            // so that the deserialization can work
            if(text.StartsWith("({") && text.EndsWith("})"))
            {
                text = text.Substring(1, text.Count() -2);
            }

			var collection = new ConfigurationCollection();
			var deserialized = (JObject)JsonConvert.DeserializeObject(text);
			collection.FilePath = Path;
			collection.Paths = GetPaths(deserialized);
			collection.Shim = GetShim(deserialized);
			collection.Map = GetMap(deserialized);

			if (options.ProcessBundles)
			{
				collection.Bundles = GetBundles(deserialized);
			}

			collection.Overrides = GetOverrides(deserialized);


			return collection;
		}

		private RequirePaths GetPaths(JObject document)
		{
			var paths = new RequirePaths();
			paths.PathList = new List<RequirePath>();
			if (document != null && document["paths"] != null)
			{
				paths.PathList = document["paths"].Select(
				r =>
				{
					var result = new RequirePath();
					var prop = (JProperty)r;
					result.Key = prop.Name;
					if (prop.Value.Type == JTokenType.String)
					{
						result.Value = prop.Value.ToString();
					}
					else
					{
						var pathObj = (JObject)prop.Value;
						result.Value = pathObj["path"].ToString();
						result.DefaultBundle = pathObj["defaultBundle"].ToString();
					}

					return result;
				}).ToList();    
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
			if (document != null && document["bundles"] != null)
			{
                bundles = document["bundles"].Value<RequireBundles>();
			}

			return bundles;
		}

		private RequireMap GetMap(JObject document)
		{
			var map = new RequireMap();
			map.MapElements = new List<RequireMapElement>();
			if (document != null && document["map"] != null)
			{
				map.MapElements = document["map"].Select(
					r =>
						{
							var currentMap = new RequireMapElement();
							currentMap.Replacements = new List<RequireReplacement>();
							var prop = (JProperty)r;
							currentMap.For = prop.Name;
							var mapDefinition = prop.Value;
							if (mapDefinition != null)
							{
								currentMap.Replacements = mapDefinition.Select(x => new RequireReplacement()
																						{
																							OldKey = ((JProperty)x).Name,
																							NewKey = ((JProperty)x).Value.ToString()
																						}).ToList();
							}

							return currentMap;
						})
						.ToList();
			}
			
			return map;
		}

		private List<CollectionOverride> GetOverrides(JObject document)
		{
			var overrideList = new List<CollectionOverride>();
			if (document["overrides"] != null)
			{
				overrideList = document["overrides"].Select(
					r =>
						{
							var overObj = new CollectionOverride();
							var prop = (JProperty)r;
							
							overObj.BundleId = prop.Name;
							var valObj = prop.Value as JObject;
							if (valObj != null)
							{
								overObj.Map = GetMap(prop.Value as JObject);
								overObj.Paths = GetPaths(prop.Value as JObject);
								overObj.Shim = GetShim(prop.Value as JObject);
								var bundledItems = valObj["bundledScripts"] as JArray;
								if (bundledItems != null)
								{
									overObj.BundledScripts = bundledItems.Select(x => x.ToString()).ToList();
								}
								
							}

							return overObj;
						})
					.ToList();
			}

			return overrideList;
		}
	}
}
