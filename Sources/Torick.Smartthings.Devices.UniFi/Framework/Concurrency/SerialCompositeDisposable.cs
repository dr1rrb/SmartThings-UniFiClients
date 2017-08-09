using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Concurrency
{
	public class SerialCompositeDisposable : IDisposable
	{
		private IImmutableList<IDisposable> _current = ImmutableList<IDisposable>.Empty;

		public void Update(IImmutableList<IDisposable> disposables)
		{
			while (true)
			{
				var current = _current;
				if (current == null)
				{
					new CompositeDisposable(disposables).Dispose();
					return;
				}
				else if (Interlocked.CompareExchange(ref _current, disposables, current) == current)
				{
					new CompositeDisposable(current.RemoveRange(disposables)).Dispose();
					return;
				}
			}
		}

		public void Dispose() => new CompositeDisposable(Interlocked.Exchange(ref _current, null)).Dispose();
	}
}