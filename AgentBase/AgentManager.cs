//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using Agent;

namespace Agent
{

  //============================================================================
  /// <summary>
  /// This is the .NET Agent Framework's base class for a manager.</summary>
  /// <remarks>
  /// The AgentManager has a collection of jobs and checks the jobs'
  /// schedules at regular intervals. The Start and Stop methods are
  /// used to control the manager, and the Init method may also be
  /// called directly. Classes that inherit from this class may override
  /// OnInit, OnStart, and OnStop to implement custom code.</remarks>
  //----------------------------------------------------------------------------
  [Serializable]
  public class AgentManager
  {
    /// <summary>
    /// Use this TraceSwitch when tracing in derived classes.</summary>
    protected static TraceSwitch ManagerSwitch 
    {
      get { return _managerSwitch; }
    }

    /// <summary>
    /// True when the manager has started.</summary>
    public bool IsRunning 
    {
      get { return _isRunning; }
    }

    /// <summary>
    /// Determine the interval at which the job schedules are checked.</summary>
    public TimeSpan HeartbeatFrequency 
    {
      get { return TimeSpan.FromMilliseconds(_timer.Interval); }
      set { _timer.Interval = value.TotalMilliseconds; }
    }

    /// <summary>
    /// Returns the collection of jobs under management.</summary>
    public AgentJobCollection Jobs 
    {
      get { return _jobs; }
    }

    /// <summary>
    /// Returns the collection of globally ignorable exceptions.</summary>
    public IgnorableExceptionCollection IgnorableExceptions
    {
      get { return _ignorableExceptions; }
    }

    /// <summary>
    /// Determines whether or not the GC should be manually invoked after a job completes.</summary>
    public bool CollectGarbage
    {
      get { return _collectGarbage; }
      set { _collectGarbage = value; }
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    // for logging & debugging
    private static TraceSwitch _managerSwitch = new TraceSwitch("AgentManager", "AgentManager TraceSwitch");
  
    // current state of the manager
    private bool _isInitialized = false;
    private bool _isRunning = false;

    // timer used to fire events to check the schedules
    private System.Timers.Timer _timer = new System.Timers.Timer();
		
    // The ManualResetEvent allows threads to communicate with each other.
    // In this case, I use it to signal that the Manager's Stop method
    // has been called so that the Timer's Elapsed event know whether or
    // not to do anything.
    private ManualResetEvent _stopSignal = new ManualResetEvent(false);

    // collection of Jobs loaded into the manager
    private AgentJobCollection _jobs = new AgentJobCollection();

    // collection of Exceptions (global; not job-level) loaded into the manager
    private IgnorableExceptionCollection _ignorableExceptions = new IgnorableExceptionCollection();

    // set/clear whether or not to collect garbage
    private bool _collectGarbage = true;

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Intializes the timer used by the manager.</summary>
    public AgentManager() 
    {
      // initialize the Timer control
      _timer = new System.Timers.Timer();
      _timer.Elapsed += new ElapsedEventHandler( Timer_Elapsed );
      _timer.AutoReset = true;
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Intializes the manager, including calling OnInit.</summary>
    public void Init() 
		{
      // Make sure we have not already initialized
      if ( _isInitialized )
        throw(new InvalidOperationException("Already initialized."));

      // logging
      Trace.WriteLineIf(_managerSwitch.TraceInfo,
        string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tManager\t{2}",
        DateTime.Now, AppDomain.GetCurrentThreadId(), "Init()") );

      // derived classes may override OnInit() with their own initialization code
			OnInit();

      // Initialize jobs
      foreach (AgentJob job in _jobs)
        job.Init(this);

      // record that Init() has been called
			_isInitialized = true;
		}

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Starts the manager, including calling OnStart.</summary>
    public void Start() 
		{
      // Make sure we have not already started
			if ( _isRunning ) 
				throw(new InvalidOperationException("Already started."));

      // logging
      Trace.WriteLineIf(_managerSwitch.TraceInfo,
        string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tManager\t{2}",
        DateTime.Now, AppDomain.GetCurrentThreadId(), "Start()"));

      // initialize, if not already done
      if ( !_isInitialized )
				Init();

      // hook for derived classes to add custom Start code
			OnStart();

      // note status change
			_isRunning = true;

      // reset Stop events
			_stopSignal.Reset();

      // start the heartbeat timer
      _timer.Interval = HeartbeatFrequency.TotalMilliseconds;
      _timer.Start();

      // logging
      Trace.WriteLineIf(_managerSwitch.TraceInfo,
        string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tManager\t{2}",
        DateTime.Now, AppDomain.GetCurrentThreadId(), "STARTED"));
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Stops the manager, including calling OnStop.</summary>
    public void Stop() 
		{
      Trace.WriteLineIf(_managerSwitch.TraceInfo,
        string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tManager\t{2}",
        DateTime.Now, AppDomain.GetCurrentThreadId(), "Stop()"));

      if ( !_isRunning ) 
        throw(new InvalidOperationException("Not started."));

      _stopSignal.Set();
      _timer.Stop();

      _isRunning = false;

      // hook for derived classes to add custom Stop code
      OnStop();
			
      Trace.WriteLineIf(_managerSwitch.TraceInfo,
        string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tManager\t{2}",
        DateTime.Now, AppDomain.GetCurrentThreadId(), "STOPPED"));
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Repeatedly called to check the jobs' schedules.</summary>
    protected void Timer_Elapsed(object sender, ElapsedEventArgs args) 
		{
      // The Timer is multi-threaded and the Elapsed event might might be called
      // even after the Stop method has been called. 
      if ( !_stopSignal.WaitOne(0, true) ) 
      {
        Trace.WriteLineIf(_managerSwitch.TraceVerbose,
          string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tManager\t{2}",
          DateTime.Now, AppDomain.GetCurrentThreadId(), "HEARTBEAT-IN"));

        bool anyJobsScheduled = false;
        try 
        {
          foreach(AgentJob job in _jobs) 
            if ( job.CheckSchedules() )
              anyJobsScheduled = true;

          if ( anyJobsScheduled && CollectGarbage )
          {
            GC.Collect();
            GC.WaitForPendingFinalizers(); 
          }
        } 
        catch (Exception e) 
        {
          Trace.WriteLineIf(_managerSwitch.TraceVerbose,
            string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tManager\t{2}[{3}]",
            DateTime.Now, AppDomain.GetCurrentThreadId(), "HEARTBEAT-EXC", e));
        }

        Trace.WriteLineIf(_managerSwitch.TraceVerbose,
          string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tManager\t{2}[{3}]",
          DateTime.Now, AppDomain.GetCurrentThreadId(), "HEARTBEAT-OUT", anyJobsScheduled));
      }
		}

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Provides a hook implement custom initialization code.</summary>
    protected virtual void OnInit() { }
		
    /// <summary>
    /// Provides a hook implement custom start code.</summary>
    protected virtual void OnStart() { }
		
    /// <summary>
    /// Provides a hook implement custom stop code.</summary>
    protected virtual void OnStop() { }

  }//class

}//namespace
