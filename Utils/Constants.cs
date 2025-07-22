namespace FASTBueno.Utilities
{
    public static class Constants
    {
        private static string fastboot_exe = "fastboot.exe";

        // Fastboot executable path
        public static string FAST_EXE { get => fastboot_exe; }

        // General constants
        public const int SECOND = 1000;

        // Fastboot magics
        public const string FAST_OKAY = "OKAY";
        public const string FAST_COMPLETED = "finished. total time:";
        public const string FAST_GETVAR_HEADER = "(bootloader) ";
        public const string FAST_ERR = "FAILED (remote: ";
        public const string FAST_ERR_UNKNOWN_COMMAND = "fastboot: usage: unknown command";
    }
}
