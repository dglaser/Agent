//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using System.Runtime.Remoting;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using Agent;

namespace Agent
{
  public class AgentService : System.ServiceProcess.ServiceBase
  {
    private AgentManager _manager;
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.Container components = null;

    public AgentService()
    {
      // This call is required by the Windows.Forms Component Designer.
      InitializeComponent();

      // TODO: Add any initialization after the InitComponent call
    }

    // The main entry point for the process
    static void Main()
    {
      System.ServiceProcess.ServiceBase[] ServicesToRun;
	
      // More than one user Service may run within the same process. To add
      // another service to this process, change the following line to
      // create a second service object. For example,
      //
      //   ServicesToRun = New System.ServiceProcess.ServiceBase[] {new Service1(), new MySecondUserService()};
      //
      ServicesToRun = new System.ServiceProcess.ServiceBase[] { new AgentService() };

      System.ServiceProcess.ServiceBase.Run(ServicesToRun);
    }

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      components = new System.ComponentModel.Container();
      this.ServiceName = "Agent";
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose( bool disposing )
    {
      if( disposing )
      {
        if (components != null) 
        {
          components.Dispose();
        }
      }
      base.Dispose( disposing );
    }

    /// <summary>
    /// Set things in motion so your service can do its work.
    /// </summary>
    protected override void OnStart(string[] args)
    {
      if ( _manager == null ) 
      {
        DynamicXmlObjectLoader loader = new DynamicXmlObjectLoader();
        _manager = (AgentManager) loader.Load(@"jobs.config"); // for a service, e.g. C:\WINDOWS\SYSTEM32\jobs.config
      }
      _manager.Start();
    }
 
    /// <summary>
    /// Stop this service.
    /// </summary>
    protected override void OnStop()
    {
      // TODO: Add code here to perform any tear-down necessary to stop your service.
      _manager.Stop();
    }
  }
}