using System.Threading;
using System.Threading.Tasks;

namespace Framework.Persistence
{
	/// <summary>
	/// Abstraction over the loading of an entity
	/// </summary>
	/// <remarks>
	/// This is a subset for the "reading" role of of the IDataPersister.
	/// </remarks>
	public interface IDataReader<T>
	{
		/// <summary>
		/// Load the file and get entity from it (see remarks)
		/// </summary>
		/// <remarks>
		/// If you are reading the data with the INTENTION OF SAVING A MODIFIED VERSION back, you
		/// **SHOULD** use the .Update() method instead.
		/// This method should be use only when you need to read the data without having the intention of
		/// saving a modified version back.
		/// </remarks>
		Task<DataReaderLoadResult<T>> Load(CancellationToken ct);

		/// <summary>
		/// If the data is always the same for the life of the app process.
		/// </summary>
		/// <remarks>
		/// False means a reload is need each time, True means the value could be save to prevent other fetches.
		/// </remarks>
		bool IsDataConstant { get; }
	}
}