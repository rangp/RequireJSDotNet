// RequireJS.NET
// Copyright VeriTech.io
// http://veritech.io
// Dual licensed under the MIT and GPL licenses:
// http://www.opensource.org/licenses/mit-license.php
// http://www.gnu.org/licenses/gpl.html

using System;

namespace RequireJsNet.Configuration
{
	public static class WriterFactory
	{
		public static IConfigWriter CreateWriter(string path, ConfigLoaderOptions options)
		{
			return new JsonWriter(path, options);
		}
	}
}
