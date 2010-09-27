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
  public class NetworkDiskSpaceWorker : AgentWorker
  {
    const long ONE_GIGABYTE = 1073741824;
    public string ServerUNC = null;
    public decimal WarningPercentFree = 20m;
    public decimal CriticalPercentFree = 5m;
    public decimal WarningGigabytesFree = 0m;
    public decimal CriticalGigabytesFree = 0m;
    public string MessageWarningPercentage = "Warning: Drive {0} has only {1:0.0}% free space ({2:0.0} GB) @ {3:u}.";
    public string MessageCriticalPercentage = "CRITICAL: Drive {0} has only {1:0.0}% free space ({2:0.0} GB) @ {3:u}.";
    public string MessageWarningGB = "Warning: Drive {0} has only {2:0.0} free GB space ({1:0.0}%) @ {3:u}.";
    public string MessageCriticalGB = "CRITICAL: Drive {0} has only {2:0.0} free GB space ({1:0.0}%) @ {3:u}.";
    public string MessageOk = "OK: Drive {0} has {1:0.0}% free space ({2:0.0} GB) @ {3:u}.";

    const int STATE_OK = 0;
    const int STATE_WARNING_PERCENTAGE = 1;
    const int STATE_CRITICAL_PERCENTAGE = 2;
    const int STATE_WARNING_GB = 3;
    const int STATE_CRITICAL_GB = 4;

    public WorkerResultStatus GetFreeFixedDiskSpaceForServer(string strUNCMachine, out string message, out int state) 
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
        objectQuery = new ObjectQuery("select Name, FreeSpace, Size from Win32_LogicalDisk where DriveType=3");
            
        using ( objectSearcher = new ManagementObjectSearcher(scope, objectQuery) ) 
        {
          using ( queryResults = objectSearcher.Get() ) 
          {
            foreach(ManagementObject moObject in queryResults) 
            {
              decimal freeSpace = Convert.ToDecimal(moObject["FreeSpace"]);
              decimal size = Convert.ToDecimal(moObject["Size"]);
              decimal percentFree = (freeSpace / size) * 100m;
              decimal freeGB = freeSpace / ONE_GIGABYTE;
              decimal sizeGB = size / ONE_GIGABYTE;

              if ( percentFree <= CriticalPercentFree ) 
              {
                workerResultStatus = WorkerResultStatus.Critical;
                state = STATE_CRITICAL_PERCENTAGE;
                message += string.Format(MessageCriticalPercentage, moObject["Name"], percentFree, freeGB, DateTime.Now) + '\n';
              }
              else if ( freeGB <= CriticalGigabytesFree ) 
              {
                workerResultStatus = WorkerResultStatus.Critical;
                state = STATE_CRITICAL_GB;
                message += string.Format(MessageCriticalGB, moObject["Name"], percentFree, freeGB, DateTime.Now) + '\n';
              }
              else if ( percentFree <= WarningPercentFree ) 
              {
                if ( workerResultStatus == WorkerResultStatus.Ok ) 
                {
                  workerResultStatus = WorkerResultStatus.Warning;
                  state = STATE_WARNING_PERCENTAGE;
                }
                message += string.Format(MessageWarningPercentage, moObject["Name"], percentFree, freeGB, DateTime.Now) + '\n';
              }
              else if ( freeGB <= WarningGigabytesFree ) 
              {
                if ( workerResultStatus == WorkerResultStatus.Ok ) 
                {
                  workerResultStatus = WorkerResultStatus.Warning;
                  state = STATE_WARNING_GB;
                }
                message += string.Format(MessageWarningGB, moObject["Name"], percentFree, freeGB, DateTime.Now) + '\n';
              }
              else 
              {
                message += string.Format(MessageOk, moObject["Name"], percentFree, freeGB, DateTime.Now) + '\n';
              }
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
        WorkerResultStatus status = GetFreeFixedDiskSpaceForServer(ServerUNC, out message, out state);
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
