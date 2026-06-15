public class GridData : GridDataBase
{
	public GridData Parent => null;

	public GridData(GridData node)
		: base(node, (node.Parent != null) ? node.Parent.ToString() : node.ToString(), "Kind")
	{
	}
}
public class GridDataBase
{
	public GridDataBase(object owner, string name, string kind)
	{
	}
}
