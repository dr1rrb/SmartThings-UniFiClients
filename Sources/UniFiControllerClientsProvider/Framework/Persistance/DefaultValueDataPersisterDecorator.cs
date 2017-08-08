using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework.Extensions;

namespace Framework.Persistence
{
	/// <summary>
	/// Decorates a <see cref="IDataPersister{TEntity}"/> which save / returns a custom default value
	/// instead of default(<typeparamref name="T"/>) when the value is non-existent in inner persister.	
	/// </summary>
	/// <typeparam name="T">Type of of the persisted entity</typeparam>
	public class DefaultValueDataPersisterDecorator<T> : IDataPersister<T>
	{
		private readonly IDataPersister<T> _inner;
		private readonly DefaultValueDataPersisterDecoratorMode _mode;
		private readonly T _customDefaultValue;
		private readonly IEqualityComparer<T> _comparer;

		/// <summary>
		/// Creates a decorator using the same default value for both read and write operations
		/// </summary>
		public DefaultValueDataPersisterDecorator(
			IDataPersister<T> inner,
			DefaultValueDataPersisterDecoratorMode mode,
			T customDefaultValue = default(T),
			IEqualityComparer<T> comparer = null)
		{
			_inner = inner;
			_mode = mode;
			_customDefaultValue = customDefaultValue;
			_comparer = comparer ?? EqualityComparer<T>.Default;
		}

		private bool CheckMode(DefaultValueDataPersisterDecoratorMode mode)
		{
			return (_mode & mode) == mode;
		}

		/// <inheritdoc />
		public async Task<DataReaderLoadResult<T>> Load(CancellationToken ct)
		{
			var result = await _inner.Load(ct);

			return GetAdjustedReadResult(result);
		}

		private DataReaderLoadResult<T> GetAdjustedReadResult(DataReaderLoadResult<T> result)
		{
			// Check for error condition
			if (result.IsError && CheckMode(DefaultValueDataPersisterDecoratorMode.ReadErrorToCustomDefault))
			{
				return new DataReaderLoadResult<T>(_customDefaultValue);
			}

			// Check for empty condition
			if (!result.IsValuePresent && CheckMode(DefaultValueDataPersisterDecoratorMode.ReadEmptyToCustomDefault))
			{
				return new DataReaderLoadResult<T>(_customDefaultValue);
			}

			// Check for value present condition where condition == default(T) -- using supplied EqualityComparer, obviously
			if (CheckMode(DefaultValueDataPersisterDecoratorMode.ReadDefaultToCustomDefault) && _comparer.Equals(result.Value, default(T)))
			{
				return new DataReaderLoadResult<T>(_customDefaultValue);
			}

			// The original result could be used "as-is"
			return result;
		}

		/// <inheritdoc />
		public async Task<DataPersisterUpdateResult<T>> Update(CancellationToken ct, DataPersisterUpdaterWithContext<T> updater)
		{
			var innerUpdated = false;

			var result = await _inner.Update(
				ct,
				context =>
				{
					innerUpdated = false;

					var adjustedContext = GetAdjustedReadResult(context);
					var innerContext = new DataPersisterTransactionContext<T>(adjustedContext);

					updater(innerContext);

					if (innerContext.IsCommitted)
					{
						innerUpdated = true;

						if (innerContext.IsRemoved)
						{
							context.RemoveAndCommit();
						}
						else
						{
							var value = innerContext.CommittedValue;
							if (CheckMode(DefaultValueDataPersisterDecoratorMode.WriteCustomDefaultToEmpty) && _comparer.Equals(value, _customDefaultValue))
							{
								context.RemoveAndCommit();
							}
							else if (CheckMode(DefaultValueDataPersisterDecoratorMode.WriteDefaultToEmpty) && _comparer.Equals(value, default(T)))
							{
								context.RemoveAndCommit();
							}
							else
							{
								context.Commit(value);
							}
						}
					}
				}
			);

			var adjustedResult = GetAdjustedReadResult(result);
			return new DataPersisterUpdateResult<T>(adjustedResult, innerUpdated || result.IsUpdated);
		}

		/// <inheritdoc />
		public async Task<DataPersisterUpdateResult<T>> Update(CancellationToken ct, DataPersisterAsyncUpdaterWithContext<T> asyncUpdater)
		{
			var innerUpdated = false;

			var result = await _inner.Update(
				ct,
				async (ct2, context) =>
				{
					innerUpdated = false;

					var adjustedContext = GetAdjustedReadResult(context);
					var innerContext = new DataPersisterTransactionContext<T>(adjustedContext);

					await asyncUpdater(ct2, innerContext);

					if (innerContext.IsCommitted)
					{
						innerUpdated = true;

						if (innerContext.IsRemoved)
						{
							context.RemoveAndCommit();
						}
						else
						{
							var value = innerContext.CommittedValue;
							if (CheckMode(DefaultValueDataPersisterDecoratorMode.WriteCustomDefaultToEmpty) && _comparer.Equals(value, _customDefaultValue))
							{
								context.RemoveAndCommit();
							}
							else if (CheckMode(DefaultValueDataPersisterDecoratorMode.WriteDefaultToEmpty) && _comparer.Equals(value, default(T)))
							{
								context.RemoveAndCommit();
							}
							else
							{
								context.Commit(value);
							}
						}
					}
				}
			);

			var adjustedResult = GetAdjustedReadResult(result);
			return new DataPersisterUpdateResult<T>(adjustedResult, innerUpdated || result.IsUpdated);
		}

		/// <inheritdoc />
		public bool IsDataConstant { get; } = false;
	}
}
