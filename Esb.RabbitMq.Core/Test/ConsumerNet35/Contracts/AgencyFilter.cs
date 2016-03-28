namespace ConsumerNet35.Contracts
{
	public class AgencyFilter
	{
		public string[] Codes { get; set; }
		public int[] ContractTypes { get; set; }
		public bool IsActiveOnly { get; set; }
	}
}