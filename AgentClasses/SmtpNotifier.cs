using System;
using System.Web.Mail;
using Agent;

namespace Agent
{
	[Serializable]
	public class SmtpNotifier : AgentNotifier
	{
		public string From = "Agent <Agent@example.com>";
		public string To;
		public string Subject = "[ALERT] {0}";
		public string Body = "{0}";
		public string SmtpServer = "localhost";

		public SmtpNotifier() { }

		protected override void Notify(WorkerResult result) 
		{
			SmtpMail.SmtpServer = SmtpServer;
			SmtpMail.Send(From, To, string.Format(Subject, result.ShortMessage), string.Format(Body, result.DetailedMessage));
		}
	}
}
