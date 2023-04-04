<Query Kind="Program">
  <Namespace>System.Diagnostics.CodeAnalysis</Namespace>
  <Namespace>System.Json</Namespace>
</Query>

public interface IVirtualList {

	public bool TryGetList<V>(out V outValue) where V : IConvertible;
}

public class VirtualList : IVirtualList {

	public bool TryGetList<V>(out V outValue) where V : IConvertible;
}

// don't use generic on class, use it on method calls
