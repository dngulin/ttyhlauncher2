namespace TtyhLauncher.GTK {
    internal static class Program {
        private static void Main() {
            var application = new LauncherAppGtk("ru.ttyh.launcher2");
            application.Run();
        }
    }
}