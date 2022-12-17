namespace Application.Api.GraphQL.Network
{
    public class ClientVersionComparer : IComparer<string?>
    {
        public int Compare(string? x, string? y)
        {
            if (String.IsNullOrEmpty(x) && String.IsNullOrEmpty(y))
            {
                return 0;
            }
            else if (String.IsNullOrEmpty(x) && !String.IsNullOrEmpty(y))
            {
                return -1;
            }
            else if (!String.IsNullOrEmpty(x) && String.IsNullOrEmpty(y))
            {
                return 1;
            }
            else if (!String.IsNullOrEmpty(x) && !String.IsNullOrEmpty(y))
            {
                var xParts = x.Split(".");
                var yParts = y.Split(".");

                if (xParts.Length != yParts.Length)
                {
                    return 0;
                }

                for (int i = 0; i < xParts.Length; i++)
                {
                    var xPart = Int64.Parse(xParts[i]);
                    var yPart = Int64.Parse(yParts[i]);

                    if (xPart < yPart)
                    {
                        return -1;
                    }
                    else if (xPart > yPart)
                    {
                        return 1;
                    }
                }
            }

            return 0;
        }
    }
}