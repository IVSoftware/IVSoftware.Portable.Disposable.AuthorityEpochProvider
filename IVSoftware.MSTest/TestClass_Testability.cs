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
        /// <remarks>
        /// Confirms that TestableEpoch makes Guid and DateTimeOffset values
        /// repeatable while preserving natural production-style call sites.
        /// </remarks>
        [TestMethod, DoNotParallelize]
        public void Test_ReadMeClaims()
        {
            string actual, expected;
            var builder = new List<string>();
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
    }
}
