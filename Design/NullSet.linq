<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

#load "Set.linq"

public class NullSet : Set
{
	public NullSet(string namespaceName) : base(namespaceName)
	{
	}
	// TODO is there anything else that NullSet needs?
}