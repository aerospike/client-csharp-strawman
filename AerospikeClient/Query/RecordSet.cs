/* 
 * Copyright 2012-2020 Aerospike, Inc.
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
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace Aerospike.Client
{
	/// <summary>
	/// This class manages record retrieval from queries.
	/// Multiple threads will retrieve records from the server nodes and put these records on the queue.
	/// The single user thread consumes these records from the queue.
	/// </summary>
	public sealed class RecordSet : IEnumerable<KeyRecord>
	{
		public static readonly KeyRecord END = new KeyRecord(null, null);

		private readonly CancellationToken cancelToken;
		private KeyRecord record;
		private volatile bool valid = true;

		/// <summary>
		/// Initialize record set with underlying producer/consumer queue.
		/// </summary>
		public RecordSet(CancellationToken cancelToken)
		{
			this.cancelToken = cancelToken;
		}

		//-------------------------------------------------------
		// Record traversal methods
		//-------------------------------------------------------

		/// <summary>
		/// Retrieve next record. Returns true if record exists and false if no more 
		/// records are available.
		/// This method will block until a record is retrieved or the query is cancelled.
		/// </summary>
		public bool Next()
		{
			if (!valid)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Close query.
		/// </summary>
		public void Dispose()
		{
			Close();
		}

		/// <summary>
		/// Close query.
		/// </summary>
		public void Close()
		{
			valid = false;
		}

		public IEnumerator<KeyRecord> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		//-------------------------------------------------------
		// Meta-data retrieval methods
		//-------------------------------------------------------

		/// <summary>
		/// Get record's unique identifier.
		/// </summary>
		public Key Key
		{
			get
			{
				return record.key;
			}
		}

		/// <summary>
		/// Get record's header and bin data.
		/// </summary>
		public Record Record
		{
			get
			{
				return record.record;
			}
		}

		/// <summary>
		/// Get CancellationToken associated with this query.
		/// </summary>
		public CancellationToken CancelToken
		{
			get
			{
				return cancelToken;
			}
		}
	}
}
