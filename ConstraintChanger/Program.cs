#region License and Terms
// Unconstrained Melody
// Copyright (c) 2009-2011 Jonathan Skeet. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConstraintChanger
{
    internal static class Program
    {
        private const string InputAssembly = "UnconstrainedMelody.Original.dll";
        private const string OutputAssembly = "UnconstrainedMelody.dll";

        private static int Main()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Console.WriteLine("Transforming UnconstrainedMelody...");

            var ildasmExe = Path.Combine(basePath, "ildasm.exe");
            var ilasmExe = Path.Combine(basePath, "ilasm.exe");

            if (!File.Exists(ildasmExe))
            {
                Console.WriteLine("  ERROR: Can't find ildasm.exe at {0}", ildasmExe);
                return 1;
            }
            if (!File.Exists(ilasmExe))
            {
                Console.WriteLine("  ERROR: Can't find ilasm.exe at {0}", ilasmExe);
                return 1;
            }

            Console.WriteLine("Using: ");
            Console.WriteLine("  ildasm.exe: " + ildasmExe);
            Console.WriteLine("  ilasm.exe: " + ilasmExe);

            try
            {
                string ilFile = Decompile(ildasmExe);
                ChangeConstraints(ilFile);
                Recompile(ilFile, ilasmExe);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}\r\n{1}", e.Message, e.StackTrace);
                return 1;
            }
            return 0;
        }

        private static string Decompile(string ildasmExe)
        {
            string ilFile = Path.GetTempFileName();
            Console.WriteLine("Decompiling to {0}", ilFile);
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = ildasmExe,
                Arguments = "\"/OUT=" + ilFile + "\" " + InputAssembly
            });
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("ildasm failed");
            }
            return ilFile;
        }

        private static void Recompile(string ilFile, string ilasmExe)
        {
            string resFile = Path.ChangeExtension(ilFile, ".res");

            string output = Path.Combine(Directory.GetCurrentDirectory(), OutputAssembly);
            Console.WriteLine("Recompiling {0} to {1}", ilFile, output);
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = ilasmExe,
                Arguments = "/OUTPUT=" + output + " /DLL " + "\"" + ilFile + "\" /RESOURCE=\"" + resFile + "\""
            });
            process.WaitForExit();
        }

        private static void ChangeConstraints(string ilFile)
        {
            string[] lines = File.ReadAllLines(ilFile);
            lines = lines.Select<string, string>(ChangeLine).ToArray();
            File.WriteAllLines(ilFile, lines);
        }

        private static string ChangeLine(string line)
        {
            // Surely this is too simple to actually work...
            return line.Replace("(UnconstrainedMelody.DelegateConstraint)", "([mscorlib]System.Delegate)")
                       .Replace("([mscorlib]System.ValueType, UnconstrainedMelody.IEnumConstraint)", "([mscorlib]System.Enum)")
                       // Roslyn puts the constrains in the opposite order...
                       .Replace("(UnconstrainedMelody.IEnumConstraint, [mscorlib]System.ValueType)", "([mscorlib]System.Enum)");
        }
    }
}
