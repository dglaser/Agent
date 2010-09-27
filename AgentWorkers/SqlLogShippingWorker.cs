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
  public class SqlLogShippingWorker : AgentWorker
  {
    public string Connection = null;
    public string Query = @"EXECUTE msdb.dbo.sp_get_log_shipping_monitor_info '{0}', '{1}', '{2}', '{3}'";
    public string PrimaryServerName = null;
    public string PrimaryDatabaseName = null;
    public string SecondaryServerName = null;
    public string SecondaryDatabaseName = null;
    public TimeSpan LastUpdatedWarningThreshold = new TimeSpan(0, 0, 12, 0, 0);
    public TimeSpan LastUpdatedCriticalThreshold = new TimeSpan(0, 0, 22, 0, 0);
    public string MessageOk = "OK: {3}.{4} is {1:0.0} minute(s) old ({2:u}) @ {0:u}.";
    public string MessageWarningOld = "WARNING: {3}.{4} is {1:0.0} minutes old ({2:u}) @ {0:u}.";
    public string MessageCriticalOld = "CRITICAL: {3}.{4} is {1:0.0} minutes old ({2:u}) @ {0:u}.";
    public string MessageCriticalNoResults = "CRITICAL: No query results for {3}.{4} @ {0:u}.";

    const int STATE_OK = 0;
    const int STATE_WARNING_OLD = 1;
    const int STATE_CRITICAL_OLD = 2;
    const int STATE_CRITICAL_NO_RESULTS = 3;

    public WorkerResultStatus DoQuery(out string message, out int state) 
    {
      message = string.Empty;
      state = STATE_OK;
      WorkerResultStatus workerResultStatus = WorkerResultStatus.Ok;

      DateTime lastLoadedLastUpdated = DateTime.MinValue;
      TimeSpan age = TimeSpan.MaxValue;

      try 
      {
        using( OleDbConnection connection = new OleDbConnection(Connection) ) 
        {
          string query = string.Format(Query, PrimaryServerName, PrimaryDatabaseName, SecondaryServerName, SecondaryDatabaseName);
          connection.Open();
          using( OleDbCommand command = new OleDbCommand(query, connection) ) 
          {
            OleDbDataReader reader = command.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.CloseConnection);

            if ( !reader.Read() ) 
            {
              state = STATE_CRITICAL_NO_RESULTS;
              message = string.Format(MessageCriticalNoResults, DateTime.Now, age.TotalMinutes, lastLoadedLastUpdated, SecondaryServerName, SecondaryDatabaseName);
              workerResultStatus = WorkerResultStatus.Critical;
            }
            else
            {
              lastLoadedLastUpdated = (DateTime) reader["last_loaded_last_updated"];
              age = DateTime.Now - lastLoadedLastUpdated;

              // if age is greater than max 
              if ( age > LastUpdatedCriticalThreshold )
              {
                state = STATE_CRITICAL_OLD;
                message = string.Format(MessageCriticalOld, DateTime.Now, age.TotalMinutes, lastLoadedLastUpdated, SecondaryServerName, SecondaryDatabaseName);
                workerResultStatus = WorkerResultStatus.Critical;
              }
              if ( age > LastUpdatedWarningThreshold )
              {
                state = STATE_CRITICAL_OLD;
                message = string.Format(MessageWarningOld, DateTime.Now, age.TotalMinutes, lastLoadedLastUpdated, SecondaryServerName, SecondaryDatabaseName);
                workerResultStatus = WorkerResultStatus.Critical;
              }
              else
              {
                message = string.Format(MessageOk, DateTime.Now, age.TotalMinutes, lastLoadedLastUpdated, SecondaryServerName, SecondaryDatabaseName);
              }
            }
          }//using command
        }//using connection
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
        WorkerResultStatus status = DoQuery(out message, out state);
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
