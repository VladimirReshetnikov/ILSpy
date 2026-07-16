// A static local function has a loop-body local whose generated name 'type2' happens to equal
// the name of a local in the enclosing method. They are distinct locals in distinct IL methods,
// so each must get its own declaration. The local function's local must be declared inside the
// local function; declaring it in the enclosing method would make its uses bind to the enclosing
// local, which is illegal inside a static local function (CS8421).
using System;
using System.Reflection;

public class Repro
{
	public static string Present(Type type)
	{
		Type type2 = FindInherited(type);
		if (type.IsGenericType && (object)type2 == null)
		{
			return "generic";
		}
		if (type.IsNested)
		{
			return Present(type2 ?? ((MemberInfo)type).DeclaringType) + "." + ((MemberInfo)type).Name;
		}
		return ((MemberInfo)type).Name;
		static Type FindInherited(Type type)
		{
			if (!type.IsNested)
			{
				return null;
			}
			if (!type.IsGenericType)
			{
				return null;
			}
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			Type declaringType = ((MemberInfo)genericTypeDefinition).DeclaringType;
			if ((object)declaringType == null)
			{
				return null;
			}
			Type[] genericArguments = genericTypeDefinition.GetGenericArguments();
			Type[] genericArguments2 = declaringType.GetGenericArguments();
			if (genericArguments.Length != genericArguments2.Length)
			{
				return null;
			}
			for (int i = 0; i < genericArguments.Length; i++)
			{
				Type type2 = genericArguments[i];
				Type type3 = genericArguments2[i];
				if (((MemberInfo)type2).Name != ((MemberInfo)type3).Name)
				{
					return null;
				}
				if (type2.GenericParameterAttributes != type3.GenericParameterAttributes)
				{
					return null;
				}
				Type[] genericParameterConstraints = type2.GetGenericParameterConstraints();
				Type[] genericParameterConstraints2 = type3.GetGenericParameterConstraints();
				if (genericParameterConstraints.Length != genericParameterConstraints2.Length)
				{
					return null;
				}
			}
			return declaringType.MakeGenericType(type.GetGenericArguments());
		}
	}
}
