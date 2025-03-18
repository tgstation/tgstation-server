using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Tgstation.Server.ReleaseNotes
{
	internal class ChangelistEqualityComparer : IEqualityComparer<Change>
	{
		public bool Equals(Change x, Change y)
		{
			if (x == y)
				return true;

			if (x == null)
				return false;

			if (y == null)
				return false;

			return JsonSerializer.Serialize(x) == JsonSerializer.Serialize(y);
		}

		public int GetHashCode([DisallowNull] Change obj)
		{
			return JsonSerializer.Serialize(obj).GetHashCode();
		}
	}
}
