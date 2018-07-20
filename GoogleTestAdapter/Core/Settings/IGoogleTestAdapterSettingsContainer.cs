using System.Collections.Generic;

namespace GoogleTestAdapter.Settings
{
    public interface IGoogleTestAdapterSettingsContainer
    {
        RunSettings SolutionSettings { get; }
        List<RunSettings> ProjectSettings { get; }

        RunSettings GetSettingsForExecutable(string executable);
    }

}