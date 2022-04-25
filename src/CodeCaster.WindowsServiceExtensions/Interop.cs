using System;
using System.Runtime.InteropServices;

namespace CodeCaster.WindowsServiceExtensions
{
    internal static class Interop
    {
        internal static class Advapi32
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct ServiceStatus
            {
                internal int serviceType;
                internal int currentState;
                internal int controlsAccepted;
                internal int win32ExitCode;
                internal int serviceSpecificExitCode;
                internal int checkPoint;
                internal int waitHint;
            }

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
        }
    }
}
