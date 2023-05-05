<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

#load "Bin.linq"
#load "Record.linq"
#load "Namespace.linq"
#load "PrimaryKey.linq"

public interface ISet
{
	public string Name { get; }
	
	public IEnumerable<IRecord> Records { get; }
	
	public INamespace Namespace { get; }
	
	public IEnumerable<ISecondaryIndex> SecondaryIndexes { get; }
	
	public Exception LastException { get; }

	public DateTimeOffset LastActivity { get; }

	public DateTimeOffset Creation { get; }
	
	public bool IsNullSet { get; }
	
	public void Insert(IRecord record);
	
	public void Delete(IRecord record);
	
	public void Update(IRecord record);
	
	public void Touch(IPrimaryKey key); // updates generation and lastUpdated

	public void Insert(IPrimaryKey key, params IBin[] bins);

	public void Delete(IPrimaryKey key);

	public void Update(IPrimaryKey key, params IBin[] bins);
	
	public bool Exists(IPrimaryKey key);
}

public interface ISecondaryIndex
{
	public ISet Set { get; }

	public IBin Bin { get; }
	
	public DateTimeOffset LastActivity { get; }
	
	public DateTimeOffset Creation { get; }
}

public class Set : ISet // do not inherit from IList
{
	public Set([NotNull] string name, IEnumerable<IRecord> records)
	{
		this.Name = name;
		this._records = records.ToList();
	}
	
	public Set([NotNull] string name)
	{
		this.Name = name;
		this._records = new List<IRecord>();
	}
	
	public string Name { get; }
	
	public IEnumerable<IRecord> Records { get => this._records; }
	
	public INamespace Namespace { get; set; }
	
	public IEnumerable<ISecondaryIndex> SecondaryIndexes { get; }
	
	public Exception LastException { get; internal set; }

	public DateTimeOffset LastActivity { get; }

	public DateTimeOffset Creation { get; }

	private List<IRecord> _records;
	
	public bool IsNullSet { get => false; }
	
	public void Insert(IRecord record) {
		this._records.Add(record);
	}

	public void Delete(IRecord record) {
		this._records.Remove(record);
	}

	public void Update(IRecord record) {
		int index = this._records.FindIndex(r => r.PrimaryKey == record.PrimaryKey);
		this._records[index] = record;
	}

	public void Touch(IPrimaryKey key) {
		int index = this._records.FindIndex(r => r.PrimaryKey == key);
		// update generation and last updated
	}

	public void Insert(IPrimaryKey key, params IBin[] bins) {
		int index = this._records.FindIndex(r => r.PrimaryKey == key);
		
	}

	public void Delete(IPrimaryKey key);

	public void Update(IPrimaryKey key, params IBin[] bins);

	public bool Exists(IPrimaryKey key);
}