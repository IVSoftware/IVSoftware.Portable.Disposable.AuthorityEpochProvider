using IVSoftware.Portable.Disposable;
using IVSoftware.WinOS.MSTest.Extensions;

namespace IVS.MSTest
{
    [TestClass]
    public class TestClass_Testability
    {
        /// <summary>
        /// Verifies the deterministic claims made in the package README.
        /// </summary>
        [TestMethod, DoNotParallelize]
        public void Test_ReadMeClaims()
        {
            string actual, expected;
            var builder = new List<string>();

            subtest_TestablEpoch();
            subtest_HasEver();

            #region S U B T E S T S

            /// <summary>
            /// Confirms that TestableEpoch makes Guid and DateTimeOffset values
            /// repeatable while preserving natural production-style call sites.
            /// </summary>
            void subtest_TestablEpoch()
            {
                using (var te = this.TestableEpoch())
                {
                    builder.Add(new Guid().WithTestability().ToString());
                    builder.Add(new Guid().WithTestability().ToString());
                    builder.Add(DateTimeOffset.UtcNow.WithTestability().ToString("O"));
                    builder.Add(DateTimeOffset.UtcNow.WithTestability().ToString("O"));

                    // <PackageReference Include="IVSoftware.WinOS.MSTest.Extensions" Version="*" />
                    // Utility for on-the-fly limit pasting. Requires human review.
                    actual = string.Join(Environment.NewLine, builder); builder.Clear();
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
312d1c21-0000-0000-0000-000000000000
312d1c21-0000-0000-0000-000000000001
2000-01-01T09:00:00.0000000+07:00
2000-01-01T09:01:00.0000000+07:00";

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting deterministic Guid and time sequences."
                    );

                    // Now, within the same using block, reset the epoch.
                    _ = new Guid().WithTestability();
                    te.ResetEpoch();

                    builder.Add(new Guid().WithTestability().ToString());
                    builder.Add(DateTimeOffset.UtcNow.WithTestability().ToString("O"));

                    actual = string.Join(Environment.NewLine, builder); builder.Clear();
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
312d1c21-0000-0000-0000-000000000000
2000-01-01T09:00:00.0000000+07:00";

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting deterministic Guid and time sequences."
                    );
                }

                using (var te2 = this.TestableEpoch())
                {
                    actual = new Guid().WithTestability().ToString();
                    expected = @" 
312d1c21-0000-0000-0000-000000000000";

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting ResetEpoch to restart the deterministic sequence."
                    );
                }
            }

            /// <summary>
            /// Verifies the semantic split between "held now" and "ever requested
            /// during the current epoch", including reset when the epoch fully unwinds.
            /// </summary>
            void subtest_HasEver()
            {
                var aep = new AuthorityEpochProvider<TestAuthority1>();

                using (aep.RequestAuthority(TestAuthority1.A))
                {
                    // A is active now, and has participated in this epoch.
                    Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.A));
                    Assert.IsTrue(aep.HasEverRequestedAuthority(TestAuthority1.A));

                    // B has not yet participated.
                    Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.B));
                    Assert.IsFalse(aep.HasEverRequestedAuthority(TestAuthority1.B));

                    using (aep.RequestAuthority(TestAuthority1.B))
                    {
                        // While B is held, both A and B are active and both have participated.
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.A));
                        Assert.IsTrue(aep.HasEverRequestedAuthority(TestAuthority1.A));
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.B));
                        Assert.IsTrue(aep.HasEverRequestedAuthority(TestAuthority1.B));
                    }

                    // After B is disposed, A remains active.
                    // B is no longer active, but it still counts as having participated
                    // during the current epoch.
                    Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.A));
                    Assert.IsTrue(aep.HasEverRequestedAuthority(TestAuthority1.A));
                    Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.B));
                    Assert.IsTrue(aep.HasEverRequestedAuthority(TestAuthority1.B));
                }

                // Once the epoch fully unwinds, neither authority is active and the
                // epoch-scoped participation history is cleared.
                Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.A));
                Assert.IsFalse(aep.HasEverRequestedAuthority(TestAuthority1.A));
                Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.B));
                Assert.IsFalse(aep.HasEverRequestedAuthority(TestAuthority1.B));
            }
            #endregion S U B T E S T S
        }
    }
}
