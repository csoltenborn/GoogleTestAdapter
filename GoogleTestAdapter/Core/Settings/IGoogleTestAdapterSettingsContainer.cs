using System.Collections.Generic;

namespace GoogleTestAdapter.Settings
{
    public interface IGoogleTestAdapterSettingsContainer
    {
        RunSettings SolutionSettings { get; set; }
        List<RunSettings> ProjectSettings { get; set; }

        RunSettings GetSettingsForExecutable(string executable);
    }

}