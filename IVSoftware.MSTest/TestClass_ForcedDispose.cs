using IVSoftware.Portable.Common;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Disposable.AuthorityEpochProvider;
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

            #region L o c a l F x				
            using var local = this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    aep.FinalDispose += localOnFinalDispose;
                },
                onDispose: (sender, e) =>
                {
                    aep.FinalDispose -= localOnFinalDispose;
                });
            void localOnFinalDispose(object? sender, EventArgs e)
            {
                builder.Add(e.ToString());
            }
            #endregion L o c a l F x

            subtest_BasicCancel();

            #region S U B T E S T S
            void subtest_BasicCancel()
            {
                using (aep.RequestAuthority(TestAuthority.A1))
                {
                    aep.CancelAuthorityEpoch(@throw: false);
                    Assert.IsTrue(aep.IsZero());
                    Assert.AreEqual(AuthorityReserved.NoAuthority, aep.Authority);
                }
                Assert.HasCount(0, builder);
            }
            #endregion S U B T E S T S
        }
    }
}
