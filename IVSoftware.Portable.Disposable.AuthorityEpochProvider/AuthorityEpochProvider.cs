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

        public bool IsZero()
        {
            throw new NotImplementedException();
        }

        public IDisposable RequestAuthority(Enum authority, IDictionary<string, object>? properties = null)
        {
            throw new NotImplementedException();
        }
    }
}
