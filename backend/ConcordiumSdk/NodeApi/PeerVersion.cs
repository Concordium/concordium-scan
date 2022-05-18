namespace ConcordiumSdk.NodeApi;

public record PeerVersion(int Major, int Minor, int Revision)
{
    public static PeerVersion Parse(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        
        var split = value.Split(".");
        if (split.Length != 3) throw new ArgumentException("Argument did not match format for a peer version.");
        if(!int.TryParse(split[0], out var major)) throw new ArgumentException("Argument did not match format for a peer version.");
        if(!int.TryParse(split[1], out var minor)) throw new ArgumentException("Argument did not match format for a peer version.");
        if(!int.TryParse(split[2], out var revision)) throw new ArgumentException("Argument did not match format for a peer version.");
        return new PeerVersion(major, minor, revision);
    }
};