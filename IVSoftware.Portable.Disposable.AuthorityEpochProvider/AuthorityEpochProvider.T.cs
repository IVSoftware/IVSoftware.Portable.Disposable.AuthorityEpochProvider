using IVSoftware.Portable.Common.Attributes;
using IVSoftware.Portable.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IVSoftware.Portable.Disposable
{
    public class AuthorityEpochProvider<T> 
        : AuthorityEpochProvider
        , IAuthorityEpochProvider<T>
        where T : struct, Enum
    {
        public new T Authority => (T)base.Authority;

        public new T[] Authorities => [.. base.Authorities.Cast<T>()];

        public bool HasRequestedAuthority(T authority) => base.HasRequestedAuthority(authority);

        public IDisposable RequestAuthority(T authority, Dictionary<string, object>? properties = null)
            => base.RequestAuthority(authority, properties);

        public new IDisposable RequestAuthority(Enum authority, Dictionary<string, object>? properties = null)
        {
            if(authority is T authorityT)
            {
                return base.RequestAuthority(authority, properties);
            }
            else
            {
                this.ThrowHard<InvalidCastException>(
                    $"Requested authority must be of type {typeof(T).Name}");
                return AuthorityToken.Empty;
            }
        }

        /// <summary>
        /// Inert disposable token allows Throw continuation after advise.
        /// </summary>
        public class AuthorityToken : IDisposable
        {
            public static AuthorityToken Empty { get; } = new AuthorityToken();
            private AuthorityToken() { }
            public void Dispose() { }
        }
    }
}
