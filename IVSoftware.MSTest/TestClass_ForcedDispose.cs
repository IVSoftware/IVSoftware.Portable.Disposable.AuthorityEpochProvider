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

            actual = this.ToStrongNamedFriendAssembly();
            actual.ToClipboardExpected();
            { }
            expected = @" 
[assembly: InternalsVisibleTo(""IVSoftware.MSTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100695db9bd80b2ad68555b025183f517a808771ddbb0d7c730a5187aa8ef76f2152d6d0449bfda81b600a18686208d7ec04a60d7356ec4d119cce75d8cc9fe5ecc580bfaa5a2bdc96a1143ef494e07cb5dbb778422df151adf79d6ce157f25152fa9c304fe11ad3e193d056456b5f818ee61150bc8745e68890194f8c24353a697"")]"
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
            aep.BeginUsing += (sender, eUnk) =>
            {
                if (eUnk is BeginUsingEventArgs e)
                {
                    builder.Add($"{nameof(aep.BeginUsing)}: {aep.Authority.ToFullKey()}");
                }
            };
            aep.FinalDispose += (sender, eUnk) =>
            {
                if (eUnk is FinalDisposeEventArgs e)
                {
                    builder.Add($"{nameof(aep.FinalDispose)}: {aep.Authority.ToFullKey()} IsDisposing={aep.IsDisposing}");
                }
            };

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
BeginUsing: TestAuthority.A1";

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
FinalDispose: TestAuthority.A1 IsDisposing=True"
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
BeginUsing: TestAuthority.A1";

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
