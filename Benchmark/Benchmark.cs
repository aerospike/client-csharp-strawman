/* 
 * Copyright 2012-2023 Aerospike, Inc.
 *
 * Portions may be licensed to Aerospike, Inc. under one or more contributor
 * license agreements.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */
using System;
using System.Data;
using System.Text;
using Aerospike.Client;
using BenchmarkDotNet.Attributes;

// make graphs
// iterations 25, 50, 10,000, 100,000 - want to trigger GC
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 25)]
public class Benchmark
{
	private readonly AerospikeClient Client;

	[Params(0, 100)]
	public long pk;

	[Params(0, 100)]
	public long length;

	private volatile int[] dataIntArray;

	private volatile Dictionary<object, object> dataDictionary;

	private volatile Dictionary<object, object>[] dataListDictionary;

	public Benchmark()
	{
		var policy = new ClientPolicy
		{
			maxCommands = 300,
			minConnsPerNode = 1,
			maxConnsPerNode = 50
		};
		Host[] hosts = new Host[] { new Host("localhost", 3000) };
		Client = new AerospikeClient(policy, hosts);

		Client.Truncate(null, "test", "test", DateTime.Now).Wait();
		Thread.Sleep(500);
	}

	[Benchmark]
	public async Task PutLong()
	{
		var key = new Key("test", "test", pk);
		var val = new Value.LongValue(pk);
		var bin = new Bin("binLong", val);
		await Client.Put(null, key, bin);
	}

	[Benchmark]
	public async Task PutString()
	{
		var key = new Key("test", "test", pk);
		var val = new Value.StringValue(pk.ToString());
		var bin = new Bin("binString", val);
		await Client.Put(null, key, bin);
	}

	[Benchmark]
	public async Task PutDouble()
	{
		var key = new Key("test", "test", pk);
		var val = new Value.DoubleValue((double)pk);
		var bin = new Bin("binDouble", val);
		await Client.Put(null, key, bin);
	}

	[Benchmark]
	public async Task PutListInt()
	{
		var key = new Key("test", "test", pk);
		var val = new Value.ListValue(dataIntArray);
		var bin = new Bin("binListInt", val);
		await Client.Put(null, key, bin);
	}

	[Benchmark]
	public async Task PutMapInt()
	{
		var key = new Key("test", "test", pk);
		var val = new Value.MapValue(dataDictionary);
		var bin = new Bin("bin", val);
		await Client.Put(null, key, bin);
	}

	[Benchmark]
	public async Task PutListMapInt()
	{
		var key = new Key("test", "test", pk);
		var val = new Value.ListValue(dataListDictionary);
		var bin = new Bin("bin", val);
		await Client.Put(null, key, bin);
	}

	[Benchmark]
	public async Task Get()
	{
		var key = new Key("test", "test", pk);
		await Client.Get(null, key);
	}

	[GlobalSetup]
	public void CreateData()
	{
		dataIntArray = new int[length];
		Array.Fill(dataIntArray, 1);
		dataDictionary = new Dictionary<object, object>(dataIntArray.Cast<object>().Select(v => new KeyValuePair<object, object>(v, v)));
		dataListDictionary = new Dictionary<object, object>[length];
		Array.Fill(dataListDictionary, dataDictionary);
	}
}
