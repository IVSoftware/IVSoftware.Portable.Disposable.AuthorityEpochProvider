using IVSoftware.Portable.Common;
using IVSoftware.Portable.Common.Exceptions;
using IVSoftware.Portable.Disposable;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using IVSoftware.WinOS.MSTest.Extensions;
using Newtonsoft.Json;
using System.Diagnostics;

namespace IVSoftware.MSTest
{
    [TestClass]
    public sealed class TestClass_ForcedDispose
    {
        enum TestAuthority1 { A, B, C,}
        enum TestAuthority2 { A, B, C,}
        enum StdTestProperties
        {
            Array,
            Json,
        }
        class JsonTestSerialize
        {
            public string? Text{ get; set; }
        }

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

        /// <summary>
        /// Excercise authorities with mixed types.
        /// </summary>
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
                    $"1. {nameof(aep.BeginUsing)}: {((Enum)e.AutoDisposableContext.Sender).ToFullKey()} IsDisposing={aep.IsDisposing}");
            };

            int countChangedMonitor = 0;
            aep.CountChanged += (sender, e) => countChangedMonitor++;
            aep.FinalDispose += (sender, e) =>
            {
                // Access a specialized property that isn't available
                // (without casting) on the explicit interface version.
                builder.Add(
                    $"1. {nameof(aep.FinalDispose)}: {string.Join(",", e.ReleasedSenders.OfType<Enum>().Select(_=>_.ToFullKey()))} IsDisposing={aep.IsDisposing}");
            };
            #endregion S P E C I A L I Z E D    E V E N T S

            #region I N T E R F A C E    E V E N T S
            ((IAuthorityEpochProvider)aep).BeginUsing += (sender, e) =>
            {
                builder.Add($"2. {nameof(aep.BeginUsing)}: {aep.Authority.ToFullKey()} IsDisposing={aep.IsDisposing}");
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
                using (aep.RequestAuthority(TestAuthority1.A))
                {
                    actual = string.Join(Environment.NewLine, builder); builder.Clear();
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
1. BeginUsing: TestAuthority1.A IsDisposing=False
2. BeginUsing: TestAuthority1.A IsDisposing=False"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting parity for 1 event each on specialized and interface connection points."
                    );
                }
                actual = string.Join(Environment.NewLine, builder); builder.Clear();
                actual.ToClipboardExpected();
                { }
                expected = @" 
1. FinalDispose: TestAuthority1.A IsDisposing=True
2. FinalDispose: TestAuthority1.A IsDisposing=True"
                ;
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                        "Expecting parity for 1 event each on specialized and interface connection points."
                );

                Assert.IsFalse(aep.IsCancelled);
                Assert.IsFalse(aep.IsDisposing);
                Assert.IsTrue(aep.IsZero());
                Assert.AreEqual(AuthorityReserved.NoAuthority, aep.Authority);
            }

            void subtest_BasicCancel()
            {
                // Request (granted)
                using (aep.RequestAuthority(TestAuthority1.A))
                {
                    actual = string.Join(Environment.NewLine, builder); builder.Clear();
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
1. BeginUsing: TestAuthority1.A IsDisposing=False
2. BeginUsing: TestAuthority1.A IsDisposing=False"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting parity for 1 event each on specialized and interface connection points."
                    );

                    Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority1.A.ToFullKey());
                    Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.A));

                    // Registered, but not granted.
                    countChangedMonitor = 0;
                    using (aep.RequestAuthority(TestAuthority2.B))
                    {
                        Assert.AreEqual(1, countChangedMonitor, "Expecting count change."); countChangedMonitor = 0;
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority1.A.ToFullKey());
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority2.B));
                    }
                    Assert.AreEqual(1, countChangedMonitor, "Expecting count change."); countChangedMonitor = 0;

                    // Registered, but not granted.
                    countChangedMonitor = 0;
                    using (aep.RequestAuthority(TestAuthority1.C))
                    {
                        Assert.AreEqual(1, countChangedMonitor, "Expecting count change."); countChangedMonitor = 0;
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority1.A.ToFullKey());
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority2.B)); // Relinquished = disposed.
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.C));
                    }
                    Assert.AreEqual(1, countChangedMonitor, "Expecting count change."); countChangedMonitor = 0;

                    // Registered, but not granted.
                    using (aep.RequestAuthority(TestAuthority2.B))
                    using (aep.RequestAuthority(TestAuthority1.C))
                    {
                        Assert.AreEqual(2, countChangedMonitor, "Expecting count change."); countChangedMonitor = 0;
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority1.A.ToFullKey());
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.A));
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority2.B));
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.C));


                        actual = string.Join(Environment.NewLine, aep.Authorities.Select(_=>_.ToFullKey()));
                        actual.ToClipboardExpected();
                        { }
                        expected = @" 
TestAuthority1.A
TestAuthority2.B
TestAuthority1.C";

                        Assert.AreEqual(
                            expected.NormalizeResult(),
                            actual.NormalizeResult(),
                            "Expecting authority type mix (this would not be possible with AuthorityProvider<T>)."
                        );

                        aep.CancelAuthorityEpoch(@throw: false);

                        Assert.IsTrue(aep.IsZero());
                        Assert.AreEqual(AuthorityReserved.NoAuthority, aep.Authority);
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.A));
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority2.B));
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.C));
                    }
                    Assert.AreEqual(0, countChangedMonitor, "Expecting *no* count change."); countChangedMonitor = 0;
                }
                Assert.IsTrue(aep.IsCancelled);
                Assert.HasCount(0, builder);
            }
            #endregion S U B T E S T S
        }

        /// <summary>
        /// Excercise authorities where with mixed types raise runtime cast exceptions.
        /// </summary>
        [TestMethod]
        public void Test_BasicAuthorityEpochT()
        {
            string actual, expected;

            AuthorityEpochProvider<TestAuthority1> aep = new();
            var builder = new List<string?>();

            #region L o c a l F x
            var builderThrow = new List<string>();
            void localOnBeginThrowOrAdvise(object? sender, Throw e)
            {
                builderThrow.Add($"{e.Mode}: {e.Message}");
                e.Handled = true;
            }
            #endregion L o c a l F x

            using var local = this.WithOnDispose(
                onInit: (sender, e) =>
                {
                    Throw.BeginThrowOrAdvise += localOnBeginThrowOrAdvise;
                },
                onDispose: (sender, e) =>
                {
                    Throw.BeginThrowOrAdvise -= localOnBeginThrowOrAdvise;
                });

            #region S P E C I A L I Z E D    E V E N T S
            aep.BeginUsing += (sender, e) =>
            {
                // Access a specialized property that isn't available
                // (without casting) on the explicit interface version.
                builder.Add(
                    $"1. {nameof(aep.BeginUsing)}: {((Enum)e.AutoDisposableContext.Sender).ToFullKey()} IsDisposing={aep.IsDisposing}");
            };

            int countChangedMonitor = 0;
            aep.CountChanged += (sender, e) => countChangedMonitor++;
            aep.FinalDispose += (sender, e) =>
            {
                // Access a specialized property that isn't available
                // (without casting) on the explicit interface version.
                builder.Add(
                    $"1. {nameof(aep.FinalDispose)}: {string.Join(",", e.ReleasedSenders.OfType<Enum>().Select(_ => _.ToFullKey()))} IsDisposing={aep.IsDisposing}");
            };
            #endregion S P E C I A L I Z E D    E V E N T S

            #region I N T E R F A C E    E V E N T S
            ((IAuthorityEpochProvider)aep).BeginUsing += (sender, e) =>
            {
                builder.Add($"2. {nameof(aep.BeginUsing)}: {aep.Authority.ToFullKey()} IsDisposing={aep.IsDisposing}");
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
                using (aep.RequestAuthority(TestAuthority1.A))
                {
                    actual = string.Join(Environment.NewLine, builder); builder.Clear();
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
1. BeginUsing: TestAuthority1.A IsDisposing=False
2. BeginUsing: TestAuthority1.A IsDisposing=False"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting parity for 1 event each on specialized and interface connection points."
                    );
                }
                actual = string.Join(Environment.NewLine, builder); builder.Clear();
                actual.ToClipboardExpected();
                { }
                expected = @" 
1. FinalDispose: TestAuthority1.A IsDisposing=True
2. FinalDispose: TestAuthority1.A IsDisposing=True"
                ;
                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                        "Expecting parity for 1 event each on specialized and interface connection points."
                );

                Assert.IsFalse(aep.IsCancelled);
                Assert.IsFalse(aep.IsDisposing);
                Assert.IsTrue(aep.IsZero());
                Assert.AreEqual(AuthorityReserved.NoAuthority, aep.Authority);
            }

            void subtest_BasicCancel()
            {
                // Request (granted)
                using (aep.RequestAuthority(TestAuthority1.A))
                {
                    actual = string.Join(Environment.NewLine, builder); builder.Clear();
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
1. BeginUsing: TestAuthority1.A IsDisposing=False
2. BeginUsing: TestAuthority1.A IsDisposing=False"
                    ;

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting parity for 1 event each on specialized and interface connection points."
                    );

                    Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority1.A.ToFullKey());
                    Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.A));

                    // Runtime cast exception.
                    countChangedMonitor = 0;
                    using (aep.RequestAuthority(TestAuthority2.B))
                    {
                        actual = string.Join(Environment.NewLine, builderThrow); builderThrow.Clear();
                        actual.ToClipboardExpected();
                        { }
                        expected = @" 
ThrowHard: Requested authority must be of type TestAuthority1";

                        Assert.AreEqual(
                            expected.NormalizeResult(),
                            actual.NormalizeResult(),
                            "Expecting Throw which we handle."
                        );
                        Assert.AreEqual(
                            0, 
                            countChangedMonitor,
                            "Expecting *no* count change due to exception."); 
                        countChangedMonitor = 0;
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority1.A.ToFullKey());
                        Assert.IsFalse(
                            aep.HasRequestedAuthority(TestAuthority2.B),
                            $"Expecting *no* grant of authority for invalid enum type"
                        );
                    }
                    Assert.AreEqual(
                        0, 
                        countChangedMonitor, 
                        "Expecting *no* count change. No token means no dispose."); 
                    countChangedMonitor = 0;

                    // Registered, but not granted.
                    countChangedMonitor = 0;
                    using (aep.RequestAuthority(TestAuthority1.C))
                    {
                        Assert.AreEqual(1, countChangedMonitor, "Expecting count change."); countChangedMonitor = 0;
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority1.A.ToFullKey());
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority2.B)); // Relinquished = disposed.
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.C));
                    }
                    Assert.AreEqual(1, countChangedMonitor, "Expecting count change."); countChangedMonitor = 0;

                    // Try again. This time, do not mix the types.
                    // Registered, but not granted.
                    using (aep.RequestAuthority(TestAuthority1.B))
                    using (aep.RequestAuthority(TestAuthority1.C))
                    {
                        Assert.AreEqual(2, countChangedMonitor, "Expecting count change."); countChangedMonitor = 0;
                        Assert.HasCount(0, builder);
                        Assert.AreEqual(aep.Authority.ToFullKey(), TestAuthority1.A.ToFullKey());
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.A));
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.B));
                        Assert.IsTrue(aep.HasRequestedAuthority(TestAuthority1.C));


                        actual = string.Join(Environment.NewLine, aep.Authorities.Select(_ => _.ToFullKey()));
                        actual.ToClipboardExpected();
                        { }
                        expected = @" 
TestAuthority1.A
TestAuthority1.B
TestAuthority1.C";

                        Assert.AreEqual(
                            expected.NormalizeResult(),
                            actual.NormalizeResult(),
                            "Expecting authority type mix (this would not be possible with AuthorityProvider<T>)."
                        );

                        aep.CancelAuthorityEpoch(@throw: false);

                        Assert.IsTrue(aep.IsZero());
                        Assert.AreEqual(AuthorityReserved.NoAuthority, aep.Authority);
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.A));
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.B));
                        Assert.IsFalse(aep.HasRequestedAuthority(TestAuthority1.C));
                    }
                    Assert.AreEqual(0, countChangedMonitor, "Expecting *no* count change."); countChangedMonitor = 0;
                }
                Assert.IsTrue(aep.IsCancelled);
                Assert.HasCount(0, builder);
            }
            #endregion S U B T E S T S
        }

        [TestMethod]
        public void Test_ReleasedSendersAndDictionary()
        {
            string actual, expected;

            AuthorityEpochProvider<TestAuthority2> aep = new();
            var builder = new List<string?>();
            var jsonObject = new JsonTestSerialize { Text = "Marklar" };

            aep.FinalDispose += (sender, e) =>
            {
                // Disposable host, by design:
                // 1. Takes a snapshot of itself
                // 2. Clear itself
                // 3. Places the immutable snapshot in the event.
                actual = JsonConvert.SerializeObject(e, Formatting.Indented);
                actual.ToClipboardExpected();
                { } // <- FIRST TIME ONLY: Adjust the message.
                actual.ToClipboardAssert("Expecting snapshot.");
                { }
                expected = @" 
{
  ""ReleasedSenders"": [
    0,
    1,
    2
  ],
  ""KeyCount"": 2,
  ""Keys"": [
    ""StdTestProperties.Array"",
    ""StdTestProperties.Json""
  ],
  ""Values"": [
    [
      ""Dogs"",
      ""Cats"",
      ""Pets""
    ],
    ""{\""Text\"":\""Marklar\""}""
  ]
}";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting snapshot."
                );
                actual = JsonConvert.SerializeObject(aep, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
{}";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting empty."
                );
            };
            using(aep.RequestAuthority(TestAuthority2.A, new Dictionary<string, object> 
            {
                {StdTestProperties.Array.ToFullKey(), new []{"Dogs", "Cats", "Pets" } }
            }))
            using(aep.RequestAuthority(TestAuthority2.B, new Dictionary<string, object>
            {
                {StdTestProperties.Json.ToFullKey(), JsonConvert.SerializeObject(jsonObject) }
            }))
            using (aep.RequestAuthority(TestAuthority2.C))
            {

                actual = JsonConvert.SerializeObject(aep, Formatting.Indented);
                actual.ToClipboardExpected();
                { }
                expected = @" 
{
  ""StdTestProperties.Array"": [
    ""Dogs"",
    ""Cats"",
    ""Pets""
  ],
  ""StdTestProperties.Json"": ""{\""Text\"":\""Marklar\""}""
}";

                Assert.AreEqual(
                    expected.NormalizeResult(),
                    actual.NormalizeResult(),
                    "Expecting serialization as [JsonDictionary]"
                );
            }
        }

        [TestMethod]
        public async Task Test_Awaiter()
        {
            string actual, expected;

            AuthorityEpochProvider<TestAuthority1> aep = new();
            var builder = new List<string?>();

            await aep;

            var stopwatch = Stopwatch.StartNew();
            TaskCompletionSource ensureTestStart = new();
            _ = Task.Run(async () =>
            {
                ensureTestStart.SetResult();
                using (aep.RequestAuthority(TestAuthority1.A))
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            });
            await ensureTestStart.Task;
            await aep;
            stopwatch.Stop();
            { }
        }
    }
}
