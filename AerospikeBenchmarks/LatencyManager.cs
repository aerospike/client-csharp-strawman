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
using System.Text;
using System.Threading;

namespace Aerospike.Benchmarks
{
	public sealed class LatencyManager : ILatencyManager
	{
		// 0.5 ms, 1 ms, 2 ms, 5 ms, 10 ms, 20 ms, 30 ms, greater
		private readonly Bucket LessThanPoint5 = new("<=0.5ms");
		private readonly Bucket LessThan1 = new("<=1ms");
		private readonly Bucket LessThan2 = new("<=2ms");
		private readonly Bucket LessThan5 = new("<=5ms");
		private readonly Bucket LessThan10 = new("<=10ms");
		private readonly Bucket LessThan20 = new("<=20ms");
		private readonly Bucket LessThan30 = new("<=30ms");
		private readonly Bucket GreaterThan30 = new(">30ms");

		public LatencyManager()
		{
		}

		public void Add(double elapsedms)
		{
			if (elapsedms <= 0.5)
			{
				LessThanPoint5.Increment();
			}
            else if (elapsedms > 0.5 &&  elapsedms <= 1)
            {
				LessThan1.Increment();
            }
			else if (elapsedms > 1 && elapsedms <= 2)
			{
				LessThan2.Increment();
			}
			else if (elapsedms > 2 && elapsedms <= 5)
			{
				LessThan5.Increment();
			}
			else if (elapsedms > 5 && elapsedms <= 10)
			{
				LessThan10.Increment();
			}
			else if (elapsedms > 10 && elapsedms <= 20)
			{
				LessThan20.Increment();
			}
			else if (elapsedms > 20 && elapsedms <= 30)
			{
				LessThan30.Increment();
			}
			else if (elapsedms > 30)
			{
				GreaterThan30.Increment();
			}
		}

		public string PrintHeader()
		{
			StringBuilder sb = new(400);
			sb.Append($"          {LessThanPoint5.Name}");
			sb.Append($"    {LessThan1.Name}");
			sb.Append($"    {LessThan2.Name}");
			sb.Append($"    {LessThan5.Name}");
			sb.Append($"    {LessThan10.Name}");
			sb.Append($"    {LessThan20.Name}");
			sb.Append($"    {LessThan30.Name}");
			sb.Append($"    {GreaterThan30.Name}");

			return sb.ToString();
		}

		/// <summary>
		/// Print latency percents for specified ranges.
		/// This function is not absolutely accurate for a given time slice because this method 
		/// is not synchronized with the Add() method.  Some values will slip into the next iteration.  
		/// It is not a good idea to add extra locks just to measure performance since that actually 
		/// affects performance.  Fortunately, the values will even out over time
		/// (ie. no double counting).
		/// </summary>
		public string PrintResults(StringBuilder sb, string prefix)
		{
			// Capture snapshot and make buckets.
			var lessThanPoint5Count = LessThanPoint5.Reset();
			var lessThan1Count = LessThan1.Reset();
			var lessThan2Count = LessThan2.Reset();
			var lessThan5Count = LessThan5.Reset();
			var lessThan10Count = LessThan10.Reset();
			var lessThan20Count = LessThan20.Reset();
			var lessThan30Count = LessThan30.Reset();
			var greaterThan30Count = GreaterThan30.Reset();
			int sum = lessThanPoint5Count + lessThan1Count + lessThan2Count + lessThan5Count + lessThan10Count + lessThan20Count + lessThan30Count + greaterThan30Count;

			// Print results.
			sb.Length = 0;
			sb.Append(prefix);
			int spaces = 6 - prefix.Length;

			for (int j = 0; j < spaces; j++)
			{
				sb.Append(' ');
			}
			
			PrintColumn(sb, LessThanPoint5.Name, sum, lessThanPoint5Count);
			PrintColumn(sb, LessThan1.Name, sum, lessThan1Count);
			PrintColumn(sb, LessThan2.Name, sum, lessThan2Count);
			PrintColumn(sb, LessThan5.Name, sum, lessThan5Count);
			PrintColumn(sb, LessThan10.Name, sum, lessThan10Count);
			PrintColumn(sb, LessThan20.Name, sum, lessThan20Count);
			PrintColumn(sb, LessThan30.Name, sum, lessThan30Count);
			PrintColumn(sb, GreaterThan30.Name, sum, greaterThan30Count);

			return sb.ToString();
		}

		public string PrintSummary(StringBuilder sb, string prefix)
		{
			var lessThanPoint5Sum = LessThanPoint5.Sum;
			var lessThan1Sum = LessThan1.Sum;
			var lessThan2Sum = LessThan2.Sum;
			var lessThan5Sum = LessThan5.Sum;
			var lessThan10Sum = LessThan10.Sum;
			var lessThan20Sum = LessThan20.Sum;
			var lessThan30Sum = LessThan30.Sum;
			var greaterThan30Sum = GreaterThan30.Sum;
			int sum = lessThanPoint5Sum + lessThan1Sum + lessThan2Sum + lessThan5Sum + lessThan10Sum + lessThan20Sum + lessThan30Sum + greaterThan30Sum;

			// Print results.
			sb.Length = 0;
			sb.Append(prefix);
			int spaces = 6 - prefix.Length;

			for (int j = 0; j < spaces; j++)
			{
				sb.Append(' ');
			}

			PrintColumn(sb, LessThanPoint5.Name, sum, lessThanPoint5Sum);
			PrintColumn(sb, LessThan1.Name, sum, lessThan1Sum);
			PrintColumn(sb, LessThan2.Name, sum, lessThan2Sum);
			PrintColumn(sb, LessThan5.Name, sum, lessThan5Sum);
			PrintColumn(sb, LessThan10.Name, sum, lessThan10Sum);
			PrintColumn(sb, LessThan20.Name, sum, lessThan20Sum);
			PrintColumn(sb, LessThan30.Name, sum, lessThan30Sum);
			PrintColumn(sb, GreaterThan30.Name, sum, greaterThan30Sum);

			return sb.ToString();
		}

		private static void PrintColumn(StringBuilder sb, string bucketName, int sum, int value)
		{
			float percent = 0;

			if (value > 0)
			{
				percent = (int)(value * 100.0 / sum);
			}
			string percentString = percent.ToString("n2") + "%";
			int spaces = bucketName.Length + 4 - percentString.Length;

			for (int j = 0; j < spaces; j++)
			{
				sb.Append(' ');
			}
			sb.Append(percentString);
		}

		private sealed class Bucket
		{
			int Count = 0;
			public int Sum = 0;
			public string Name { get; }

			public Bucket(string name)
			{
				this.Name = name;
			}

			public void Increment()
			{
				Interlocked.Increment(ref Count);
			}

			public int Reset()
			{
				int c = Interlocked.Exchange(ref Count, 0);
				Sum += c;
				return c;
			}
		}
	}
}
