<Query Kind="Program">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

public interface IPrimaryKey
{
	public bool TryGetKey<V>(out V outValue) where V : struct;
	
	public byte[] Digest { get; }
}

public class PrimaryKey<T> : IPrimaryKey
where T : struct
{
	public T Key { get; }
	
	public byte[] Digest { get; }
	
	public bool TryGetKey<V>(out V outValue) where V : struct
	{
		outValue = (V)Convert.ChangeType(this.Key, typeof(V));
		return true;				
	}
	
	// TODO what behavior do we want to enforce regarding Keys using this class?
}


