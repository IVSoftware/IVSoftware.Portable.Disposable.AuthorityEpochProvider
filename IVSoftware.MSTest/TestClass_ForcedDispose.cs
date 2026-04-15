using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Disposable.AuthorityEpochProvider;

namespace IVSoftware.MSTest
{
    [TestClass]
    public sealed class TestClass_ForcedDispose
    {
        enum TestAuthority { A1, A2, A3,}
        [TestMethod]
        public void Test_BasicAuthorityEpoch()
        {
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
            void localOnFinalDispose(object? sender, FinalDisposeEventArgs e)
            {
                builder.Add(e.ToString());
            }
            #endregion L o c a l F x

            subtest_CancelOne();

            #region S U B T E S T S
            void subtest_CancelOne()
            {
                using (aep.RequestAuthority(TestAuthority.A1))
                {
                    aep.CancelAuthorityEpoch(@throw: false);
                }
                Assert.HasCount(0, builder);
            }
            #endregion S U B T E S T S
        }
    }
}
