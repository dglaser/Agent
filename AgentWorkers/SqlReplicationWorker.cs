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
  public class SqlReplicationWorker : AgentWorker
  {
    public string Connection = null;
    public string Query = @"
select top 1
    result =
      case runstatus
        when 1 then 0 -- START
        when 2 then 0 -- SUCCEED
        when 3 then 0 -- IN PROGRESS
        when 4 then 0 -- IDLE
        when 5 then 1 -- RETRY
        when 6 then 2 -- FAIL
        else 3 -- Unknown
      end,
    name,
    time,
    runstatus,
    runstatus_desc =
      case runstatus
        when 1 then 'START'
        when 2 then 'SUCCEED'
        when 3 then 'IN PROGRESS'
        when 4 then 'IDLE'
        when 5 then 'RETRY'
        when 6 then 'FAIL'
        else 'Unknown'
      end,
    comments,
    current_delivery_latency,
    delivery_latency
  from distribution.dbo.MSdistribution_history h
  inner join distribution.dbo.MSdistribution_agents a
    on (a.id = h.agent_id)
  where name like '{0}%'
  order by time desc
";
    public string DistributionAgentName = null;
    public TimeSpan MaximumDeliveryLatency = TimeSpan.Zero;
    public string MessageOk = "OK: {4} Run status is {1}:{2}; latency is {3:0.#} seconds @ {0:u}.";
    public string MessageWarningRunStatus = "WARNING: {4} Run status is {1}:{2} @ {0:u}.";
    public string MessageCriticalRunStatus = "CRITICAL: {4} Run status is {1}:{2} @ {0:u}.";
    public string MessageCriticalLatency = "CRITICAL: {4} Latency ({3:0.#} seconds) is long @ {0:u}.";
    public string MessageCriticalNoResults = "CRITICAL: No query results for {4} @ {0:u}.";

    const int STATE_OK = 0;
    const int STATE_WARNING_RUNSTATUS = 1;
    const int STATE_CRITICAL_RUNSTATUS = 2;
    const int STATE_CRITICAL_LATENCY = 3;
    const int STATE_CRITICAL_NO_RESULTS = 4;

    public WorkerResultStatus DoQuery(out string message, out int state) 
    {
      message = string.Empty;
      state = STATE_OK;
      WorkerResultStatus workerResultStatus = WorkerResultStatus.Ok;

      int result = 0;
      string agentName = DistributionAgentName;
      DateTime when = DateTime.Now;
      int runStatus = 0;
      string runStatusDesc = null;
      TimeSpan latency = TimeSpan.Zero;

      try 
      {
        using( OleDbConnection connection = new OleDbConnection(Connection) ) 
        {
          string query = string.Format(Query, DistributionAgentName);
          connection.Open();
          using( OleDbCommand command = new OleDbCommand(query, connection) ) 
          {
            OleDbDataReader reader = command.ExecuteReader(CommandBehavior.SingleResult | CommandBehavior.CloseConnection);

            if ( !reader.Read() ) 
            {
              state = STATE_CRITICAL_NO_RESULTS;
              message = string.Format(MessageCriticalNoResults, DateTime.Now, runStatus, runStatusDesc, latency.TotalSeconds, agentName);
              workerResultStatus = WorkerResultStatus.Critical;
            }
            else
            {
              result = Convert.ToInt32( reader["result"] );
              agentName = (string) reader["name"];
              when = (DateTime) reader["time"];
              runStatus = Convert.ToInt32( reader["runstatus"] );
              runStatusDesc = (string) reader["runstatus_desc"];
              latency = TimeSpan.FromMilliseconds( Convert.ToDouble(reader["current_delivery_latency"]) );

              // if latency is greater than max 
              if ( latency > MaximumDeliveryLatency )
              {
                state = STATE_CRITICAL_LATENCY;
                message = string.Format(MessageCriticalLatency, when, runStatus, runStatusDesc, latency.TotalSeconds, agentName);
                workerResultStatus = WorkerResultStatus.Critical;
              }
              else if ( result >= 2 )
              {
                state = STATE_CRITICAL_RUNSTATUS;
                message = string.Format(MessageCriticalRunStatus, when, runStatus, runStatusDesc, latency.TotalSeconds, agentName);
                workerResultStatus = WorkerResultStatus.Critical;
              }
              else if ( result == 1 )
              {
                state = STATE_WARNING_RUNSTATUS;
                message = string.Format(MessageWarningRunStatus, when, runStatus, runStatusDesc, latency.TotalSeconds, agentName);
                workerResultStatus = WorkerResultStatus.Warning;
              }
              else
              {
                message = string.Format(MessageOk, when, runStatus, runStatusDesc, latency.TotalSeconds, agentName);
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
