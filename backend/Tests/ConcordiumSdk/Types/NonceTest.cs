using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.Types;

public class NonceTest
{
    [Fact]
    public void Increment()
    {
        var target = new Nonce(10);
        var result = target.Increment();
        Assert.Equal(new Nonce(11), result);
        Assert.Equal(new Nonce(10), target); // Ensure target is not modified
    }
}