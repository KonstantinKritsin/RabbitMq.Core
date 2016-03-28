using System;
using System.Collections.Generic;

namespace ConsumerNet35.Contracts
{
	public class PartnerData
	{
		public int Id { get; set; }
		public string Code { get; set; }
		public Guid PartnerGuid { get; set; }
		public PartnerBankDetailsCommon BankDetailsCommon { get; set; }
		public List<PartnerDocument> AgreementList { get; set; }
	}
}