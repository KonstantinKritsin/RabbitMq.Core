using System;
using Esb.RabbitMq.Core.Contracts;
using RabbitMQ.Client.Events;

namespace SubscriberNet35
{
	class Program
	{
		static void Main(string[] args)
		{
		    QueueSerivceFactory factory = null;

            try
            {
                factory = new QueueSerivceFactory();
                var service = factory.CreateService();
                var bchAgents = service.GetBCHAgents(new AgencyFilter
                {
                    Codes = new[] { "555", "10540" },
                    ContractTypes = new[] { 1, 2, 3 },
                    IsActiveOnly = true
                });

                foreach (var a in bchAgents)
                {
                    Console.WriteLine("agent: " + a.AgentCode);
                    foreach (var agreement in a.Agreements)
                        Console.WriteLine("\tagreement: " + agreement);
                }

                var details = service.GetAgentDetailsByCode("555");
                Console.WriteLine("details id: " + details.Id);
                Console.WriteLine("partner guid: " + details.PartnerGuid);
                Console.WriteLine("agent code: " + details.Code);
                Console.WriteLine("common bank details:");
                Console.WriteLine("\tinn: " + details.BankDetailsCommon.Inn);
                Console.WriteLine("\tvat" + details.BankDetailsCommon.Vat);
                Console.WriteLine("agreements:");
                foreach (var d in details.AgreementList)
                {
                    Console.WriteLine($"\tstatus: [{d.StatusReason:D}:{d.StatusReason:G}]");
                    Console.WriteLine($"\tstart date: {d.DocumentStartDate.GetValueOrDefault():F}");
                    Console.WriteLine("\tbanc details:");
                    Console.WriteLine("\t\tname: " + d.BankDetails.BankName);
                    Console.WriteLine("\t\tbik: " + d.BankDetails.Bic);
                }
            }
		    catch (Exception e)
		    {
		        Console.WriteLine(e.Message);
		    }
		    finally
            {
                factory?.Dispose();
            }

		    Console.ReadKey();
		}
	}
}
