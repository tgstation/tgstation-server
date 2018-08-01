using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tgstation.Server.Api.Tests
{
	[TestClass]
	public sealed class TestModels
	{
		static IEnumerable<Tuple<Type, ModelAttribute>> Models()
		{
			var modelAttributeType = typeof(ModelAttribute);
			var targetAssembly = modelAttributeType.Assembly;
			foreach(var I in targetAssembly.GetTypes())
			{
				var attr = (ModelAttribute)I.GetCustomAttributes(modelAttributeType, false).FirstOrDefault();
				if (attr != default(ModelAttribute))
					yield return new Tuple<Type, ModelAttribute>(I, attr);
			}
		}

		[TestMethod]
		public void TestModelsPropertiesPermissions()
		{
			var permissionsAttributeType = typeof(PermissionsAttribute);
			foreach (var I in Models())
			{
				foreach (var J in I.Item1.GetProperties())
				{
					var perm = (PermissionsAttribute)J.GetCustomAttributes(permissionsAttributeType, false).FirstOrDefault();
				}
			}
		}
	}
}
