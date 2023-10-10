using System.Diagnostics;

namespace Application.Observability;

internal static class TraceContext
{
    internal static Activity StartActivity(string name)
    {
        return CreateActivity(name).Start();
    }

    private static Activity CreateActivity(string name)
    {
        if (Activity.Current?.ParentId != null)
        {
            return new Activity(name)
                .SetParentId(Activity.Current.ParentId);
        }

        return new Activity(name);
    }
}
