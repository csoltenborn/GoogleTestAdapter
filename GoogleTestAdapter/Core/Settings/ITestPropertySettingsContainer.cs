
namespace GoogleTestAdapter.Settings
{
    public interface ITestPropertySettingsContainer
    {
        bool TryGetSettings(string testName, out ITestPropertySettings settings);
    }
}
