namespace AppVolumeHotkeys;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                _ = new AudioSessionService().GetSessions();
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        ApplicationConfiguration.Initialize();
        var startMinimized = args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);
        Application.Run(new Form1(startMinimized));
        return 0;
    }    
}
