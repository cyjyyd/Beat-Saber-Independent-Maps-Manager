using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using BeatSaberIndependentMapsManager.Services;
using BeatSaberIndependentMapsManager.ViewModels;
using BeatSaberIndependentMapsManager.Abstractions;

namespace BeatSaberIndependentMapsManager.Tests.Services
{
    public class DummyMainView : IMainView
    {
        public bool IsDisposed => false;
        public void InvokeLog(string message) { }
        public void Log(string message) { }
        public void UpdateProgress(int progress) { }
        public void UpdateStatus(string action, string status, int progress) { }
    }

    public class MainPresenterTests
    {
        [Fact]
        public async Task ApplyScanResult_ConcurrentAccess_DoesNotThrowException()
        {
            var config = new Config();
            var viewModel = new MainViewModel(new DummyMainView(), config);

            var tasks = new List<Task>();

            // Simulate concurrent scan results applying to the same viewModel state
            for (int i = 0; i < 50; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    var result = new SongScanResult
                    {
                        ResultType = ScanResultType.MusicPack,
                        MusicPackName = "Pack" + (index % 5), // Will cause duplicate names and internal collisions
                        MusicPackPath = "C:\\test\\pack" + index,
                        PackSongs = new Dictionary<string, SongMap>
                        {
                            { "A" + index, new SongMap() },
                            { "B" + index, new SongMap() }
                        }
                    };

                    viewModel.ApplyScanResult(result);
                }));

                tasks.Add(Task.Run(() =>
                {
                    var result = new SongScanResult
                    {
                        ResultType = ScanResultType.DelicatedSong,
                        DelicatedSong = new ParsedSongResult { bsr = "C" + index, song = new SongMap() }
                    };

                    viewModel.ApplyScanResult(result);
                }));
            }

            var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(tasks));

            Assert.Null(exception); // Must not throw ArgumentException from Dictionary concurrent operations
            Assert.NotEmpty(viewModel.MusicPackInfo);
            Assert.NotEmpty(viewModel.DelicatedSongList);
        }
    }
}

