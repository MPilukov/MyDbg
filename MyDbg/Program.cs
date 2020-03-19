using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyDbg
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.Write("Введите PID процесса : ");
            //var pidStr = Console.ReadLine();

            //if (!int.TryParse(pidStr, out var pid))
            //{
            //    Console.WriteLine("Не валидный PID.");
            //}

            var startProcess = new ProcessStartInfo(
                "C:/Repos/ModulBank-CRM/Web Services/CrmIntegrations/AsyncService/AsyncService/bin/Ass/Modulbank.Crm.AsyncService.exe");
            startProcess.CreateNoWindow = true;
            //startProcess.WindowStyle = ProcessWindowStyle.Hidden;

            var targetProcess = Process.Start(startProcess);
            var pid = targetProcess.Id;

            uint timeout = 10000;

            try
            {
                using (var target = DataTarget.AttachToProcess(pid, timeout, AttachFlag.Invasive))
                {
                    PrintDumpInfo(target, Console.WriteLine);
                }

                targetProcess.Kill();
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Не удалось прицепиться к файлу : {exc.ToString()}.");
            }

            Console.WriteLine("Введите любое значение для выхода.");
            Console.ReadLine();
        }

        private static void PrintDumpInfo(DataTarget target, Action<string> trace)
        {
            trace($"Pointer size : {target.PointerSize}");
            trace($"Architecture: {target.Architecture}");

            trace($"ClrVersions:");

            foreach (var clr in target.ClrVersions)
            {
                trace($"{clr.Version}");

                var runTime = clr.CreateRuntime();

                var heap = runTime.Heap;
                var types = new Dictionary<string, int>();

                foreach (var obj in heap.EnumerateObjects())
                {
                    var gen = heap.GetGeneration(obj);

                    var typeName = GetTypeName(obj);                    

                    if (types.ContainsKey(typeName))
                    {
                        types[typeName] = types[typeName] + 1;
                    }
                    else
                    {
                        types.Add(typeName, 1);
                    }
                }

                foreach (var typeData in types.OrderByDescending(x => x.Value).Where(x => x.Value > 100))
                {
                    trace($"Type : {typeData.Key} ({typeData.Value})");
                }

                //var modules = runTime.Modules;
                //foreach (var module in modules)
                //{
                //    foreach (var method in module.EnumerateTypes())
                //    {
                //        trace($"Method : {method.Name}");
                //    }
                //}
            }
        }

        private static string GetTypeName(ClrObject obj)
        {
            var typeName = obj.Type.Name.ToString();

            if (typeName.Contains("System.RuntimeType"))
            {
                var baseType = obj.Type.BaseType.ToString();
                if (baseType.Contains("Microsoft.Diagnostics.Runtime"))
                {
                    return "Microsoft.Diagnostics.Runtime";
                }
                else if (baseType.Contains("System.Reflection"))
                {
                    return "System.Reflection";
                }
                else if (baseType.Contains("System.Object"))
                {
                    return "System.Object";
                }
                else if (baseType.Contains("System.Array"))
                {
                    return "System.Array";
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return typeName;
            }
        }
    }
}
