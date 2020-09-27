using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace crozzle
{
	public class BlockingCollectionEnumerator<T> : IEnumerator<T>
	{
		private readonly BlockingCollection<T> _collection;
		private readonly CancellationToken _cancellationToken;

		public BlockingCollectionEnumerator(BlockingCollection<T> collection, CancellationToken cancellationToken)
		{
			_collection = collection;
			_cancellationToken = cancellationToken;
		}

		public void Dispose()
		{
		}

		public T Current { get; set; } = default(T);

		object IEnumerator.Current => this.Current;

		public bool MoveNext()
		{
			if(_cancellationToken.IsCancellationRequested)
			{
				return false;
			}
			try
			{
				this.Current = (_collection.Take());
				return true;
			}
			catch (TaskCanceledException)
			{
				return false;
			}
		}

		public void Reset()
		{
		}
	}

	public class BlockingCollectionEnumerable<T> : IEnumerable<T>
	{
		readonly BlockingCollection<T> _collection;
		readonly CancellationToken _cancellationToken;

		public BlockingCollectionEnumerable(BlockingCollection<T> collection, CancellationToken cancellation)
		{
			this._collection = collection;
			this._cancellationToken = cancellation;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new BlockingCollectionEnumerator<T>(_collection, _cancellationToken);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new BlockingCollectionEnumerator<T>(_collection, _cancellationToken);
		}
	}
}
