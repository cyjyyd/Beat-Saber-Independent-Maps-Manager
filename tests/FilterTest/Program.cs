using BeatSaberIndependentMapsManager.Tests;

// Run comprehensive filter tests
var tests = new LocalCacheFilterTests(args.Length > 0 ? args[0] : "");
tests.RunAllTests();