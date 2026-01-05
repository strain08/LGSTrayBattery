using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace LGSTrayUI
{
    internal class Program
    {
        [STAThread]
        static void Main()
        {
            // TODO Whatever you want to do before starting
            // the WPF application and loading all WPF dlls
            try
            {
                RunApp();
            }
            catch (Exception e)
            {
                long unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();

                using StreamWriter writer = new($"./main_crashlog_{unixTime}.log", false);
                writer.WriteLine(e.ToString());
            }
        }

        // Ensure the method is not inlined, so you don't
        // need to load any WPF dll in the Main method
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void RunApp()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
