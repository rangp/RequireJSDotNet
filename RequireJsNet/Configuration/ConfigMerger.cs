// RequireJS.NET
// Copyright VeriTech.io
// http://veritech.io
// Dual licensed under the MIT and GPL licenses:
// http://www.opensource.org/licenses/mit-license.php
// http://www.gnu.org/licenses/gpl.html

using System;
using System.Collections.Generic;
using System.Linq;

using RequireJsNet.Models;

namespace RequireJsNet.Configuration
{
    public class ConfigMerger
    {
        private readonly List<ConfigurationCollection> collections;
        private readonly ConfigurationCollection finalCollection = new ConfigurationCollection();

        private readonly ConfigLoaderOptions options;

        public ConfigMerger(List<ConfigurationCollection> collections, ConfigLoaderOptions options)
        {
            this.options = options;
            this.collections = collections;
            finalCollection.Paths = new RequirePaths();
            finalCollection.BundlePaths = new RequirePaths();
            finalCollection.Paths.PathList = new List<RequirePath>();
            finalCollection.BundlePaths.PathList = new List<RequirePath>();
            finalCollection.Shim = new RequireShim();
            finalCollection.Shim.ShimEntries = new List<ShimEntry>();
            finalCollection.Bundles = new RequireBundles();
        }

        public ConfigurationCollection GetMerged()
        {
            foreach (var coll in collections)
            {
                if (coll.Paths != null && coll.Paths.PathList != null)
                {
                    MergePaths(coll);    
                }

                if (coll.Shim != null && coll.Shim.ShimEntries != null)
                {
                    MergeShims(coll);    
                }

                if (coll.BundlePaths != null && coll.BundlePaths.PathList != null)
                {
                    MergeBundlePaths(coll);    
                }
            }

            this.MergeBundles(this.collections);    
            
            return finalCollection;
        }

        private void MergePaths(ConfigurationCollection collection)
        {
            var finalPaths = finalCollection.Paths.PathList;
            foreach (var path in collection.Paths.PathList)
            {
                var existing = finalPaths.Where(r => r.Key == path.Key).FirstOrDefault();
                if (existing != null)
                {
                    existing.Value = path.Value;
                }
                else
                {
                    finalPaths.Add(path);
                }
            }
        }

        private void MergeShims(ConfigurationCollection collection)
        {
            var finalShims = finalCollection.Shim.ShimEntries;
            foreach (var shim in collection.Shim.ShimEntries)
            {
                var existingKey = finalShims.Where(r => r.For == shim.For).FirstOrDefault();
                if (existingKey != null)
                {
                    existingKey.Exports = shim.Exports;
                    existingKey.Dependencies.AddRange(shim.Dependencies);

                    // distinct by Dependency
                    existingKey.Dependencies = existingKey.Dependencies
                                                            .GroupBy(r => r.Dependency)
                                                            .Select(r => r.LastOrDefault())
                                                            .ToList();
                }
                else
                {
                    finalShims.Add(shim);
                }
            }
        }

        private void MergeBundlePaths(ConfigurationCollection collection)
        {
            var finalPaths = finalCollection.BundlePaths.PathList;
            foreach (var path in collection.BundlePaths.PathList)
            {
                var existing = finalPaths.Where(r => r.Key == path.Key).FirstOrDefault();
                if (existing != null)
                {
                    existing.Value = path.Value;
                }
                else
                {
                    finalPaths.Add(path);
                }
            }
        }

        private void MergeBundles(List<ConfigurationCollection> collection)
        {
            if (!collection.SelectMany(r => r.Bundles).Any())
            {
                return;
            }

            this.MergeExistingBundles(collection);
            this.EnsureNoDuplicatesInBundles();
        }

        private void EnsureNoDuplicatesInBundles()
        {
            var newBundles = new RequireBundles();

            foreach (var requireBundle in finalCollection.Bundles)
            {
                newBundles[requireBundle.Key] = requireBundle.Value.Distinct().ToList();
            }

            finalCollection.Bundles = newBundles;
        }

        private void MergeExistingBundles(List<ConfigurationCollection> collection)
        {
            foreach (var configuration in collection)
            {
                foreach (var bundle in configuration.Bundles)
                {
                    var existingBundle = finalCollection.Bundles.Where(r => r.Key == bundle.Key).FirstOrDefault();
                    if (existingBundle.Value == null)
                    {
                        existingBundle = bundle;
                        finalCollection.Bundles.Add(existingBundle.Key, bundle.Value);
                    }
                    else
                    {
                        // add without checking for duplicates, we'll filter them out later
                        existingBundle.Value.AddRange(bundle.Value);
                    }
                }
            }
            
        }
    }
}
