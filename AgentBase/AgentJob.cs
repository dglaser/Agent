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
  /// This is the .NET Agent Framework's base class for a job.</summary>
  /// <remarks>
  /// The AgentJob has a collection of schedulers, a collection of notifiers,
  /// and a worker.</remarks>
  //----------------------------------------------------------------------------
  [Serializable]
  public class AgentJob
  {
    /// <summary>
    /// Use this TraceSwitch when tracing in derived classes.</summary>
    protected static TraceSwitch JobSwitch
    {
      get { return _jobSwitch; }
    }

    /// <summary>
    /// The name of the job.</summary>
    public string Name 
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Collection of the job's schedulers.</summary>
    public AgentSchedulerCollection Schedulers 
    {
      get { return _schedulers; }
    }
    
    /// <summary>
    /// Collection of the job's notifiers.</summary>
    public AgentNotifierCollection Notifiers
    {
      get { return _notifiers; }
    }

    /// <summary>
    /// The job's worker.</summary>
    public AgentWorker Worker 
    {
      get { return _worker; }
      set { _worker = value; }
    }

    /// <summary>
    /// The previous result from running the worker.</summary>
    public WorkerResult LastWorkerResult 
    {
      get { return _lastWorkerResult; }
    }


    
    // related to ignoring exceptions:

    /// <summary>
    /// The job's ignorable exceptions.</summary>
    public IgnorableExceptionCollection IgnorableExceptions 
    {
      get { return _ignorableExceptions; }
    }

    /// <summary>
    /// The global ignorable exceptions (usually set by the manager).</summary>
    public IgnorableExceptionCollection GlobalIgnorableExceptions 
    {
      get { return _globalIgnorableExceptions; }
      set { _globalIgnorableExceptions = (value == null) ? new IgnorableExceptionCollection() : value; }
    }

    /// <summary>
    /// Maximum number of consecutive "ignorable exceptions" to be ignored.</summary>
    public int MaximumConsecutiveExceptionIgnoreCount
    {
      get { return _maximumConsecutiveExceptionIgnoreCount; }
      set { _maximumConsecutiveExceptionIgnoreCount = value; }
    }

    /// <summary>
    /// Number of milliseconds to wait before retrying after encountering an exception.</summary>
    public long ExceptionRetryDelayMilliseconds
    {
      get { return _exceptionRetryDelayMilliseconds; }
      set { _exceptionRetryDelayMilliseconds = value; }
    }
    
    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    // trace switch for logging
    private static TraceSwitch _jobSwitch = new TraceSwitch("AgentJob", "AgentJob TraceSwitch");

    // the name of the job
    private string _name;

    // collection of the job's schedulers
    private AgentSchedulerCollection _schedulers = new AgentSchedulerCollection();
    
    // collection of the job's notifiers
    private AgentNotifierCollection _notifiers = new AgentNotifierCollection();

    // the job's worker
    private AgentWorker _worker = null;

    // the previous result from running the worker
    private WorkerResult _lastWorkerResult = null;



    // related to ignoring exceptions:

    // maximum number of consecutive "ignorable exceptions" to be ignored
    private int _maximumConsecutiveExceptionIgnoreCount = 3;

    // number of milliseconds to wait before retrying after encountering an exception
    private long _exceptionRetryDelayMilliseconds = 20000;

    // the job's ignorable exceptions
    private IgnorableExceptionCollection _ignorableExceptions = new IgnorableExceptionCollection();

    // the global ignorable exceptions (usually in the manager)
    private IgnorableExceptionCollection _globalIgnorableExceptions = null;

    // current count of consecutive "ignorable exceptions" that have been encountered
    private int totalConsecutiveIgnorableExceptionsCount = 0;

    // count of consecutive "ignorable exceptions" encountered for most recent specific exception
    private int lastIgnorableExceptionCount = 0;

    // name of most recent specific exception
    private string lastIgnorableExceptionName = string.Empty;

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Default the job's name to the name of the job's class.</summary>
    public AgentJob()
		{
			Name = this.GetType().Name;
		}

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Initialize the job (call before the manager starts).</summary>
    public void Init(AgentManager manager)
    {
      // reference the manager's global ignorable exception list
      _globalIgnorableExceptions = manager.IgnorableExceptions;

      // Hook up Scheduler_OnScheduled to all Scheduler events.
      foreach(AgentScheduler scheduler in _schedulers)
        scheduler.Scheduled += new EventHandler(Scheduler_OnScheduled);
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Check's the job's schedules.</summary>
    public bool CheckSchedules() 
    {
      bool scheduled = false;
      foreach(AgentScheduler scheduler in _schedulers)
        if ( scheduler.CheckSchedule() )
          scheduled = true;
      return scheduled;
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// This method is is called when a scheduler determines that a job should be run.</summary>
    protected void Scheduler_OnScheduled(object sender, EventArgs args) 
    {
      // make sure the job really has a worker
      if ( _worker != null ) 
      {
        // this will store the worker's result
        WorkerResult result;

        // log the that the worker is running
        Trace.WriteLineIf(_jobSwitch.TraceVerbose,
          string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tJob\t{2}\t{3}",
          DateTime.Now, AppDomain.GetCurrentThreadId(), "Worker",
          _worker.Description ));

        try 
        {
          // run the worker!
          result = _worker.Run();
        } 
        catch (Exception e)
        {
          // if there was an exception trying to run the worker, then store that as the result
          result = new WorkerResult(AgentWorker.STATE_EXCEPTION, WorkerResultStatus.Exception, _worker.Description,
            string.Format(_worker.MessageException, DateTime.Now, e.Message),
            e );
        }

        // Check to see if the result is an ignorable exception and if so when the worker should be rescheduled
        long retryDelayMilliseconds = 0;
        bool ignoreException = CheckForIgnorableException(result, ref retryDelayMilliseconds);

        // log the worker result
        Trace.WriteLineIf(_jobSwitch.TraceInfo,
          string.Format("{0:yyyy-MM-dd HH:mm:ss.ffff}\t{1}\tJob\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
          DateTime.Now, AppDomain.GetCurrentThreadId(), "Worker",
          _worker.Description, result.Status,
          (result.WorkerException == null) ? null : result.WorkerException.GetType(),
          (result.WorkerException == null) ? string.Empty : result.WorkerException.Message,
          ignoreException ? "IGNORE" : "NOTIFY" ));

        // determine how to proceed
        if ( ignoreException ) 
        {
          // this was an exception that can be ignored, but we need to reschedule
          // the job with the scheduler that called us
          AgentScheduler scheduler = sender as AgentScheduler;
          if ( scheduler != null )
          {
            TimeSpan retryDelayTimeSpan = new TimeSpan( TimeSpan.TicksPerMillisecond * retryDelayMilliseconds );
            scheduler.RequestRescheduling( retryDelayTimeSpan );
          }
        }
        else
        {
          // this is a normal worker result that is not ignored, so let's request notification
          foreach(AgentNotifier notifier in _notifiers)
            notifier.RequestNotification(result);
        }

        // store this result as the previous result
        _lastWorkerResult = result;
      }//if
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Check's to see if the worker's result is an exception that can be ignored and the work rescheduled.</summary>
    protected virtual bool CheckForIgnorableException(WorkerResult result, ref long retryDelayMilliseconds) 
    {
      // assume the worst
      bool ignorableException = true;

      // was there an exception?
      if ( result.Status != WorkerResultStatus.Exception ) 
      {
        // no exception - therefore not an ignorable exception
        ignorableException = false;
      } 
      else 
      {
        // there was an exception - search through the job's ignorable exeptions for a match
        AgentIgnorableException exception = _ignorableExceptions.Find(result.WorkerException);

        // if there was no match, search through the global ignorable exceptions for a match
        if ( exception == null )
          exception = _globalIgnorableExceptions.Find(result.WorkerException);

        // was this an ignorable exception?
        if ( exception == null ) 
        {
          // if there was still no match, then it was not an ignorable exception
          ignorableException = false;
        }
        else
        {
          // IGNORABLE EXCEPTION

          // return the number of milliseconds to wait before re-trying after this exception
          retryDelayMilliseconds = exception.RetryDelayMilliseconds;

          // is this the same as the previous ignorable exception?
          if ( lastIgnorableExceptionName != exception.Name ) 
          {
            // it is not the same - reset the previous name to this one and the count to 0
            lastIgnorableExceptionName = exception.Name;
            lastIgnorableExceptionCount = 0;
          }

          // increment the consecutive specific ignorable exception count
          lastIgnorableExceptionCount++;
          // increment the consecutive total ignorable exception count
          totalConsecutiveIgnorableExceptionsCount++;
          
          // see if the consecutive counts exceeds the limits
          if ( lastIgnorableExceptionCount > exception.MaximumConsecutiveIgnoreCount ) 
          {
            // specific one does exceed the limit, so it is no longer ignorable
            ignorableException = false;
          } 
          else if ( totalConsecutiveIgnorableExceptionsCount > MaximumConsecutiveExceptionIgnoreCount ) 
          {
            // total count exceeds the limit, so it is no longer ignorable
            ignorableException = false;
          }
      
        } // exception is ignorable

      } // result is an exception

      // is this an ignorable exception?
      if ( !ignorableException ) 
      {
        // it is NOT an ignorable exeption so reset the counts and previous exception name
        totalConsecutiveIgnorableExceptionsCount = 0;
        lastIgnorableExceptionCount = 0;
        lastIgnorableExceptionName = string.Empty;
      }

      // return the final status of whether this was ignorable or not
      return ignorableException;
    }

  }//class


  //============================================================================
  /// <summary>
  /// This is the .NET Agent Framework's job collection.</summary>
  /// <remarks>
  /// AgentJobCollection derives from CollectionBase.</remarks>
  //----------------------------------------------------------------------------
  [Serializable]
	public class AgentJobCollection : CollectionBase 
	{
    /// <summary>
    /// Add a job to the collection.</summary>
    public void Add(AgentJob job) 
		{
			List.Add(job);
		}

    /// <summary>
    /// Remove a job from the collection.</summary>
    public void Remove(AgentJob job)
		{
			List.Remove(job);
		}

    /// <summary>
    /// Set or retrieve a job at the specific index in the collection.</summary>
    public AgentJob this[int index] 
		{
			get 
			{
				return (AgentJob) List[index];
			}
			set 
			{
				List[index] = value;
			}
		}
	}//class

}//namespace
