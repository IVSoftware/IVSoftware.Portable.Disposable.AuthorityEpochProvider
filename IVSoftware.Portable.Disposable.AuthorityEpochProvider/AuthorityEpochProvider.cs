using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IVSoftware.Portable.Disposable.AuthorityEpochProvider
{
    public class AuthorityEpochProvider : IAuthorityEpochProvider
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
                if(e.AutoDisposableContext.Sender is Enum authority)
                {
                    Current?.Authority = authority;
                }
                else
                {
                    this.ThrowFramework<InvalidCastException>(
                        $"Expecting an Enum that is the current authority.",
                        @throw: true);
                }
                Current?.IsCancelled = false;
                base.OnBeginUsing(e);
                Current?.OnBeginUsing(e);
            }
            protected override void OnFinalDispose(FinalDisposeEventArgs e)
            {
                Current?.IsDisposing = true;
                try
                {
                    base.OnFinalDispose(e);
                    Current?.OnFinalDispose(e);
                }
                finally
                {
                    Current?.IsDisposing = false;
                    Current?.Authority = AuthorityReserved.NoAuthority;
                }
            }
        }
        protected virtual void OnBeginUsing(BeginUsingEventArgs e) 
        {
            BeginUsing?.Invoke(this, e);
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

        public event EventHandler? BeginUsing;
        public event EventHandler? FinalDispose;
        public void CancelAuthorityEpoch(bool @throw)
        {
            IsCancelled = true;
            DHost.Current = null;
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
    }
}
