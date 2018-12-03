using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Reflector.CodeModel;
using Reflector.Emit.CodeModel;
using OperandType = Reflector.Emit.CodeModel.OperandType;

namespace Reflector.Emit.ReflectionEmitLanguage
{
	internal sealed class EmitLanguageWriter : ILanguageWriter
	{
		public EmitLanguageWriter(IServiceProvider serviceProvider, IFormatter formatter, ILanguageWriterConfiguration configuration)
		{
			search = new CodeModelSearch(serviceProvider);
			_formatter = formatter;
			_configuration = configuration;
		}

		public void WriteAssembly(IAssembly value)
		{
			ITypeDeclaration target = search.FindType(typeof(AssemblyBuilder));
			ITypeDeclaration typeDeclaration = search.FindType(typeof(AppDomain));
			IMethodDeclaration method = Helper.GetMethod(typeDeclaration, "DefineDynamicAssembly");
			ITypeDeclaration typeDeclaration2 = search.FindType(typeof(AssemblyBuilderAccess));
			IFieldDeclaration target2 = search.FindField(typeDeclaration2, "RunAndSave");
			_formatter.WriteKeyword("public");
			_formatter.Write(" ");
			_formatter.WriteReference("AssemblyBuilder", "", target);
			_formatter.Write(" BuildAssembly" + value.Name);
			_formatter.Write("(");
			_formatter.WriteReference("AppDomain", "", typeDeclaration);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("domain");
			_formatter.Write(")");
			_formatter.WriteLine();
			_formatter.Write("{");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			WriteAssemblyReference(value);
			_formatter.WriteReference("AssemblyBuilder", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("assembly");
			_formatter.Write(" = domain.");
			_formatter.WriteReference("DefineDynamicAssembly", "", method);
			_formatter.Write("(assemblyName, ");
			_formatter.WriteReference("AssemblyBuilderAccess", "", typeDeclaration2);
			_formatter.Write(".");
			_formatter.WriteReference("RunAndSave", "", target2);
			_formatter.Write(");");
			_formatter.WriteLine();
			_formatter.WriteKeyword("return");
			_formatter.Write(" ");
			_formatter.Write("assembly;");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
			_formatter.Write("}");
			_formatter.WriteLine();
		}

		public void WriteAssemblyReference(IAssemblyReference value)
		{
			ITypeDeclaration typeDeclaration = search.FindType(typeof(AssemblyName));
			ITypeDeclaration target = search.FindType(typeof(Version));
			IMethodDeclaration method = Helper.GetMethod(typeDeclaration, "SetPublicKey");
			IMethodDeclaration method2 = Helper.GetMethod(typeDeclaration, "SetPublicKeyToken");
			IPropertyDeclaration target2 = search.FindProperty(typeDeclaration, "Version");
			_formatter.WriteReference("AssemblyName", "", typeDeclaration);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("assemblyName");
			_formatter.Write(" = ");
			_formatter.WriteKeyword("new");
			_formatter.Write(" ");
			_formatter.WriteReference("AssemblyName", "", typeDeclaration);
			_formatter.Write("(");
			_formatter.WriteLiteral(value.Name);
			_formatter.Write(");");
			_formatter.WriteLine();
			if (value.PublicKey.Length != 0)
			{
				_formatter.WriteComment("setting public key");
				WriteByteArray("publicKey", value.PublicKey);
				_formatter.Write("assemblyName.");
				_formatter.WriteReference("SetPublicKey", "", method);
				_formatter.Write("(publicKey);");
				_formatter.WriteLine();
			}
			if (value.PublicKeyToken.Length != 0)
			{
				_formatter.WriteComment("setting public key token");
				WriteByteArray("publicKeyToken", value.PublicKeyToken);
				_formatter.Write("assemblyName.");
				_formatter.WriteReference("SetPublicKeyToken", "", method2);
				_formatter.Write("(publicKeyToken);");
				_formatter.WriteLine();
			}
			_formatter.Write("assemblyName");
			_formatter.Write(".");
			_formatter.WriteReference("Version", "", target2);
			_formatter.Write(" = ");
			_formatter.WriteKeyword("new");
			_formatter.Write(" ");
			_formatter.WriteReference("Version", "", target);
			_formatter.Write(string.Format("({0},{1},{2},{3});", new object[]
			{
				value.Version.Major,
				value.Version.Minor,
				value.Version.Revision,
				value.Version.Build
			}));
			_formatter.WriteLine();
		}

		private void WriteByteArray(string local, byte[] array)
		{
			ITypeDeclaration target = search.FindType(typeof(byte));
			_formatter.WriteReference("byte", "", target);
			_formatter.Write("[] ");
			_formatter.WriteDeclaration(local);
			_formatter.Write(" = ");
			_formatter.WriteKeyword("new");
			_formatter.Write(" ");
			_formatter.WriteReference("byte", "", target);
			_formatter.Write("[]{");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			for (int i = 0; i < array.Length; i++)
			{
				if (i != 0)
				{
					_formatter.Write(", ");
				}
				if ((i + 1) % 8 == 0)
				{
					_formatter.WriteLine();
				}
				_formatter.Write(array[i].ToString());
			}
			_formatter.WriteLine();
			_formatter.Write("};");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
		}

		public void WriteEventDeclaration(IEventDeclaration value)
		{
			_formatter.WriteDeclaration(value.ToString());
		}

		public void WriteExpression(IExpression value)
		{
			_formatter.Write("Not supported by the Reflection.Emit language");
		}

		public void WriteFieldDeclaration(IFieldDeclaration value)
		{
			ITypeDeclaration target = search.FindType(typeof(FieldBuilder));
			ITypeDeclaration typeDeclaration = search.FindType(typeof(TypeBuilder));
			IMethodDeclaration method = Helper.GetMethod(typeDeclaration, "DefineField");
			ITypeDeclaration typeDeclaration2 = search.FindType(typeof(FieldAttributes));
			_formatter.WriteKeyword("public");
			_formatter.Write(" ");
			_formatter.WriteReference("FieldBuilder", "", target);
			_formatter.Write(" BuildField");
			_formatter.Write(value.Name);
			_formatter.Write("(");
			_formatter.WriteReference("TypeBuilder", "", typeDeclaration);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("type");
			_formatter.Write(")");
			_formatter.WriteLine();
			_formatter.Write("{");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			_formatter.WriteReference("FieldBuilder", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("field");
			_formatter.Write(" = type.");
			_formatter.WriteReference("DefineField", "", method);
			_formatter.Write("(");
			_formatter.WriteIndent();
			_formatter.WriteLine();
			_formatter.WriteLiteral(value.Name);
			_formatter.Write(", ");
			_formatter.WriteLine();
			WriteTypeOf(value.FieldType);
			_formatter.Write(", ");
			_formatter.WriteLine();
			_formatter.Write("  ");
			_formatter.WriteReference("FieldAttributes", "", typeDeclaration2);
			_formatter.Write(".");
			switch (value.Visibility)
			{
			case FieldVisibility.PrivateScope:
				_formatter.WriteReference("PrivateScope", "", search.FindField(typeDeclaration2, "PrivateScope"));
				break;
			case FieldVisibility.Private:
				_formatter.WriteReference("Private", "", search.FindField(typeDeclaration2, "Private"));
				break;
			case FieldVisibility.FamilyAndAssembly:
				_formatter.WriteReference("FamANDAssem", "", search.FindField(typeDeclaration2, "FamANDAssem"));
				break;
			case FieldVisibility.Assembly:
				_formatter.WriteReference("Assembly", "", search.FindField(typeDeclaration2, "Assembly"));
				break;
			case FieldVisibility.Family:
				_formatter.WriteReference("Family", "", search.FindField(typeDeclaration2, "Family"));
				break;
			case FieldVisibility.FamilyOrAssembly:
				_formatter.WriteReference("FamORAssem", "", search.FindField(typeDeclaration2, "FamORAssem"));
				break;
			case FieldVisibility.Public:
				_formatter.WriteReference("Public", "", search.FindField(typeDeclaration2, "Public"));
				break;
			}
			if (value.Static)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				_formatter.WriteReference("FieldAttributes", "", typeDeclaration2);
				_formatter.Write(".");
				_formatter.WriteReference("Static", "", search.FindField(typeDeclaration2, "Static"));
			}
			if (value.SpecialName)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				_formatter.WriteReference("FieldAttributes", "", typeDeclaration2);
				_formatter.Write(".");
				_formatter.WriteReference("SpecialName", "", search.FindField(typeDeclaration2, "SpecialName"));
			}
			if (value.Literal)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				_formatter.WriteReference("FieldAttributes", "", typeDeclaration2);
				_formatter.Write(".");
				_formatter.WriteReference("Literal", "", search.FindField(typeDeclaration2, "Literal"));
			}
			_formatter.WriteLine();
			_formatter.Write(");");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
			_formatter.WriteKeyword("return");
			_formatter.Write(" field;");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
			_formatter.Write("}");
			_formatter.WriteLine();
		}

		public void WriteMethodDeclaration(IMethodDeclaration value)
		{
			operandLocals = new Hashtable();
			ITypeDeclaration memberType = search.FindType(typeof(MethodBuilder));
			DeclareMethod(value);
			_formatter.Write("{");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			GenerateDefineMethod(value);
			GenerateGenericArguments(value);
			GenerateOperandLocals(value);
			GenerateCallingConvention(value);
			GenerateCustomAttributes(value, memberType, "method");
			GenerateParameters(value);
			GenerateGetIlGenerator();
		    if (value.Body is IMethodBody methodBody)
			{
				GenerateLocals(methodBody);
				Hashtable labels = PrepareLabels(methodBody);
				GenerateBody(methodBody, labels);
			}
			_formatter.WriteComment("finished");
			_formatter.WriteKeyword("return");
			_formatter.Write(" ");
			_formatter.Write("method");
			_formatter.Write(";");
			_formatter.WriteLine();
			_formatter.WriteOutdent();
			_formatter.Write("}");
			_formatter.WriteLine();
		}

		private void GenerateGenericArguments(IMethodDeclaration value)
		{
			if (value.GenericArguments.Count != 0)
			{
				ITypeDeclaration target = search.FindType("System.Reflection.Emit.GenericTypeParameterBuilder");
				ITypeDeclaration value2 = search.FindType(typeof(MethodBuilder));
				ITypeDeclaration typeDeclaration = search.FindType(typeof(int));
				IMemberDeclaration method = Helper.GetMethod(value2, "DefineGenericParameters");
				ITypeDeclaration typeDeclaration2 = search.FindType("System.Reflection.GenericParameterAttributes");
				IMethodDeclaration method2 = Helper.GetMethod(typeDeclaration2, "SetGenericParameterAttributes");
				_formatter.WriteReference("GenericTypeParameterBuilder", "", target);
				_formatter.Write("[] ");
				_formatter.WriteDeclaration("genericParameters");
				_formatter.Write(" = ");
				_formatter.Write("method");
				_formatter.Write(".");
				_formatter.WriteReference("DefineGenericParameters", "", method);
				_formatter.Write("(");
				for (int i = 0; i < value.GenericArguments.Count; i++)
				{
					IGenericParameter genericParameter = (IGenericParameter)value.GenericArguments[i];
					if (i != 0)
					{
						_formatter.Write(", ");
					}
					_formatter.WriteLiteral(genericParameter.Name);
				}
				_formatter.Write(");");
				_formatter.WriteLine();
				for (int j = 0; j < value.GenericArguments.Count; j++)
				{
					IGenericParameter genericParameter2 = (IGenericParameter)value.GenericArguments[j];
					_formatter.WriteComment("Generic parameter " + genericParameter2.Name);
					_formatter.Write("genericParameters[" + j.ToString() + "].");
					_formatter.WriteReference("SetGenericParameterAttributes", "", method2);
					_formatter.Write("(");
					_formatter.WriteReference("GenericParameterAttributes", "", typeDeclaration2);
					_formatter.Write(".");
					_formatter.WriteReference(genericParameter2.Variance.ToString(), "", search.FindField(typeDeclaration2, genericParameter2.Variance.ToString()));
					_formatter.Write(");");
					_formatter.WriteLine();
				}
				_formatter.WriteLine();
			}
		}

		private void GenerateMethodAttributesLocal(IMethodDeclaration value)
		{
			ITypeDeclaration typeDeclaration = search.FindType(typeof(MethodAttributes));
			ITypeDeclaration typeDeclaration2 = search.FindType(typeof(MethodBuilder));
			_formatter.WriteComment("Method attributes");
			_formatter.WriteReference(Helper.GetNameWithResolutionScope(typeDeclaration), "", typeDeclaration);
			_formatter.Write(" ");
			_formatter.Write("methodAttributes");
			_formatter.Write(" = ");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			_formatter.Write("  ");
			_formatter.WriteReference(Helper.GetNameWithResolutionScope(typeDeclaration), "", typeDeclaration);
			_formatter.Write(".");
			_formatter.WriteReference(value.Visibility.ToString(), "", search.FindField(typeDeclaration, value.Visibility.ToString()));
			if (value.Abstract)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				WriteEnumValue(typeDeclaration, "Abstract");
			}
			else if (value.Virtual)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				WriteEnumValue(typeDeclaration, "Virtual");
			}
			if (value.Final)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				WriteEnumValue(typeDeclaration, "Final");
			}
			if (value.HideBySignature)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				WriteEnumValue(typeDeclaration, "HideBySig");
			}
			if (value.Static)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				WriteEnumValue(typeDeclaration, "Static");
			}
			if (value.NewSlot)
			{
				_formatter.WriteLine();
				_formatter.Write("| ");
				WriteEnumValue(typeDeclaration, "NewSlot");
			}
			_formatter.Write(";");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
		}

		private void WriteEnumValue(ITypeDeclaration methodAttributes, string value)
		{
			_formatter.WriteReference(Helper.GetNameWithResolutionScope(methodAttributes), "", methodAttributes);
			_formatter.Write(".");
			_formatter.WriteReference(value, "", search.FindField(methodAttributes, value));
		}

		private void GenerateCallingConvention(IMethodDeclaration value)
		{
		}

		private void GenerateCustomAttributes(Reflector.CodeModel.ICustomAttributeProvider value, ITypeDeclaration memberType, string ownerName)
		{
			if (value.Attributes.Count != 0)
			{
				_formatter.WriteComment("Adding custom attributes to " + ownerName);
				foreach (object obj in value.Attributes)
				{
					ICustomAttribute attribute = (ICustomAttribute)obj;
					GenerateCustomAttribute(memberType, ownerName, attribute);
				}
			}
		}

		private void GenerateCustomAttribute(ITypeDeclaration memberType, string ownerName, ICustomAttribute attribute)
		{
			ITypeDeclaration target = search.FindType(typeof(Type));
			ITypeDeclaration target2 = search.FindType(typeof(PropertyInfo));
			ITypeDeclaration target3 = search.FindType(typeof(FieldInfo));
			ITypeDeclaration target4 = search.FindType(typeof(object));
			ITypeDeclaration target5 = search.FindType(typeof(CustomAttributeBuilder));
			IMethodDeclaration method = Helper.GetMethod(memberType, "SetCustomAttribute");
			_formatter.WriteComment(attribute.ToString());
			_formatter.Write(ownerName);
			_formatter.Write(".");
			_formatter.WriteReference("SetCustomAttribute", "", method);
			_formatter.Write("(");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			_formatter.WriteKeyword("new");
			_formatter.Write(" ");
			_formatter.WriteReference("CustomAttributeBuilder", "", target5);
			_formatter.Write("(");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			string value = (string)operandLocals[attribute.Constructor];
			_formatter.Write(value);
			_formatter.Write(",");
			_formatter.WriteLine();
			ArrayList arrayList = new ArrayList();
			Hashtable hashtable = new Hashtable();
			Hashtable hashtable2 = new Hashtable();
			foreach (IExpression obj in attribute.Arguments)
			{
				IExpression expression = (IExpression)obj;
			    if (expression is IMemberInitializerExpression memberInitializerExpression)
				{
					if (memberInitializerExpression.Member is IFieldReference)
					{
						hashtable.Add(memberInitializerExpression.Member, memberInitializerExpression);
					}
					else
					{
						if (!(memberInitializerExpression.Member is IPropertyReference))
						{
							throw new NotSupportedException(memberInitializerExpression.Member.ToString());
						}
						hashtable2.Add(memberInitializerExpression.Member, memberInitializerExpression);
					}
				}
				else
				{
					arrayList.Add(expression);
				}
			}
			_formatter.WriteKeyword("new");
			_formatter.Write(" ");
			_formatter.WriteReference("Type", "", target);
			_formatter.Write("[]{");
			_formatter.WriteIndent();
			for (int i = 0; i < arrayList.Count; i++)
			{
				if (i != 0)
				{
					_formatter.Write(",");
				}
				_formatter.WriteLine();
				IExpression expression2 = arrayList[i] as IExpression;
				GenerateCustomAttributeArgumentExpression(expression2);
			}
			if (arrayList.Count > 0)
			{
				_formatter.WriteLine();
			}
			_formatter.Write("}");
			if (hashtable2.Count > 0 || hashtable.Count > 0)
			{
				_formatter.Write(",");
			}
			_formatter.WriteOutdent();
			_formatter.WriteLine();
			if (hashtable2.Count > 0)
			{
				_formatter.WriteComment("properties");
				_formatter.WriteKeyword("new");
				_formatter.Write(" ");
				_formatter.WriteReference("PropertyInfo", "", target2);
				_formatter.Write("[]{");
				_formatter.WriteLine();
				_formatter.WriteIndent();
				foreach (object obj2 in hashtable2.Keys)
				{
					IPropertyReference key = (IPropertyReference)obj2;
					string value2 = (string)operandLocals[key];
					_formatter.Write(value2);
					_formatter.Write(",");
					_formatter.WriteLine();
				}
				_formatter.Write("},");
				_formatter.WriteOutdent();
				_formatter.WriteLine();
				_formatter.WriteKeyword("new");
				_formatter.Write(" ");
				_formatter.WriteReference("Object", "", target4);
				_formatter.Write("[]{");
				_formatter.WriteLine();
				_formatter.WriteIndent();
				int num = 0;
				foreach (object obj3 in hashtable2.Keys)
				{
					IPropertyReference key2 = (IPropertyReference)obj3;
					if (num != 0)
					{
						_formatter.Write(",");
						_formatter.WriteLine();
					}
					IMemberInitializerExpression memberInitializerExpression2 = (IMemberInitializerExpression)hashtable2[key2];
					GenerateCustomAttributeArgumentExpression(memberInitializerExpression2.Value);
					num++;
				}
				_formatter.WriteLine();
				_formatter.Write("}");
				if (hashtable.Count > 0)
				{
					_formatter.Write(",");
				}
				_formatter.WriteOutdent();
				_formatter.WriteLine();
			}
			if (hashtable.Count > 0)
			{
				_formatter.WriteComment("fields");
				_formatter.WriteKeyword("new");
				_formatter.Write(" ");
				_formatter.WriteReference("FieldInfo", "", target3);
				_formatter.Write("[]{");
				_formatter.WriteLine();
				_formatter.WriteIndent();
				foreach (object obj4 in hashtable.Keys)
				{
					IFieldReference key3 = (IFieldReference)obj4;
					string value3 = (string)operandLocals[key3];
					_formatter.Write(value3);
					_formatter.Write(",");
					_formatter.WriteLine();
				}
				_formatter.Write("},");
				_formatter.WriteOutdent();
				_formatter.WriteLine();
				_formatter.WriteKeyword("new");
				_formatter.Write(" ");
				_formatter.WriteReference("Object", "", target4);
				_formatter.Write("[]{");
				_formatter.WriteLine();
				_formatter.WriteIndent();
				int num2 = 0;
				foreach (object obj5 in hashtable.Keys)
				{
					IFieldReference key4 = (IFieldReference)obj5;
					if (num2 != 0)
					{
						_formatter.Write(",");
						_formatter.WriteLine();
					}
					IMemberInitializerExpression memberInitializerExpression3 = (IMemberInitializerExpression)hashtable[key4];
					GenerateCustomAttributeArgumentExpression(memberInitializerExpression3.Value);
					num2++;
				}
				_formatter.WriteLine();
				_formatter.Write("}");
				_formatter.WriteOutdent();
				_formatter.WriteLine();
			}
			_formatter.Write(")");
			_formatter.WriteLine();
			_formatter.WriteOutdent();
			_formatter.Write(");");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
		}

		private void GenerateCustomAttributeArgumentExpression(IExpression expression)
		{
		    if (expression is ILiteralExpression literalExpression)
			{
				if (literalExpression.Value is string)
				{
					_formatter.WriteLiteral(literalExpression.Value.ToString());
				}
				else
				{
					_formatter.Write(literalExpression.Value.ToString());
				}
			}
			else
			{
			    if (expression is ITypeOfExpression typeOfExpression)
				{
					WriteTypeOf(typeOfExpression.Type);
				}
				else
				{
					_formatter.Write(expression.ToString());
				}
			}
		}

		private void GenerateDefineMethod(IMethodDeclaration value)
		{
			_formatter.WriteComment("Declaring method builder");
			ITypeDeclaration target = search.FindType(typeof(MethodBuilder));
			ITypeDeclaration value2 = search.FindType(typeof(TypeBuilder));
			IMethodDeclaration method = Helper.GetMethod(value2, "DefineMethod");
			GenerateMethodAttributesLocal(value);
			_formatter.WriteReference("MethodBuilder", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("method");
			_formatter.Write(" = ");
			_formatter.Write("type");
			_formatter.Write(".");
			_formatter.WriteReference("DefineMethod", "", method);
			_formatter.Write("(");
			_formatter.WriteLiteral(value.Name);
			_formatter.Write(", ");
			_formatter.Write("methodAttributes");
			_formatter.Write(");");
			_formatter.WriteLine();
		}

		private void GenerateGetIlGenerator()
		{
			ITypeDeclaration target = search.FindType(typeof(ILGenerator));
			ITypeDeclaration value = search.FindType(typeof(MethodBuilder));
			IMethodDeclaration method = Helper.GetMethod(value, "GetILGenerator");
			_formatter.WriteReference("ILGenerator", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("gen");
			_formatter.Write(" =  method.");
			_formatter.WriteReference("GetILGenerator", "", method);
			_formatter.Write("();");
			_formatter.WriteLine();
		}

		private Hashtable BuildEHOffsetTable(IMethodBody body)
		{
			Hashtable hashtable = new Hashtable();
			Hashtable hashtable2 = new Hashtable();
			foreach (object obj in body.ExceptionHandlers)
			{
				IExceptionHandler exceptionHandler = (IExceptionHandler)obj;
				hashtable[exceptionHandler.TryOffset] = exceptionHandler;
				if (exceptionHandler.Type == ExceptionHandlerType.Filter)
				{
					hashtable[exceptionHandler.FilterOffset] = exceptionHandler;
				}
				else
				{
					hashtable[exceptionHandler.HandlerOffset] = exceptionHandler;
				}
				IExceptionHandler exceptionHandler2 = hashtable2[exceptionHandler.TryOffset] as IExceptionHandler;
				if (exceptionHandler2 == null)
				{
					hashtable2[exceptionHandler.TryOffset] = exceptionHandler;
				}
				else if (exceptionHandler2.HandlerOffset + exceptionHandler2.HandlerLength < exceptionHandler.HandlerOffset + exceptionHandler.HandlerLength)
				{
					hashtable2[exceptionHandler.TryOffset] = exceptionHandler;
				}
			}
			foreach (object obj2 in hashtable2.Values)
			{
				IExceptionHandler exceptionHandler3 = (IExceptionHandler)obj2;
				hashtable[exceptionHandler3.HandlerOffset + exceptionHandler3.HandlerLength] = exceptionHandler3;
			}
			return hashtable;
		}

		private void GenerateBody(IMethodBody body, Hashtable labels)
		{
			ITypeDeclaration typeDeclaration = search.FindType(typeof(OpCodes));
			ITypeDeclaration value = search.FindType(typeof(ILGenerator));
			IMethodDeclaration method = Helper.GetMethod(value, "MarkLabel");
			IMethodDeclaration method2 = Helper.GetMethod(value, "BeginExceptionBlock");
			IMethodDeclaration method3 = Helper.GetMethod(value, "BeginCatchBlock");
			IMethodDeclaration method4 = Helper.GetMethod(value, "BeginFinallyBlock");
			IMethodDeclaration method5 = Helper.GetMethod(value, "BeginFaultBlock");
			IMethodDeclaration method6 = Helper.GetMethod(value, "EndExceptionBlock");
			IMethodDeclaration method7 = Helper.GetMethod(value, "BeginExceptFilterBlock");
			Hashtable hashtable = BuildOpCodeTable(typeDeclaration);
			Hashtable hashtable2 = BuildEHOffsetTable(body);
			_formatter.WriteComment("Writing body");
			foreach (object obj in body.Instructions)
			{
				IInstruction instruction = (IInstruction)obj;
			    if (hashtable2[instruction.Offset] is IExceptionHandler exceptionHandler)
				{
					if (exceptionHandler.TryOffset == instruction.Offset)
					{
						_formatter.Write("gen.");
						_formatter.WriteReference("BeginExceptionBlock", "", method2);
						_formatter.Write("();");
						_formatter.WriteLine();
					}
					else
					{
						_formatter.Write("gen.");
						if (exceptionHandler.Type == ExceptionHandlerType.Filter)
						{
							_formatter.Write("FILTERS NOT SUPPORTED");
						}
						else if (instruction.Offset == exceptionHandler.HandlerOffset)
						{
							switch (exceptionHandler.Type)
							{
							case ExceptionHandlerType.Finally:
								_formatter.WriteReference("BeginFinallyBlock", "", method4);
								_formatter.Write("();");
								break;
							case ExceptionHandlerType.Catch:
								_formatter.WriteReference("BeginCatchBlock", "", method3);
								_formatter.Write("(");
								WriteTypeOf(exceptionHandler.CatchType);
								_formatter.Write(");");
								break;
							case ExceptionHandlerType.Filter:
								_formatter.WriteReference("BeginExceptFilterBlock", "", method7);
								_formatter.Write("();");
								break;
							case ExceptionHandlerType.Fault:
								_formatter.WriteReference("BeginFaultBlock", "", method5);
								_formatter.Write("();");
								break;
							default:
								throw new InvalidOperationException();
							}
						}
						else
						{
							_formatter.WriteReference("EndExceptionBlock", "", method6);
							_formatter.Write("();");
						}
						_formatter.WriteLine();
					}
				}
				string text = (string)labels[instruction.Offset];
				if (text != null)
				{
					_formatter.Write("gen.");
					_formatter.WriteReference("MarkLabel", "", method);
					_formatter.Write("(" + text + ");");
					_formatter.WriteLine();
				}
				_formatter.Write("gen.Emit(");
				_formatter.WriteReference("OpCodes", "OpCodes", typeDeclaration);
				_formatter.Write(".");
				string opCodeFieldName = InstructionHelper.GetOpCodeFieldName(instruction.Code);
				_formatter.WriteReference(opCodeFieldName, InstructionHelper.GetInstructionName(instruction.Code), hashtable[opCodeFieldName]);
				if (instruction.Value != null)
				{
					_formatter.Write(",");
					string text2 = (string)operandLocals[instruction.Value];
					if (InstructionHelper.GetOperandType(instruction.Code) == OperandType.ShortBranchTarget || InstructionHelper.GetOperandType(instruction.Code) == OperandType.BranchTarget)
					{
						string value2 = (string)labels[instruction.Value];
						_formatter.Write(value2);
					}
					else if (text2 != null)
					{
						_formatter.Write(text2);
					}
					else if (instruction.Value is string)
					{
						_formatter.WriteLiteral(instruction.Value.ToString());
					}
					else
					{
						_formatter.Write(instruction.Value.ToString());
					}
				}
				_formatter.Write(");");
				_formatter.WriteLine();
			}
		}

		private Hashtable PrepareLabels(IMethodBody body)
		{
			ITypeDeclaration target = search.FindType(typeof(Label));
			ITypeDeclaration value = search.FindType(typeof(ILGenerator));
			IMethodDeclaration method = Helper.GetMethod(value, "DefineLabel");
			Hashtable hashtable = new Hashtable();
			foreach (object obj in GetLabels(body))
			{
				int num = (int)obj;
				if (!hashtable.ContainsKey(num))
				{
					if (hashtable.Count == 0)
					{
						_formatter.WriteComment("Preparing labels");
					}
					string value2 = $"label{num}";
					_formatter.WriteReference("Label", "", target);
					_formatter.Write(" ");
					_formatter.WriteDeclaration(value2);
					_formatter.Write(" =  gen.");
					_formatter.WriteReference("DefineLabel", "", method);
					_formatter.Write("();");
					_formatter.WriteLine();
					hashtable.Add(num, value2);
				}
			}
			return hashtable;
		}

		private void GenerateLocals(IMethodBody body)
		{
			if (body.LocalVariables.Count != 0)
			{
				ITypeDeclaration value = search.FindType(typeof(ILGenerator));
				IMethodDeclaration method = Helper.GetMethod(value, "DeclareLocal");
				ITypeDeclaration target = search.FindType(typeof(LocalBuilder));
				_formatter.WriteComment("Preparing locals");
				foreach (object obj in body.LocalVariables)
				{
					IVariableDeclaration variableDeclaration = (IVariableDeclaration)obj;
					_formatter.WriteReference("LocalBuilder", "", target);
					_formatter.Write(" ");
					_formatter.WriteDeclaration(variableDeclaration.Name);
					_formatter.Write(" =  gen.");
					_formatter.WriteReference("DeclareLocal", "", method);
					_formatter.Write("(");
					WriteTypeOf(variableDeclaration.VariableType);
					_formatter.Write(");");
					_formatter.WriteLine();
				}
			}
		}

		private void GenerateParameters(IMethodDeclaration value)
		{
			ITypeDeclaration typeDeclaration = search.FindType(typeof(ParameterBuilder));
			ITypeDeclaration value2 = search.FindType(typeof(MethodBuilder));
			IMethodDeclaration method = Helper.GetMethod(value2, "DefineParameter");
			IMethodDeclaration method2 = Helper.GetMethod(value2, "SetParameters");
			IMethodDeclaration method3 = Helper.GetMethod(value2, "SetReturnType");
			ITypeDeclaration typeDeclaration2 = search.FindType(typeof(ParameterAttributes));
			IFieldDeclaration target = search.FindField(typeDeclaration2, "None");
			IFieldDeclaration target2 = search.FindField(typeDeclaration2, "RetVal");
			_formatter.WriteComment("Setting return type");
			_formatter.Write("method");
			_formatter.Write(".");
			_formatter.WriteReference("SetReturnType", "", method3);
			_formatter.Write("(");
			WriteTypeOf(value.ReturnType.Type);
			_formatter.Write(");");
			_formatter.WriteLine();
			if (value.ReturnType.Attributes.Count > 0)
			{
				_formatter.WriteComment("return value");
				_formatter.WriteReference("ParameterBuilder", "", typeDeclaration);
				_formatter.Write(" ");
				_formatter.WriteDeclaration("returnValue");
				_formatter.Write(" =  method.");
				_formatter.WriteReference("DefineParameter", "", method);
				_formatter.Write("(0, ");
				_formatter.WriteReference("ParameterAttributes", "", typeDeclaration2);
				_formatter.Write(".");
				_formatter.WriteReference("RetVal", "", target2);
				_formatter.Write(", ");
				_formatter.WriteLiteral("");
				_formatter.Write(");");
				_formatter.WriteLine();
				GenerateCustomAttributes(value.ReturnType, typeDeclaration, "returnValue");
			}
			_formatter.WriteComment("Adding parameters");
			if (value.Parameters.Count != 0)
			{
				_formatter.Write("method.");
				_formatter.WriteReference("SetParameters", "", method2);
				_formatter.Write("(");
				_formatter.WriteIndent();
				for (int i = 0; i < value.Parameters.Count; i++)
				{
					if (i != 0)
					{
						_formatter.Write(",");
					}
					_formatter.WriteLine();
					IParameterDeclaration parameterDeclaration = value.Parameters[i];
					WriteTypeOf(parameterDeclaration.ParameterType);
				}
				_formatter.WriteLine();
				_formatter.Write(");");
				_formatter.WriteOutdent();
				_formatter.WriteLine();
			}
			int num = 1;
			foreach (object obj in value.Parameters)
			{
				IParameterDeclaration parameterDeclaration2 = (IParameterDeclaration)obj;
				_formatter.WriteComment("Parameter " + parameterDeclaration2.Name);
				_formatter.WriteReference("ParameterBuilder", "", typeDeclaration);
				_formatter.Write(" ");
				_formatter.WriteDeclaration(parameterDeclaration2.Name);
				_formatter.Write(" =  method.");
				_formatter.WriteReference("DefineParameter", "", method);
				_formatter.Write("(");
				_formatter.Write(num.ToString() + ", ");
				_formatter.WriteReference("ParameterAttributes", "", typeDeclaration2);
				_formatter.Write(".");
				_formatter.WriteReference("None", "", target);
				_formatter.Write(", ");
				_formatter.WriteLiteral(parameterDeclaration2.Name);
				_formatter.Write(");");
				_formatter.WriteLine();
				GenerateCustomAttributes(parameterDeclaration2, typeDeclaration, parameterDeclaration2.Name);
				num++;
			}
		}

		private void DeclareMethod(IMethodDeclaration value)
		{
			ITypeDeclaration target = search.FindType(typeof(MethodBuilder));
			ITypeDeclaration target2 = search.FindType(typeof(TypeBuilder));
			_formatter.WriteKeyword("public");
			_formatter.Write(" ");
			_formatter.WriteReference("MethodBuilder", "", target);
			_formatter.Write(" BuildMethod");
			_formatter.Write(value.Name);
			_formatter.Write("(");
			_formatter.WriteReference("TypeBuilder", "", target2);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("type");
			_formatter.Write(")");
			_formatter.WriteLine();
		}

		private void WriteTypeOf(IType type)
		{
		    if (type is ITypeReference typeReference && typeReference.GenericType != null)
			{
				ITypeDeclaration value = search.FindType(typeof(Type));
				IMethodDeclaration method = Helper.GetMethod(value, "MakeGenericType");
				WriteTypeOf(typeReference.GenericType);
				_formatter.Write(".");
				_formatter.WriteReference("MakeGenericType", "", method);
				_formatter.Write("(");
				for (int i = 0; i < typeReference.GenericArguments.Count; i++)
				{
					if (i != 0)
					{
						_formatter.Write(", ");
					}
					IType type2 = typeReference.GenericArguments[i];
					WriteTypeOf(type2);
				}
				_formatter.Write(")");
			}
			else
			{
			    if (type is IGenericArgument genericArgument)
				{
					_formatter.Write("genericParameters[" + genericArgument.Position.ToString() + "]");
				}
				else
				{
					_formatter.WriteKeyword("typeof");
					_formatter.Write("(");
				    if (type is ITypeReference typeReference2)
					{
						if (typeReference2.GenericArguments.Count != 0)
						{
							_formatter.WriteReference($"{typeReference2.Namespace}.{typeReference2.Name}<>", "", type);
						}
						else
						{
							_formatter.WriteReference(type.ToString(), "", type);
						}
					}
					else
					{
						_formatter.WriteReference($"{type}", "", type);
					}
					_formatter.Write(")");
				}
			}
		}

		private void GenerateOperandLocals(IMethodDeclaration value)
		{
			_formatter.WriteComment("Preparing Reflection instances");
			operandLocals = new Hashtable();
			int i = 1;
			foreach (object obj in value.Attributes)
			{
				ICustomAttribute attribute = (ICustomAttribute)obj;
				i = DeclareCustomAttributeConstructor(i, attribute);
			}
			foreach (object obj2 in value.Parameters)
			{
				IParameterDeclaration parameterDeclaration = (IParameterDeclaration)obj2;
				foreach (object obj3 in parameterDeclaration.Attributes)
				{
					ICustomAttribute attribute2 = (ICustomAttribute)obj3;
					i = DeclareCustomAttributeConstructor(i, attribute2);
				}
			}
			foreach (object obj4 in value.ReturnType.Attributes)
			{
				ICustomAttribute attribute3 = (ICustomAttribute)obj4;
				i = DeclareCustomAttributeConstructor(i, attribute3);
			}

		    if (value.Body is IMethodBody methodBody)
			{
				foreach (object obj5 in methodBody.Instructions)
				{
					IInstruction instruction = (IInstruction)obj5;
				    if (instruction.Value is IMethodReference methodReference)
					{
						if (!operandLocals.ContainsKey(methodReference))
						{
							if (methodReference.Name == ".ctor")
							{
								string text = $"{methodReference.Name.TrimStart(new char[] {'.'})}{i++}";
								operandLocals.Add(methodReference, text);
								DeclareConstructorInfoLocal(methodReference, text);
							}
							else
							{
								string text2 = $"method{i++}";
								operandLocals.Add(methodReference, text2);
								DeclareMethodInfoLocal(methodReference, text2);
							}
						}
					}
					else
					{
					    if (instruction.Value is IFieldReference fieldReference)
						{
							if (!operandLocals.ContainsKey(fieldReference))
							{
								string text3 = $"field{i++}";
								operandLocals.Add(fieldReference, text3);
								DeclareFieldInfoLocal(fieldReference, text3);
							}
						}
						else
						{
						    if (instruction.Value is IEventReference eventReference)
							{
								string text4 = string.Format("eventt{0}", new object[0]);
								_formatter.Write("EVENT NOT SUPPORTED");
							}
						}
					}
				}
			}
		}

		private int DeclareCustomAttributeConstructor(int i, ICustomAttribute attribute)
		{
			if (!operandLocals.ContainsKey(attribute.Constructor))
			{
				string text = $"{attribute.Constructor.Name.TrimStart('.')}{i++}";
				operandLocals.Add(attribute.Constructor, text);
				DeclareConstructorInfoLocal(attribute.Constructor, text);
			}
			foreach (IExpression expression in attribute.Arguments)
			{
			    if (expression is IMemberInitializerExpression memberInitializerExpression && !operandLocals.ContainsKey(memberInitializerExpression.Member))
				{
				    if (memberInitializerExpression.Member is IFieldReference fieldReference)
					{
						string text2 = $"field{i++}";
						operandLocals.Add(fieldReference, text2);
						DeclareFieldInfoLocal(fieldReference, text2);
					}

				    if (memberInitializerExpression.Member is IPropertyReference propertyReference)
					{
						string text3 = $"property{i++}";
						operandLocals.Add(propertyReference, text3);
						DeclarePropertyInfoLocal(propertyReference, text3);
					}
				}
			}
			return i;
		}

		private void DeclarePropertyInfoLocal(IPropertyReference property, string name)
		{
			ITypeDeclaration target = search.FindType(typeof(PropertyInfo));
			ITypeDeclaration typeDeclaration = search.FindType(typeof(BindingFlags));
			IFieldDeclaration target2 = search.FindField(typeDeclaration, "Public");
			IFieldDeclaration target3 = search.FindField(typeDeclaration, "NonPublic");
			_formatter.WriteReference("PropertyInfo", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration(name);
			_formatter.Write(" = ");
			WriteTypeOf(property.DeclaringType);
			_formatter.Write(".");
			_formatter.WriteReference("GetProperty", "", null);
			_formatter.Write("(");
			_formatter.WriteLiteral(property.Name);
			_formatter.Write(", ");
			_formatter.WriteReference("BindingFlags", "", typeDeclaration);
			_formatter.Write(".");
			_formatter.WriteReference("Public", "", target2);
			_formatter.Write(" | ");
			_formatter.WriteReference("BindingFlags", "", typeDeclaration);
			_formatter.Write(".");
			_formatter.WriteReference("NonPublic", "", target3);
			_formatter.Write(", ");
			_formatter.WriteKeyword("null");
			_formatter.Write(", ");
			WriteTypeOf(property.PropertyType);
			_formatter.Write(", ");
			_formatter.WriteKeyword("null");
			_formatter.Write(", ");
			_formatter.WriteKeyword("null");
			_formatter.Write(");");
			_formatter.WriteLine();
		}

		private void DeclareFieldInfoLocal(IFieldReference field, string name)
		{
			ITypeDeclaration target = search.FindType(typeof(FieldInfo));
			ITypeDeclaration typeDeclaration = search.FindType(typeof(BindingFlags));
			IFieldDeclaration target2 = search.FindField(typeDeclaration, "Public");
			IFieldDeclaration target3 = search.FindField(typeDeclaration, "NonPublic");
			_formatter.WriteReference("FieldInfo", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration(name);
			_formatter.Write(" = ");
			WriteTypeOf(field.DeclaringType);
			_formatter.Write(".");
			_formatter.WriteReference("GetField", "", null);
			_formatter.Write("(");
			_formatter.WriteLiteral(field.Name);
			_formatter.Write(", ");
			_formatter.WriteReference("BindingFlags", "", typeDeclaration);
			_formatter.Write(".");
			_formatter.WriteReference("Public", "", target2);
			_formatter.Write(" | ");
			_formatter.WriteReference("BindingFlags", "", typeDeclaration);
			_formatter.Write(".");
			_formatter.WriteReference("NonPublic", "", target3);
			_formatter.Write(");");
			_formatter.WriteLine();
		}

		private void DeclareMethodInfoLocal(IMethodReference method, string name)
		{
			ITypeDeclaration target = search.FindType(typeof(MethodInfo));
			_formatter.WriteReference("MethodInfo", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration(name);
			_formatter.Write(" = ");
			WriteTypeOf(method.DeclaringType);
			_formatter.Write(".");
			_formatter.WriteReference("GetMethod", "", null);
			_formatter.Write("(");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			_formatter.WriteLiteral(method.Name);
			_formatter.Write(", ");
			_formatter.WriteLine();
			GenerateMethodSignature(method);
			_formatter.Write(");");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
		}

		private void GenerateMethodSignature(IMethodReference method)
		{
			ITypeDeclaration target = search.FindType(typeof(Type));
			ITypeDeclaration typeDeclaration = search.FindType(typeof(BindingFlags));
			IFieldDeclaration target2 = search.FindField(typeDeclaration, "Public");
			IFieldDeclaration target3 = search.FindField(typeDeclaration, "NonPublic");
			IFieldDeclaration target4 = search.FindField(typeDeclaration, "Static");
			IFieldDeclaration target5 = search.FindField(typeDeclaration, "Instance");
			_formatter.WriteReference("BindingFlags", "", typeDeclaration);
			_formatter.Write(".");
			if (method.HasThis)
			{
				_formatter.WriteReference("Instance", "", target5);
			}
			else
			{
				_formatter.WriteReference("Static", "", target4);
			}
			_formatter.Write(" | ");
			_formatter.WriteReference("BindingFlags", "", typeDeclaration);
			_formatter.Write(".");
			_formatter.WriteReference("Public", "", target2);
			_formatter.Write(" | ");
			_formatter.WriteReference("BindingFlags", "", typeDeclaration);
			_formatter.Write(".");
			_formatter.WriteReference("NonPublic", "", target3);
			_formatter.Write(", ");
			_formatter.WriteLine();
			_formatter.WriteKeyword("null");
			_formatter.Write(", ");
			_formatter.WriteLine();
			_formatter.WriteKeyword("new");
			_formatter.Write(" ");
			_formatter.WriteReference("Type", "", target);
			_formatter.Write("[]{");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			for (int i = 0; i < method.Parameters.Count; i++)
			{
				IParameterDeclaration parameterDeclaration = method.Parameters[i];
				WriteTypeOf(parameterDeclaration.ParameterType);
				if (i + 1 != method.Parameters.Count)
				{
					_formatter.Write(",");
				}
				_formatter.WriteLine();
			}
			_formatter.Write("}, ");
			_formatter.WriteLine();
			_formatter.WriteOutdent();
			_formatter.WriteKeyword("null");
			_formatter.WriteLine();
		}

		private void DeclareConstructorInfoLocal(IMethodReference constructor, string name)
		{
			ITypeDeclaration target = search.FindType(typeof(ConstructorInfo));
			_formatter.WriteReference("ConstructorInfo", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration(name);
			_formatter.Write(" = ");
			WriteTypeOf(constructor.DeclaringType);
			_formatter.Write(".");
			_formatter.WriteReference("GetConstructor", "", null);
			_formatter.Write("(");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			GenerateMethodSignature(constructor);
			_formatter.Write(");");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
		}

		private ArrayList GetLabels(IMethodBody body)
		{
			ArrayList arrayList = new ArrayList();
			foreach (object obj in body.Instructions)
			{
				IInstruction instruction = (IInstruction)obj;
				if (InstructionHelper.GetOperandType(instruction.Code) == OperandType.BranchTarget || InstructionHelper.GetOperandType(instruction.Code) == OperandType.ShortBranchTarget)
				{
					arrayList.Add(instruction.Value);
				}
			}
			return arrayList;
		}

		private Hashtable BuildOpCodeTable(ITypeDeclaration opCodes)
		{
			Hashtable hashtable = new Hashtable(opCodes.Fields.Count);
			foreach (object obj in opCodes.Fields)
			{
				IFieldDeclaration fieldDeclaration = (IFieldDeclaration)obj;
				string name = fieldDeclaration.Name;
				hashtable.Add(name, fieldDeclaration);
			}
			return hashtable;
		}

		public void WriteModule(IModule value)
		{
			ITypeDeclaration typeDeclaration = search.FindType(typeof(AssemblyBuilder));
			ITypeDeclaration target = search.FindType(typeof(ModuleBuilder));
			IMethodDeclaration method = Helper.GetMethod(typeDeclaration, "DefineDynamicModule");
			_formatter.WriteKeyword("public");
			_formatter.Write(" ");
			_formatter.WriteReference("ModuleBuilder", "", target);
			_formatter.Write(" BuildModule" + value.Name.Replace('.', '_'));
			_formatter.Write("(");
			_formatter.WriteReference("AssemblyBuilder", "", typeDeclaration);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("assembly");
			_formatter.Write(")");
			_formatter.WriteLine();
			_formatter.Write("{");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			_formatter.WriteReference("ModuleBuilder", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("module");
			_formatter.Write(" = assembly.");
			_formatter.WriteReference("DefineDynamicModule", "", method);
			_formatter.Write("(");
			_formatter.WriteLiteral(value.Name);
			_formatter.Write(");");
			_formatter.WriteLine();
			_formatter.WriteKeyword("return");
			_formatter.Write(" ");
			_formatter.Write("module;");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
			_formatter.Write("}");
			_formatter.WriteLine();
		}

		public void WriteModuleReference(IModuleReference value)
		{
			_formatter.Write("Not supported by the Reflection.Emit language.");
		}

		public void WriteNamespace(INamespace value)
		{
			_formatter.Write("Not supported by the Reflection.Emit language.");
		}

		public void WritePropertyDeclaration(IPropertyDeclaration value)
		{
			_formatter.Write("Not supported by the Reflection.Emit language.");
		}

		public void WriteResource(IResource value)
		{
			_formatter.Write("Not supported by the Reflection.Emit language.");
		}

		public void WriteStatement(IStatement value)
		{
			_formatter.Write("Not supported by the Reflection.Emit language.");
		}

		public void WriteTypeDeclaration(ITypeDeclaration value)
		{
			ITypeDeclaration typeDeclaration = search.FindType(typeof(ModuleBuilder));
			ITypeDeclaration target = search.FindType(typeof(TypeBuilder));
			IMethodDeclaration method = Helper.GetMethod(typeDeclaration, "DefineType");
			ITypeDeclaration typeDeclaration2 = search.FindType(typeof(TypeAttributes));
			_formatter.WriteKeyword("public");
			_formatter.Write(" ");
			_formatter.WriteReference("TypeBuilder", "", target);
			_formatter.Write(" Build" + Helper.GetNameWithResolutionScope(value).Replace('.', '_'));
			_formatter.Write("(");
			_formatter.WriteReference("ModuleBuilder", "", typeDeclaration);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("module");
			_formatter.Write(")");
			_formatter.WriteLine();
			_formatter.Write("{");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			_formatter.WriteReference("TypeBuilder", "", target);
			_formatter.Write(" ");
			_formatter.WriteDeclaration("type");
			_formatter.Write(" = module.");
			_formatter.WriteReference("DefineType", "", method);
			_formatter.Write("(");
			_formatter.WriteLine();
			_formatter.WriteIndent();
			_formatter.WriteLiteral(Helper.GetNameWithResolutionScope(value));
			_formatter.Write(", ");
			_formatter.WriteLine();
			_formatter.WriteReference("TypeAttributes", "", typeDeclaration2);
			_formatter.Write(".");
			_formatter.WriteReference(value.Visibility.ToString(), "", search.FindField(typeDeclaration2, value.Visibility.ToString()));
			_formatter.Write(", ");
			_formatter.WriteLine();
			WriteTypeOf(value.BaseType);
			_formatter.Write(", ");
			_formatter.WriteLine();
			_formatter.WriteKeyword("new");
			_formatter.WriteReference("Type", "", search.FindType(typeof(Type)));
			_formatter.Write("[]{");
			_formatter.WriteIndent();
			for (int i = 0; i < value.Interfaces.Count; i++)
			{
				if (i != 0)
				{
					_formatter.Write(", ");
				}
				_formatter.WriteLine();
				WriteTypeOf(value.Interfaces[i]);
			}
			_formatter.WriteLine();
			_formatter.Write("}");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
			_formatter.Write(");");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
			_formatter.WriteKeyword("return");
			_formatter.Write(" type;");
			_formatter.WriteOutdent();
			_formatter.WriteLine();
			_formatter.Write("}");
		}

		private const string methodAttributesLocalName = "methodAttributes";

		private readonly IFormatter _formatter;

		private readonly ILanguageWriterConfiguration _configuration;

		private readonly CodeModelSearch search;

		private Hashtable operandLocals;
	}
}
