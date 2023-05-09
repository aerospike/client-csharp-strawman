<Query Kind="Program">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

#load "Bin.linq"
#load "Set.linq"
#load "PrimaryKey.linq"

void Main()
{
	// Creating a record and add a bin to it
	var recordBins = new List<IBin> { new Bin<string>("binAName", "aa"), new Bin<string>("binBName", "bb") };
	var record = new Record("recordName", recordBins).Dump();
	var intBin = new Bin<int>("binIntName", 1234);
	record.Add(intBin);
	record.Dump();
}

public interface IRecord : IList<IBin>
{
	public string Name { get; }
	
	public IEnumerable<IBin> Bins { get; }
	
	[AllowNull]
	public ISet Set { get; }
	
	public TimeSpan TTL { get; set; }
	
	public int? Generation { get; set; }
	
	public IPrimaryKey PrimaryKey { get; }
	
	public object Tag { get; set; }
	
	public Exception LastException { get; }
	
	public DateTimeOffset LastUpdate { get; }
	
	public bool Touch(); // return true if record exists, we need to check the wire protocol
}

public class Record : IRecord, IList<IBin>
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
	public IPrimaryKey PrimaryKey { get; }
	
	public object Tag { get; set; }
	
	public Exception LastException { get; internal set; }
	
	public DateTimeOffset LastUpdate { get; }
	
	public int Count { get => this._bins.Count; }
	
	public bool IsFixedSize { get => false; }

	public bool IsReadOnly { get => false; }

	public bool IsSynchronized { get => false; }

	public object SyncRoot { get => this; }

	private List<IBin> _bins;

	public IBin this[int index] { get => _bins[index]; set => _bins[index] = value; }

	public int IndexOf(IBin bin) => this._bins.FindIndex(b => b.Name == bin.Name);

	public void Insert(int index, IBin bin) => this._bins.Insert(index, bin);

	public void RemoveAt(int index) => this._bins.RemoveAt(index);

	public void Add(IBin bin) => this._bins.Add(bin);
	
	public void Add(IEnumerable<IBin> bins);

	public void Clear() => this._bins.Clear();

	public bool Contains(IBin bin) => this._bins.Contains(bin);

	public void CopyTo(IBin[] array, int index) => this._bins.CopyTo(array, index);

	public bool Remove(IBin bin) => this._bins.Remove(bin);

	public IEnumerator GetEnumerator() => this._bins.GetEnumerator();

	IEnumerator<IBin> IEnumerable<IBin>.GetEnumerator() => this._bins.GetEnumerator();
	
	public bool Touch() {
		return true;
	}

	// TODO POCO
}
