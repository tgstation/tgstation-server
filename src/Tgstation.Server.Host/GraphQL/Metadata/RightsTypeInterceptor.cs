using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.GraphQL.Types;

namespace Tgstation.Server.Host.GraphQL.Metadata
{
	/// <summary>
	/// Fixes the names used for the default flags types in API rights.
	/// </summary>
	sealed class RightsTypeInterceptor : TypeInterceptor
	{
		/// <summary>
		/// Prefix normally used by hot chocolate for flag enums.
		/// </summary>
		const string IsPrefix = "is";

		/// <summary>
		/// Name given to default None fields.
		/// </summary>
		const string NoneFieldName = $"{IsPrefix}None";

		/// <summary>
		/// Names of rights GraphQL input types.
		/// </summary>
		private readonly HashSet<string> inputNames;

		/// <summary>
		/// Initializes a new instance of the <see cref="RightsTypeInterceptor"/> class.
		/// </summary>
		public RightsTypeInterceptor()
		{
			var rightTypes = Enum.GetValues<RightsType>();
			inputNames = new HashSet<string>(rightTypes.Length);

			foreach (var rightType in rightTypes)
			{
				var rightName = rightType.ToString();
				inputNames.Add($"{rightName}RightsFlagsInput");
			}
		}

		/// <summary>
		/// Fix the "is" prefix on a given set of <paramref name="fields"/>.
		/// </summary>
		/// <typeparam name="TField">The <see cref="Type"/> of <see cref="FieldDefinitionBase"/> to correct.</typeparam>
		/// <param name="fields">The <see cref="IBindableList{T}"/> of <typeparamref name="TField"/>s to operate on.</param>
		static void FixFields(IBindableList<InputFieldDefinition> fields)
		{
			InputFieldDefinition? noneField = null;

			foreach (var field in fields)
			{
				var fieldName = field.Name;
				if (fieldName == NoneFieldName)
				{
					noneField = field;
					continue;
				}

				if (!fieldName.StartsWith(IsPrefix))
					throw new InvalidOperationException("Expected flags enum type field to start with \"is\"!");

				field.Name = $"can{fieldName[IsPrefix.Length..]}";
				field.Type = TypeReference.Parse($"{ScalarNames.Boolean}!");
			}

			if (noneField == null)
				throw new InvalidOperationException($"Expected flags enum type field to contain \"{NoneFieldName}\"!");

			fields.Remove(noneField);
		}

		/// <summary>
		/// Fix the <paramref name="inputValueFormatter"/> for a tweaked field.
		/// </summary>
		/// <param name="inputValueFormatter">The <see cref="IInputValueFormatter"/> to fix.</param>
		static void FixFormatter(IInputValueFormatter inputValueFormatter)
		{
			// now we're hacking privates, but there's a dictionary with bad keys here that needs adjusting
			var dictionary = (Dictionary<string, object>)(inputValueFormatter
				.GetType()
				.GetField("_flags", BindingFlags.Instance | BindingFlags.NonPublic)
				?.GetValue(inputValueFormatter)
				?? throw new InvalidOperationException("Could not locate private enum mapping dictionary field!"));

			foreach (var key in dictionary.Keys.ToList())
			{
				if (key == NoneFieldName)
				{
					dictionary.Remove(key);
					continue;
				}

				var value = dictionary[key];
				var newKey = $"can{key.Substring(IsPrefix.Length)}";
				dictionary.Remove(key);
				dictionary.Add(newKey, value);
			}
		}

		/// <inheritdoc />
		public override void OnAfterRegisterDependencies(ITypeDiscoveryContext discoveryContext, DefinitionBase definition)
		{
			ArgumentNullException.ThrowIfNull(definition);

			if (definition is InputObjectTypeDefinition inputTypeDef)
			{
				const string PermissionSetInputName = $"{nameof(PermissionSet)}Input";
				const string InstancePermissionSetInputName = $"{nameof(InstancePermissionSet)}Input";

				var name = inputTypeDef.Name;
				if (inputNames.Contains(name))
					FixFields(inputTypeDef.Fields);
				else if (name == PermissionSetInputName || name == InstancePermissionSetInputName)
					foreach (var field in inputTypeDef.Fields)
						FixFormatter(field.Formatters.Single());
			}
		}
	}
}
