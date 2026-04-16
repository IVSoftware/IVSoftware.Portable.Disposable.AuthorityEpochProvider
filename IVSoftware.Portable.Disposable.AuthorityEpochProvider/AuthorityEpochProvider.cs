using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Disposable
{
    [JsonDictionary]
    public partial class AuthorityEpochProvider : IAuthorityEpochProvider
    {
        public AuthorityEpochProvider() => DHost = new(this);
        private class DHostAuthorityProvider : DisposableHost 
        {
            public DHostAuthorityProvider(AuthorityEpochProvider current)
            {
                Current = current;
            }
            public AuthorityEpochProvider? Current { get; set; }
            public TaskCompletionSource<Enum>? TCS { get; private set; }
            protected override void OnBeginUsing(BeginUsingEventArgs e)
            {
                // No one is listening to the BC event; raise it anyway.
                base.OnBeginUsing(e);

                if (Current is null)
                {   /* G T K - N O O P */
                    // Indicates cancellation.
                    // This derelict host is abandoned by its epoch: existing tokens
                    // continue to dispose normally as their using scopes exit,
                    // but no further events are raised on the main instance.
                }
                else
                {
                    TCS = new TaskCompletionSource<Enum>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    if (e.AutoDisposableContext.Sender is Enum authority)
                    {
                        Current.Authority = authority;
                    }
                    else
                    {
                        this.ThrowFramework<InvalidCastException>(
                            $"Expecting an Enum that is the current authority.",
                            @throw: true);
                    }
                    Current.IsCancelled = false;

                    Current.OnBeginUsing(e);
                    foreach (EventHandler handler in Current.BeginUsingInvocationList.ToArray())
                    {
                        handler?.Invoke(this, e);
                    }
                }
            }
            protected override void OnCountChanged(CountChangedEventArgs e)
            {
                base.OnCountChanged(e);
                Current?.OnCountChanged(e);
            }
            protected override void OnFinalDispose(FinalDisposeEventArgs e)
            {
                // No one is listening to the BC event; raise it anyway.
                base.OnFinalDispose(e);

                try
                {
                    if (Current is null)
                    {   /* G T K - N O O P */
                        // Indicates cancellation.
                        // This derelict host is abandoned by its epoch: existing tokens
                        // continue to dispose normally as their using scopes exit,
                        // but no further events are raised on the main instance.
                    }
                    else
                    {
                        Current.IsDisposing = true;
                        Current.OnFinalDispose(e);
                        foreach (EventHandler handler in Current.FinalDisposeInvocationList.ToArray())
                        {
                            handler?.Invoke(this, e);
                        }
                    }
                }
                finally
                {
                    Current?.IsDisposing = false;
                    TCS?.TrySetResult(Current?.Authority ?? AuthorityReserved.NoAuthority);
                    TCS = null;
                    Current?.Authority = AuthorityReserved.NoAuthority; 
                }
            }
        }

        protected virtual void OnBeginUsing(BeginUsingEventArgs e) 
        {
            BeginUsing?.Invoke(this, e);
        }

        protected virtual void OnCountChanged(CountChangedEventArgs e)
        {
            CountChanged?.Invoke(this, e);
        }
        protected virtual void OnFinalDispose(FinalDisposeEventArgs e)
        {
            FinalDispose?.Invoke(this, e);
        }

        private DHostAuthorityProvider DHost
        {
            get => _dhost;
            set
            {
                if (!Equals(_dhost, value))
                {
                    _dhost = value;
                }
            }
        }
        DHostAuthorityProvider _dhost = null!;


        public Enum Authority { get; private set; } = AuthorityReserved.NoAuthority;

        public Enum[] Authorities =>
            DHost
            .Tokens
            .Select(_ => _.Sender)
            .OfType<Enum>()
            .ToArray();

        public bool IsDisposing { get; private set; }

        public bool IsCancelled { get; private set; }

        // Native DisposableHost events.
        public event EventHandler<BeginUsingEventArgs>? BeginUsing;
        public event EventHandler<CountChangedEventArgs>? CountChanged;
        public event EventHandler<FinalDisposeEventArgs>? FinalDispose;
        public void CancelAuthorityEpoch(bool @throw = false)
        {
            IsCancelled = true;
            DHost.Current = null;
            TCS?.TrySetCanceled();
            DHost = new (this);
            var msg = $"{Authority.ToFullKey()} authority epoch has been cancelled.";
            Authority = AuthorityReserved.NoAuthority;
            if(@throw) throw new OperationCanceledException(msg);
        }

        public bool HasRequestedAuthority(Enum authority)
            => Authorities.Any(_ => _.ToFullKey() == authority.ToFullKey());

        public bool IsZero() => DHost.IsZero();

        public IDisposable RequestAuthority(Enum authority, Dictionary<string, object>? properties = null)
            => DHost.GetToken(sender: authority, properties);
        #region A W A I T
        TaskCompletionSource<Enum>? _tcs = default;
        private TaskCompletionSource<Enum>? TCS => DHost.TCS;
        public TaskAwaiter<Enum> GetAwaiter()
        {
            return (TCS?.Task ?? Task.FromResult(Authority)).GetAwaiter();
        }
        #endregion A W A I T

        #region E V E N T S

#if false && ABSTRACT
        -----------------------
        Contract Event Handling
        -----------------------

        +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        Important:
        This path is only used when consumers subscribe via the interface
        (IAuthorityEpochProvider) rather than the concrete type. In typical
        usage, consumers subscribe directly to the concrete events, making
        this fanout path relatively uncommon.
        +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        IAuthorityEpochProvider is decoupled from specialized events.

        event EventHandler? BeginUsing;
        event EventHandler? FinalDispose;
        
        ---
        In contrast, DisposableHost (used for this implementation) has:

        public event BeginUsingEventHandler BeginUsing;
        public event FinalDisposeEventHandler FinalDispose;
        ___

        ∴ Handlers subscribed through the interface (plain old EventHandler)
           are added to the invocation lists and invoked during the lifecycle 
           pipeline once the native (specialized) event has been raised using
           the same event args (with the ability to downcast them in the handler).
#endif

        private object _eventLock = new();
        event EventHandler? IAuthorityEpochProvider.BeginUsing
        {
            add
            {
                if (value is null) return;
                lock (_eventLock) BeginUsingInvocationList.Add(value);
            }

            remove
            {
                if (value is null) return;
                lock (_eventLock) BeginUsingInvocationList.Remove(value);
            }
        }
        private List<EventHandler> BeginUsingInvocationList { get; } = new();


        event EventHandler? IAuthorityEpochProvider.FinalDispose
        {
            add
            {
                if (value is null) return;
                lock (_eventLock) FinalDisposeInvocationList.Add(value);
            }

            remove
            {
                if (value is null) return;
                lock (_eventLock) FinalDisposeInvocationList.Remove(value);
            }
        }
        private List<EventHandler> FinalDisposeInvocationList { get; } = new();
        #endregion E V E N T S
    }
}
