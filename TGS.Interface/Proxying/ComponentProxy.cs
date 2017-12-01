using ImpromptuInterface;
using System;
using System.Dynamic;
using System.Reflection;
using System.Threading.Tasks;

namespace TGS.Interface.Proxying
{
	sealed class ComponentProxy : DynamicObject
	{
		readonly ICallBinder callBinder;
		readonly Type componentType;

		MethodInfo currentInvokeMember;
		object[] currentArgs;

		public ComponentProxy(Type _componentType, ICallBinder _callBinder)
		{
			callBinder = _callBinder;
			componentType = _componentType;
		}

		Task<T> HandleCall<T>()
		{
			return callBinder.HandleCall<T>(componentType.Name, currentInvokeMember, currentArgs);
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			currentInvokeMember = componentType.GetMethod(binder.Name);
			if (currentInvokeMember == null)
			{
				result = null;
				return false;
			}

			currentArgs = args;

			var ourType = GetType();
			var nonGenericMethod = ourType.GetMethod(nameof(HandleCall), BindingFlags.NonPublic | BindingFlags.Instance);
			var retTask = currentInvokeMember.ReturnType;
			var retType = retTask.GenericTypeArguments[0];
			var methodInfo = nonGenericMethod.MakeGenericMethod(retType);

			result = methodInfo.Invoke(this, null);
			return true;
		}

		public T ToComponent<T>() where T : class
		{
			return Impromptu.ActLike<T>(this);
		}
	}
}
