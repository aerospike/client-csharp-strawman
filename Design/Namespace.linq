<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

#load "Set.linq"
#load "NullSet.linq"
#load "Cluster.linq"

public interface INamespace
{
	public string Name { get; }
	
	public IEnumerable<ISet> Sets { get; set; }
	
	public void Modify(); // example: policies
}

public class Namespace : INamespace
{
	static private readonly Regex SetNameRegEx = new Regex("set=(?<setname>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static private readonly Regex NamespaceRegEx = new Regex("ns=(?<namespace>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	
	public Namespace([NotNull]string name)
	{
        this.Name = name;
	}
	
	public Namespace([NotNull] string name, IEnumerable<string> setAttribs)
	{
		this.Name = name;
		
		var setNames = from setAttrib in setAttribs
                           let matches = SetNameRegEx.Match(setAttrib)
                           select matches.Groups["setname"].Value;

        var nsSets = setNames
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => new Set(s))
                        .ToList();

        nsSets.Add(new NullSet(name));

        this.Sets = nsSets.ToArray();
	}
	
	public string Name { get; }
	
	public IEnumerable<ISet> Sets { get; set; }
	
	public void Modify() {
	}
	
	public static IEnumerable<Namespace> Create(IConnection connection)
    {
        var setsAttrib = Info.Request(connection, "sets");

        var asNamespaces = (from nsSets in setsAttrib.Split(';', StringSplitOptions.RemoveEmptyEntries)
                            let ns = NamespaceRegEx.Match(nsSets).Groups["namespace"].Value
                            group nsSets by ns into nsGrp
                            let nsBins = Info.Request(connection, $"bins/{nsGrp.Key}")
                            select new Namespace(nsGrp.Key, nsGrp.ToList())).ToList();

        var namespaces = Info.Request(connection, "namespaces")?.Split(';');

        if(namespaces != null)
        {
            foreach(var ns in namespaces)
            {
                if(!asNamespaces.Any(ans => ans.Name == ns))
                    asNamespaces.Add(new Namespace(ns));
            }
        }

        return asNamespaces.ToArray();
    }
}