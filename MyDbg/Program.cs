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
                var typeCounter = new Dictionary<string, int>();
                var types = new Dictionary<string, ObjectInfo>();

                foreach (var obj in heap.EnumerateObjects())
                {
                    var gen = heap.GetGeneration(obj);

                    var typeData = GetTypeData(obj);                    

                    if (typeCounter.ContainsKey(typeData.TypeName))
                    {
                        typeCounter[typeData.TypeName] = typeCounter[typeData.TypeName] + 1;                        
                    }
                    else
                    {
                        typeCounter.Add(typeData.TypeName, 1);
                        types.Add(typeData.TypeName, typeData);
                    }
                }

                foreach (var typeData in typeCounter.OrderByDescending(x => x.Value).Where(x => x.Value > 100))
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

        private static ObjectInfo SwithType(string typeName)
        {
            switch (typeName)
            {
                case "Free":
                    return new ObjectInfo
                    {
                        TypeName = typeName,
                        IsFree = true,
                    };
                case "System.Exception":
                    return new ObjectInfo
                    {
                        TypeName = typeName,
                    };
                case "System.SystemException":
                    return new ObjectInfo
                    {
                        TypeName = typeName,
                    };
                case "System.Object":
                    return new ObjectInfo
                    {
                        TypeName = typeName,
                    };
                case "System.AppDomain":
                    return new ObjectInfo
                    {
                        TypeName = typeName,
                    };
            }

            return null;
        }

        private static ObjectInfo GetTypeData(ClrObject obj)
        {
            var typeName = obj.Type.Name.ToString();

            var data = SwithType(typeName);
            if (data != null)
            {
                return data;
            }

            var baseType = obj.Type.BaseType?.ToString() ?? "";
            var baseData = SwithType(baseType);
            if (baseData != null)
            {
                return baseData;
            }

            if (typeName.Contains("System.RuntimeType"))
            {
                if (baseType.Contains("Microsoft.Diagnostics.Runtime"))
                {
                    return new ObjectInfo
                    {
                        TypeName = typeName, // "Microsoft.Diagnostics.Runtime";
                    };                    
                }
                else if (baseType.Contains("System.Reflection"))
                {
                    return new ObjectInfo
                    {
                        TypeName = typeName, // "System.Reflection";
                    };
                }
                else if (baseType.Contains("System.Object"))
                {
                    return new ObjectInfo
                    {
                        TypeName = typeName, 
                    };
                }
                else if (baseType.Contains("System.Array"))
                {
                    return new ObjectInfo
                    {
                        TypeName = typeName,
                    };
                }
                else if (baseType.Contains("ystem.Collections.Generic.EqualityComparer"))
                {
                    return new ObjectInfo
                    {
                        TypeName = typeName,
                    };
                }
                else if (baseType.Contains("System.ValueType"))
                {
                    return new ObjectInfo
                    {
                        TypeName = baseType,
                    };
                }
                else
                {
                    return new ObjectInfo
                    {
                        TypeName = baseType,
                    };
                }
            }
            else
            {
                return new ObjectInfo
                {
                    TypeName = typeName,
                };
            }
        }
    }
}
