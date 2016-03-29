using System;

namespace Esb.RabbitMq.Core.Contracts
{
	public class PartnerDocument
	{
	    public DateTime? DocumentStartDate { get; set; }
		public ContractStatusReason StatusReason { get; set; }
		public PartnerBankDetails BankDetails { get; set; }
	}
}