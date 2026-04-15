using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace IVSoftware.Portable.Disposable.AuthorityEpochProvider
{
    public class AuthorityEpochProvider : IAuthorityEpochProvider
    {
        private DisposableHost _dhost = new();

        public Enum Authority { get; private set; } = AuthorityReserved.NoAuthority;

        public Enum[] Authorities =>
            _dhost
            .Tokens
            .Select(_ => _.Sender)
            .OfType<Enum>()
            .ToArray();

        public bool IsDisposing { get; private set; }

        public bool IsCancellationPending { get; private set; }

        public event EventHandler? BeginUsing;
        public event EventHandler? FinalDispose;

        public void CancelAuthorityEpoch(bool @throw)
        {
            _dhost = new();
            var msg = $"{Authority.ToFullKey()} authority epoch has been cancelled.";
            Authority = AuthorityReserved.NoAuthority;
            if(@throw) throw new OperationCanceledException(msg);
        }

        public bool HasRequestedAuthority(Enum authority)
        {
            throw new NotImplementedException();
        }

        public bool IsZero() => _dhost.IsZero();

        public IDisposable RequestAuthority(Enum authority, Dictionary<string, object>? properties = null)
            => _dhost.GetToken(sender: authority, properties);
        #region E V E N T S

        #if false && ABSTRACT
        Interface-level event projection

        The underlying DisposableHost exposes strongly-typed event args
        (e.g., BeginUsingEventArgs, FinalDisposeEventArgs). The
        IAuthorityEpochProvider contract deliberately surfaces only
        EventHandler to avoid coupling the interface to those concrete types.

        This region maintains separate invocation lists for the interface
        events and relays them from the strongly-typed overrides.

        This allows:

        - Consumers to depend only on the abstraction
        - The implementation to evolve its internal event args independently
        - A stable, minimal contract surface for cross-package use

        Invocation lists are managed explicitly to preserve thread safety
        and avoid exposing the underlying event infrastructure.

        #endif

        private object _eventLock = new();
        event EventHandler? IAuthorityEpochProvider.BeginUsing
        {
            add
            {
                if (value is null) return;
                lock (_eventLock) _beginUsingInvocationList.Add(value);
            }

            remove
            {
                if (value is null) return;
                lock (_eventLock) _beginUsingInvocationList.Remove(value);
            }
        }
        private readonly List<EventHandler> _beginUsingInvocationList = new();


        event EventHandler? IAuthorityEpochProvider.FinalDispose
        {
            add
            {
                if (value is null) return;
                lock (_eventLock) _finalDisposeInvocationList.Add(value);
            }

            remove
            {
                if (value is null) return;
                lock (_eventLock) _finalDisposeInvocationList.Remove(value);
            }
        }
        private readonly List<EventHandler> _finalDisposeInvocationList = new();
        #endregion E V E N T S
    }
}
