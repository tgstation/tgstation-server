using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Tgstation.Server.Common.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> <see langword="class"/>es.
	/// </summary>
	public static class ValueTaskExtensions
	{
		/// <summary>
		/// Fully <see langword="await"/> a given list of <paramref name="tasks"/>.
		/// </summary>
		/// <typeparam name="T">The <see cref="ValueTask{TResult}.Result"/> type.</typeparam>
		/// <param name="tasks">An <see cref="IEnumerable{T}"/> of <see cref="ValueTask{TResult}"/>s.</param>
		/// <param name="totalTasks">The number of elements in <paramref name="tasks"/>.</param>
		/// <returns>A <see cref="ValueTask"/> representing the combined <see langword="await"/>.</returns>
		public static async ValueTask<T[]> WhenAll<T>(IEnumerable<ValueTask<T>> tasks, int totalTasks)
		{
			if (tasks == null)
				throw new ArgumentNullException(nameof(tasks));

			// We don't allocate the list if no task throws
			Exception? exception = null;
			int i = 0;
			var results = new T[totalTasks];
			foreach (var task in tasks)
			{
				try
				{
					results[i] = await task.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					exception ??= ex;
				}

				++i;
			}

			Debug.Assert(i == totalTasks, "Incorrect totalTasks specified!");

			if (exception != null)
				throw exception;

			return results;
		}

		/// <summary>
		/// Fully <see langword="await"/> a given list of <paramref name="tasks"/>.
		/// </summary>
		/// <typeparam name="T">The <see cref="ValueTask{TResult}.Result"/> type.</typeparam>
		/// <param name="tasks">An <see cref="IReadOnlyList{T}"/> of <see cref="ValueTask{TResult}"/>s.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> containing the <see cref="Array"/> of results based on <paramref name="tasks"/>.</returns>
		/// <exception cref="AggregateException">An <see cref="AggregateException"/> containing any <see cref="Exception"/>s thrown by the <paramref name="tasks"/>.</exception>
		public static async ValueTask<T[]> WhenAll<T>(IReadOnlyList<ValueTask<T>> tasks)
		{
			if (tasks == null)
				throw new ArgumentNullException(nameof(tasks));

			var totalTasks = tasks.Count;
			if (totalTasks == 0)
				return Array.Empty<T>();

			// We don't allocate the list if no task throws
			Exception? exception = null;
			var results = new T[totalTasks];
			for (var i = 0; i < totalTasks; i++)
				try
				{
					results[i] = await tasks[i].ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					exception ??= ex;
				}

			if (exception != null)
				throw exception;

			return results;
		}

		/// <summary>
		/// Fully <see langword="await"/> a given list of <paramref name="tasks"/>.
		/// </summary>
		/// <typeparam name="T">The <see cref="ValueTask{TResult}.Result"/> type.</typeparam>
		/// <param name="tasks">An <see cref="Array"/> of <see cref="ValueTask{TResult}"/>s.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> containing the <see cref="Array"/> of results based on <paramref name="tasks"/>.</returns>
		public static ValueTask<T[]> WhenAll<T>(params ValueTask<T>[] tasks) => WhenAll((IReadOnlyList<ValueTask<T>>)tasks);

		/// <summary>
		/// Fully <see langword="await"/> a given list of <paramref name="tasks"/>.
		/// </summary>
		/// <param name="tasks">An <see cref="IEnumerable{T}"/> of <see cref="ValueTask"/>s.</param>
		/// <returns>A <see cref="ValueTask"/> representing the combined <see langword="await"/>.</returns>
		public static async ValueTask WhenAll(IEnumerable<ValueTask> tasks)
		{
			if (tasks == null)
				throw new ArgumentNullException(nameof(tasks));

			// We don't allocate the list if no task throws
			Exception? exception = null;
			foreach (var task in tasks)
				try
				{
					await task.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					exception ??= ex;
				}

			if (exception != null)
				throw exception;
		}

		/// <summary>
		/// Fully <see langword="await"/> a given list of <paramref name="tasks"/>.
		/// </summary>
		/// <param name="tasks">An <see cref="IReadOnlyList{T}"/> of <see cref="ValueTask"/>s.</param>
		/// <returns>A <see cref="ValueTask"/> representing the combined <see langword="await"/>.</returns>
		/// <exception cref="AggregateException">An <see cref="AggregateException"/> containing any <see cref="Exception"/>s thrown by the <paramref name="tasks"/>.</exception>
		public static async ValueTask WhenAll(IReadOnlyList<ValueTask> tasks)
		{
			if (tasks == null)
				throw new ArgumentNullException(nameof(tasks));

			// We don't allocate the list if no task throws
			List<Exception>? exceptions = null;
			for (var i = 0; i < tasks.Count; ++i)
				try
				{
					var task = tasks[i];
					await task.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					exceptions ??= new(tasks.Count - i);
					exceptions.Add(ex);
				}

			if (exceptions != null)
				throw new AggregateException(exceptions);
		}

		/// <summary>
		/// Fully <see langword="await"/> a given list of <paramref name="tasks"/>.
		/// </summary>
		/// <param name="tasks">An <see cref="Array"/> of <see cref="ValueTask"/>s.</param>
		/// <returns>A <see cref="ValueTask"/> representing the combined <see langword="await"/>.</returns>
		/// <exception cref="AggregateException">An <see cref="AggregateException"/> containing any <see cref="Exception"/>s thrown by the <paramref name="tasks"/>.</exception>
		public static ValueTask WhenAll(params ValueTask[] tasks) => WhenAll((IReadOnlyList<ValueTask>)tasks);
	}
}
