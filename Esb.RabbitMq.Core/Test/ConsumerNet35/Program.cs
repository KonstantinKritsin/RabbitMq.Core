using System;
using System.Collections.Generic;
using System.Linq;
using Esb.RabbitMq.Core.Contracts;

namespace ConsumerNet35
{
    class Program
    {
        static void Main(string[] args)
        {
            using (new QueueHost<ICMSService>(new ExperimentalImplementation()))
            {
                Console.WriteLine("start listening");
                Console.ReadKey();
            }

            Console.WriteLine("work done...");
        }
    }


    public class ExperimentalImplementation : ICMSService
    {
        //public Func<AgencyFilter, BlockAgentInfo[]> GetBCHAgents { get; set; }
        public BlockAgentInfo[] GetBCHAgents(AgencyFilter filter)
        {
            Console.WriteLine($"codes: [{string.Join(",", filter.Codes)}], types: [{string.Join(",", filter.ContractTypes.Select(t => t.ToString()).ToArray())}], isActiveOnly: {filter.IsActiveOnly}");
            return new[]
            {
                new BlockAgentInfo
                {
                    AgentCode = filter.Codes.FirstOrDefault() ?? "555",
                    Agreements = new[] {"asdf"}
                },
                new BlockAgentInfo
                {
                    AgentCode = filter.Codes.LastOrDefault() ?? "555",
                    Agreements = new[] {"qwer"}
                }
            };
        }

        public PartnerData GetAgentDetailsByCode(string code)
        {
            Console.WriteLine("code: " + code);
            return new PartnerData
            {
                Code = code,
                Id = 999,
                PartnerGuid = Guid.NewGuid(),
                BankDetailsCommon = new PartnerBankDetailsCommon
                {
                    Inn = "11112222333344445555",
                    Vat = "0534654"
                },
                AgreementList = new List<PartnerDocument>
                {
                    new PartnerDocument
                    {
                        StatusReason = ContractStatusReason.Canceled,
                        DocumentStartDate = null,
                        BankDetails = new PartnerBankDetails
                        {
                            BankName = "Alfa-bank",
                            Bic = "9876543210"
                        }
                    },
                    new PartnerDocument
                    {
                        StatusReason = ContractStatusReason.Paused,
                        DocumentStartDate = DateTime.Now,
                        BankDetails = new PartnerBankDetails
                        {
                            BankName = "ООО \"Рога и копыта\"",
                            Bic = "0123456789"
                        }
                    }
                }
            };
        }
    }
}
