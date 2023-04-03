<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

#load "Bin.linq"
#load "Record.linq"
#load "Namespace.linq"

public interface ISet
{
	public string Name { get; }
	
	public IEnumerable<IRecord> Records { get; }
	
	public INamespace Namespace { get; }
	
	public void Create();
	
	public void Insert(IRecord record);
	
	public void Modify(); // example: policies
	
	public void Drop(IBin bin);
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
	
	private List<IRecord> _records;
	
	public void Create() {
	}
	
	public void Insert(IRecord record) {
		this._records.Add(record);
	}
	
	public void Modify() {
	}
	
	public void Drop(IBin bin) { // TODO would we know the record we are dropping the bin from?
	}
	
	// understand put(primary key), get(primary key, filter queries), query(filter expression), operate(primary key), 
	// truncate(), delete(primary key, filter queries?), LINQ, dynamic policy support
	
	// TODO class called NullSet which extends Set
	// can we avoid a one to one mapping from our calls to wire protocol? can we translate CDT to expressions?
	
	// next step is taking virtual list and thinking about how to implement that
	// Policy, Client, Cluster, and config management as one block of work, dynamic config, look into performance metrics for auditing, logging
	// policy should not be at statement level, make imuttable 
	// LINQ and queries
	
	// build pipeline, talk to Julian. look into MS Build. should be a plug in
}