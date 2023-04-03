<Query Kind="Statements">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
</Query>

#load "Set.linq"

public class NullSet : Set
{
	private NullSet() : base("NullSet")
	{
	}
	
	private static NullSet instance = null;
	
	public static NullSet Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new NullSet();
            }
            return instance;
        }
    }
	// TODO is there anything else that NullSet needs?
	// for every namespace, so can't be singleton
}