//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// .NET Agent Framework - A Job Scheduler and Notification Service
//
// Author:  Luther Ananda Miller - luther@anandus.com
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

using System;
using System.IO;
using System.Net;
using Agent;

namespace Agent.Worker
{
  [Serializable]
  public class FtpDownloadWorker : AgentWorker
  {
    public string DownloadFileUrl;
    public string LocalPath;
    public string User = "anonymous";
    public string Password = "developer@example.com";
    public string MessageCritical = "FTP failed at {0:u}; status {1}.";
    public string MessageOk = "FTP succeeded at {0:u}.";

    public override WorkerResult Run() 
    {
      const int STATE_OK = 0;
      const int STATE_CRITICAL = 1;

      FtpWebRequest ftp = new FtpWebRequest(new Uri(DownloadFileUrl));
      ftp.Credentials = new NetworkCredential(User, Password);
      ftp.Method = "GET";

      FtpWebResponse response = ftp.GetResponse() as FtpWebResponse;

      if ( response.Status == 226 ) //226 = Transfer Complete 
      {
        Stream responseStream = response.GetResponseStream();
        FileStream fileStream = File.Open(LocalPath, FileMode.Create);
        byte [] buffer = new byte[32768];
        if (responseStream.CanRead) 
        {
          for (int bytesRead; (bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0; )
            fileStream.Write(buffer, 0, bytesRead);
        }
        responseStream.Close();
        fileStream.Close();

        return new WorkerResult(STATE_OK, WorkerResultStatus.Ok,
          string.Format(Description),
          string.Format(MessageOk, DateTime.Now) );
      } 
      else 
      {
        Stream responseStream = response.GetResponseStream();
        StreamReader responseReader = new StreamReader( responseStream );
        string responseText = responseReader.ReadToEnd();
        responseReader.Close();
        responseStream.Close();

        return new WorkerResult(STATE_CRITICAL, WorkerResultStatus.Critical,
          string.Format(Description),
          string.Format(MessageCritical, DateTime.Now, response.StatusDescription, responseText) );
      }

    }
  }//class
}//namespace
