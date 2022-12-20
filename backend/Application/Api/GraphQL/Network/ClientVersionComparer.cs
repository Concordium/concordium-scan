namespace Application.Api.GraphQL.Network
{
    public class ClientVersionComparer : IComparer<string>
    {
        public int Compare(string? v1, string? v2)
        {
            var isV1 = v1 != null;
            var isV2 = v2 != null;

            if (!isV1 && !isV2) return 0;
            if (isV1 && !isV2) return -1;
            if (!isV1 && isV2) return 1;

            Semver.SemVersion? v1Semver = null, v2Semver = null;
            var isV1Semver = Semver.SemVersion.TryParse(v1, Semver.SemVersionStyles.Any, out v1Semver);
            var isV2Semver = Semver.SemVersion.TryParse(v2, Semver.SemVersionStyles.Any, out v2Semver);

            if (!isV1Semver && !isV2Semver) return v1!.CompareTo(v2!);
            if (isV1Semver && !isV2Semver) return -1;
            if (!isV1Semver && isV2Semver) return 1;

            return v1Semver.CompareSortOrderTo(v2Semver);
        }
    }
}