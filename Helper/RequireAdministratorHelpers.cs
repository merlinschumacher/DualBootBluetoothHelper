using System.Runtime.InteropServices;
using System.Security.Principal;

namespace DualBootBluetoothHelper.Helper
{
    /// <summary>Checks if the current user has administrative priviledges</summary>
    internal static class RequireAdministratorHelper
    {
        [DllImport("libc")]
        public static extern uint getuid();

        public static void RequireAdministrator()
        {
            string name = System.AppDomain.CurrentDomain.FriendlyName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    if (!principal.IsInRole(WindowsBuiltInRole.Administrator) || !identity.IsSystem)
                    {
                        throw new InvalidOperationException($"Application must be run as administrator. Right click the {name} file and select 'run as administrator'.");
                    }
                }
            }
            else if (getuid() != 0)
            {
                throw new InvalidOperationException($"Application must be run as root/sudo. From terminal, run the executable as 'sudo {name}'");
            }
        }
    }
}