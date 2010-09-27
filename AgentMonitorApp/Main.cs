//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using System.Data;
using System.Configuration;
using System.Runtime.Remoting;

namespace AgentMonitorApp
{
  //============================================================================
  /// <summary>
  /// This is an example of a remoting client that queries the agent for the
  /// status of the jobs that the agent is managing.</summary>
  //----------------------------------------------------------------------------
  class AgentMonitorApp
	{
    public static DataSet LoadReportDataSet()
    {
      string reportUrl = ConfigurationSettings.AppSettings["AgentRemotingManagerUrl"];
      IObjectHandle managerDataSetReportGenerator = (IObjectHandle) RemotingServices.Connect(typeof(IObjectHandle), reportUrl);
      string dataSetXml = (string) managerDataSetReportGenerator.Unwrap();
      DataSet ds = new DataSet();
      ds.ReadXml( new System.IO.StringReader(dataSetXml), XmlReadMode.Auto );
      return ds;
    }
    
    /// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
      // configure remoting so we can act as a client
      RemotingConfiguration.Configure(AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".config");

      Console.WriteLine("*** AGENT MONITOR ***");
      Console.WriteLine();
      Console.WriteLine("Press CTRL-C to end.");
      Console.WriteLine();

      // Query for job status from agent every 10 seconds
      for (; ; System.Threading.Thread.Sleep(10000) ) 
      {
        Console.WriteLine("JOB STATUS");
        Console.WriteLine("======================================================================");
        DataSet ds = LoadReportDataSet();
        foreach (DataRow row in ds.Tables[0].Rows) 
        {
          bool firstCol = true;
          foreach (DataColumn col in ds.Tables[0].Columns) 
          {
            Console.WriteLine( (firstCol ? "" : "  ") + row[col].ToString() );
            firstCol = false;
          }
          Console.WriteLine();
        }
        Console.WriteLine("======================================================================");
      }

    }
	}
}
