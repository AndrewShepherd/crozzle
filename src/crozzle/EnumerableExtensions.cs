using System;
using System.Collections.Generic;
using System.Text;

namespace crozzle
{
	public class BufferEnumerator<T> : IEnumerator<T>
	{
		readonly IEnumerator<T> _source;
		readonly List<T> _buffer;
		int _index = -1;
		public BufferEnumerator(IEnumerator<T> source, List<T> buffer)
		{
			this._source = source;
			this._buffer = buffer;
		}

		object System.Collections.IEnumerator.Current
		{
			get
			{
				return _buffer[_index];
			}
		}
		

		public T Current => _buffer[_index];
		public bool MoveNext()
		{
			_index++;
			if (_index < _buffer.Count)
				return true;
			if (!_source.MoveNext())
				return false;
			_buffer.Add(_source.Current);
			return true;
		}
		public void Reset()
		{
			_index = -1;
		}
		public void Dispose()
		{
		}
	}

	public class BufferEnumerable<T> : IEnumerable<T>, IDisposable
	{
		IEnumerator<T> source;
		List<T> buffer;
		public BufferEnumerable(IEnumerable<T> source)
		{
			this.source = source.GetEnumerator();
			this.buffer = new List<T>();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new BufferEnumerator<T>(source, buffer);
		}
		public void Dispose()
		{
			source.Dispose();
		}
	}


	public static class EnumerableExtensions
	{
		public static BufferEnumerable<T> Buffer<T>(this IEnumerable<T> source)
		{
			return new BufferEnumerable<T>(source);
		}
	}
}
