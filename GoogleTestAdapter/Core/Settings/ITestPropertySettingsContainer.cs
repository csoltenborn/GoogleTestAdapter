
namespace GoogleTestAdapter.Settings
{
    public interface ITestPropertySettingsContainer
    {
        bool TryGetSettings(string key, out ITestPropertySettings settings);
    }
}
