using IVSoftware.Portable.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IVSoftware.Portable.Disposable
{
    /// <summary>
    /// Provides deterministic values within a disposable test epoch.
    /// </summary>
    /// <remarks>
    /// Ambient Guid and DateTimeOffset values remain natural at call sites.
    /// Tests opt in by entering a local epoch and calling WithTestability.
    /// </remarks>
    public static class TestableEpochExtensions
    {
        /// <summary>
        /// Controls when a test value is returned relative to incrementing.
        /// </summary>
        /// <remarks>
        /// Used by TestableEpochExtensions to expose deterministic sequences.
        /// </remarks>
        public enum IncrMode
        {
            /// <summary>
            /// Increments first, then returns the incremented value.
            /// </summary>
            Prefix,

            /// <summary>
            /// Returns the current value without incrementing.
            /// </summary>
            Current,

            /// <summary>
            /// Returns the current value, then increments for next access.
            /// </summary>
            Postfix,
        }

        /// <summary>
        /// Starts a deterministic test epoch for local value generation.
        /// </summary>
        /// <remarks>
        /// Only one test epoch may be active at a time.
        /// Parallel callers throw.
        /// </remarks>
        public static IDisposable TestableEpoch(this object? @this)
        {
            if (DHostTokenDispenser.IsZero())
            {
                return DHostTokenDispenser.GetToken(sender: @this);
            }
            else
            {
                throw new InvalidOperationException(
                    "TestableEpoch is single-threaded. Mark the test with [DoNotParallelize].");
            }
        }

        static DisposableHost DHostTokenDispenser
        {
            get
            {
                if (_dhostTokenDispenser is null)
                {
                    _dhostTokenDispenser = new DisposableHost();
                    _dhostTokenDispenser.BeginUsing += (sender, e) =>
                    {
                        _guidCurrent = GuidReset;
                        _utcCurrent = UtcReset;
                    };
                }
                return _dhostTokenDispenser;
            }
        }
        static DisposableHost? _dhostTokenDispenser = null;

        #region G U I D
        /// <summary>
        /// Gets the first Guid produced by a fresh test epoch.
        /// </summary>
        public static Guid GuidReset { get; } =
            new Guid("312D1C21-0000-0000-0000-000000000000");

        /// <summary>
        /// Returns a deterministic Guid when a test epoch is active.
        /// </summary>
        public static Guid WithTestability(
            this Guid @this,
            IncrMode? mode = IncrMode.Postfix)
        {
            if (DHostTokenDispenser.IsZero())
            {
                return @this;
            }
            else
            {
                mode ??= IncrMode.Postfix;

                switch ((IncrMode)mode)
                {
                    case IncrMode.Current:
                        return _guidCurrent;
                    case IncrMode.Prefix:
                    case IncrMode.Postfix:
                        // Deterministic increment
                        var bytes = _guidCurrent.ToByteArray();
                        for (int i = bytes.Length - 1; i >= 0; i--)
                        {
                            if (++bytes[i] != 0)
                                break; // carry complete
                        }
                        if (mode == IncrMode.Postfix)
                        {
                            var pre = _guidCurrent;
                            _guidCurrent = new Guid(bytes);
                            return pre;
                        }
                        else
                        {
                            _guidCurrent = new Guid(bytes);
                            return _guidCurrent;
                        }

                    default:
                        @this.ThrowHard<NotSupportedException>($"The {mode.ToFullKey()} case is not supported.");
                        return new Guid();
                }
            }
        }
        #endregion G U I D

        #region U T C
        /// <summary>
        /// Gets the first timestamp produced by a fresh test epoch.
        /// </summary>
        public static DateTimeOffset UtcReset { get; } =
            new DateTimeOffset(2000, 1, 1, 9, 0, 0, TimeSpan.FromHours(7));

        /// <summary>
        /// Gets or sets the default timestamp increment for test epochs.
        /// </summary>
        public static TimeSpan DefaultIncr
        {
            get
            {
                return _defaultIncr < _minIncr ? _minIncr : _defaultIncr;
            }
            set
            {
                _defaultIncr = value;
            }
        }

        // The DEFAULT for the DEFAULT INCREMENTER
        static TimeSpan _defaultIncr = TimeSpan.FromMinutes(1);

        static DateTimeOffset _utcCurrent = UtcReset;

        static Guid _guidCurrent = GuidReset;

        static readonly TimeSpan _minIncr = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Returns a deterministic timestamp when a test epoch is active.
        /// </summary>
        public static DateTimeOffset WithTestability(
            this DateTimeOffset @this,
            TimeSpan? incr = null,
            IncrMode? mode = IncrMode.Postfix)
        {
            if (DHostTokenDispenser.IsZero())
            {
                return @this;
            }
            else
            {
                incr ??= DefaultIncr;
                mode ??= IncrMode.Postfix;

                switch ((IncrMode)mode)
                {
                    case IncrMode.Prefix:
                        _utcCurrent += (TimeSpan)incr;
                        return _utcCurrent;
                    case IncrMode.Current:
                        return _utcCurrent;
                    case IncrMode.Postfix:
                        var pre = _utcCurrent;
                        _utcCurrent += (TimeSpan)incr;
                        return pre;
                    default:
                        @this.ThrowHard<NotSupportedException>($"The {mode.ToFullKey()} case is not supported.");
                        return DateTimeOffset.MinValue;
                }
            }
        }
        #endregion U T C

        /// <summary>
        /// Resets the active test epoch back to its initial values.
        /// </summary>
        public static void ResetEpoch(this IDisposable @this)
        {
            if (DHostTokenDispenser.IsZero())
            {
                @this.ThrowSoft<InvalidOperationException>(
                    $"Reset was requested when no active epoch is running.");
            }
            else
            {
                if (DHostTokenDispenser.Tokens.Any(_ => ReferenceEquals(_, @this)))
                {
                    if (@this is DisposableHost.DisposableToken token)
                    {
                        _guidCurrent = GuidReset;
                        _utcCurrent = UtcReset;
                    }
                    else
                    {
                        @this.ThrowHard<InvalidCastException>("Receiver must be a DisposableHost.DisposableToken}");
                    }
                }
                else
                {
                    @this.ThrowHard<InvalidOperationException>(
                        $"Epoch can only be reset from the current active token.");
                }
            }
        }
        private static string ToFullKey(this Enum @this)
            => $"{@this.GetType().Name}.{@this}";
    }
}
