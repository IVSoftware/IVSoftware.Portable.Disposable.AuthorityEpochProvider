using IVSoftware.Portable.Common;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using IVSoftware.WinOS.MSTest.Extensions;

namespace IVSoftware.MSTest
{
    [TestClass]
    public sealed class TestClass_ForcedDispose
    {
        enum TestAuthority { A1, A2, A3,}


        [TestMethod]
        public void Test_MakeFriend()
        {
            string actual, expected;

            actual = typeof(TestClass_ForcedDispose).ToStrongNamedFriendAssembly();
            actual.ToClipboardExpected();
            { }
            File.WriteAllText("internals-visible-to.log", actual);
            expected = @" 
[assembly: InternalsVisibleTo(""System.Private.CoreLib, PublicKey=00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab"")]"
            ;

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting public key."
            );
        }

        [TestMethod]
        public void Test_BasicAuthorityEpoch()
        {
            string actual, expected;

            AuthorityEpochProvider aep = new();

            var builder = new List<string?>();

            #region S P E C I A L I Z E D    E V E N T S
            aep.BeginUsing += (sender, e) =>
            {
                // Access a specialized property that isn't available
                // (without casting) on the explicit interface version.
                builder.Add(
                    $"1. {nameof(aep.BeginUsing)}: {((Enum)e.AutoDisposableContext.Sender).ToFullKey()}");
            };
            aep.FinalDispose += (sender, e) =>
            {
                // Access a specialized property that isn't available
                // (without casting) on the explicit interface version.
                builder.Add(
                    $"1. {nameof(aep.BeginUsing)}: {string.Join(",", e.ReleasedSenders.OfType<Enum>().Select(_=>_.ToFullKey()))}");
            };
            #endregion S P E C I A L I Z E D    E V E N T S

            #region I N T E R F A C E    E V E N T S
            ((IAuthorityEpochProvider)aep).BeginUsing += (sender, e) =>
            {
                builder.Add($"2. {nameof(aep.BeginUsing)}: {aep.Authority.ToFullKey()}");
            };
            ((IAuthorityEpochProvider)aep).FinalDispose += (sender, e) =>
            {
                builder.Add($"2. {nameof(aep.FinalDispose)}: {aep.Authority.ToFullKey()} IsDisposing={aep.IsDisposing}");
            };
            #endregion I N T E R F A C E    E V E N T S

            subtest_BasicEpoch();
            subtest_BasicCancel();

            #region S U B T E S T S

            void subtest_BasicEpoch()
            {
                using (aep.RequestAuthority(TestAuthority.A1))
                {
                    actual = string.Join(Environment.NewLine, builder); builder.Clear();
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
1. BeginUsing: TestAuthority.A1
2. BeginUsing: TestAuthority.A1"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting begin using."
                    );
                }
                actual = string.Join(Environment.NewLine, builder); builder.Clear();
                actual.ToClipboardExpected();
                { }
                expected = @" 
1. BeginUsing: TestAuthority.A1
2. FinalDispose: TestAuthority.A1 IsDisposing=True"
                ;
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting begin using."
                );

                Assert.IsFalse(aep.IsCancelled);
                Assert.IsFalse(aep.IsDisposing);
                Assert.IsTrue(aep.IsZero());
                Assert.AreEqual(AuthorityReserved.NoAuthority, aep.Authority);
            }

            void subtest_BasicCancel()
            {
                using (aep.RequestAuthority(TestAuthority.A1))
                {
                    actual = string.Join(Environment.NewLine, builder); builder.Clear();
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
1. BeginUsing: TestAuthority.A1
2. BeginUsing: TestAuthority.A1"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting begin using."
                    );

                    Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority.A1.ToFullKey());
                    Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority.A1));

                    using (aep.RequestAuthority(TestAuthority.A2))
                    {
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority.A1.ToFullKey());
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority.A2));
                    }

                    using (aep.RequestAuthority(TestAuthority.A3))
                    {
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority.A1.ToFullKey());
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority.A2)); // Relinquished = disposed.
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority.A3));
                    }

                    using (aep.RequestAuthority(TestAuthority.A2))
                    using (aep.RequestAuthority(TestAuthority.A3))
                    {
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority.A1.ToFullKey());
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority.A1));
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority.A2));
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority.A3));

                        aep.CancelAuthorityEpoch(@throw: false);

                        Assert.IsTrue(aep.IsZero());
                        Assert.AreEqual(AuthorityReserved.NoAuthority, aep.Authority);
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority.A1));
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority.A2));
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority.A3));
                    }
                }
                Assert.IsTrue(aep.IsCancelled);
                Assert.HasCount(0, builder);
            }
            #endregion S U B T E S T S
        }
    }
}
