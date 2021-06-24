// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class GameHostSuspendTest
    {
        private TestTestGame game;
        private HeadlessGameHost host;

        private const int timeout = 10000;

        [Test]
        public void TestPauseResume()
        {
            var gameCreated = new ManualResetEventSlim();

            IBindable<GameThreadState> updateThreadState = null;

            var task = Task.Run(() =>
            {
                using (host = new HeadlessGameHost(@"host", false))
                {
                    game = new TestTestGame();
                    gameCreated.Set();
                    host.Run(game);
                }
            });

            Assert.IsTrue(gameCreated.Wait(timeout));
            Assert.IsTrue(game.BecameAlive.Wait(timeout));

            // check scheduling is working before suspend
            var completed = new ManualResetEventSlim();
            game.Schedule(() =>
            {
                updateThreadState = host.UpdateThread.State.GetBoundCopy();
                updateThreadState.BindValueChanged(state =>
                {
                    Assert.IsTrue(ThreadSafety.IsUpdateThread);
                });
                completed.Set();
            });

            Assert.IsTrue(completed.Wait(timeout / 10));
            Assert.AreEqual(GameThreadState.Running, updateThreadState.Value);

            host.Suspend();

            // in single-threaded execution, the main thread may already be in the process of updating one last time.
            int gameUpdates = 0;
            game.Scheduler.AddDelayed(() => ++gameUpdates, 0, true);
            Assert.That(() => gameUpdates, Is.LessThan(2).After(timeout / 10));
            Assert.AreEqual(GameThreadState.Paused, updateThreadState.Value);

            // check that scheduler doesn't process while suspended..
            completed.Reset();
            game.Schedule(() => completed.Set());
            Assert.IsFalse(completed.Wait(timeout / 10));

            // ..and does after resume.
            host.Resume();
            Assert.IsTrue(completed.Wait(timeout / 10));
            Assert.AreEqual(GameThreadState.Running, updateThreadState.Value);

            game.Exit();
            Assert.IsTrue(task.Wait(timeout));
            Assert.AreEqual(GameThreadState.Exited, updateThreadState.Value);
        }

        private class TestTestGame : TestGame
        {
            public readonly ManualResetEventSlim BecameAlive = new ManualResetEventSlim();

            protected override void LoadComplete()
            {
                BecameAlive.Set();
            }
        }
    }
}
