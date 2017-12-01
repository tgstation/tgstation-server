using ImpromptuInterface;
using System;
using System.Dynamic;
using System.Reflection;
using System.Threading.Tasks;
using TGS.Interface.Components;

namespace TGS.Interface.Proxying
{
	/// <summary>
	/// Dynamic proxy used to forward <see cref="ITGComponent"/> calls into a <see cref="ICallBinder"/>. Should not be used across threads
	/// </summary>
	sealed class ComponentProxy : DynamicObject
	{
		/// <summary>
		/// The <see cref="ICallBinder"/> to forward calls to
		/// </summary>
		readonly ICallBinder callBinder;
		/// <summary>
		/// The <see cref="Type"/> of the <see cref="ITGComponent"/> we're mocking
		/// </summary>
		readonly Type componentType;

		/// <summary>
		/// Used as a temporary variable during a call to <see cref="HandleCall{T}"/>
		/// </summary>
		MethodInfo currentInvokeMember;
		/// <summary>
		/// Used as a temporary variable during a call to <see cref="HandleCall{T}"/>
		/// </summary>
		object[] currentArgs;

		/// <summary>
		/// Check that <paramref name="componentType"/> is an inteface extending <see cref="ITGComponent"/>
		/// </summary>
		/// <param name="componentType">The <see cref="Type"/> to check</param>
		static void CheckComponentType(Type componentType)
		{
			if (!componentType.IsInterface || !typeof(ITGComponent).IsAssignableFrom(componentType))
				throw new ArgumentException(String.Format("{0} must be an interface that extends {2}", nameof(componentType), nameof(ITGComponent)));
		}

		/// <summary>
		/// Construct a <see cref="ComponentProxy"/>
		/// </summary>
		/// <param name="_componentType">The <see cref="Type"/> of the <see cref="ITGComponent"/> to mock</param>
		/// <param name="_callBinder"></param>
		public ComponentProxy(Type _componentType, ICallBinder _callBinder)
		{
			callBinder = _callBinder;
			componentType = _componentType;
			CheckComponentType(componentType);
		}

		/// <summary>
		/// Fowards a call to the <see cref="callBinder"/>
		/// </summary>
		/// <typeparam name="T">The return type of the returned <see cref="Task"/></typeparam>
		/// <returns>A <see cref="Task{Type}"/> returning <typeparamref name="T"/> wrapping the call</returns>
		Task<T> HandleCall<T>()
		{
			return callBinder.HandleCall<T>(componentType.Name, currentInvokeMember, currentArgs);
		}

		/// <summary>
		/// Called when a mock call to <see cref="componentType"/> is made through <see langword="this"/>
		/// </summary>
		/// <param name="binder">The <see cref="InvokeMemberBinder"/> describing the call</param>
		/// <param name="args">The call arguments</param>
		/// <param name="result">The result of the call</param>
		/// <returns><see langword="true"/> if we handled the call, <see langword="false"/> otherwise</returns>
		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			currentInvokeMember = componentType.GetMethod(binder.Name);
			if (currentInvokeMember == null)
			{
				result = null;
				return false;
			}

			var retTask = currentInvokeMember.ReturnType;
			if(!typeof(Task).IsAssignableFrom(retTask))
			{
				result = null;
				return false;
			}

			currentArgs = args;

			var ourType = GetType();
			var nonGenericMethod = ourType.GetMethod(nameof(HandleCall), BindingFlags.NonPublic | BindingFlags.Instance);
			var retType = retTask.GenericTypeArguments[0];
			var methodInfo = nonGenericMethod.MakeGenericMethod(retType);

			result = methodInfo.Invoke(this, null);
			return true;
		}

		/// <summary>
		/// Converts <see langword="this"/> into mock of a <see cref="ITGComponent"/>
		/// </summary>
		/// <typeparam name="T">The type of <see cref="ITGComponent"/> to mock</typeparam>
		/// <returns><see langword="this"/> mocked as <typeparamref name="T"/></returns>
		public T ToComponent<T>() where T : class
		{
			CheckComponentType(typeof(T));
			return Impromptu.ActLike<T>(this);
		}
	}
}
