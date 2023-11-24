using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="ModelBuilder"/> <see langword="class"/>.
	/// </summary>
	static class ModelBuilderExtensions
	{
		/// <summary>
		/// Set a given <typeparamref name="TEntity"/>'s property's column charset to "utf8mb4". Only for use with the MySQL/MariaDB provider.
		/// </summary>
		/// <typeparam name="TEntity">The entity.</typeparam>
		/// <param name="modelBuilder">The <see cref="ModelBuilder"/>.</param>
		/// <param name="expression">The <see cref="Expression"/> accessing the relevant property.</param>
		/// <returns><paramref name="modelBuilder"/>.</returns>
		public static ModelBuilder MapMySqlTextField<TEntity>(
			this ModelBuilder modelBuilder,
			Expression<Func<TEntity, string>> expression)
			where TEntity : class
		{
			var property = modelBuilder
				.Entity<TEntity>()
				.Property(expression);
			property
				.HasCharSet("utf8mb4");

			var propertyInfo = GetPropertyFromExpression(expression);
			var stringLengthAttribute = propertyInfo.GetCustomAttribute<StringLengthAttribute>();

			if (stringLengthAttribute?.MaximumLength == Limits.MaximumStringLength)
				property.HasColumnType("longtext");

			return modelBuilder;
		}

		/// <summary>
		/// Get the <see cref="PropertyInfo"/> pointed to by an <paramref name="expression"/>.
		/// </summary>
		/// <typeparam name="TEntity">The entity.</typeparam>
		/// <param name="expression">The <see cref="Expression"/> accessing the relevant property.</param>
		/// <returns>The <see cref="PropertyInfo"/> pointed to by <paramref name="expression"/>.</returns>
		static PropertyInfo GetPropertyFromExpression<TEntity>(Expression<Func<TEntity, string>> expression)
		{
			MemberExpression memberExpression;

			// this line is necessary, because sometimes the expression comes in as Convert(originalexpression)
			if (expression.Body is UnaryExpression unaryExpression)
				if (unaryExpression.Operand is MemberExpression unaryAsMember)
					memberExpression = unaryAsMember;
				else
					throw new ArgumentException("Cannot get property from expression!", nameof(expression));
			else if (expression.Body is MemberExpression)
				memberExpression = (MemberExpression)expression.Body;
			else
				throw new ArgumentException("Cannot get property from expression!", nameof(expression));

			return (PropertyInfo)memberExpression.Member;
		}
	}
}
