// Copyright (c) Microsoft. All rights reserved.

namespace Client.Tests.Helpers;

public abstract class BaseTestCase : IDisposable, IAsyncDisposable
{
    protected readonly ITestOutputHelper Console;
    private TestOutputTextWriter _writer;

    protected BaseTestCase(ITestOutputHelper console)
    {
        this.Console = console;
        this._writer = new TestOutputTextWriter(console);
        System.Console.SetOut(this._writer);
    }

    protected void Log(string message)
    {
        this.Console.WriteLine(message);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this._writer == null)
        {
            return;
        }

        if (disposing)
        {
            try { this._writer.Dispose(); }
            catch (NullReferenceException) { }

            this._writer = null!;
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (this._writer != null)
        {
            try { await this._writer.DisposeAsync().ConfigureAwait(false); }
            catch (NullReferenceException) { }

            this._writer = null!;
        }

        GC.SuppressFinalize(this);
    }
}
