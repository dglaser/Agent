//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using System.Threading;
using Agent;

namespace Agent.Test
{
  //============================================================================
  /// <summary>
  /// This is an example of an agent running from a Console application. This
  /// can also be used in conjunction with the monitor app for testing
  /// remoting.
  /// This is a good way to test your jobs.config file.</summary>
  //----------------------------------------------------------------------------
  class AgentTestApp
	{
		static void Main(string[] args)
		{
      Console.WriteLine("*** AGENT Console App ***");
      Console.WriteLine();
      Console.WriteLine("Press CTRL-C to end.");
      Console.WriteLine();

      System.Diagnostics.Trace.Listeners.Add( new System.Diagnostics.TextWriterTraceListener( Console.Out ) );

      Console.WriteLine("Loading Manager...");
			
      DynamicXmlObjectLoader loader = new DynamicXmlObjectLoader();
      AgentManager manager = (AgentManager) loader.Load(@"..\..\jobs.config");

      Console.WriteLine();
      
      manager.Start();
			Console.WriteLine("Manager started.");
      Console.WriteLine();

      Console.WriteLine("{0} jobs", manager.Jobs.Count);
      foreach(AgentJob job in manager.Jobs)
        Console.WriteLine("Job Loaded: {0}", job.Name);
      Console.WriteLine();
      
      for ( ; ; );
      
			//Thread.Sleep(new TimeSpan(0, 0, 10));

			Console.WriteLine("Stopping...");
			manager.Stop();
			//Console.WriteLine("Stopped. Press [ENTER]");
			//Console.ReadLine();
		}
	}
}
