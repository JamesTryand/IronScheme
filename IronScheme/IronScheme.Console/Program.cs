#region License
/* Copyright (c) 2007,2008,2009,2010,2011 Llewellyn Pritchard 
 * All rights reserved.
 * This source code is subject to terms and conditions of the BSD License.
 * See docs/license.txt. */
#endregion

using System;
using System.Diagnostics;
using System.Text;
using IronScheme.Hosting;
using IronScheme.Remoting.Server;
using System.IO;

namespace IronScheme.Runtime
{
  class Program
  {
    static string[] PrefixArgs(string[] args, params string[] extra)
    {
      var newargs = new string[args.Length + extra.Length];
      Array.Copy(extra, newargs, extra.Length);
      Array.Copy(args, 0, newargs, extra.Length, args.Length);
      return newargs;
    }

    static int Main(string[] args)
    {
      var exename = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

      switch (exename)
      {
        case "isc":
        case "isc32":
          args = PrefixArgs(args, "-nologo");
          break;
      }

      if (args.Length > 0 && args[0] == "-profile")
      {
        const string PROFILER_GUID = "{9E2B38F2-7355-4C61-A54F-434B7AC266C0}";

        if (ExecuteRegsvr32(false, true))
        {
          ProcessStartInfo psi;

          // create a process executor
          psi = new ProcessStartInfo(typeof(Program).Assembly.Location);

          // ----- SET THE CLR ENVIRONMENT VARIABLES -------------------

          // set the COR_PROFILER env var. This indicates to the CLR which COM object to
          // instantiate for profiling.
          if (psi.EnvironmentVariables.ContainsKey("COR_PROFILER") == true)
            psi.EnvironmentVariables["COR_PROFILER"] = PROFILER_GUID;
          else
            psi.EnvironmentVariables.Add("COR_PROFILER", PROFILER_GUID);

          ////// set the Cor_Enable_Profiling env var. This indicates to the CLR whether or
          ////// not we are using the profiler at all.  1 = yes, 0 = no.
          if (psi.EnvironmentVariables.ContainsKey("COR_ENABLE_PROFILING") == true)
            psi.EnvironmentVariables["COR_ENABLE_PROFILING"] = "1";
          else
            psi.EnvironmentVariables.Add("COR_ENABLE_PROFILING", "1");

          // ----- RUN THE PROCESS -------------------


          string[] argargs = new string[args.Length - 1];
          Array.Copy(args, 1, argargs, 0, argargs.Length);

          psi.Arguments = string.Join(" ", argargs);
          psi.UseShellExecute = false;

          try
          {
            Process p = Process.Start(psi);
            //p.PriorityBoostEnabled = true;

            p.WaitForExit();

            return p.ExitCode;
          }
          finally
          {
            ExecuteRegsvr32(false, false);
          }
        }
        else
        {
          // remove -profile
          string[] argargs = new string[args.Length - 1];
          Array.Copy(args, 1, argargs, 0, argargs.Length);

          args = argargs;
        }
      }

      if (Array.IndexOf(args, "--remoting-server") >= 0)
      {
        Host.Start();
        args = Array.FindAll(args, x => x != "--remoting-server");
      }
      //Encoding oi = Console.InputEncoding;
      Encoding oo = Console.OutputEncoding;

      //Debugger.Launch();

      try
      {
        //Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        return IronSchemeConsoleHost.Execute(args);
      }
      finally
      {
        Console.OutputEncoding = oo;
        //Console.InputEncoding = oi;
      }

    }

    // https://github.com/sawilde/opencover/blob/master/main/OpenCover.Framework/ProfilerRegistration.cs
    static bool ExecuteRegsvr32(bool userRegistration, bool register)
    {
      const string UserRegistrationString = "/n /i:user";

      var ppath = GetProfilerPath();

      if (!File.Exists(ppath))
      {
        if (register)
        {
          Console.WriteLine("WARNING: Profiler DLL ({0}) not available in application directory. Profiling will be disabled.", Path.GetFileName(ppath));
        }

        return false;
      }

      var startInfo = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "regsvr32.exe"),
                               string.Format("/s {2} {0} \"{1}\"", userRegistration ? UserRegistrationString : String.Empty,
                               ppath, register ? string.Empty : "/u")) { CreateNoWindow = true };

      var process = Process.Start(startInfo);
      process.WaitForExit();
      return true;
    }

    static string GetProfilerPath()
    {
      var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
      return Path.Combine(dir, string.Format("IronScheme.Profiler.x{0}.dll", IntPtr.Size == 8 ? "64" : "86"));
    }
  }
}
