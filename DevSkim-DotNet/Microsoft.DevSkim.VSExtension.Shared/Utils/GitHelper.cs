using System.IO;
using System.Linq;

namespace Microsoft.DevSkim.VSExtension.Utils
{
    internal class GitHelper
    {
        public GitHelper()
        {
            if (File.Exists(communityPath))
            {
                pathToGit = communityPath;
            }
            else if (File.Exists(professionalPath))
            {
                pathToGit = professionalPath;
            }
            else if (File.Exists(enterprisePath))
            {
                pathToGit = enterprisePath;
            }
        }

        public bool GitAvailable { get { return !string.IsNullOrEmpty(pathToGit); } }

        public bool IsIgnored(string path)
        {
            if (GitAvailable)
            {
                ExternalCommandRunner.RunExternalCommand(pathToGit, out string stdOut, out string _, "check-ignore", path);
                if (path != null && stdOut.Contains(path.Split(Path.DirectorySeparatorChar).Last()))
                {
                    return true;
                }
            }
            return false;
        }

        private const string communityPath = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Community\\Common7\\IDE\\CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer\\Git\\mingw32\\bin\\git.exe";
        private const string enterprisePath = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\Common7\\IDE\\CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer\\Git\\mingw32\\bin\\git.exe";
        private const string professionalPath = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Professional\\Common7\\IDE\\CommonExtensions\\Microsoft\\TeamFoundation\\Team Explorer\\Git\\mingw32\\bin\\git.exe";
        private string pathToGit = string.Empty;
    }
}