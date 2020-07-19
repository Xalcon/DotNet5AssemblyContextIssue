using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace DotnetAssemblyUnloading
{
    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExecuteAndUnload(string assemblyPath, out WeakReference alcWeakRef)
        {
            var alc = new CollectableAssemblyLoadContext();
            Assembly a = alc.LoadFromAssemblyPath(assemblyPath);

            alcWeakRef = new WeakReference(alc, trackResurrection: true);

            var args = new object[1] { new string[] { "Hello" } };
            var method = a.GetTypes().First(t => t.Name == "EntryPoint").GetMethod("Main");
            int result = (int)method.Invoke(null, args);

            alc.Unload();

            return result;
        }

        static void Main(string[] args)
        {
            Console.WriteLine(RuntimeInformation.FrameworkDescription);

            WeakReference testAlcWeakRef;
            int result = ExecuteAndUnload(new FileInfo("PluginAssembly.dll").FullName, out testAlcWeakRef);
            for (int i = 0; testAlcWeakRef.IsAlive && (i < 10); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if(testAlcWeakRef.IsAlive)
            {
                Console.WriteLine("Unable to unload assembly context");
                return;
            }

            Console.WriteLine("Successully unloaded assembly context");

            Thread.Sleep(1000);

            File.Delete("PluginAssembly.dll");
        }
    }
}
