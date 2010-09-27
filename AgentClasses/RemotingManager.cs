using System;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Data;
using Agent;

namespace Agent
{
  public class RemotingManager : AgentManager
  {
    public RemotingManager() : base()
    {
      // remoting configuration in order to allow other programs to retrieve status
      RemotingConfiguration.Configure(AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".config");
    }

    protected override void OnStart() 
    {
      base.OnStart();

      // for remoting
      ManagerDataSetReportGenerator.Manager = this;
    }

    protected override void OnStop() 
    {
      base.OnStop();

      // for remoting
      ManagerDataSetReportGenerator.Manager = null;
    }

  }//class


  public class ManagerDataSetReportGenerator : MarshalByRefObject, IObjectHandle 
  {
    public static AgentManager Manager = null;

    protected DataSet GenerateReportDataSet() 
    {
      DataSet ds = new DataSet("Manager Report");
      DataTable t = ds.Tables.Add("Jobs");
      t.Columns.Add("Name");
      t.Columns.Add("Status");

      if ( Manager != null )
        foreach( AgentJob job in Manager.Jobs ) 
        {
          t.Rows.Add( new object []
          {
            job.Name,
            (job.LastWorkerResult == null) ? DBNull.Value : (object) job.LastWorkerResult.DetailedMessage,
          } );
        }

      return ds;
    }

    public object Unwrap() 
    {
      System.IO.StringWriter sw = new System.IO.StringWriter();
      GenerateReportDataSet().WriteXml(sw, XmlWriteMode.WriteSchema);
      sw.Flush();
      return sw.ToString();
    }

  }//class

}//namespace
