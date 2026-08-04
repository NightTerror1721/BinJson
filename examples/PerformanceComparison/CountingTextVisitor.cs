using System;
using Krampus.BinJson.Text;

internal sealed class CountingTextVisitor : BJsonTextVisitor
{
    public int ScalarCount { get; private set; }

    public override void OnNull() => ScalarCount++;
    public override void OnBoolean(bool value) => ScalarCount++;
    public override void OnSignedInteger(long value) => ScalarCount++;
    public override void OnUnsignedInteger(ulong value) => ScalarCount++;
    public override void OnFloat(double value) => ScalarCount++;
    public override void OnString(string value) => ScalarCount++;
}
