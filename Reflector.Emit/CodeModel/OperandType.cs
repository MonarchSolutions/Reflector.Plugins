namespace Reflector.Emit.CodeModel
{
	internal enum OperandType
	{
		BranchTarget,
		ShortBranchTarget = 15,
		Field = 1,
		Int32,
		Int64,
		Method,
		None,
		Phi,
		Double,
		Signature = 9,
		String,
		Switch,
		Token,
		Type,
		Variable,
		SByte = 16,
		Single,
		ShortVariable
	}
}
