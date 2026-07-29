public interface CyclicInterfaceRefKindA : CyclicInterfaceRefKindB
{
	new void M(ref int x);
}
public interface CyclicInterfaceRefKindB : CyclicInterfaceRefKindA
{
	new void M(ref int x);
}
