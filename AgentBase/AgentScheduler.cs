//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using System.Collections;
using System.Diagnostics;

namespace Agent
{

  //============================================================================
  /// <summary>
  /// This is the .NET Agent Framework's base class for a scheduler.</summary>
  /// <remarks>
  /// The AgentScheduler defines a Scheduled event and a method for firing that
  /// event. It also provides abstract methods that must be implemented in
  /// derived classes.</remarks>
  //----------------------------------------------------------------------------
  [Serializable]
	public abstract class AgentScheduler
	{
    /// <summary>
    /// Use this TraceSwitch when tracing in derived classes.</summary>
    protected static TraceSwitch SchedulerSwitch
    {
      get { return _schedulerSwitch; }
    }

    // for logging and debugging
    private static TraceSwitch _schedulerSwitch = new TraceSwitch("AgentScheduler", "AgentScheduler TraceSwitch");
    
    /// <summary>
    /// This event will be fired when .</summary>
    public event EventHandler Scheduled;
		
    /// <summary>
    /// This method is called to check the schedule.
    /// True should be returned if the checking the schedule has
    /// resulted in firing the Scheduled event. Note that if checking
    /// the schedule determines that the Scheduled event should be
    /// fired, then the FireScheduled method should also be called.</summary>
    public abstract bool CheckSchedule();
    
    /// <summary>
    /// This method is called when the job wants to reschedule
    /// for a given period of time into the future. This is typically
    /// called when exceptions are being ignored and an attempt to
    /// retry the job will be made.</summary>
    public abstract void RequestRescheduling(TimeSpan when);

    /// <summary>
    /// This method is called to fire the Scheduled event.</summary>
    public void FireScheduled() 
		{
			Scheduled(this, new EventArgs());
		}
	}//class


  //============================================================================
  /// <summary>
  /// This is the .NET Agent Framework's scheduler collection.</summary>
  /// <remarks>
  /// AgentSchedulerCollection derives from CollectionBase.</remarks>
  //----------------------------------------------------------------------------
  [Serializable]
  public class AgentSchedulerCollection : CollectionBase 
  {
    /// <summary>
    /// Add a scheduler to the collection.</summary>
    public void Add(AgentScheduler scheduler) 
    {
      List.Add(scheduler);
    }

    /// <summary>
    /// Remove a scheduler from the collection.</summary>
    public void Remove(AgentScheduler scheduler)
    {
      List.Remove(scheduler);
    }

    /// <summary>
    /// Set or retrieve a scheduler at the specific index in the collection.</summary>
    public AgentScheduler this[int index] 
    {
      get 
      {
        return (AgentScheduler) List[index];
      }
      set 
      {
        List[index] = value;
      }
    }
  }//class

}//namespace
