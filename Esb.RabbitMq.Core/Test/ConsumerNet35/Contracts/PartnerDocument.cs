using System;

namespace ConsumerNet35.Contracts
{
	public class PartnerDocument
	{
		public string AgentCode { get; set; }
		public DateTime? DocumentStartDate { get; set; }
		public ContractStatusReason StatusReason { get; set; }
		public PartnerBankDetails BankDetails { get; set; }
	}
}