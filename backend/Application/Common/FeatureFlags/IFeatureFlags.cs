namespace Application.Common.FeatureFlags;

public interface IFeatureFlags
{
    bool IsEnabled(string featureName);
}