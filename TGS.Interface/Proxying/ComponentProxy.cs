using System;
using System.Dynamic;
using System.Threading.Tasks;

namespace TGS.Interface.Proxying
{
	sealed class ComponentProxy : DynamicObject
	{
		readonly ICallBinder callBinder;
		readonly Type componentType;

		InvokeMemberBinder currentInvokeMemberBinder;
		object[] currentArgs;

		public ComponentProxy(Type _componentType, ICallBinder _callBinder)
		{
			callBinder = _callBinder;
			componentType = _componentType;
		}

		Task<T> HandleCall<T>()
		{
			return callBinder.HandleCall<T>(componentType.Name, currentInvokeMemberBinder.Name, currentArgs);
		}

		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			if (componentType.GetMethod(binder.Name) == null)
			{
				result = null;
				return false;
			}

			currentInvokeMemberBinder = binder;
			currentArgs = args;

			var methodInfo = GetType().GetMethod(nameof(HandleCall)).MakeGenericMethod(binder.ReturnType);
			result = methodInfo.Invoke(callBinder, new object[] { });
			return true;
		}
	}
}
