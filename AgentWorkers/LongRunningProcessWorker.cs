//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using System.IO;
using System.Management;
using System.Data;
using System.Data.OleDb;
using System.Net;
using Agent;

namespace Agent.Worker
{
  [Serializable]
  public class LongRunningProcessWorker : AgentWorker
  {
    public string ServerUNC = null;
    public TimeSpan WarningProcessAge = TimeSpan.Parse("00:03:00");
    public TimeSpan CriticalProcessAge = TimeSpan.Parse("00:08:00");
    public string ProcessName = "excel.exe";
    public string MessageWarningProcessAge = "Warning: Process {0} is {1} old @ {2:u}.";
    public string MessageCriticalProcessAge = "CRITICAL: Process {0} is {1} old @ {2:u}.";
    public string MessageOkNotFound = "OK: Process {0} not found @ {2:u}.";
    public string MessageOkFound = "OK: Process {0} is {1} old @ {2:u}.";

    const int STATE_OK = 0;
    const int STATE_WARNING_AGE = 1;
    const int STATE_CRITICAL_AGE = 2;

    // Convert DMTF date format to DateTime
    public static DateTime ToDateTime(string dmtf) 
    {
      int year = System.Int32.Parse(dmtf.Substring(0, 4));
      int month = System.Int32.Parse(dmtf.Substring(4, 2));
      int day = System.Int32.Parse(dmtf.Substring(6, 2));
      int hour = System.Int32.Parse(dmtf.Substring(8, 2));
      int minute = System.Int32.Parse(dmtf.Substring(10, 2));
      int second = System.Int32.Parse(dmtf.Substring(12, 2));
      int millisec = System.Int32.Parse(dmtf.Substring(15, 3));
      DateTime dateRet = new DateTime(year, month, day, hour, minute, second, millisec);
      return dateRet;
    }

    public WorkerResultStatus GetProcessAgeForServer(string strUNCMachine, string processName, out string message, out int state) 
    {
      WorkerResultStatus workerResultStatus = WorkerResultStatus.Ok;
      message = string.Empty;
      state = STATE_OK;

      try 
      {
        ConnectionOptions options = new ConnectionOptions();
        ManagementObjectSearcher objectSearcher;
        ObjectQuery objectQuery;
        ManagementObjectCollection queryResults;
        ManagementScope scope;

        //options.Username = @"DOMAIN\USERID";
        //options.Password = "PASSWORD";

        //Get rid of an extraneous trailing backslash if it exists
        strUNCMachine = strUNCMachine.TrimEnd(new char [] { '\\' } );

        scope = new ManagementScope(strUNCMachine + @"\root\cimv2", options);
        objectQuery = new ObjectQuery( string.Format("select CreationDate from Win32_Process where Name = '{0}'", processName) );
            
        using ( objectSearcher = new ManagementObjectSearcher(scope, objectQuery) ) 
        {
          using ( queryResults = objectSearcher.Get() ) 
          {
            foreach(ManagementObject moObject in queryResults) 
            {
              DateTime creationDate = LongRunningProcessWorker.ToDateTime( moObject["CreationDate"].ToString() );
              TimeSpan processAge = DateTime.Now - creationDate;

              processAge = new TimeSpan(processAge.Ticks - (processAge.Ticks % TimeSpan.TicksPerSecond)); // reduce to nearest second

              if ( processAge >= CriticalProcessAge ) 
              {
                workerResultStatus = WorkerResultStatus.Critical;
                state = STATE_CRITICAL_AGE;
                message += string.Format(MessageCriticalProcessAge, processName, processAge, DateTime.Now) + '\n';
              }
              else if ( processAge >= WarningProcessAge ) 
              {
                workerResultStatus = WorkerResultStatus.Warning;
                state = STATE_WARNING_AGE;
                message += string.Format(MessageWarningProcessAge, processName, processAge, DateTime.Now) + '\n';
              }
              else 
              {
                message += string.Format(MessageOkFound, processName, processAge, DateTime.Now) + '\n';
              }
            }
            if ( message.Length == 0 )
            {
              message += string.Format(MessageOkNotFound, processName, null, DateTime.Now) + '\n';
            }
          } //using queryResults
        } //using objectSearcher

        //Get rid of extra CrLf if it exists.
        message = message.TrimEnd(new char [] {'\n'});
      } 
      finally 
      {
        //GC.Collect();
      }

      return workerResultStatus;
    }

    public override WorkerResult Run() 
    {
      try 
      {
        int state;
        string message;
        WorkerResultStatus status = GetProcessAgeForServer(ServerUNC, ProcessName, out message, out state);
        return new WorkerResult(state, status,
          string.Format(Description),
          message );
      }
      catch (Exception e)
      {
        // handle special exceptions here...
        throw( e );
      }
    }
  }//class
}
