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
        T IAuthorityEpochProvider<T>.Authority => (T)base.Authority;

        T[] IAuthorityEpochProvider<T>.Authorities => [.. base.Authorities.Cast<T>()];

        public bool HasRequestedAuthority(T authority) => base.HasRequestedAuthority(authority);

        public IDisposable RequestAuthority(T authority, Dictionary<string, object>? properties = null)
            => base.RequestAuthority(authority, properties);
    }
}
