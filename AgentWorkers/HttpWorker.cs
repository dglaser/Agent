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
  public class HttpWorker : AgentWorker
  {
    public string Url = null;
    public string Method = "GET";
    public TimeSpan WarningResponseTime = TimeSpan.Parse("00:00:30");
    public TimeSpan CriticalResponseTime = TimeSpan.Parse("00:02:00");
    public string MessageWarningResponseTime = "Warning: Response time was {2:#.##} seconds @ {3:u}.";
    public string MessageCriticalResponseTime = "CRITICAL: Response time was {2:#.##} seconds @ {3:u}.";
    public string MessageCriticalError = "CRITICAL: Response was {0} - {1} after {2:#.##} seconds @ {3:u}.";
    public string MessageOk = "OK: Response time was {2:#.##} seconds @ {3:u}.";

    const int STATE_OK = 0;
    const int STATE_WARNING_TIME = 1;
    const int STATE_CRITICAL_TIME = 2;
    const int STATE_CRITICAL_ERROR = 3;

    public WorkerResultStatus MakeRequest(out string message, out int state) 
    {
      WorkerResultStatus workerResultStatus = WorkerResultStatus.Ok;
      message = string.Empty;
      state = STATE_OK;

      HttpWebRequest request = null;
      HttpWebResponse response = null;

      DateTime startTime = DateTime.Now;
      DateTime endTime = startTime;
      TimeSpan responseTime = TimeSpan.Zero;

      try 
      {
        request = (HttpWebRequest) WebRequest.Create(Url);
        request.Method = Method;
        request.Credentials = CredentialCache.DefaultCredentials;
        response = (HttpWebResponse) request.GetResponse();
        endTime = DateTime.Now;
        responseTime = endTime - startTime;

        if ( responseTime >= CriticalResponseTime ) 
        {
          message = string.Format(MessageCriticalResponseTime, response.StatusCode, response.StatusDescription,
            responseTime.TotalSeconds, endTime);
          state = STATE_CRITICAL_TIME;
          workerResultStatus = WorkerResultStatus.Critical;
        }
        else if ( responseTime >= WarningResponseTime ) 
        {
          message = string.Format(MessageWarningResponseTime, response.StatusCode, response.StatusDescription,
            responseTime.TotalSeconds, endTime);
          state = STATE_WARNING_TIME;
          workerResultStatus = WorkerResultStatus.Warning;
        }
        else
        {
          message = string.Format(MessageOk, response.StatusCode, response.StatusDescription,
            responseTime.TotalSeconds, endTime);
        }
      } 
      catch (WebException we) 
      {
        endTime = DateTime.Now;
        responseTime = endTime - startTime;
        message = string.Format(MessageCriticalError, we.Status, we.Message,
          responseTime.TotalSeconds, endTime);
        state = STATE_WARNING_TIME;
        workerResultStatus = WorkerResultStatus.Warning;
      }
      finally 
      {
        if ( response != null )
          response.Close();
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
        WorkerResultStatus status = MakeRequest(out message, out state);
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
