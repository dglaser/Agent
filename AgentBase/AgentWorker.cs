//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using System.Diagnostics;

namespace Agent
{

  //============================================================================
  /// <summary>
  /// This is the .NET Agent Framework's base class for a worker.</summary>
  /// <remarks>
  /// The AgentWorker defines some properties of a worker and the abstract
  /// method Run that must be implemented by derived worker classes.</remarks>
  //----------------------------------------------------------------------------
  [Serializable]
  public abstract class AgentWorker
  {
    /// <summary>
    /// Use this TraceSwitch when tracing in derived classes.</summary>
    protected static TraceSwitch WorkerSwitch 
    {
      get { return _workerSwitch; }
    }
    
    /// <summary>
    /// Name of the worker.</summary>
    public string Name 
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Description of the worker.</summary>
    public string Description
    {
      get { return _description; }
      set { _description = value; }
    }

    /// <summary>
    /// The value to be used for the WorkerResult's state if
    /// an exception is encountered. Exceptions may be handled in
    /// the Run method, but uncaught exceptions may be handled
    /// elsewhere and will use this value.</summary>
    public const int STATE_EXCEPTION = int.MinValue;

    /// <summary>
    /// Message to be used when an exception is encountered.
    /// The Run method would string.Format to format this message
    /// and store it in the WorkerResult. The default message
    /// assumes that the parameters included in string.Format will
    /// be the current date/time and the exception's Message
    /// property. Exceptions may be handled in the Run method,
    /// but uncaught exceptions may be handled elsewhere and
    /// will use this value.</summary>
    public string MessageException
    {
      get { return _messageException; }
      set { _messageException = value; }
    }

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    private static TraceSwitch _workerSwitch = new TraceSwitch("AgentWorker", "AgentWorker TraceSwitch");
    private string _name;
    private string _description = null;
    private string _messageException = "Exception at {0:u}: {1}";

    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    /// <summary>
    /// Default the job's name to the name of the job's class.</summary>
    public AgentWorker() 
    {
      Name = this.GetType().Name;
    }

    /// <summary>
    /// This is where the worker performs its work and returns a WorkResult.</summary>
    public abstract WorkerResult Run();

  }//class


  //============================================================================
  /// <summary>
  /// This enumeration defines the result status of a worker.</summary>
  /// <remarks>
  /// This enumeration uses the FlagsAttribute and defines statuses
  /// for Ok, Warning, Critical, and Exception. Two additional values
  /// are combinations of the others - All and WarningAndCritical.</remarks>
  //----------------------------------------------------------------------------
  [Serializable]
	[Flags]
	public enum WorkerResultStatus 
	{
		Ok			            = 0x00000001,
		Warning		          = 0x00000010,
		Critical	          = 0x00000100,
		Exception	          = 0x10000000,
		All                 = Ok | Warning | Critical | Exception,
		WarningAndCritical  = Warning | Critical | Exception
	}


  //============================================================================
  /// <summary>
  /// This class defines the entire result of a worker.</summary>
  /// <remarks>
  /// The result includes the state, which is an arbitrary number defined
  /// by the worker class itself, the WorkerResultStatus, a short message,
  /// a detailed message, and a worker exception.</remarks>
  //----------------------------------------------------------------------------
  [Serializable]
	public class WorkerResult 
	{
    private int _state;
    private WorkerResultStatus _status;
    private string _shortMessage;
    private string _detailedMessage;
    private Exception _workerException;

    /// <summary>
    /// Worker-specific state number that should change if the state changes.</summary>
    /// <remarks>
    /// Notifier classes use a change in this number to indicate a change in
    /// state of the job for notification purposes.</remarks>
    public int State 
    {
      get { return _state; }
      set { _state = value; }
    }

    /// <summary>
    /// Result of running the worker.</summary>
    public WorkerResultStatus Status 
    {
      get { return _status; }
      set { _status = value; }
    }

    /// <summary>
    /// Short message to be delivered by a notifier.</summary>
    public string ShortMessage 
    {
      get { return _shortMessage; }
      set { _shortMessage = value; }
    }

    /// <summary>
    /// Detailed message to be delivered by a notifier.</summary>
    public string DetailedMessage 
    { 
      get { return _detailedMessage; } 
      set { _detailedMessage = value; } 
    }

    /// <summary>
    /// Exception encountered by the worker.</summary>
    public Exception WorkerException 
    {
      get { return _workerException; }
      set { _workerException = value; }
    }


    /// <summary>
    /// Constructor to intialize with minimal values.</summary>
    public WorkerResult(int state, WorkerResultStatus status) 
		{
			_state = state;
			_status = status;
		}

    /// <summary>
    /// Constructor to intialize with values including a short message.</summary>
    public WorkerResult(int state, WorkerResultStatus status, string shortMessage) 
		{
			_state = state;
			_status = status;
			_shortMessage = shortMessage;
		}

    /// <summary>
    /// Constructor to intialize with values including short and detailed messages.</summary>
    public WorkerResult(int state, WorkerResultStatus status, string shortMessage, string detailedMessage) 
		{
			_state = state;
			_status = status;
			_shortMessage = shortMessage;
			_detailedMessage = detailedMessage;
		}

    /// <summary>
    /// Constructor to intialize with values when there is an exception.</summary>
    public WorkerResult(int state, WorkerResultStatus status, string shortMessage, string detailedMessage, Exception workerException) 
    {
      _state = state;
      _status = status;
      _shortMessage = shortMessage;
      _detailedMessage = detailedMessage;
      _workerException = workerException;
    }
  }//class

}//namespace
