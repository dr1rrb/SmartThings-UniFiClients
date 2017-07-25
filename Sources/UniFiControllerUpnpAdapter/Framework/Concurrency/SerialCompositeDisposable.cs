using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Framework.Concurrency
{
	public class SerialCompositeDisposable : IDisposable
	{
		private IImmutableList<IDisposable> _current = ImmutableList<IDisposable>.Empty;

		public void Update(IImmutableList<IDisposable> disposables)
		{
			// It's not thread safe, but as I use it in a Observer.OnNext, I don't care ...

			new CompositeDisposable(ImmutableList.RemoveRange(_current, disposables)).Dispose();
			_current = disposables;
		}

		public void Dispose()
		{
			new CompositeDisposable(_current).Dispose();
		}
	}
}