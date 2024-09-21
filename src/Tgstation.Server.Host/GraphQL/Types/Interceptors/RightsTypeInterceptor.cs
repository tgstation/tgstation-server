using System;
using System.Collections.Generic;

using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.GraphQL.Types.Interceptors
{
	/// <summary>
	/// Fixes the names used for the default flags types in API rights.
	/// </summary>
	sealed class RightsTypeInterceptor : TypeInterceptor
	{
		/// <summary>
		/// Names of rights GraphQL object types.
		/// </summary>
		private readonly HashSet<string> objectNames;

		/// <summary>
		/// Names of rights GraphQL input types.
		/// </summary>
		private readonly HashSet<string> inputNames;

		/// <summary>
		/// Initializes a new instance of the <see cref="RightsTypeInterceptor"/> class.
		/// </summary>
		public RightsTypeInterceptor()
		{
			objectNames = new HashSet<string>();
			inputNames = new HashSet<string>();

			foreach (var rightType in Enum.GetValues<RightsType>())
			{
				var flagName = $"{rightType}RightsFlags";

				objectNames.Add(flagName);
				inputNames.Add($"{flagName}Input");
			}
		}

		/// <summary>
		/// Fix the "is" prefix on a given set of <paramref name="fields"/>.
		/// </summary>
		/// <typeparam name="TField">The <see cref="Type"/> of <see cref="FieldDefinitionBase"/> to correct.</typeparam>
		/// <param name="fields">The <see cref="IBindableList{T}"/> of <typeparamref name="TField"/>s to operate on.</param>
		static void FixFields<TField>(IBindableList<TField> fields)
			where TField : FieldDefinitionBase
		{
			TField? noneField = null;

			const string NoneFieldName = "isNone";
			foreach (var field in fields)
			{
				var fieldName = field.Name;
				if (fieldName == NoneFieldName)
				{
					noneField = field;
					continue;
				}

				const string IsPrefix = "is";
				if (!fieldName.StartsWith(IsPrefix))
					throw new InvalidOperationException("Expected flags enum type field to start with \"is\"!");

				field.Name = $"can{fieldName[IsPrefix.Length..]}";
			}

			if (noneField == null)
				throw new InvalidOperationException($"Expected flags enum type field to contain \"{NoneFieldName}\"!");

			fields.Remove(noneField);
		}

		/// <inheritdoc />
		public override void OnBeforeRegisterDependencies(ITypeDiscoveryContext discoveryContext, DefinitionBase definition)
		{
			ArgumentNullException.ThrowIfNull(definition);

			if (definition is ObjectTypeDefinition objectTypeDef)
			{
				if (objectNames.Contains(objectTypeDef.Name))
					FixFields(objectTypeDef.Fields);
			}
			else if (definition is InputObjectTypeDefinition inputTypeDef)
				if (inputNames.Contains(inputTypeDef.Name))
					FixFields(inputTypeDef.Fields);
		}
	}
}
