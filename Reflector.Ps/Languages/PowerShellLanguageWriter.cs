using Reflector.CodeModel;

namespace Reflector.Ps.Languages
{
	internal sealed class PowerShellLanguageWriter : ILanguageWriter
	{
		public PowerShellLanguageWriter(IFormatter formatter, ILanguageWriterConfiguration configuration)
		{
			this.formatter = formatter;
			this.configuration = configuration;
			visitor = new VisitorWriter(formatter, configuration);
		}

		public void WriteMethodDeclaration(IMethodDeclaration value)
		{
			visitor.VisitMethodDeclaration(value);
		}

		public void WriteStatement(IStatement value)
		{
			visitor.VisitStatement(value);
		}

		public void WriteExpression(IExpression value)
		{
			visitor.VisitExpression(value);
		}

		public void WriteAssembly(IAssembly value)
		{
			visitor.VisitAssembly(value);
		}

		public void WriteAssemblyReference(IAssemblyReference value)
		{
			visitor.VisitAssemblyReference(value);
		}

		public void WriteEventDeclaration(IEventDeclaration value)
		{
			visitor.VisitEventDeclaration(value);
		}

		public void WriteFieldDeclaration(IFieldDeclaration value)
		{
			visitor.VisitFieldDeclaration(value);
		}

		public void WriteModule(IModule value)
		{
			visitor.VisitModule(value);
		}

		public void WriteModuleReference(IModuleReference value)
		{
			visitor.VisitModuleReference(value);
		}

		public void WriteNamespace(INamespace value)
		{
			visitor.VisitNamespace(value);
		}

		public void WritePropertyDeclaration(IPropertyDeclaration value)
		{
			visitor.VisitPropertyDeclaration(value);
		}

		public void WriteResource(IResource value)
		{
			visitor.VisitResource(value);
		}

		public void WriteTypeDeclaration(ITypeDeclaration value)
		{
			visitor.VisitTypeDeclaration(value);
		}

		private IFormatter formatter;

		private ILanguageWriterConfiguration configuration;

		private VisitorWriter visitor;
	}
}
