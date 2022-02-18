using System.ComponentModel;

namespace AddActionsWorkflow.Options
{
    internal partial class OptionsProvider
    {
        public class GeneralOptions : BaseOptionPage<General> { }
    }

    public class General : BaseOptionModel<General>
    {
        [Category("Generator")]
        [DisplayName("Default file name")]
        [Description("The base name of the workflow (.yaml) file to be generated")]
        [DefaultValue("build")]
        public string DefaultName { get; set; } = "build";

        [Category("Generator")]
        [DisplayName("Randomize file name")]
        [Description("If true, a suffix is added to the Default file name to avoid conflicts")]
        [DefaultValue(true)]
        public bool RandomizeFileName { get; set; } = true;

        [Category("Generator")]
        [DisplayName("Overwrite if exists")]
        [Description("If true, this will overwrite same-named workflow files if exists")]
        [DefaultValue(false)]
        public bool OverwriteExisting { get; set; } = false;

        [Category("Generator")]
        [DisplayName("Solution Folder")]
        [Description("The Solution Items folder to add these to in the Visual Studio solution")]
        [DefaultValue("Solution Items")]
        public string SolutionFolderName { get; set; } = "Solution Items";

        [Category("Generator")]
        [DisplayName("Current branch")]
        [Description("Will use the current branch name or 'main' if false")]
        [DefaultValue(true)]
        public bool UseCurrentBranchName { get; set; } = true;
    }
}
