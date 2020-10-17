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

		bool _currentIsSet = false;
		T _currentValue = default!;

		public T Current
		{
			get
			{
				if(_currentIsSet)
				{
					return _currentValue;
				}
				else
				{
					throw new InvalidOperationException("Cannot call current right now");
				}
			}
		}

		object? IEnumerator.Current => this.Current;

		public bool MoveNext()
		{
			if(_cancellationToken.IsCancellationRequested)
			{
				_currentIsSet = false;
				return false;
			}
			try
			{
				this._currentValue = (_collection.Take());
				_currentIsSet = true;
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
