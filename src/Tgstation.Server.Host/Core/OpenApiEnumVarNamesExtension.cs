using System;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Implements the "x-enum-varnames" OpenAPI 3.0 extension.
	/// </summary>
	sealed class OpenApiEnumVarNamesExtension : IOpenApiExtension
	{
		/// <summary>
		/// The <see cref="Type"/> of the <see cref="Enum"/> being described.
		/// </summary>
		readonly Type enumType;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenApiEnumVarNamesExtension"/> class.
		/// </summary>
		/// <param name="enumType">The value of <see cref="enumType"/>,.</param>
		private OpenApiEnumVarNamesExtension(Type enumType)
		{
			this.enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
		}

		/// <summary>
		/// Applies the extension to a give <paramref name="openApiSchema"/>.
		/// </summary>
		/// <param name="openApiSchema">The <see cref="OpenApiSchema"/> to apply to.</param>
		/// <param name="enumType">The <see cref="Type"/> of the <see cref="Enum"/> being described.</param>
		public static void Apply(OpenApiSchema openApiSchema, Type enumType)
		{
			if (openApiSchema == null)
				throw new ArgumentNullException(nameof(openApiSchema));

			openApiSchema.Extensions.Add("x-enum-varnames", new OpenApiEnumVarNamesExtension(enumType));
		}

		/// <inheritdoc />
		public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
		{
			if (writer == null)
				throw new ArgumentNullException(nameof(writer));

			if (specVersion != OpenApiSpecVersion.OpenApi3_0)
				throw new InvalidOperationException("This extension only applies to OpenAPI 3.0!");

			writer.WriteStartArray();
			foreach (var enumValue in Enum.GetValues(enumType))
			{
				var enumName = enumValue.ToString();
				var field = enumType.GetField(enumName);
				if (field.IsDefined(typeof(ObsoleteAttribute), false))
					enumName = $"DEPRECATED_{enumName}";

				writer.WriteValue(enumName);
			}

			writer.WriteEndArray();
		}
	}
}
