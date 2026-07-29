using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class BaseMultiAttribute : Attribute
{
}
public class DerivedMultiAttribute : BaseMultiAttribute
{
}
[DerivedMulti]
[DerivedMulti]
public class RepeatedInheritedMultiple
{
}
