// Copyright (c) Microsoft. All rights reserved.

using System.Text;

namespace Client.Tests.Helpers;

public sealed class TestOutputTextWriter : TextWriter
{
    private readonly ITestOutputHelper _output;

    private readonly StringBuilder _buffer = new();

    public TestOutputTextWriter(ITestOutputHelper output)
    {
        this._output = output;
        this._buffer = new();
    }

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char value)
    {
        if (value == '\n')
        {
            this._output.WriteLine(this._buffer.ToString());
            this._buffer.Clear();
        }
        else
        {
            this._buffer.Append(value);
        }
    }

    public override void WriteLine(string? value)
    {
        if (this._buffer.Length > 0)
        {
            this._output.WriteLine(this._buffer.ToString());
            this._buffer.Clear();
        }

        this._output.WriteLine(value ?? string.Empty);
    }

    public override void Flush()
    {
        if (this._buffer.Length > 0)
        {
            this._output.WriteLine(this._buffer.ToString());
            this._buffer.Clear();
        }
    }

    public override void Close()
    {
        if (this._buffer.Length > 0)
        {
            this._output.WriteLine(this._buffer.ToString());
            this._buffer.Clear();
        }
    }

    public new void Dispose()
    {
        if (this._buffer.Length > 0)
        {
            this._output.WriteLine(this._buffer.ToString());
            this._buffer.Clear();
        }
    }
}
