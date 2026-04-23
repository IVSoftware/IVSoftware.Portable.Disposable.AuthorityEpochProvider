using IVSoftware.Portable.Disposable;
using System.Collections;

namespace IVS.MSTest;

[TestClass]
public class TestClass_Basics
{
    [TestMethod]
    public void Test_DictRemove()
    {
        AuthorityEpochProvider aep = new();

        aep.BeginUsing += (sender, e) =>
        {
            Assert.IsNotNull(aep[nameof(IList)]);
            Assert.IsNotNull(e.AutoDisposableContext.Properties[nameof(IList)]);
            aep.Remove(nameof(IList));
            Assert.IsFalse(aep.ContainsKey(nameof(IList)));
        };
        aep.FinalDispose += (sender, e) =>
        {
            // Attempting to repro a BUGIRL in ModelDataExchangeAuthorityProvider subclass.
            // There is no sign here of any issue.
            Assert.IsFalse(aep.ContainsKey(nameof(IList)));
            Assert.HasCount(0, aep.Keys);
            Assert.AreEqual(0, e.KeyCount);
            Assert.HasCount(0, e.Keys);
        };

        using (aep.RequestAuthority(TestAuthority1.A, new Dictionary<string, object>()
        {
            { nameof(IList), Array.Empty<object>() }
        })) 
        {
            // N O O P    S C O P E
        }
    }
}
