using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace IVSoftware.Portable.Disposable
{
    /// <summary>
    /// public reserved value designed to not conflict with defined flags.
    /// </summary>
    public enum AuthorityReserved { NoAuthority = int.MinValue, }

    /// <summary>
    /// Coordinates a reference-counted authority epoch across participants.
    /// </summary>
    /// <remarks>
    /// Establishes a shared lifetime where the first request defines authority
    /// and the last release completes the epoch. Supports concurrent access,
    /// reentry awareness, and deterministic teardown via FinalDispose.
    /// </remarks>
    public interface IAuthorityEpochProvider
    {
        /// <summary>
        /// The first-come-first-served Authority for this epoch.
        /// </summary>
        /// <remarks>
        /// This value is stable; even if the original authority relinquishes its token 
        /// the authority persists until all epoch participants have surrendered theirs.
        /// </remarks>
        Enum Authority { get; }

        /// <summary>
        /// Lists authority requests in chronological order.
        /// </summary>
        Enum[] Authorities { get; }

        /// <summary>
        /// Request authority.
        /// </summary>
        /// <remarks>
        /// - Authority is established on the count 0 -> 1 edge.
        /// - The 1 -> 0 edge raises the FinalDispose event with Authority intact and IsDisposing=true.
        /// - 1+ requestors are added as tokens and are visible to the HasAuthority method.
        /// </remarks>
        IDisposable RequestAuthority(Enum authority, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Returns a value indicating whether the current epoch has any active
        /// (not disposed) token for the specified authority.
        /// </summary>
        bool HasRequestedAuthority(Enum authority);

        /// <summary>
        /// Returns a value indicating whether the current epoch has at any time
        /// requested a token for the specified authority.
        /// </summary>
        bool HasEverRequestedAuthority(Enum authority);

        /// <summary>
        /// Threadsafe, concurrent check on whether the epoch is idle.
        /// </summary>
        bool IsZero();

        /// <summary>
        /// Indicates that the current authority is in the process of being relinquished.
        /// </summary>
        /// <remarks>
        /// This scheme is often employed to manage circularity. In reentry scenarios,
        /// it becomes important to know whether a given Authority is is a building or
        /// in a disposing phase of its eopch lifetime.
        /// </remarks>
        bool IsDisposing { get; }

        #region C A N C E L
        /// <summary>
        /// Immediately returns authority to (T)NoAuthorityReserved.NoAuthority.
        /// </summary>
        /// <remarks>
        /// This reference now points to a new DisposableHost.
        /// - IsZero() is true.
        /// - HasAuthority is false;
        /// - IsDisposing is false.
        /// </remarks>
        void CancelAuthorityEpoch(bool @throw = false);
        
        /// <summary>
        /// Indicates that the epoch was cancelled.
        /// </summary>
        bool IsCancelled { get; }
        #endregion C A N C E L

        /// <summary>
        /// Announces that a new Authority epoch has begun.
        /// </summary>
        event EventHandler? BeginUsing;

        /// <remarks>
        /// Raised on the 1 → 0 transition when the final participant releases its token.
        /// 
        /// All participants have relinquished authority; now is the time for dependent cleanup to take place.
        /// This event signals entry into the disposal phase, during which:
        /// - <see cref="IsZero"/> returns true (i.e., reference count has reached zero).
        /// - <see cref="Authority"/> remains stable and reflects the epoch being torn down.
        /// - <see cref="IsDisposing"/> is true. For example, this matters when an authority
        ///   is accumulating events vs releasing them as a batch.
        /// </remarks>
        event EventHandler? FinalDispose;
    }

    public interface IAuthorityEpochProvider<T> : IAuthorityEpochProvider
    where T : struct, Enum
    {
        new T Authority { get; }
        new T[] Authorities { get; }


        /// <summary>
        /// Request authority.
        /// </summary>
        /// <remarks>
        /// - Authority is established on the count 0 -> 1 edge.
        /// - The 1 -> 0 edge raises the FinalDispose event with Authority intact and IsDisposing=true.
        /// - 1+ requestors are added as tokens and are visible to the HasAuthority method.
        /// </remarks>
        IDisposable RequestAuthority(T authority, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Returns a value indicating whether the current epoch has any active
        /// (not disposed) token for the specified authority.
        /// </summary>
        bool HasRequestedAuthority(T authority);


        /// <summary>
        /// Returns a value indicating whether the current epoch has at any time
        /// requested a token for the specified authority.
        /// </summary>
        bool HasEverRequestedAuthority(T authority);
    }
}
