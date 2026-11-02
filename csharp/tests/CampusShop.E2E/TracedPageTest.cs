using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace CampusShop.E2E;

/// <summary>
/// 在 PageTest 基础上为用例录制 Playwright trace：失败时把 trace 保存到
/// traces 目录（CI 通过 E2E_TRACE_DIR 指到工作区），通过的用例不留文件。
/// </summary>
public abstract class TracedPageTest : PageTest
{
    private string? _tracePath;

    [SetUp]
    public async Task StartTracing()
    {
        var traceDir = Environment.GetEnvironmentVariable("E2E_TRACE_DIR")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "traces");
        Directory.CreateDirectory(traceDir);

        var testName = TestContext.CurrentContext.Test.Name;
        foreach (var invalid in Path.GetInvalidFileNameChars())
            testName = testName.Replace(invalid, '_');

        _tracePath = Path.Combine(traceDir, $"{testName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    [TearDown]
    public async Task StopTracing()
    {
        if (_tracePath is null) return;

        var failed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;
        await Context.Tracing.StopAsync(new TracingStopOptions
        {
            // Path 为 null 时丢弃 trace，只有失败用例才落盘。
            Path = failed ? _tracePath : null,
        });
    }
}
