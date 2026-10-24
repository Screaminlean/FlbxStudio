// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
using System.Reflection;

namespace FlbxStudio.Helpers
{
    /// <summary>
    /// Provides helper values and methods shared across the application.
    /// </summary>
    public static class HelperFunctions
    {
        /// <summary>
        /// The display name of the application.
        /// </summary>
        public static string AppName = "FlbxStudio";

        /// <summary>
        /// Gets the current assembly version string.
        /// </summary>
        /// <returns>The assembly version or an empty string when unavailable.</returns>
        public static string GetAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }
    }
}
