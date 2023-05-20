using System.Reflection;

namespace Core;

public static class DerivedTypesHelper
{
	public static IEnumerable<Type> GetDerivedTypes(Type baseType)
	{
		return Assembly.GetAssembly(baseType)!.GetTypes()
			.Where(t =>
				t is {IsClass: true, IsAbstract: false} &&
				(baseType.IsInterface ? baseType.IsAssignableFrom(t) : t.IsSubclassOf(baseType))
			);
	}
}