namespace Framework.Persistence
{
	/// <summary>
	/// Interface for a DataPersister who can provide observation
	/// of its values.
	/// </summary>
	public interface IObservableDataPersister<T> : IDataPersister<T>, IObservableData<DataReaderLoadResult<T>>
	{
	}
}