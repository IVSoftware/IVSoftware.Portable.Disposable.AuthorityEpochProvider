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
                    Current.TCS = new TaskCompletionSource<Enum>(
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
                    Current?.TCS?.SetResult(Current?.Authority ?? AuthorityReserved.NoAuthority);
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
        public void CancelAuthorityEpoch(bool @throw)
        {
            IsCancelled = true;
            DHost.Current = null;
            DHost = new (this);
            var msg = $"{Authority.ToFullKey()} authority epoch has been cancelled.";
            Authority = AuthorityReserved.NoAuthority;
            TCS?.SetCanceled();
            if(@throw) throw new OperationCanceledException(msg);
        }

        public bool HasRequestedAuthority(Enum authority)
            => Authorities.Any(_ => _.ToFullKey() == authority.ToFullKey());

        public bool IsZero() => DHost.IsZero();

        public IDisposable RequestAuthority(Enum authority, Dictionary<string, object>? properties = null)
            => DHost.GetToken(sender: authority, properties);
        #region A W A I T
        TaskCompletionSource<Enum>? _tcs = default;
        private TaskCompletionSource<Enum>? TCS { get; set; }
        public TaskAwaiter<Enum> GetAwaiter()
        {
            return (TCS?.Task ?? Task.FromResult(Authority)).GetAwaiter();
        }
        #endregion A W A I T

        #region E V E N T S
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
