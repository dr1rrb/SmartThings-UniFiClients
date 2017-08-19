using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Torick
{
	public delegate Task<TResult> FuncAsync<TResult>(CancellationToken ct);
	public delegate Task<TResult> FuncAsync<in T1, TResult>(CancellationToken ct, T1 t1);
	public delegate Task<TResult> FuncAsync<in T1, in T2, TResult>(CancellationToken ct, T1 t1, T2 t2);
	public delegate Task<TResult> FuncAsync<in T1, in T2, in T3, TResult>(CancellationToken ct, T1 t1, T2 t2, T3 t3);
	public delegate Task<TResult> FuncAsync<in T1, in T2, in T3, in T4, TResult>(CancellationToken ct, T1 t1, T2 t2, T3 t3, T4 t4);
}
