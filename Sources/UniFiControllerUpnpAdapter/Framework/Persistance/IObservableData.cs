using System;

namespace Framework.Persistence
{
	/// <summary>
	/// Generic interface for a value who can be observed.
	/// </summary>
	public interface IObservableData<out T>
	{
		/// <summary>
		/// Gets an observable sequence which provides only updated values. (hot observable)
		/// </summary>
		/// <remarks>
		/// If no new values is observed, the first value could take time before being observed.
		/// </remarks>
		IObservable<T> Observe();

		/// <summary>
		/// Gets an observable sequence which replays the last value current value and continue with updated values. (cold observable)
		/// </summary>
		IObservable<T> GetAndObserve();
	}
}