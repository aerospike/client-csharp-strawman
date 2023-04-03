<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

#load "Bin.linq"
#load "Set.linq"
#load "PrimaryKey.linq"

public interface IRecord : IList
{
	public string Name { get; }
	
	public IEnumerable<IBin> Bins { get; }
	
	[AllowNull]
	public ISet Set { get; internal set; }
	
	public TimeSpan TTL { get; set; }
	
	public int? Generation { get; set; }
	
	public IPrimaryKey PrimaryKey { get; }
}

public class Record<T> : IRecord, IList<IBin>
	where T : struct
{
	public Record([NotNull] string name, IEnumerable<IBin> bins)
	{
		this.Name = name;
		this._bins = bins.ToList();
	}
	
	public Record([NotNull] string name)
	{
		this.Name = name;
		this._bins = new List<IBin>();
	}
	
	public string Name { get; }
	
	public IEnumerable<IBin> Bins { get => this._bins; }
	
	[AllowNull]
	public ISet Set { get; internal set; }
	
	public TimeSpan TTL { get; set; }
	
	public int? Generation { get; set; }
	
	[AllowNull]
	T PrimaryKey { get; set; }
	
	byte[] Digest { get; set; }
	
	public int Count { get => this._bins.Count; }
	
	public bool IsFixedSize { get => false; }
	
	public bool IsReadOnly { get => false; }
	
	private List<IBin> _bins;
	
	public IBin this[int index] { get => _bins[index]; set => _bins[index] = value; }
	
	public int IndexOf(IBin bin) => this._bins.FindIndex(b => b.Name == bin.Name);
	
	public void Insert(int index, IBin bin) => this._bins.Insert(index, bin);
	
	public void RemoveAt(int index) => this._bins.RemoveAt(index);
	
	public void Add(IBin bin) => this._bins.Add(bin);
	
	public void Clear() => this._bins.Clear();
	
	public bool Contains(IBin bin) => this._bins.Contains(bin);
	
	public void CopyTo(IBin[] array, int index) => this._bins.CopyTo(array, index);
	
	public bool Remove(IBin bin) => this._bins.Remove(bin);
	
	public IEnumerator<IBin> GetEnumerator() => this._bins.GetEnumerator();
	
	// TODO POCO
}