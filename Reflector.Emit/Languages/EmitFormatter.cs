using Reflector.CodeModel;

namespace Reflector.Emit.Languages
{
	internal sealed class EmitFormatter : IFormatter
	{
		public EmitFormatter(IFormatter formatter)
		{
			this._formatter = formatter;
		}

		public IFormatter Formatter => _formatter;

	    public void WriteCustom(string value)
	    {
            //TODO:
	        Write(value);
	    }

	    public void Write(string value)
		{
			_formatter.Write(value);
		}

		public void Write(string format, params object[] args)
		{
			string value = string.Format(format, args);
			Write(value);
		}

		public void WriteComment(string value)
		{
			string value2 = $"// {value}";
			_formatter.WriteComment(value2);
			_formatter.WriteLine();
		}

		public void WriteComment(string format, params object[] args)
		{
			string value = string.Format(format, args);
			WriteComment(value);
		}

		public void WriteDeclaration(string value)
		{
			_formatter.WriteDeclaration(value);
		}

		public void WriteDeclaration(string format, params object[] args)
		{
			string value = string.Format(format, args);
			WriteDeclaration(value);
		}

		public void WriteDeclaration(string value, object target)
		{
			WriteDeclaration(value);
		}

		public void WriteIndent()
		{
			_formatter.WriteIndent();
		}

		public void WriteKeyword(string value)
		{
			_formatter.WriteKeyword(value);
		}

		public void WriteLine()
		{
			_formatter.WriteLine();
		}

		public void WriteLiteral(string value)
		{
			string value2 = $"\"{value}\"";
			_formatter.WriteLiteral(value2);
		}

		public void WriteLiteral(string format, params object[] args)
		{
			string value = string.Format(format, args);
			WriteLiteral(value);
		}

		public void WriteOutdent()
		{
			_formatter.WriteOutdent();
		}

		public void WriteProperty(string name, string value)
		{
			_formatter.WriteProperty(name, value);
		}

		public void WriteReference(string value, string description, object target)
		{
			_formatter.WriteReference(value, description, target);
		}

		public void WriteEndStatement()
		{
			_formatter.Write(";");
			_formatter.WriteLine();
		}

		public void WriteMethodInvocation(string instanceName, string methodName, bool endStatement, params string[] args)
		{
			Write("{0}.{1}(", instanceName, methodName);
			if (args.Length > 3)
			{
				_formatter.WriteLine();
				_formatter.WriteIndent();
				for (int i = 0; i < args.Length; i++)
				{
					if (i != 0)
					{
						_formatter.Write(",");
					}
					_formatter.Write(args[i]);
					_formatter.WriteLine();
				}
				_formatter.Write(")");
				if (endStatement)
				{
					_formatter.Write(";");
				}
				_formatter.WriteOutdent();
				if (endStatement)
				{
					_formatter.WriteLine();
				}
			}
			else
			{
				for (int j = 0; j < args.Length; j++)
				{
					if (j != 0)
					{
						_formatter.Write(",");
					}
					_formatter.Write(args[j]);
				}
				_formatter.Write(")");
				if (endStatement)
				{
					WriteEndStatement();
				}
			}
		}

		public void WriteVariableDeclaration(string variableType, string variableName)
		{
			Write("{0} {1}", variableType, variableName);
		}

		public void WriteVariableDeclaration(string variableType, string variableName, object target)
		{
			Write("{0} ", variableType);
			WriteReference(variableName, "", target);
		}

		public void WriteEqual()
		{
			_formatter.Write(" = ");
		}

		public void WriteTypeOf(IType type)
		{
			ITypeReference typeReference = type as ITypeReference;
			string value;
			if (typeReference != null)
			{
				value = Helper.GetNameWithResolutionScope(typeReference);
			}
			else
			{
				value = type.ToString();
			}
			_formatter.WriteKeyword("typeof");
			Write("(");
			WriteReference(value, "", type);
			Write(")");
		}

		public void WriteAssignType(string instance, string property, IType type)
		{
			Write("{0}.{1}", instance, property);
			WriteEqual();
			WriteKeyword("new");
			Write(" ");
			Write("TypeTypeReference(");
			WriteTypeOf(type);
			Write(")");
			WriteEndStatement();
		}

		public void WriteVisibility(string instance, MethodVisibility visibility)
		{
			if ((visibility & MethodVisibility.Assembly) != MethodVisibility.PrivateScope)
			{
				Write("{0}.Attributes |= System.CodeDom.MethodVisibility.Assembly", instance);
				WriteEndStatement();
			}
			if ((visibility & MethodVisibility.Family) != MethodVisibility.PrivateScope)
			{
				Write("{0}.Attributes |= System.CodeDom.MethodVisibility.Family", instance);
				WriteEndStatement();
			}
			if ((visibility & MethodVisibility.Private) != MethodVisibility.PrivateScope)
			{
				Write("{0}.Attributes |= System.CodeDom.MethodVisibility.Private", instance);
				WriteEndStatement();
			}
		}

		private IFormatter _formatter;
	}
}
