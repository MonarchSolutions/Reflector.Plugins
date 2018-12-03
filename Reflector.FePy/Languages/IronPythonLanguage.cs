using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Reflector.CodeModel;
using Reflector.CodeModel.Memory;
using Reflector.Disassembler;

namespace Reflector.FePy.Languages
{
    internal class IronPythonLanguage : ILanguage
    {
        public IronPythonLanguage()
        {
            addInMode = false;
        }

        public IronPythonLanguage(bool addInMode)
        {
            this.addInMode = addInMode;
        }

        public string Name
        {
            get
            {
                if (addInMode)
                {
                    return "IronPython";
                }
                return "IronPython";
            }
        }

        public string FileExtension => ".py";

        public bool Translate => true;

        public ILanguageWriter GetWriter(IFormatter formatter, ILanguageWriterConfiguration configuration)
        {
            return new LanguageWriter(formatter, configuration);
        }

        public Language LanguageType => Language.CSharpNet;

        private bool addInMode;

        internal class LanguageWriter : ILanguageWriter
        {
            public LanguageWriter(IFormatter formatter, ILanguageWriterConfiguration configuration)
            {
                this._formatter = formatter;
                this.configuration = configuration;
                if (specialTypeNames == null)
                {
                    specialTypeNames = new Hashtable
                    {
                        ["Void"] = " None",
                        ["Object"] = "Object",
                        ["String"] = "String",
                        ["SByte"] = "SByte",
                        ["Byte"] = "Byte",
                        ["Int16"] = "Int16",
                        ["UInt16"] = "UInt16",
                        ["Int32"] = "Int32",
                        ["UInt32"] = "UInt32",
                        ["Int64"] = "Int64",
                        ["UInt64"] = "UInt64",
                        ["Char"] = "Char",
                        ["Boolean"] = "Boolean",
                        ["Single"] = "Single",
                        ["Double"] = "Double",
                        ["Decimal"] = "Decimal"
                    };
                }
                if (specialMethodNames == null)
                {
                    specialMethodNames =
                        new Hashtable
                        {
                            ["op_UnaryPlus"] = "Positive",
                            ["op_Addition"] = "Add",
                            ["op_Increment"] = "Inc",
                            ["op_UnaryNegation"] = "Negative",
                            ["op_Subtraction"] = "Subtract",
                            ["op_Decrement"] = "Dec",
                            ["op_Multiply"] = "Multiply",
                            ["op_Division"] = "Divide",
                            ["op_Modulus"] = "Modulus",
                            ["op_BitwiseAnd"] = "BitwiseAnd",
                            ["op_BitwiseOr"] = "BitwiseOr",
                            ["op_ExclusiveOr"] = "BitwiseXor",
                            ["op_Negation"] = "LogicalNot",
                            ["op_OnesComplement"] = "BitwiseNot",
                            ["op_LeftShift"] = "ShiftLeft",
                            ["op_RightShift"] = "ShiftRight",
                            ["op_Equality"] = "Equal",
                            ["op_Inequality"] = "NotEqual",
                            ["op_GreaterThanOrEqual"] = "GreaterThanOrEqual",
                            ["op_LessThanOrEqual"] = "LessThanOrEqual",
                            ["op_GreaterThan"] = "GreaterThan",
                            ["op_LessThan"] = "LessThan",
                            ["op_True"] = "True",
                            ["op_False"] = "False",
                            ["op_Implicit"] = "Implicit",
                            ["op_Explicit"] = "Explicit"
                        };
                }
                string a;
                if ((a = configuration["NumberFormat"]) != null)
                {
                    if (a == "Hexadecimal")
                    {
                        _numberFormat = NumberFormat.Hexadecimal;
                        return;
                    }
                    if (a == "Decimal")
                    {
                        _numberFormat = NumberFormat.Decimal;
                        return;
                    }
                }
                _numberFormat = NumberFormat.Auto;
            }

            public void WriteAssembly(IAssembly value)
            {
                _formatter.Write("# Assembly");
                _formatter.WriteLine();
                _formatter.WriteDeclaration(value.Name);
                if (value.Version != null)
                {
                    _formatter.Write(", ");
                    _formatter.Write("Version");
                    _formatter.Write(" ");
                    _formatter.Write(value.Version.ToString());
                }
                _formatter.WriteLine();
                if (configuration["ShowCustomAttributes"] == "true" && value.Attributes.Count != 0)
                {
                    _formatter.WriteLine();
                    WriteCustomAttributeList(value, _formatter);
                    _formatter.WriteLine();
                }
                _formatter.WriteProperty("Location", value.Location);
                _formatter.WriteProperty("Name", value.ToString());
                switch (value.Type)
                {
                    case AssemblyType.Console:
                        _formatter.WriteProperty("Type", "Console Application");
                        return;
                    case AssemblyType.Application:
                        _formatter.WriteProperty("Type", "Windows Application");
                        return;
                    case AssemblyType.Library:
                        _formatter.WriteProperty("Type", "Library");
                        return;
                    default:
                        return;
                }
            }

            public void WriteAssemblyReference(IAssemblyReference value)
            {
                _formatter.Write("# Assembly Reference");
                _formatter.Write(" ");
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteLine();
                _formatter.WriteProperty("Version", value.Version.ToString());
                _formatter.WriteProperty("Name", value.ToString());
            }

            public void WriteModule(IModule value)
            {
                _formatter.Write("# Module");
                _formatter.WriteLine();
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteLine();
                if (configuration["ShowCustomAttributes"] == "true" && value.Attributes.Count != 0)
                {
                    _formatter.WriteLine();
                    WriteCustomAttributeList(value, _formatter);
                    _formatter.WriteLine();
                }
                _formatter.WriteProperty("Version", value.Version.ToString());
                _formatter.WriteProperty("Location", value.Location);
                string text = Environment.ExpandEnvironmentVariables(value.Location);
                if (File.Exists(text))
                {
                    _formatter.WriteProperty("Size", new FileInfo(text).Length + " Bytes");
                }
            }

            public void WriteModuleReference(IModuleReference value)
            {
                _formatter.Write("# Module Reference");
                _formatter.WriteLine();
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteLine();
            }

            public void WriteResource(IResource value)
            {
                _formatter.WriteDeclaration(value.Name, value);
                _formatter.WriteLine();
                IEmbeddedResource embeddedResource = value as IEmbeddedResource;
                if (embeddedResource != null && embeddedResource.Value != null)
                {
                    _formatter.WriteProperty("Size", embeddedResource.Value.Length.ToString(CultureInfo.InvariantCulture) + " bytes");
                }
                IFileResource fileResource = value as IFileResource;
                if (fileResource != null)
                {
                    _formatter.WriteProperty("Location", fileResource.Location);
                }
            }

            public void WriteNamespace(INamespace value)
            {
                if (value.Name.Length != 0)
                {
                    _formatter.WriteKeyword(" from ");
                    WriteDeclaration(value.Name, _formatter);
                    _formatter.WriteKeyword(" import ");
                    _formatter.WriteLine();
                }
                if (configuration["ShowNamespaceBody"] == "true")
                {
                    _formatter.WriteLine();
                    ArrayList arrayList = new ArrayList();
                    foreach (object obj in value.Types)
                    {
                        ITypeDeclaration value2 = (ITypeDeclaration)obj;
                        if (Helper.IsVisible(value2, configuration.Visibility))
                        {
                            arrayList.Add(value2);
                        }
                    }
                    arrayList.Sort();
                    for (int i = 0; i < arrayList.Count; i++)
                    {
                        if (i != 0)
                        {
                            _formatter.WriteLine();
                        }
                        WriteTypeDeclaration((ITypeDeclaration)arrayList[i]);
                    }
                }
            }

            public void WriteTypeDeclaration(ITypeDeclaration value)
            {
                bool flag = false;
                bool flag2 = false;
                TypeVisibility visibility = value.Visibility;
                if (!Helper.IsEnumeration(value))
                {
                    if (visibility == TypeVisibility.Private)
                    {
                        _formatter.WriteComment("class ");
                        flag = true;
                    }
                    else
                    {
                        _formatter.WriteKeyword("class ");
                    }
                    flag2 = true;
                    WriteDeclaration(value.Name, value, _formatter);
                }
                if (Helper.IsEnumeration(value))
                {
                    bool flag3 = true;
                    WriteDeclaration(value.Name, value, _formatter);
                    _formatter.WriteComment(" #Enumeration ");
                    foreach (var obj in Helper.GetFields(value, configuration.Visibility))
                    {
                        IFieldDeclaration fieldDeclaration = (IFieldDeclaration)obj;
                        if (!fieldDeclaration.SpecialName || !fieldDeclaration.RuntimeSpecialName || fieldDeclaration.FieldType.Equals(value))
                        {
                            if (flag3 && configuration["ShowTypeDeclarationBody"] == "true")
                            {
                                _formatter.WriteIndent();
                            }
                            if (flag3)
                            {
                                _formatter.WriteLine();
                                flag3 = false;
                            }
                            else
                            {
                                _formatter.WriteLine();
                            }
                            WriteDeclaration(fieldDeclaration.Name, fieldDeclaration, _formatter);
                            IExpression initializer = fieldDeclaration.Initializer;
                            if (initializer != null)
                            {
                                _formatter.Write("=");
                                WriteExpression(initializer, _formatter);
                            }
                        }
                    }
                }
                bool flag4 = false;
                if (value.Interface)
                {
                    flag4 = true;
                    _formatter.Write("(");
                    WriteGenericArgumentList(value.GenericArguments, _formatter);
                }
                if (!Helper.IsValueType(value))
                {
                    WriteGenericArgumentList(value.GenericArguments, _formatter);
                    ITypeReference baseType = value.BaseType;
                    if (baseType != null && !IsType(baseType, "System", "Object"))
                    {
                        _formatter.Write("(");
                        WriteType(baseType, _formatter);
                        flag4 = true;
                    }
                }
                foreach (object obj2 in value.Interfaces)
                {
                    ITypeReference type = (ITypeReference)obj2;
                    _formatter.Write(flag4 ? ", " : " (");
                    WriteType(type, _formatter);
                    flag4 = true;
                }
                if (flag4)
                {
                    _formatter.Write("):");
                }
                if (!flag4 && flag2)
                {
                    _formatter.Write("():");
                }
                if (flag)
                {
                    _formatter.WriteComment(" #Private ");
                }
                if (value.Sealed)
                {
                    _formatter.WriteComment(" #Sealed ");
                }
                if (value.Abstract)
                {
                    _formatter.WriteComment(" #Abstract ");
                }
                if (configuration["ShowTypeDeclarationBody"] == "true" && !Helper.IsEnumeration(value) && !Helper.IsDelegate(value))
                {
                    _formatter.WriteLine();
                    _formatter.WriteIndent();
                    bool flag5 = false;
                    ICollection methods = Helper.GetMethods(value, configuration.Visibility);
                    if (methods.Count > 0)
                    {
                        if (flag5)
                        {
                            _formatter.WriteLine();
                        }
                        flag5 = true;
                        _formatter.WriteComment("# Methods");
                        _formatter.WriteLine();
                        foreach (object obj3 in methods)
                        {
                            IMethodDeclaration methodDeclaration = (IMethodDeclaration)obj3;
                            WriteMethodDeclaration(methodDeclaration);
                            if (methodDeclaration.Static)
                            {
                                _formatter.WriteComment(" #Static ");
                            }
                            _formatter.WriteLine();
                        }
                    }
                    ICollection properties = Helper.GetProperties(value, configuration.Visibility);
                    if (properties.Count > 0)
                    {
                        if (flag5)
                        {
                            _formatter.WriteLine();
                        }
                        flag5 = true;
                        _formatter.WriteComment("# Properties");
                        _formatter.WriteLine();
                        foreach (object obj4 in properties)
                        {
                            IPropertyDeclaration value2 = (IPropertyDeclaration)obj4;
                            WritePropertyDeclaration(value2);
                            _formatter.WriteLine();
                        }
                    }
                    ICollection events = Helper.GetEvents(value, configuration.Visibility);
                    if (events.Count > 0)
                    {
                        if (flag5)
                        {
                            _formatter.WriteLine();
                        }
                        flag5 = true;
                        _formatter.WriteComment("# Events");
                        _formatter.WriteLine();
                        foreach (object obj5 in events)
                        {
                            IEventDeclaration value3 = (IEventDeclaration)obj5;
                            WriteEventDeclaration(value3);
                            _formatter.WriteLine();
                        }
                    }
                    ICollection fields = Helper.GetFields(value, configuration.Visibility);
                    if (fields.Count > 0)
                    {
                        if (flag5)
                        {
                            _formatter.WriteLine();
                        }
                        flag5 = true;
                        _formatter.WriteComment("# Fields");
                        _formatter.WriteLine();
                        foreach (object obj6 in fields)
                        {
                            IFieldDeclaration fieldDeclaration2 = (IFieldDeclaration)obj6;
                            if (!fieldDeclaration2.SpecialName || fieldDeclaration2.Name != "value__")
                            {
                                WriteFieldDeclaration(fieldDeclaration2);
                                _formatter.WriteLine();
                            }
                        }
                    }
                    ICollection nestedTypes = Helper.GetNestedTypes(value, configuration.Visibility);
                    if (nestedTypes.Count > 0)
                    {
                        if (flag5)
                        {
                            _formatter.WriteLine();
                        }
                        flag5 = true;
                        _formatter.WriteComment("# Nested Types");
                        _formatter.WriteLine();
                        _formatter.WriteIndent();
                        foreach (object obj7 in nestedTypes)
                        {
                            ITypeDeclaration value4 = (ITypeDeclaration)obj7;
                            WriteTypeDeclaration(value4);
                            _formatter.WriteLine();
                        }
                        _formatter.WriteOutdent();
                    }
                    _formatter.WriteLine();
                    _formatter.WriteOutdent();
                }
            }

            public void WriteFieldDeclaration(IFieldDeclaration value)
            {
                if (configuration["ShowCustomAttributes"] == "true" && value.Attributes.Count != 0)
                {
                    WriteCustomAttributeList(value, _formatter);
                    _formatter.WriteLine();
                }
                if (!IsEnumerationElement(value))
                {
                    WriteDeclaration(value.Name, value, _formatter);
                    _formatter.Write(" <type ");
                    WriteType(value.FieldType, _formatter);
                    _formatter.Write("> ");
                }
                else
                {
                    WriteDeclaration(value.Name, value, _formatter);
                }
                byte[] array = null;
                IExpression initializer = value.Initializer;
                if (initializer != null)
                {
                    ILiteralExpression literalExpression = initializer as ILiteralExpression;
                    if (literalExpression != null && literalExpression.Value != null && literalExpression.Value is byte[])
                    {
                        array = (byte[])literalExpression.Value;
                    }
                    else
                    {
                        _formatter.Write(" = ");
                        WriteExpression(initializer, _formatter);
                    }
                }
                IsEnumerationElement(value);
                if (array != null)
                {
                    _formatter.WriteComment(" // data size: " + array.Length.ToString(CultureInfo.InvariantCulture) + " bytes");
                }
                WriteDeclaringType(value.DeclaringType as ITypeReference, _formatter);
            }

            public void WriteMethodDeclaration(IMethodDeclaration value)
            {
                string name = value.Name;
                bool flag = false;
                MethodVisibility visibility = value.Visibility;
                bool flag2;
                bool flag3;
                if (visibility == MethodVisibility.Private)
                {
                    _formatter.WriteComment("def ");
                    flag2 = true;
                    flag3 = true;
                }
                else
                {
                    _formatter.WriteKeyword("def ");
                    flag2 = true;
                    flag3 = false;
                }
                if (IsConstructor(value))
                {
                    _formatter.WriteDeclaration((value.DeclaringType as ITypeReference).Name, value);
                    flag = true;
                }
                else
                {
                    WriteDeclaration(name, value, _formatter);
                }
                WriteGenericArgumentList(value.GenericArguments, _formatter);
                if (value.Parameters.Count > 0 || value.CallingConvention == MethodCallingConvention.VariableArguments)
                {
                    _formatter.Write("(");
                    WriteParameterDeclarationList(value.Parameters, _formatter, configuration);
                    _formatter.Write("):");
                }
                if (value.Parameters.Count <= 0)
                {
                    _formatter.Write("():");
                }
                if (flag2 && !flag)
                {
                    _formatter.Write(" <returns: ");
                    WriteType(value.ReturnType.Type, _formatter);
                    _formatter.Write("> ");
                }
                if (flag2 && flag)
                {
                    _formatter.Write(" <returns: instance> ");
                }
                if (flag3)
                {
                    _formatter.WriteComment(" #Private ");
                }
                IBlockStatement blockStatement = value.Body as IBlockStatement;
                if (blockStatement != null)
                {
                    bool flag4 = false;
                    WriteVariableList(blockStatement.Statements, _formatter, ref flag4);
                    if (flag4)
                    {
                        _formatter.WriteOutdent();
                    }
                    else
                    {
                        _formatter.WriteLine();
                    }
                    _formatter.WriteIndent();
                    blockStatementLevel = 0;
                    WriteStatement(blockStatement, _formatter);
                    WritePendingOutdent(_formatter);
                    _formatter.WriteLine();
                    _formatter.WriteOutdent();
                }
                WriteDeclaringType(value.DeclaringType as ITypeReference, _formatter);
            }

            public void WritePropertyDeclaration(IPropertyDeclaration value)
            {
                IMethodDeclaration methodDeclaration = null;
                if (value.GetMethod != null)
                {
                    methodDeclaration = value.GetMethod.Resolve();
                }
                IMethodDeclaration methodDeclaration2 = null;
                if (value.SetMethod != null)
                {
                    methodDeclaration2 = value.SetMethod.Resolve();
                }
                string name = value.Name;
                WriteDeclaration(name, value, _formatter);
                _formatter.Write(" <type ");
                WriteType(value.PropertyType, _formatter);
                _formatter.Write("> ");
                if (methodDeclaration != null)
                {
                    _formatter.WriteComment(" #Get ");
                }
                if (methodDeclaration2 != null)
                {
                    _formatter.WriteComment(" #Set ");
                }
            }

            public void WriteEventDeclaration(IEventDeclaration value)
            {
                if (configuration["ShowCustomAttributes"] == "true" && value.Attributes.Count != 0)
                {
                    WriteCustomAttributeList(value, _formatter);
                }
                _formatter.WriteDeclaration(value.Name);
                _formatter.Write(" <type ");
                WriteType(value.EventType, _formatter);
                _formatter.Write(">");
                WriteDeclaringType(value.DeclaringType as ITypeReference, _formatter);
            }

            private void WriteDeclaringTypeReference(ITypeReference value, IFormatter formatter)
            {
                ITypeReference typeReference = value.Owner as ITypeReference;
                if (typeReference != null)
                {
                    WriteDeclaringTypeReference(typeReference, formatter);
                }
                WriteType(value, formatter);
                formatter.Write(".");
            }

            private string GetPythonStyleResolutionScope(ITypeReference reference)
            {
                string text = reference.ToString();
                for (; ; )
                {
                    ITypeReference typeReference = reference.Owner as ITypeReference;
                    if (typeReference == null)
                    {
                        break;
                    }
                    reference = typeReference;
                    text = reference.ToString() + "." + text;
                }
                string @namespace = reference.Namespace;
                if (@namespace.Length == 0)
                {
                    return text;
                }
                return @namespace + "." + text;
            }

            private void WriteType(IType type, IFormatter formatter)
            {
                ITypeReference typeReference = type as ITypeReference;
                if (typeReference != null)
                {
                    string nameWithResolutionScope = Helper.GetNameWithResolutionScope(typeReference);
                    WriteTypeReference(typeReference, formatter, nameWithResolutionScope, typeReference);
                    return;
                }
                IArrayType arrayType = type as IArrayType;
                if (arrayType != null)
                {
                    formatter.Write("System.Array[");
                    WriteType(arrayType.ElementType, formatter);
                    IArrayDimensionCollection dimensions = arrayType.Dimensions;
                    if (dimensions.Count != 0)
                    {
                        formatter.Write("[");
                    }
                    for (int i = 0; i < dimensions.Count; i++)
                    {
                        if (i != 0)
                        {
                            formatter.Write(",");
                        }
                        if (dimensions[i].LowerBound != 0 && dimensions[i].UpperBound != -1 && (dimensions[i].LowerBound != -1 || dimensions[i].UpperBound != -1))
                        {
                            formatter.Write((dimensions[i].LowerBound != -1) ? dimensions[i].LowerBound.ToString(CultureInfo.InvariantCulture) : ".");
                            formatter.Write("..");
                            formatter.Write((dimensions[i].UpperBound != -1) ? dimensions[i].UpperBound.ToString(CultureInfo.InvariantCulture) : ".");
                        }
                    }
                    formatter.Write("]");
                    return;
                }

                if (type is IPointerType pointerType)
                {
                    WriteType(pointerType.ElementType, formatter);
                    return;
                }

                if (type is IReferenceType referenceType)
                {
                    WriteType(referenceType.ElementType, formatter);
                    return;
                }

                if (type is IOptionalModifier optionalModifier)
                {
                    WriteType(optionalModifier.ElementType, formatter);
                    formatter.Write(" ");
                    formatter.Write("(");
                    WriteType(optionalModifier.Modifier, formatter);
                    formatter.Write(")");
                    return;
                }

                if (type is IRequiredModifier requiredModifier)
                {
                    WriteType(requiredModifier.ElementType, formatter);
                    formatter.Write(" (");
                    WriteType(requiredModifier.Modifier, formatter);
                    formatter.Write(")");
                    return;
                }

                if (type is IFunctionPointer functionPointer)
                {
                    WriteType(functionPointer.ReturnType.Type, formatter);
                    formatter.Write(" (");
                    for (int j = 0; j < functionPointer.Parameters.Count; j++)
                    {
                        if (j != 0)
                        {
                            formatter.Write(", ");
                        }
                        WriteType(functionPointer.Parameters[j].ParameterType, formatter);
                    }
                    formatter.Write(")");
                    return;
                }

                if (type is IGenericParameter genericParameter)
                {
                    formatter.Write(genericParameter.Name);
                    return;
                }

                if (type is IGenericArgument genericArgument)
                {
                    WriteType(genericArgument.Resolve(), formatter);
                    return;
                }
                throw new NotSupportedException();
            }

            private void WriteParameterDeclaration(IParameterDeclaration value, IFormatter formatter, ILanguageWriterConfiguration configuration)
            {
                if (configuration != null && configuration["ShowCustomAttributes"] == "true" && value.Attributes.Count != 0)
                {
                    WriteCustomAttributeList(value, formatter);
                    formatter.Write(" ");
                }
                IType parameterType = value.ParameterType;
                if (!string.IsNullOrEmpty(value.Name))
                {
                    formatter.Write(value.Name);
                }
                else
                {
                    formatter.Write("A ");
                    if (parameterType != null)
                    {
                        WriteType(parameterType, formatter);
                    }
                }
                formatter.Write(" ");
                if (parameterType != null)
                {
                    formatter.Write("<type ");
                    WriteType(parameterType, formatter);
                    formatter.Write(">");
                }
                else
                {
                    formatter.Write("...");
                }
                IExpression defaultParameterValue = GetDefaultParameterValue(value);
                if (defaultParameterValue != null)
                {
                    formatter.Write(" = ");
                    WriteExpression(defaultParameterValue, formatter);
                }
            }

            private void WriteParameterDeclarationList(IParameterDeclarationCollection parameters, IFormatter formatter, ILanguageWriterConfiguration configuration)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    IParameterDeclaration parameterDeclaration = parameters[i];
                    if (parameterDeclaration.ParameterType != null || i + 1 != parameters.Count)
                    {
                        if (i != 0)
                        {
                            formatter.Write(", ");
                        }
                        WriteParameterDeclaration(parameterDeclaration, formatter, configuration);
                    }
                }
            }

            private void WriteCustomAttribute(ICustomAttribute customAttribute, IFormatter formatter)
            {
                ITypeReference typeReference = customAttribute.Constructor.DeclaringType as ITypeReference;
                string text = typeReference.Name;
                if (text.EndsWith("Attribute"))
                {
                    text = text.Substring(0, text.Length - 9);
                }
                WriteReference(text, formatter, GetMethodReferenceDescription(customAttribute.Constructor), customAttribute.Constructor);
                ExpressionCollection arguments = customAttribute.Arguments;
                if (arguments.Count != 0)
                {
                    formatter.Write("(");
                    for (int i = 0; i < arguments.Count; i++)
                    {
                        if (i != 0)
                        {
                            formatter.Write(", ");
                        }
                        WriteExpression(arguments[i], formatter);
                    }
                    formatter.Write(")");
                }
            }

            private void WriteCustomAttributeList(ICustomAttributeProvider provider, IFormatter formatter)
            {
                ArrayList arrayList = new ArrayList();
                for (int i = 0; i < provider.Attributes.Count; i++)
                {
                    ICustomAttribute customAttribute = provider.Attributes[i];
                    if (!IsType(customAttribute.Constructor.DeclaringType, "System.Runtime.InteropServices", "DefaultParameterValueAttribute", "System"))
                    {
                        arrayList.Add(customAttribute);
                    }
                }
                if (arrayList.Count > 0)
                {
                    string value = null;
                    IAssembly assembly = provider as IAssembly;
                    if (assembly != null)
                    {
                        value = "assembly:";
                    }
                    IModule module = provider as IModule;
                    if (module != null)
                    {
                        value = "module:";
                    }

                    if (provider is IMethodReturnType methodReturnType)
                    {
                        value = "return:";
                    }
                    if (assembly != null || module != null)
                    {
                        for (int j = 0; j < arrayList.Count; j++)
                        {
                            ICustomAttribute customAttribute2 = (ICustomAttribute)arrayList[j];
                            formatter.Write("[");
                            formatter.WriteKeyword(value);
                            formatter.Write(" ");
                            WriteCustomAttribute(customAttribute2, formatter);
                            formatter.Write("]");
                            if (j != arrayList.Count - 1)
                            {
                                formatter.WriteLine();
                            }
                        }
                    }
                }
            }

            private void WriteGenericArgumentList(ITypeCollection parameters, IFormatter formatter)
            {
                if (parameters.Count > 0)
                {
                    formatter.Write("<");
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        if (i != 0)
                        {
                            formatter.Write(", ");
                        }
                        WriteType(parameters[i], formatter);
                    }
                    formatter.Write(">");
                }
            }

            private void WriteGenericParameterConstraint(IType value, IFormatter formatter)
            {
                if (value is IDefaultConstructorConstraint defaultConstructorConstraint)
                {
                    return;
                }

                if (value is IReferenceTypeConstraint referenceTypeConstraint)
                {
                    return;
                }

                if (value is IValueTypeConstraint valueTypeConstraint)
                {
                }
            }

            public void WriteExpression(IExpression value)
            {
                WriteExpression(value, _formatter);
            }

            private void WriteExpression(IExpression value, IFormatter formatter)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value is ILiteralExpression expression)
                {
                    WriteLiteralExpression(expression, formatter);
                    return;
                }
                if (value is IAssignExpression assignExpression)
                {
                    WriteAssignExpression(assignExpression, formatter);
                    return;
                }
                if (value is ITypeOfExpression ofExpression)
                {
                    WriteTypeOfExpression(ofExpression, formatter);
                    return;
                }
                if (value is IFieldOfExpression fieldOfExpression)
                {
                    WriteFieldOfExpression(fieldOfExpression, formatter);
                    return;
                }
                if (value is IMethodOfExpression methodOfExpression)
                {
                    WriteMethodOfExpression(methodOfExpression, formatter);
                    return;
                }
                if (value is IMemberInitializerExpression initializerExpression)
                {
                    WriteMemberInitializerExpression(initializerExpression, formatter);
                    return;
                }
                if (value is ITypeReferenceExpression referenceExpression)
                {
                    WriteTypeReferenceExpression(referenceExpression, formatter);
                    return;
                }
                if (value is IFieldReferenceExpression fieldReferenceExpression)
                {
                    WriteFieldReferenceExpression(fieldReferenceExpression, formatter);
                    return;
                }
                if (value is IEventReferenceExpression eventReferenceExpression)
                {
                    WriteEventReferenceExpression(eventReferenceExpression, formatter);
                    return;
                }
                if (value is IMethodReferenceExpression methodReferenceExpression)
                {
                    WriteMethodReferenceExpression(methodReferenceExpression, formatter);
                    return;
                }
                if (value is IArgumentListExpression listExpression)
                {
                    WriteArgumentListExpression(listExpression, formatter);
                    return;
                }
                if (value is IStackAllocateExpression allocateExpression)
                {
                    WriteStackAllocateExpression(allocateExpression, formatter);
                    return;
                }
                if (value is IPropertyReferenceExpression propertyReferenceExpression)
                {
                    WritePropertyReferenceExpression(propertyReferenceExpression, formatter);
                    return;
                }
                if (value is IArrayCreateExpression createExpression)
                {
                    WriteArrayCreateExpression(createExpression, formatter);
                    return;
                }
                if (value is IBlockExpression blockExpression)
                {
                    WriteBlockExpression(blockExpression, formatter);
                    return;
                }
                if (value is IBaseReferenceExpression)
                {
                    return;
                }
                if (value is IUnaryExpression unaryExpression)
                {
                    WriteUnaryExpression(unaryExpression, formatter);
                    return;
                }
                if (value is IBinaryExpression binaryExpression)
                {
                    WriteBinaryExpression(binaryExpression, formatter);
                    return;
                }
                if (value is ITryCastExpression castExpression)
                {
                    WriteTryCastExpression(castExpression, formatter);
                    return;
                }
                if (value is ICanCastExpression canCastExpression)
                {
                    WriteCanCastExpression(canCastExpression, formatter);
                    return;
                }
                if (value is ICastExpression expression1)
                {
                    WriteCastExpression(expression1, formatter);
                    return;
                }
                if (value is IConditionExpression conditionExpression)
                {
                    WriteConditionExpression(conditionExpression, formatter);
                    return;
                }
                if (value is INullCoalescingExpression coalescingExpression)
                {
                    WriteNullCoalescingExpression(coalescingExpression, formatter);
                    return;
                }
                if (value is IDelegateCreateExpression delegateCreateExpression)
                {
                    WriteDelegateCreateExpression(delegateCreateExpression, formatter);
                    return;
                }
                if (value is IAnonymousMethodExpression methodExpression)
                {
                    WriteAnonymousMethodExpression(methodExpression, formatter);
                    return;
                }
                if (value is IArgumentReferenceExpression argumentReferenceExpression)
                {
                    WriteArgumentReferenceExpression(argumentReferenceExpression, formatter);
                    return;
                }
                if (value is IVariableDeclarationExpression declarationExpression)
                {
                    WriteVariableDeclarationExpression(declarationExpression, formatter);
                    return;
                }
                if (value is IVariableReferenceExpression variableReferenceExpression)
                {
                    WriteVariableReferenceExpression(variableReferenceExpression, formatter);
                    return;
                }
                if (value is IPropertyIndexerExpression indexerExpression)
                {
                    WritePropertyIndexerExpression(indexerExpression, formatter);
                    return;
                }
                if (value is IArrayIndexerExpression arrayIndexerExpression)
                {
                    WriteArrayIndexerExpression(arrayIndexerExpression, formatter);
                    return;
                }
                if (value is IMethodInvokeExpression invokeExpression)
                {
                    WriteMethodInvokeExpression(invokeExpression, formatter);
                    return;
                }
                if (value is IDelegateInvokeExpression delegateInvokeExpression)
                {
                    WriteDelegateInvokeExpression(delegateInvokeExpression, formatter);
                    return;
                }
                if (value is IObjectCreateExpression objectCreateExpression)
                {
                    WriteObjectCreateExpression(objectCreateExpression, formatter);
                    return;
                }
                if (value is IThisReferenceExpression thisReferenceExpression)
                {
                    WriteThisReferenceExpression(thisReferenceExpression, formatter);
                    return;
                }
                if (value is IAddressOfExpression addressOfExpression)
                {
                    WriteAddressOfExpression(addressOfExpression, formatter);
                    return;
                }
                if (value is IAddressReferenceExpression addressReferenceExpression)
                {
                    WriteAddressReferenceExpression(addressReferenceExpression, formatter);
                    return;
                }
                if (value is IAddressOutExpression outExpression)
                {
                    WriteAddressOutExpression(outExpression, formatter);
                    return;
                }
                if (value is IAddressDereferenceExpression dereferenceExpression)
                {
                    WriteAddressDereferenceExpression(dereferenceExpression, formatter);
                    return;
                }
                if (value is ISizeOfExpression sizeOfExpression)
                {
                    WriteSizeOfExpression(sizeOfExpression, formatter);
                    return;
                }
                if (value is ITypeOfTypedReferenceExpression typedReferenceExpression)
                {
                    WriteTypeOfTypedReferenceExpression(typedReferenceExpression, formatter);
                    return;
                }
                if (value is IValueOfTypedReferenceExpression ofTypedReferenceExpression)
                {
                    WriteValueOfTypedReferenceExpression(ofTypedReferenceExpression, formatter);
                    return;
                }
                if (value is ITypedReferenceCreateExpression referenceCreateExpression)
                {
                    WriteTypedReferenceCreateExpression(referenceCreateExpression, formatter);
                    return;
                }
                if (value is IGenericDefaultExpression defaultExpression)
                {
                    WriteGenericDefaultExpression(defaultExpression, formatter);
                    return;
                }
                if (value is IQueryExpression queryExpression)
                {
                    WriteQueryExpression(queryExpression, formatter);
                    return;
                }
                if (value is ILambdaExpression lambdaExpression)
                {
                    WriteLambdaExpression(lambdaExpression, formatter);
                    return;
                }
                if (value is ISnippetExpression snippetExpression)
                {
                    WriteSnippetExpression(snippetExpression, formatter);
                    return;
                }
                throw new ArgumentException("Invalid expression type.", "value");
            }

            private void WriteExpressionList(ExpressionCollection expressions, IFormatter formatter)
            {
                for (int i = 0; i < expressions.Count; i++)
                {
                    if (i != 0)
                    {
                        formatter.Write(", ");
                    }
                    WriteExpression(expressions[i], formatter);
                }
            }

            private void WriteGenericDefaultExpression(IGenericDefaultExpression value, IFormatter formatter)
            {
                formatter.WriteComment("default");
                formatter.Write("(");
                WriteType(value.GenericArgument, formatter);
                formatter.Write(")");
            }

            private void WriteTypeOfTypedReferenceExpression(ITypeOfTypedReferenceExpression value, IFormatter formatter)
            {
                formatter.WriteComment("__reftype");
                formatter.Write("(");
                WriteExpression(value.Expression, formatter);
                formatter.Write(")");
            }

            private void WriteValueOfTypedReferenceExpression(IValueOfTypedReferenceExpression value, IFormatter formatter)
            {
                formatter.WriteComment("__refvalue");
                formatter.Write("(");
                WriteExpression(value.Expression, formatter);
                formatter.Write(")");
            }

            private void WriteTypedReferenceCreateExpression(ITypedReferenceCreateExpression value, IFormatter formatter)
            {
                formatter.WriteComment("__makeref");
                formatter.Write("(");
                WriteExpression(value.Expression, formatter);
                formatter.Write(")");
            }

            private void WriteMemberInitializerExpression(IMemberInitializerExpression value, IFormatter formatter)
            {
                WriteMemberReference(value.Member, formatter);
                formatter.Write("=");
                WriteExpression(value.Value, formatter);
            }

            private void WriteMemberReference(IMemberReference memberReference, IFormatter formatter)
            {
                if (memberReference is IFieldReference fieldReference)
                {
                    WriteFieldReference(fieldReference, formatter);
                }

                if (memberReference is IMethodReference methodReference)
                {
                    WriteMethodReference(methodReference, formatter);
                }

                if (memberReference is IPropertyReference propertyReference)
                {
                    WritePropertyReference(propertyReference, formatter);
                }

                if (memberReference is IEventReference eventReference)
                {
                    WriteEventReference(eventReference, formatter);
                }
            }

            private void WriteTargetExpression(IExpression expression, IFormatter formatter)
            {
                WriteExpression(expression, formatter);
            }

            private void WriteTypeOfExpression(ITypeOfExpression expression, IFormatter formatter)
            {
                formatter.Write("(");
                WriteType(expression.Type, formatter);
                formatter.Write(")");
            }

            private void WriteFieldOfExpression(IFieldOfExpression value, IFormatter formatter)
            {
                formatter.Write("(");
                WriteType(value.Field.DeclaringType, formatter);
                formatter.Write(".");
                formatter.WriteReference(value.Field.Name, GetFieldReferenceDescription(value.Field), value.Field);
                if (value.Type != null)
                {
                    formatter.Write(", ");
                    WriteType(value.Type, formatter);
                }
                formatter.Write(")");
            }

            private void WriteMethodOfExpression(IMethodOfExpression value, IFormatter formatter)
            {
                formatter.Write("(");
                WriteType(value.Method.DeclaringType, formatter);
                formatter.Write(".");
                formatter.WriteReference(value.Method.Name, GetMethodReferenceDescription(value.Method), value.Method);
                if (value.Type != null)
                {
                    formatter.Write(", ");
                    WriteType(value.Type, formatter);
                }
                formatter.Write(")");
            }

            private void WriteArrayElementType(IType type, IFormatter formatter)
            {
                if (type is IArrayType arrayType)
                {
                    WriteArrayElementType(arrayType.ElementType, formatter);
                    return;
                }
                WriteType(type, formatter);
            }

            private void WriteArrayCreateExpression(IArrayCreateExpression expression, IFormatter formatter)
            {
                formatter.WriteKeyword("(System.Array[");
                WriteExpressionList(expression.Dimensions, formatter);
                formatter.Write("]");
                if (expression.Initializer != null)
                {
                    formatter.Write(", (");
                    WriteExpression(expression.Initializer, formatter);
                    formatter.Write(")");
                }
                formatter.Write(")");
            }

            private void WriteBlockExpression(IBlockExpression expression, IFormatter formatter)
            {
                formatter.Write(" ( ");
                if (expression.Expressions.Count > 16)
                {
                    formatter.WriteLine();
                    formatter.WriteIndent();
                }
                for (int i = 0; i < expression.Expressions.Count; i++)
                {
                    if (i != 0)
                    {
                        formatter.Write(", ");
                        if (i % 16 == 0)
                        {
                            formatter.WriteLine();
                        }
                    }
                    WriteExpression(expression.Expressions[i], formatter);
                }
                if (expression.Expressions.Count > 16)
                {
                    formatter.WriteOutdent();
                    formatter.WriteLine();
                }
                formatter.Write(" ) ");
            }

            private void WriteTryCastExpression(ITryCastExpression expression, IFormatter formatter)
            {
                WriteType(expression.TargetType, formatter);
                formatter.Write("(");
                WriteExpression(expression.Expression, formatter);
                formatter.Write(")");
            }

            private void WriteCanCastExpression(ICanCastExpression expression, IFormatter formatter)
            {
                formatter.Write("(");
                WriteExpression(expression.Expression, formatter);
                formatter.Write(" ");
                formatter.Write("==");
                formatter.Write(" ");
                WriteType(expression.TargetType, formatter);
                formatter.Write(")");
            }

            private void WriteCastExpression(ICastExpression expression, IFormatter formatter)
            {
                formatter.Write("(");
                WriteExpression(expression.Expression, formatter);
                formatter.Write(" ");
                formatter.Write(" <type ");
                WriteType(expression.TargetType, formatter);
                formatter.Write(">");
                formatter.Write(")");
            }

            private void WriteConditionExpression(IConditionExpression expression, IFormatter formatter)
            {
                formatter.WriteKeyword("if");
                formatter.Write(" ");
                WriteExpression(expression.Condition, formatter);
                formatter.Write(": ");
                formatter.Write(" ");
                WriteExpression(expression.Then, formatter);
                formatter.Write("; ");
                formatter.WriteKeyword("else:");
                formatter.Write(" ");
                WriteExpression(expression.Else, formatter);
            }

            private void WriteNullCoalescingExpression(INullCoalescingExpression value, IFormatter formatter)
            {
                formatter.WriteKeyword("if");
                formatter.Write(" ");
                WriteExpression(value.Condition, formatter);
                formatter.WriteKeyword(" not ");
                formatter.WriteKeyword(" None ");
                formatter.Write(": ");
                formatter.Write(" ");
                WriteExpression(value.Condition, formatter);
                formatter.Write("; ");
                formatter.WriteKeyword("else:");
                formatter.WriteLine();
                WriteExpression(value.Expression, formatter);
            }

            private void WriteDelegateCreateExpression(IDelegateCreateExpression expression, IFormatter formatter)
            {
                WriteTypeReference(expression.DelegateType, formatter);
                formatter.Write("(");
                WriteTargetExpression(expression.Target, formatter);
                formatter.Write(",");
                WriteMethodReference(expression.Method, formatter);
                formatter.Write(")");
            }

            private void WriteAnonymousMethodExpression(IAnonymousMethodExpression value, IFormatter formatter)
            {
                bool flag = false;
                for (int i = 0; i < value.Parameters.Count; i++)
                {
                    if (value.Parameters[i].Name != null && value.Parameters[i].Name.Length > 0)
                    {
                        flag = true;
                    }
                }
                formatter.WriteComment("delegate");
                formatter.Write(" ");
                if (flag)
                {
                    formatter.Write("(");
                    WriteParameterDeclarationList(value.Parameters, formatter, configuration);
                    formatter.Write(")");
                    formatter.Write(" ");
                }
                formatter.WriteLine();
                formatter.WriteIndent();
                WriteBlockStatement(value.Body, formatter);
                formatter.WriteOutdent();
            }

            private void WriteTypeReferenceExpression(ITypeReferenceExpression expression, IFormatter formatter)
            {
                WriteTypeReference(expression.Type, formatter);
            }

            private void WriteFieldReferenceExpression(IFieldReferenceExpression expression, IFormatter formatter)
            {
                if (expression.Target != null)
                {
                    WriteTargetExpression(expression.Target, formatter);
                    if (!(expression.Target is IBaseReferenceExpression))
                    {
                        formatter.Write(".");
                    }
                }
                WriteFieldReference(expression.Field, formatter);
            }

            private void WriteArgumentReferenceExpression(IArgumentReferenceExpression expression, IFormatter formatter)
            {
                TextFormatter textFormatter = new TextFormatter();
                WriteParameterDeclaration(expression.Parameter.Resolve(), textFormatter, null);
                textFormatter.Write(" // Parameter");
                if (expression.Parameter.Name != null)
                {
                    WriteReference(expression.Parameter.Name, formatter, textFormatter.ToString(), null);
                }
            }

            private void WriteArgumentListExpression(IArgumentListExpression expression, IFormatter formatter)
            {
                formatter.WriteComment("__arglist");
            }

            private void WriteVariableReferenceExpression(IVariableReferenceExpression expression, IFormatter formatter)
            {
                WriteVariableReference(expression.Variable, formatter);
            }

            private void WriteVariableReference(IVariableReference value, IFormatter formatter)
            {
                IVariableDeclaration variableDeclaration = value.Resolve();
                TextFormatter textFormatter = new TextFormatter();
                WriteVariableDeclaration(variableDeclaration, textFormatter);
                textFormatter.Write(" # Local Variable");
                formatter.WriteReference(variableDeclaration.Name, textFormatter.ToString(), null);
            }

            private void WritePropertyIndexerExpression(IPropertyIndexerExpression expression, IFormatter formatter)
            {
                WriteTargetExpression(expression.Target, formatter);
                formatter.Write("[");
                bool flag = true;
                foreach (object obj in expression.Indices)
                {
                    IExpression value = (IExpression)obj;
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        formatter.Write(", ");
                    }
                    WriteExpression(value, formatter);
                }
                formatter.Write("]");
            }

            private void WriteArrayIndexerExpression(IArrayIndexerExpression expression, IFormatter formatter)
            {
                WriteTargetExpression(expression.Target, formatter);
                formatter.Write("[");
                for (int i = 0; i < expression.Indices.Count; i++)
                {
                    if (i != 0)
                    {
                        formatter.Write(", ");
                    }
                    WriteExpression(expression.Indices[i], formatter);
                }
                formatter.Write("]");
            }

            private void WriteMethodInvokeExpression(IMethodInvokeExpression expression, IFormatter formatter)
            {
                IMethodReferenceExpression methodReferenceExpression = expression.Method as IMethodReferenceExpression;
                if (methodReferenceExpression != null)
                {
                    WriteMethodReferenceExpression(methodReferenceExpression, formatter);
                }
                else
                {
                    formatter.Write("(");
                    WriteExpression(expression.Method, formatter);
                    formatter.Write("^");
                    formatter.Write(")");
                }
                if (expression.Arguments.Count > 0)
                {
                    formatter.Write("(");
                    WriteExpressionList(expression.Arguments, formatter);
                    formatter.Write(")");
                }
            }

            private void WriteMethodReferenceExpression(IMethodReferenceExpression expression, IFormatter formatter)
            {
                if (expression.Target != null)
                {
                    if (expression.Target is IBinaryExpression)
                    {
                        formatter.Write("(");
                        WriteExpression(expression.Target, formatter);
                        formatter.Write(")");
                    }
                    else
                    {
                        WriteTargetExpression(expression.Target, formatter);
                    }
                    if (!(expression.Target is IBaseReferenceExpression))
                    {
                        formatter.Write(".");
                    }
                }
                WriteMethodReference(expression.Method, formatter);
            }

            private void WriteEventReferenceExpression(IEventReferenceExpression expression, IFormatter formatter)
            {
                if (expression.Target != null)
                {
                    WriteTargetExpression(expression.Target, formatter);
                    if (!(expression.Target is IBaseReferenceExpression))
                    {
                        formatter.Write(".");
                    }
                }
                WriteEventReference(expression.Event, formatter);
            }

            private void WriteDelegateInvokeExpression(IDelegateInvokeExpression expression, IFormatter formatter)
            {
                if (expression.Target != null)
                {
                    WriteTargetExpression(expression.Target, formatter);
                }
                formatter.Write("(");
                WriteExpressionList(expression.Arguments, formatter);
                formatter.Write(")");
            }

            private void WriteObjectCreateExpression(IObjectCreateExpression value, IFormatter formatter)
            {
                if (value.Constructor != null)
                {
                    WriteTypeReference((ITypeReference)value.Type, formatter, GetMethodReferenceDescription(value.Constructor), value.Constructor);
                }
                else
                {
                    WriteType(value.Type, formatter);
                }
                if (value.Arguments.Count > 0)
                {
                    formatter.Write("(");
                    WriteExpressionList(value.Arguments, formatter);
                    formatter.Write(")");
                }
                IBlockExpression initializer = value.Initializer;
                if (initializer != null && initializer.Expressions.Count > 0)
                {
                    formatter.Write(" ");
                    WriteExpression(initializer, formatter);
                }
            }

            private void WritePropertyReferenceExpression(IPropertyReferenceExpression expression, IFormatter formatter)
            {
                if (expression.Target != null)
                {
                    WriteTargetExpression(expression.Target, formatter);
                    if (!(expression.Target is IBaseReferenceExpression))
                    {
                        formatter.Write(".");
                    }
                }
                WritePropertyReference(expression.Property, formatter);
            }

            private void WriteThisReferenceExpression(IThisReferenceExpression expression, IFormatter formatter)
            {
                formatter.WriteKeyword("self");
            }

            private void WriteAddressOfExpression(IAddressOfExpression expression, IFormatter formatter)
            {
                WriteExpression(expression.Expression, formatter);
            }

            private void WriteAddressReferenceExpression(IAddressReferenceExpression expression, IFormatter formatter)
            {
                WriteExpression(expression.Expression, formatter);
            }

            private void WriteAddressOutExpression(IAddressOutExpression expression, IFormatter formatter)
            {
                WriteExpression(expression.Expression, formatter);
            }

            private void WriteAddressDereferenceExpression(IAddressDereferenceExpression expression, IFormatter formatter)
            {
                IAddressOfExpression addressOfExpression = expression.Expression as IAddressOfExpression;
                if (addressOfExpression != null)
                {
                    WriteExpression(addressOfExpression.Expression, formatter);
                    return;
                }
                WriteExpression(expression.Expression, formatter);
            }

            private void WriteSizeOfExpression(ISizeOfExpression expression, IFormatter formatter)
            {
                formatter.Write("(");
                WriteType(expression.Type, formatter);
                formatter.Write(")");
            }

            private void WriteStackAllocateExpression(IStackAllocateExpression expression, IFormatter formatter)
            {
                formatter.Write(" ");
                WriteType(expression.Type, formatter);
                formatter.Write("[");
                WriteExpression(expression.Expression, formatter);
                formatter.Write("]");
            }

            private void WriteLambdaExpression(ILambdaExpression value, IFormatter formatter)
            {
                if (value.Parameters.Count > 1)
                {
                    formatter.Write("(");
                }
                for (int i = 0; i < value.Parameters.Count; i++)
                {
                    if (i != 0)
                    {
                        formatter.Write(", ");
                    }
                    WriteDeclaration(value.Parameters[i].Name, formatter);
                }
                if (value.Parameters.Count > 1)
                {
                    formatter.Write(")");
                }
                formatter.Write(" ");
                formatter.Write("=>");
                formatter.Write(" ");
                WriteExpression(value.Body, formatter);
            }

            private void WriteQueryExpression(IQueryExpression value, IFormatter formatter)
            {
                formatter.Write("(");
                WriteFromClause(value.From, formatter);
                if (value.Body.Clauses.Count > 0 || value.Body.Continuation != null)
                {
                    formatter.WriteLine();
                    formatter.WriteIndent();
                }
                else
                {
                    formatter.Write(" ");
                }
                WriteQueryBody(value.Body, formatter);
                formatter.Write(")");
                if (value.Body.Clauses.Count > 0 || value.Body.Continuation != null)
                {
                    formatter.WriteOutdent();
                }
            }

            private void WriteQueryBody(IQueryBody value, IFormatter formatter)
            {
                for (int i = 0; i < value.Clauses.Count; i++)
                {
                    WriteQueryClause(value.Clauses[i], formatter);
                    formatter.WriteLine();
                }
                WriteQueryOperation(value.Operation, formatter);
                if (value.Continuation != null)
                {
                    formatter.Write(" ");
                    WriteQueryContinuation(value.Continuation, formatter);
                }
            }

            private void WriteQueryContinuation(IQueryContinuation value, IFormatter formatter)
            {
                formatter.Write(" ");
                WriteDeclaration(value.Variable.Name, formatter);
                formatter.WriteLine();
                WriteQueryBody(value.Body, formatter);
            }

            private void WriteQueryClause(IQueryClause value, IFormatter formatter)
            {
                if (value is IWhereClause clause)
                {
                    WriteWhereClause(clause, formatter);
                    return;
                }
                if (value is ILetClause letClause)
                {
                    WriteLetClause(letClause, formatter);
                    return;
                }
                if (value is IFromClause fromClause)
                {
                    WriteFromClause(fromClause, formatter);
                    return;
                }
                if (value is IJoinClause joinClause)
                {
                    WriteJoinClause(joinClause, formatter);
                    return;
                }
                if (value is IOrderClause orderClause)
                {
                    WriteOrderClause(orderClause, formatter);
                    return;
                }
                throw new NotSupportedException();
            }

            private void WriteQueryOperation(IQueryOperation value, IFormatter formatter)
            {
                if (value is ISelectOperation operation)
                {
                    WriteSelectOperation(operation, formatter);
                    return;
                }
                if (value is IGroupOperation groupOperation)
                {
                    WriteGroupOperation(groupOperation, formatter);
                    return;
                }
                throw new NotSupportedException();
            }

            private void WriteFromClause(IFromClause value, IFormatter formatter)
            {
                formatter.Write(" ");
                WriteDeclaration(value.Variable.Name, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("in");
                formatter.Write(" ");
                WriteExpression(value.Expression, formatter);
            }

            private void WriteWhereClause(IWhereClause value, IFormatter formatter)
            {
                formatter.WriteKeyword("where");
                formatter.Write(" ");
                WriteExpression(value.Expression, formatter);
            }

            private void WriteLetClause(ILetClause value, IFormatter formatter)
            {
                formatter.Write(" ");
                WriteDeclaration(value.Variable.Name, formatter);
                formatter.Write(" = ");
                WriteExpression(value.Expression, formatter);
            }

            private void WriteJoinClause(IJoinClause value, IFormatter formatter)
            {
                formatter.Write(" ");
                WriteDeclaration(value.Variable.Name, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("in");
                formatter.Write(" ");
                WriteExpression(value.In, formatter);
                formatter.Write(" ");
                formatter.WriteComment("on");
                formatter.Write(" ");
                WriteExpression(value.On, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("equals");
                formatter.Write(" ");
                WriteExpression(value.Equality, formatter);
                if (value.Into != null)
                {
                    formatter.Write(" ");
                    formatter.WriteKeyword("into");
                    formatter.Write(" ");
                    WriteDeclaration(value.Into.Name, formatter);
                }
            }

            private void WriteOrderClause(IOrderClause value, IFormatter formatter)
            {
                formatter.Write(" ");
                //TODO:
                //this.WriteExpression(value.Expression, formatter);
                //if (value.Direction == OrderDirection.Descending)
                //{
                //    formatter.Write(" ");
                //    formatter.WriteComment("descending");
                //}
                WriteExpression(value.ExpressionAndDirections[0].Expression, formatter);
                if (value.ExpressionAndDirections[0].Direction == OrderDirection.Descending)
                {
                    formatter.Write(" ");
                    formatter.WriteComment("descending");
                }
            }

            private void WriteSelectOperation(ISelectOperation value, IFormatter formatter)
            {
                formatter.Write(" ");
                WriteExpression(value.Expression, formatter);
            }

            private void WriteGroupOperation(IGroupOperation value, IFormatter formatter)
            {
                formatter.Write(" ");
                WriteExpression(value.Item, formatter);
                formatter.Write(" ");
                formatter.Write(" ");
                WriteExpression(value.Key, formatter);
            }

            private void WriteSnippetExpression(ISnippetExpression expression, IFormatter formatter)
            {
                formatter.WriteComment(expression.Value);
            }

            private void WriteUnaryExpression(IUnaryExpression expression, IFormatter formatter)
            {
                switch (expression.Operator)
                {
                    case UnaryOperator.Negate:
                        formatter.Write("-");
                        WriteExpression(expression.Expression, formatter);
                        return;
                    case UnaryOperator.BooleanNot:
                        formatter.WriteKeyword("not");
                        formatter.Write(" ");
                        WriteExpression(expression.Expression, formatter);
                        return;
                    case UnaryOperator.BitwiseNot:
                        formatter.WriteKeyword("not");
                        formatter.Write(" ");
                        WriteExpression(expression.Expression, formatter);
                        return;
                    case UnaryOperator.PreIncrement:
                        formatter.Write("++");
                        WriteExpression(expression.Expression, formatter);
                        return;
                    case UnaryOperator.PreDecrement:
                        formatter.Write("--");
                        WriteExpression(expression.Expression, formatter);
                        return;
                    case UnaryOperator.PostIncrement:
                        WriteExpression(expression.Expression, formatter);
                        formatter.Write("+= 1");
                        return;
                    case UnaryOperator.PostDecrement:
                        WriteExpression(expression.Expression, formatter);
                        formatter.Write("-= 1");
                        return;
                    default:
                        throw new NotSupportedException(expression.Operator.ToString());
                }
            }

            private void WriteBinaryExpression(IBinaryExpression expression, IFormatter formatter)
            {
                formatter.Write("(");
                WriteExpression(expression.Left, formatter);
                formatter.Write(" ");
                WriteBinaryOperator(expression.Operator, formatter);
                formatter.Write(" ");
                WriteExpression(expression.Right, formatter);
                formatter.Write(")");
            }

            private void WriteBinaryOperator(BinaryOperator operatorType, IFormatter formatter)
            {
                switch (operatorType)
                {
                    case BinaryOperator.Add:
                        formatter.Write("+");
                        return;
                    case BinaryOperator.Subtract:
                        formatter.Write("-");
                        return;
                    case BinaryOperator.Multiply:
                        formatter.Write("*");
                        return;
                    case BinaryOperator.Divide:
                        formatter.WriteKeyword("/");
                        return;
                    case BinaryOperator.Modulus:
                        formatter.WriteKeyword("%");
                        return;
                    case BinaryOperator.ShiftLeft:
                        formatter.WriteKeyword("<<");
                        return;
                    case BinaryOperator.ShiftRight:
                        formatter.WriteKeyword(">>");
                        return;
                    case BinaryOperator.IdentityEquality:
                    case BinaryOperator.ValueEquality:
                        formatter.Write("==");
                        return;
                    case BinaryOperator.IdentityInequality:
                    case BinaryOperator.ValueInequality:
                        formatter.Write("!=");
                        return;
                    case BinaryOperator.BitwiseOr:
                        formatter.WriteKeyword("|");
                        return;
                    case BinaryOperator.BitwiseAnd:
                        formatter.WriteKeyword("&");
                        return;
                    case BinaryOperator.BitwiseExclusiveOr:
                        formatter.WriteKeyword("^");
                        return;
                    case BinaryOperator.BooleanOr:
                        formatter.WriteKeyword("or");
                        return;
                    case BinaryOperator.BooleanAnd:
                        formatter.WriteKeyword("and");
                        return;
                    case BinaryOperator.LessThan:
                        formatter.Write("<");
                        return;
                    case BinaryOperator.LessThanOrEqual:
                        formatter.Write("<=");
                        return;
                    case BinaryOperator.GreaterThan:
                        formatter.Write(">");
                        return;
                    case BinaryOperator.GreaterThanOrEqual:
                        formatter.Write(">=");
                        return;
                    default:
                        throw new NotSupportedException(operatorType.ToString());
                }
            }

            private void WriteLiteralExpression(ILiteralExpression value, IFormatter formatter)
            {
                if (value.Value == null)
                {
                    formatter.WriteLiteral("None");
                    return;
                }
                if (value.Value is char c)
                {
                    string text = new string(new[]
                    {
                        c
                    });
                    text = QuoteLiteralExpression(text);
                    formatter.WriteLiteral("'" + text + "'");
                    return;
                }
                if (value.Value is string text2)
                {
                    text2 = QuoteLiteralExpression(text2);
                    formatter.WriteLiteral("'" + text2 + "'");
                    return;
                }
                if (value.Value is byte bt)
                {
                    WriteNumber(bt, formatter);
                    return;
                }
                if (value.Value is sbyte sbt)
                {
                    WriteNumber(sbt, formatter);
                    return;
                }
                if (value.Value is short s)
                {
                    WriteNumber(s, formatter);
                    return;
                }
                if (value.Value is ushort ui2)
                {
                    WriteNumber(ui2, formatter);
                    return;
                }
                if (value.Value is int i)
                {
                    WriteNumber(i, formatter);
                    return;
                }
                if (value.Value is uint u)
                {
                    WriteNumber(u, formatter);
                    return;
                }
                if (value.Value is long l)
                {
                    WriteNumber(l, formatter);
                    return;
                }
                if (value.Value is ulong ui8)
                {
                    WriteNumber(ui8, formatter);
                    return;
                }
                if (value.Value is float f)
                {
                    formatter.WriteLiteral(f.ToString(CultureInfo.InvariantCulture));
                    return;
                }
                if (value.Value is double d)
                {
                    formatter.WriteLiteral(d.ToString("R", CultureInfo.InvariantCulture));
                    return;
                }
                if (value.Value is decimal dec)
                {
                    formatter.WriteLiteral(dec.ToString(CultureInfo.InvariantCulture));
                    return;
                }
                if (value.Value is bool b)
                {
                    formatter.WriteLiteral(b ? "True" : "False");
                    return;
                }
                throw new ArgumentException("expression");
            }

            private void WriteNumber(IConvertible value, IFormatter formatter)
            {
                IFormattable formattable = (IFormattable)value;
                switch (GetNumberFormat(value))
                {
                    case NumberFormat.Hexadecimal:
                        formatter.WriteLiteral("0x" + formattable.ToString("x", CultureInfo.InvariantCulture));
                        return;
                    case NumberFormat.Decimal:
                        formatter.WriteLiteral(formattable.ToString(null, CultureInfo.InvariantCulture));
                        return;
                    default:
                        return;
                }
            }

            private NumberFormat GetNumberFormat(IConvertible value)
            {
                NumberFormat numberFormat = _numberFormat;
                if (numberFormat != NumberFormat.Auto)
                {
                    return numberFormat;
                }
                long num = (long)((value is ulong ul) ? ul : ((ulong)value.ToInt64(CultureInfo.InvariantCulture)));
                if (num < 16L)
                {
                    return NumberFormat.Decimal;
                }
                if (num % 10L == 0L && num < 1000L)
                {
                    return NumberFormat.Decimal;
                }
                return NumberFormat.Hexadecimal;
            }

            private void WriteTypeReference(ITypeReference typeReference, IFormatter formatter)
            {
                WriteType(typeReference, formatter);
            }

            private void WriteTypeReference(ITypeReference typeReference, IFormatter formatter, string description, object target)
            {
                string text = typeReference.Name;
                if (typeReference.Namespace == "System" && specialTypeNames.Contains(text))
                {
                    text = (string)specialTypeNames[text];
                }
                ITypeReference genericType = typeReference.GenericType;
                if (genericType != null)
                {
                    WriteReference(text, formatter, description, genericType);
                    WriteGenericArgumentList(typeReference.GenericArguments, formatter);
                    return;
                }
                WriteReference(text, formatter, description, target);
            }

            private void WriteFieldReference(IFieldReference fieldReference, IFormatter formatter)
            {
                WriteReference(fieldReference.Name, formatter, GetFieldReferenceDescription(fieldReference), fieldReference);
            }

            private void WriteMethodReference(IMethodReference methodReference, IFormatter formatter)
            {
                IMethodReference genericMethod = methodReference.GenericMethod;
                if (genericMethod != null)
                {
                    WriteReference(methodReference.Name, formatter, GetMethodReferenceDescription(methodReference), genericMethod);
                    WriteGenericArgumentList(methodReference.GenericArguments, formatter);
                    return;
                }
                WriteReference(methodReference.Name, formatter, GetMethodReferenceDescription(methodReference), methodReference);
            }

            private void WritePropertyReference(IPropertyReference propertyReference, IFormatter formatter)
            {
                WriteReference(propertyReference.Name, formatter, GetPropertyReferenceDescription(propertyReference), propertyReference);
            }

            private void WriteEventReference(IEventReference eventReference, IFormatter formatter)
            {
                WriteReference(eventReference.Name, formatter, GetEventReferenceDescription(eventReference), eventReference);
            }

            public void WriteStatement(IStatement value)
            {
                WriteStatement(value, _formatter);
            }

            private void WriteStatement(IStatement value, IFormatter formatter)
            {
                WriteStatement(value, formatter, false);
            }

            private void WriteStatement(IStatement value, IFormatter formatter, bool lastStatement)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value is IBlockStatement statement)
                {
                    WriteBlockStatement(statement, formatter);
                    return;
                }
                if (value is IExpressionStatement expressionStatement)
                {
                    WriteExpressionStatement(expressionStatement, formatter);
                    return;
                }
                if (value is IGotoStatement gotoStatement)
                {
                    WriteGotoStatement(gotoStatement, formatter);
                    return;
                }
                if (value is ILabeledStatement labeledStatement)
                {
                    WriteLabeledStatement(labeledStatement, formatter);
                    return;
                }
                if (value is IConditionStatement conditionStatement)
                {
                    WriteConditionStatement(conditionStatement, formatter);
                    return;
                }
                if (value is IMethodReturnStatement returnStatement)
                {
                    WriteMethodReturnStatement(returnStatement, formatter, lastStatement);
                    return;
                }
                if (value is IForStatement forStatement)
                {
                    WriteForStatement(forStatement, formatter);
                    return;
                }
                if (value is IForEachStatement eachStatement)
                {
                    WriteForEachStatement(eachStatement, formatter);
                    return;
                }
                if (value is IUsingStatement usingStatement)
                {
                    WriteUsingStatement(usingStatement, formatter);
                    return;
                }
                if (value is IFixedStatement fixedStatement)
                {
                    WriteFixedStatement(fixedStatement, formatter);
                    return;
                }
                if (value is IWhileStatement whileStatement)
                {
                    WriteWhileStatement(whileStatement, formatter);
                    return;
                }
                if (value is IDoStatement doStatement)
                {
                    WriteDoStatement(doStatement, formatter);
                    return;
                }
                if (value is ITryCatchFinallyStatement finallyStatement)
                {
                    WriteTryCatchFinallyStatement(finallyStatement, formatter);
                    return;
                }
                if (value is IThrowExceptionStatement exceptionStatement)
                {
                    WriteThrowExceptionStatement(exceptionStatement, formatter);
                    return;
                }
                if (value is IAttachEventStatement eventStatement)
                {
                    WriteAttachEventStatement(eventStatement, formatter);
                    return;
                }
                if (value is IRemoveEventStatement removeEventStatement)
                {
                    WriteRemoveEventStatement(removeEventStatement, formatter);
                    return;
                }
                if (value is ISwitchStatement switchStatement)
                {
                    WriteSwitchStatement(switchStatement, formatter);
                    return;
                }
                if (value is IBreakStatement breakStatement)
                {
                    WriteBreakStatement(breakStatement, formatter);
                    return;
                }
                if (value is IContinueStatement continueStatement)
                {
                    WriteContinueStatement(continueStatement, formatter);
                    return;
                }
                if (value is IMemoryCopyStatement copyStatement)
                {
                    WriteMemoryCopyStatement(copyStatement, formatter);
                    return;
                }
                if (value is IMemoryInitializeStatement initializeStatement)
                {
                    WriteMemoryInitializeStatement(initializeStatement, formatter);
                    return;
                }
                if (value is IDebugBreakStatement debugBreakStatement)
                {
                    WriteDebugBreakStatement(debugBreakStatement, formatter);
                    return;
                }
                if (value is ILockStatement lockStatement)
                {
                    WriteLockStatement(lockStatement, formatter);
                    return;
                }
                if (value is ICommentStatement commentStatement)
                {
                    WriteCommentStatement(commentStatement, formatter);
                    return;
                }
                throw new ArgumentException("Invalid statement type.", nameof(value));
            }

            private void WritePendingOutdent(IFormatter formatter)
            {
                if (pendingOutdent > 0)
                {
                    formatter.WriteOutdent();
                    pendingOutdent = 0;
                }
            }

            private void MakePendingOutdent()
            {
                pendingOutdent = 1;
            }

            private void WriteStatementSeparator(IFormatter formatter)
            {
                if (firstStmt)
                {
                    firstStmt = false;
                }
                else if (!forLoop)
                {
                    formatter.WriteLine();
                }
                WritePendingOutdent(formatter);
            }

            private void WriteBlockStatement(IBlockStatement statement, IFormatter formatter)
            {
                blockStatementLevel++;
                if (statement.Statements.Count > 0)
                {
                    WriteStatementList(statement.Statements, formatter);
                }
                blockStatementLevel++;
            }

            private void WriteStatementList(StatementCollection statements, IFormatter formatter)
            {
                firstStmt = true;
                for (int i = 0; i < statements.Count; i++)
                {
                    WriteStatement(statements[i], formatter, i == statements.Count - 1);
                }
            }

            private void WriteMemoryCopyStatement(IMemoryCopyStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.Write("(");
                WriteExpression(statement.Source, formatter);
                formatter.Write(", ");
                WriteExpression(statement.Destination, formatter);
                formatter.Write(", ");
                WriteExpression(statement.Length, formatter);
                formatter.Write(")");
            }

            private void WriteMemoryInitializeStatement(IMemoryInitializeStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.Write("(");
                WriteExpression(statement.Offset, formatter);
                formatter.Write(", ");
                WriteExpression(statement.Value, formatter);
                formatter.Write(", ");
                WriteExpression(statement.Length, formatter);
                formatter.Write(")");
            }

            private void WriteDebugBreakStatement(IDebugBreakStatement value, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteComment("debug");
            }

            private void WriteLockStatement(ILockStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                WriteExpression(statement.Expression, formatter);
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteStatement(statement.Body, formatter);
                }
                formatter.WriteOutdent();
            }

            internal static IExpression InverseBooleanExpression(IExpression expression)
            {
                IBinaryExpression binaryExpression = expression as IBinaryExpression;
                if (binaryExpression != null)
                {
                    switch (binaryExpression.Operator)
                    {
                        case BinaryOperator.IdentityEquality:
                            return new BinaryExpression
                            {
                                Left = binaryExpression.Left,
                                Operator = BinaryOperator.IdentityInequality,
                                Right = binaryExpression.Right
                            };
                        case BinaryOperator.IdentityInequality:
                            return new BinaryExpression
                            {
                                Left = binaryExpression.Left,
                                Operator = BinaryOperator.IdentityEquality,
                                Right = binaryExpression.Right
                            };
                        case BinaryOperator.ValueEquality:
                            return new BinaryExpression
                            {
                                Left = binaryExpression.Left,
                                Operator = BinaryOperator.ValueInequality,
                                Right = binaryExpression.Right
                            };
                        case BinaryOperator.ValueInequality:
                            return new BinaryExpression
                            {
                                Left = binaryExpression.Left,
                                Operator = BinaryOperator.ValueEquality,
                                Right = binaryExpression.Right
                            };
                        case BinaryOperator.BooleanOr:
                            {
                                IExpression expression2 = InverseBooleanExpression(binaryExpression.Left);
                                IExpression expression3 = InverseBooleanExpression(binaryExpression.Right);
                                if (expression2 != null && expression3 != null)
                                {
                                    return new BinaryExpression
                                    {
                                        Left = expression2,
                                        Operator = BinaryOperator.BooleanAnd,
                                        Right = expression3
                                    };
                                }
                                break;
                            }
                        case BinaryOperator.BooleanAnd:
                            {
                                IExpression expression4 = InverseBooleanExpression(binaryExpression.Left);
                                IExpression expression5 = InverseBooleanExpression(binaryExpression.Right);
                                if (expression4 != null && expression5 != null)
                                {
                                    return new BinaryExpression
                                    {
                                        Left = expression4,
                                        Operator = BinaryOperator.BooleanOr,
                                        Right = expression5
                                    };
                                }
                                break;
                            }
                        case BinaryOperator.LessThan:
                            return new BinaryExpression
                            {
                                Left = binaryExpression.Left,
                                Operator = BinaryOperator.GreaterThanOrEqual,
                                Right = binaryExpression.Right
                            };
                        case BinaryOperator.LessThanOrEqual:
                            return new BinaryExpression
                            {
                                Left = binaryExpression.Left,
                                Operator = BinaryOperator.GreaterThan,
                                Right = binaryExpression.Right
                            };
                        case BinaryOperator.GreaterThan:
                            return new BinaryExpression
                            {
                                Left = binaryExpression.Left,
                                Operator = BinaryOperator.LessThanOrEqual,
                                Right = binaryExpression.Right
                            };
                        case BinaryOperator.GreaterThanOrEqual:
                            return new BinaryExpression
                            {
                                Left = binaryExpression.Left,
                                Operator = BinaryOperator.LessThan,
                                Right = binaryExpression.Right
                            };
                    }
                }
                IUnaryExpression unaryExpression = expression as IUnaryExpression;
                if (unaryExpression != null && unaryExpression.Operator == UnaryOperator.BooleanNot)
                {
                    return unaryExpression.Expression;
                }
                return new UnaryExpression
                {
                    Operator = UnaryOperator.BooleanNot,
                    Expression = expression
                };
            }

            private void WriteVariableListEntry(IVariableDeclaration variable, IFormatter formatter, ref bool hasvar)
            {
                if (variable != null)
                {
                    if (!hasvar)
                    {
                        formatter.WriteLine();
                        formatter.WriteIndent();
                        hasvar = true;
                    }
                    WriteVariableDeclaration(variable, formatter);
                }
            }

            private void WriteVariableList(IVariableDeclarationExpression expression, IFormatter formatter, ref bool hasvar)
            {
                if (expression != null)
                {
                    WriteVariableListEntry(expression.Variable, formatter, ref hasvar);
                }
            }

            private void WriteVariableList(IStatement statement, IFormatter formatter, ref bool hasvar)
            {
                if (statement is IBlockStatement blockStatement)
                {
                    WriteVariableList(blockStatement.Statements, formatter, ref hasvar);
                    return;
                }

                if (statement is ILabeledStatement labeledStatement)
                {
                    WriteVariableList(labeledStatement.Statement, formatter, ref hasvar);
                    return;
                }

                if (statement is IForEachStatement forEachStatement)
                {
                    WriteVariableListEntry(forEachStatement.Variable, formatter, ref hasvar);
                    WriteVariableList(forEachStatement.Body, formatter, ref hasvar);
                    return;
                }

                if (statement is IConditionStatement conditionStatement)
                {
                    WriteVariableList(conditionStatement.Then, formatter, ref hasvar);
                    WriteVariableList(conditionStatement.Else, formatter, ref hasvar);
                    return;
                }

                if (statement is IForStatement forStatement)
                {
                    WriteVariableList(forStatement.Initializer, formatter, ref hasvar);
                    WriteVariableList(forStatement.Body, formatter, ref hasvar);
                    return;
                }

                if (statement is ISwitchStatement switchStatement)
                {
                    foreach (object obj in switchStatement.Cases)
                    {
                        ISwitchCase switchCase = (ISwitchCase)obj;
                        WriteVariableList(switchCase.Body, formatter, ref hasvar);
                    }
                    return;
                }

                if (statement is IDoStatement doStatement)
                {
                    WriteVariableList(doStatement.Body, formatter, ref hasvar);
                    return;
                }

                if (statement is ILockStatement lockStatement)
                {
                    WriteVariableList(lockStatement.Body, formatter, ref hasvar);
                    return;
                }

                if (statement is IWhileStatement whileStatement)
                {
                    WriteVariableList(whileStatement.Body, formatter, ref hasvar);
                    return;
                }

                if (statement is IFixedStatement fixedStatement)
                {
                    WriteVariableListEntry(fixedStatement.Variable, formatter, ref hasvar);
                    WriteVariableList(fixedStatement.Body, formatter, ref hasvar);
                    return;
                }

                if (statement is IUsingStatement usingStatement)
                {
                    if (usingStatement.Expression is IAssignExpression assignExpression)
                    {
                        IVariableDeclarationExpression variableDeclarationExpression = assignExpression.Target as IVariableDeclarationExpression;
                        if (variableDeclarationExpression != null)
                        {
                            WriteVariableListEntry(variableDeclarationExpression.Variable, formatter, ref hasvar);
                        }
                    }
                    return;
                }

                if (statement is ITryCatchFinallyStatement tryCatchFinallyStatement)
                {
                    WriteVariableList(tryCatchFinallyStatement.Try, formatter, ref hasvar);
                    foreach (object obj2 in tryCatchFinallyStatement.CatchClauses)
                    {
                        ICatchClause catchClause = (ICatchClause)obj2;
                        WriteVariableList(catchClause.Body, formatter, ref hasvar);
                    }
                    WriteVariableList(tryCatchFinallyStatement.Fault, formatter, ref hasvar);
                    WriteVariableList(tryCatchFinallyStatement.Finally, formatter, ref hasvar);
                    return;
                }

                if (statement is IExpressionStatement expressionStatement)
                {
                    WriteVariableList(expressionStatement.Expression as IVariableDeclarationExpression, formatter, ref hasvar);
                }
            }

            private void WriteVariableList(StatementCollection statements, IFormatter formatter, ref bool hasvar)
            {
                foreach (IStatement statement in statements)
                {
                    WriteVariableList(statement, formatter, ref hasvar);
                }
            }

            private void WriteCommentStatement(ICommentStatement statement, IFormatter formatter)
            {
                WriteComment(statement.Comment, formatter);
            }

            private void WriteComment(IComment comment, IFormatter formatter)
            {
                string[] array = comment.Text.Split(new char[]
                {
                    '\n'
                });
                if (array.Length <= 1)
                {
                    foreach (string value in array)
                    {
                        formatter.WriteComment("# ");
                        formatter.WriteComment(value);
                        formatter.WriteLine();
                    }
                    return;
                }
                formatter.WriteComment("{ ");
                formatter.WriteLine();
                foreach (string value2 in array)
                {
                    formatter.WriteComment(value2);
                    formatter.WriteLine();
                }
                formatter.WriteComment("}");
                formatter.WriteLine();
            }

            private void WriteMethodReturnStatement(IMethodReturnStatement statement, IFormatter formatter, bool lastStatement)
            {
                WriteStatementSeparator(formatter);
                if (statement.Expression == null)
                {
                    return;
                }
                if (lastStatement)
                {
                    int num = blockStatementLevel;
                }
                formatter.Write("return ");
                WriteExpression(statement.Expression, formatter);
                if (!lastStatement || blockStatementLevel > 1)
                {
                    formatter.WriteLine();
                }
            }

            private void WriteConditionStatement(IConditionStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("if");
                formatter.Write(" ");
                if (statement.Condition is IBinaryExpression)
                {
                    WriteExpression(statement.Condition, formatter);
                }
                else
                {
                    formatter.Write("(");
                    WriteExpression(statement.Condition, formatter);
                    formatter.Write(")");
                }
                formatter.WriteComment(":");
                formatter.WriteLine();
                if (statement.Then.Statements.Count > 1)
                {
                    formatter.WriteIndent();
                }
                else
                {
                    formatter.WriteIndent();
                }
                if (statement.Then != null)
                {
                    WriteStatement(statement.Then, formatter);
                }
                if (statement.Then.Statements.Count > 1)
                {
                    formatter.WriteOutdent();
                }
                else
                {
                    MakePendingOutdent();
                }
                if (statement.Else != null && statement.Else.Statements.Count > 0)
                {
                    WritePendingOutdent(formatter);
                    formatter.WriteLine();
                    formatter.WriteKeyword("else:");
                    formatter.WriteLine();
                    formatter.WriteIndent();
                    if (statement.Else != null)
                    {
                        WriteStatement(statement.Else, formatter);
                    }
                    if (statement.Else.Statements.Count > 1)
                    {
                        WritePendingOutdent(formatter);
                        formatter.WriteOutdent();
                        return;
                    }
                    MakePendingOutdent();
                }
            }

            private void WriteTryCatchFinallyStatement(ITryCatchFinallyStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                if ((statement.Finally != null && statement.Finally.Statements.Count > 0) || statement.CatchClauses.Count > 0)
                {
                    formatter.WriteKeyword("try:");
                    formatter.WriteLine();
                    formatter.WriteIndent();
                }
                if (statement.Try != null)
                {
                    WriteStatement(statement.Try, formatter);
                }
                WritePendingOutdent(formatter);
                formatter.WriteLine();
                if (statement.CatchClauses.Count > 0)
                {
                    formatter.WriteOutdent();
                    formatter.WriteKeyword("except");
                    firstStmt = true;
                    foreach (object obj in statement.CatchClauses)
                    {
                        ICatchClause catchClause = (ICatchClause)obj;
                        WriteStatementSeparator(formatter);
                        ITypeReference value = (ITypeReference)catchClause.Variable.VariableType;
                        bool flag = catchClause.Variable.Name.Length == 0;
                        bool flag2 = IsType(value, "System", "Object");
                        if (!flag || !flag2)
                        {
                            formatter.Write(" ");
                            WriteType(catchClause.Variable.VariableType, formatter);
                            formatter.WriteKeyword(":");
                            formatter.WriteLine();
                            formatter.WriteIndent();
                        }
                        if (catchClause.Condition != null)
                        {
                            formatter.WriteIndent();
                            formatter.WriteKeyword("if");
                            formatter.Write(" ");
                            WriteExpression(catchClause.Condition, formatter);
                            formatter.WriteKeyword(": ");
                        }
                        if (catchClause.Body != null)
                        {
                            WriteStatement(catchClause.Body, formatter);
                        }
                    }
                }
                if (statement.Finally != null && statement.Finally.Statements.Count > 0)
                {
                    formatter.WriteLine();
                    formatter.WriteOutdent();
                    formatter.WriteKeyword("finally:");
                    formatter.WriteKeyword(" ");
                    formatter.WriteLine();
                    formatter.WriteIndent();
                    if (statement.Finally != null)
                    {
                        WriteStatement(statement.Finally, formatter);
                        formatter.WriteOutdent();
                    }
                    WritePendingOutdent(formatter);
                }
                if (statement.CatchClauses.Count > 0)
                {
                    WritePendingOutdent(formatter);
                    formatter.WriteOutdent();
                }
            }

            private void WriteAssignExpression(IAssignExpression value, IFormatter formatter)
            {
                if (value.Expression is IBinaryExpression binaryExpression && value.Target.Equals(binaryExpression.Left))
                {
                    string text = string.Empty;
                    switch (binaryExpression.Operator)
                    {
                        case BinaryOperator.Add:
                            text = "inc";
                            break;
                        case BinaryOperator.Subtract:
                            text = "dec";
                            break;
                    }
                    if (text.Length != 0)
                    {
                        if (text == "inc")
                        {
                            WriteExpression(value.Target, formatter);
                            formatter.Write(" += ");
                            WriteExpression(binaryExpression.Right, formatter);
                        }
                        if (text == "dec")
                        {
                            WriteExpression(value.Target, formatter);
                            formatter.Write(" -= ");
                            WriteExpression(binaryExpression.Right, formatter);
                        }
                        return;
                    }
                }
                WriteExpression(value.Target, formatter);
                formatter.Write(" = ");
                WriteExpression(value.Expression, formatter);
            }

            private void WriteExpressionStatement(IExpressionStatement statement, IFormatter formatter)
            {
                if (!(statement.Expression is IVariableDeclarationExpression))
                {
                    WriteStatementSeparator(formatter);
                    IUnaryExpression unaryExpression = statement.Expression as IUnaryExpression;
                    if (unaryExpression != null && unaryExpression.Operator == UnaryOperator.PostIncrement)
                    {
                        WriteExpression(unaryExpression.Expression, formatter);
                        formatter.Write(" += 1");
                        return;
                    }
                    if (unaryExpression != null && unaryExpression.Operator == UnaryOperator.PostDecrement)
                    {
                        WriteExpression(unaryExpression.Expression, formatter);
                        formatter.Write(" -= 1");
                        return;
                    }
                    WriteExpression(statement.Expression, formatter);
                }
            }

            private void WriteForStatement(IForStatement statement, IFormatter formatter)
            {
                bool flag = false;
                IExpressionStatement expressionStatement = statement.Initializer as IExpressionStatement;
                IExpressionStatement expressionStatement2 = statement.Increment as IExpressionStatement;
                IAssignExpression assignExpression = null;
                IAssignExpression assignExpression2 = null;
                if (expressionStatement != null)
                {
                    assignExpression = (expressionStatement.Expression as IAssignExpression);
                }
                if (expressionStatement2 != null)
                {
                    assignExpression2 = (expressionStatement2.Expression as IAssignExpression);
                }
                IBinaryExpression binaryExpression = statement.Condition as IBinaryExpression;
                IBinaryExpression binaryExpression2 = null;
                if (assignExpression != null && assignExpression2 != null && binaryExpression != null)
                {
                    IVariableReferenceExpression variableReferenceExpression = assignExpression.Target as IVariableReferenceExpression;
                    IVariableReferenceExpression variableReferenceExpression2 = assignExpression2.Target as IVariableReferenceExpression;
                    binaryExpression2 = (assignExpression2.Expression as IBinaryExpression);
                    if (variableReferenceExpression != null && variableReferenceExpression2 != null && binaryExpression2 != null && binaryExpression.Left is IVariableReferenceExpression variableReferenceExpression3 && variableReferenceExpression.Variable == variableReferenceExpression2.Variable && variableReferenceExpression.Variable == variableReferenceExpression3.Variable)
                    {
                        if (binaryExpression2.Left is IVariableReferenceExpression variableReferenceExpression4 && binaryExpression2.Right is ILiteralExpression literalExpression && variableReferenceExpression.Variable == variableReferenceExpression4.Variable && literalExpression.Value.Equals(1))
                        {
                            if (binaryExpression2.Operator == BinaryOperator.Add && (binaryExpression.Operator == BinaryOperator.LessThan || binaryExpression.Operator == BinaryOperator.LessThanOrEqual))
                            {
                                flag = true;
                            }
                            else if (binaryExpression2.Operator == BinaryOperator.Subtract && (binaryExpression.Operator == BinaryOperator.GreaterThan || binaryExpression.Operator == BinaryOperator.GreaterThanOrEqual))
                            {
                                flag = true;
                            }
                        }
                    }
                }
                if (flag)
                {
                    formatter.WriteKeyword("for ");
                    firstStmt = true;
                    WriteStatement(statement.Initializer, formatter);
                    if (binaryExpression2.Operator == BinaryOperator.Add)
                    {
                        formatter.WriteComment(" to ");
                    }
                    else
                    {
                        formatter.WriteComment(" downto ");
                    }
                    WriteExpression(binaryExpression.Right, formatter);
                    if (binaryExpression2.Operator == BinaryOperator.Add)
                    {
                        if (binaryExpression.Operator == BinaryOperator.LessThan)
                        {
                            formatter.WriteLiteral("-1 ");
                        }
                    }
                    else if (binaryExpression.Operator == BinaryOperator.GreaterThan)
                    {
                        formatter.WriteLiteral("+1 ");
                    }
                }
                else
                {
                    if (statement.Initializer != null)
                    {
                        WriteStatement(statement.Initializer, formatter);
                        WriteStatementSeparator(formatter);
                    }
                    formatter.WriteKeyword("while");
                    formatter.Write(" ");
                    formatter.Write("(");
                    if (statement.Condition != null)
                    {
                        WriteExpression(statement.Condition, formatter);
                    }
                    else
                    {
                        formatter.WriteLiteral("True");
                    }
                    formatter.Write(")");
                }
                formatter.Write(" ");
                formatter.WriteKeyword(":");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteStatement(statement.Body, formatter);
                }
                if (!flag && statement.Increment != null)
                {
                    WriteStatement(statement.Increment, formatter);
                }
                formatter.WriteOutdent();
            }

            private void WriteForEachStatement(IForEachStatement value, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                TextFormatter textFormatter = new TextFormatter();
                WriteVariableDeclaration(value.Variable, textFormatter);
                formatter.WriteKeyword("for");
                formatter.Write(" ");
                formatter.WriteReference(value.Variable.Name, textFormatter.ToString(), null);
                formatter.Write(" ");
                formatter.WriteKeyword("in");
                formatter.Write(" ");
                WriteExpression(value.Expression, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword(":");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (value.Body != null)
                {
                    WriteStatement(value.Body, formatter);
                }
                formatter.WriteOutdent();
            }

            private void WriteUsingStatement(IUsingStatement statement, IFormatter formatter)
            {
                IVariableReference variableReference = null;
                IAssignExpression assignExpression = statement.Expression as IAssignExpression;
                if (assignExpression != null)
                {
                    IVariableDeclarationExpression variableDeclarationExpression = assignExpression.Target as IVariableDeclarationExpression;
                    if (variableDeclarationExpression != null)
                    {
                        variableReference = variableDeclarationExpression.Variable;
                    }
                    IVariableReferenceExpression variableReferenceExpression = assignExpression.Target as IVariableReferenceExpression;
                    if (variableReferenceExpression != null)
                    {
                        variableReference = variableReferenceExpression.Variable;
                    }
                }
                WriteStatementSeparator(formatter);
                formatter.Write("with ");
                if (variableReference != null)
                {
                    formatter.Write(" ");
                    WriteVariableReference(variableReference, formatter);
                }
                formatter.Write(":");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (variableReference != null)
                {
                    WriteVariableReference(variableReference, formatter);
                    formatter.Write(" ");
                    formatter.WriteKeyword("=");
                    formatter.Write(" ");
                    WriteExpression(assignExpression.Expression, formatter);
                    WriteStatementSeparator(formatter);
                }
                formatter.WriteKeyword("try:");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteBlockStatement(statement.Body, formatter);
                }
                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.WriteKeyword("finally:");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (variableReference != null)
                {
                    firstStmt = true;
                    WriteVariableReference(variableReference, formatter);
                    formatter.Write(".");
                    formatter.Write("Dispose()");
                    formatter.WriteLine();
                }
                else
                {
                    firstStmt = true;
                    WriteExpression(statement.Expression);
                    formatter.Write(".");
                    formatter.Write("Dispose()");
                    formatter.WriteLine();
                }
                formatter.WriteOutdent();
                formatter.WriteOutdent();
            }

            private void WriteFixedStatement(IFixedStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.Write(" ");
                formatter.Write("(");
                WriteVariableDeclaration(statement.Variable, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("=");
                formatter.Write(" ");
                WriteExpression(statement.Expression, formatter);
                formatter.Write(")");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteBlockStatement(statement.Body, formatter);
                }
                formatter.WriteOutdent();
            }

            private void WriteWhileStatement(IWhileStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("while");
                formatter.Write(" ");
                if (statement.Condition != null)
                {
                    formatter.Write("(");
                    WriteExpression(statement.Condition, formatter);
                    formatter.Write(")");
                }
                else
                {
                    formatter.WriteLiteral("True");
                }
                formatter.Write(" ");
                formatter.WriteKeyword(":");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteStatement(statement.Body, formatter);
                }
                formatter.WriteOutdent();
            }

            private void WriteDoStatement(IDoStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteComment("repeat");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteStatement(statement.Body, formatter);
                }
                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.WriteComment("until");
                formatter.Write(" ");
                if (statement.Condition != null)
                {
                    WriteExpression(InverseBooleanExpression(statement.Condition), formatter);
                    return;
                }
                formatter.WriteLiteral("True");
            }

            private void WriteBreakStatement(IBreakStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("break");
                formatter.WriteLine();
            }

            private void WriteContinueStatement(IContinueStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("continue");
                formatter.WriteLine();
            }

            private void WriteThrowExceptionStatement(IThrowExceptionStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("raise");
                formatter.Write(" ");
                if (statement.Expression != null)
                {
                    WriteExpression(statement.Expression, formatter);
                    return;
                }
                WriteDeclaration("Exception", formatter);
            }

            private void WriteVariableDeclarationExpression(IVariableDeclarationExpression expression, IFormatter formatter)
            {
                WriteVariableReference(expression.Variable, formatter);
            }

            private void WriteVariableDeclaration(IVariableDeclaration variableDeclaration, IFormatter formatter)
            {
                WriteDeclaration(variableDeclaration.Name, formatter);
                formatter.Write(" = ");
                WriteType(variableDeclaration.VariableType, formatter);
                formatter.Write("()");
                if (variableDeclaration.Pinned)
                {
                    formatter.Write(" ");
                }
                if (!forLoop)
                {
                    formatter.WriteLine();
                }
            }

            private void WriteAttachEventStatement(IAttachEventStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                WriteEventReferenceExpression(statement.Event, formatter);
                formatter.Write(" += ");
                WriteExpression(statement.Listener, formatter);
                formatter.WriteLine();
            }

            private void WriteRemoveEventStatement(IRemoveEventStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                WriteEventReferenceExpression(statement.Event, formatter);
                formatter.Write(" -= ");
                WriteExpression(statement.Listener, formatter);
                formatter.WriteLine();
            }

            private void WriteSwitchStatement(ISwitchStatement statement, IFormatter formatter)
            {
                bool flag = true;
                foreach (ISwitchCase switchCase in statement.Cases)
                {
                    if (switchCase is IConditionCase conditionCase)
                    {
                        if (flag)
                        {
                            formatter.WriteKeyword("if");
                            formatter.Write(" ");
                            WriteExpression(statement.Expression, formatter);
                            formatter.Write(" = ");
                            WriteSwitchCaseCondition(conditionCase.Condition, formatter);
                            flag = false;
                            formatter.WriteIndent();
                        }
                        else
                        {
                            formatter.WriteOutdent();
                            formatter.WriteKeyword("elif");
                            formatter.Write(" ");
                            WriteExpression(statement.Expression, formatter);
                            formatter.Write(" = ");
                            WriteSwitchCaseCondition(conditionCase.Condition, formatter);
                            formatter.WriteIndent();
                        }
                    }

                    if (switchCase is IDefaultCase defaultCase)
                    {
                        formatter.WriteKeyword("else:");
                        formatter.WriteLine();
                        formatter.WriteIndent();
                    }
                    if (switchCase.Body != null)
                    {
                        WriteStatement(switchCase.Body, formatter);
                    }
                }
            }

            private void WriteSwitchCaseCondition(IExpression condition, IFormatter formatter)
            {
                if (condition is IBinaryExpression binaryExpression && binaryExpression.Operator == BinaryOperator.BooleanOr)
                {
                    WriteSwitchCaseCondition(binaryExpression.Left, formatter);
                    WriteSwitchCaseCondition(binaryExpression.Right, formatter);
                    return;
                }
                WriteExpression(condition, formatter);
                formatter.Write(":");
                formatter.WriteLine();
            }

            private void WriteGotoStatement(IGotoStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteComment("goto");
                formatter.Write(" ");
                WriteDeclaration(statement.Name, formatter);
            }

            private void WriteLabeledStatement(ILabeledStatement statement, IFormatter formatter)
            {
                if (statement.Statement != null)
                {
                    WriteStatementSeparator(formatter);
                }
                formatter.WriteLine();
                formatter.WriteOutdent();
                WriteDeclaration(statement.Name, formatter);
                formatter.Write(":");
                formatter.WriteLine();
                formatter.WriteIndent();
                firstStmt = true;
                if (statement.Statement != null)
                {
                    WriteStatement(statement.Statement, formatter);
                }
            }

            private void WriteDeclaringType(ITypeReference value, IFormatter formatter)
            {
                formatter.WriteProperty("Declaring Type", GetPythonStyleResolutionScope(value));
                WriteDeclaringAssembly(Helper.GetAssemblyReference(value), formatter);
            }

            private void WriteDeclaringAssembly(IAssemblyReference value, IFormatter formatter)
            {
                if (value != null)
                {
                    string value2 = (value.Name != null && value.Version != null) ? (value.Name + ", Version=" + value.Version.ToString()) : value.ToString();
                    formatter.WriteProperty("Assembly", value2);
                }
            }

            private string GetTypeReferenceDescription(ITypeReference typeReference)
            {
                return Helper.GetNameWithResolutionScope(typeReference);
            }

            private string GetFieldReferenceDescription(IFieldReference fieldReference)
            {
                IFormatter formatter = new TextFormatter();
                WriteType(fieldReference.FieldType, formatter);
                formatter.Write(" ");
                formatter.Write(GetTypeReferenceDescription(fieldReference.DeclaringType as ITypeReference));
                formatter.Write(".");
                WriteDeclaration(fieldReference.Name, formatter);
                return formatter.ToString();
            }

            private string GetMethodReferenceDescription(IMethodReference value)
            {
                IFormatter formatter = new TextFormatter();
                if (IsConstructor(value))
                {
                    formatter.Write(GetTypeReferenceDescription(value.DeclaringType as ITypeReference));
                    formatter.Write(".");
                    formatter.Write(Helper.GetNameWithResolutionScope(value.DeclaringType as ITypeReference));
                }
                else
                {
                    WriteType(value.ReturnType.Type, formatter);
                    formatter.Write(" ");
                    formatter.Write(Helper.GetNameWithResolutionScope(value.DeclaringType as ITypeReference));
                    formatter.Write(".");
                    formatter.Write(value.Name);
                }
                WriteGenericArgumentList(value.GenericArguments, formatter);
                formatter.Write("(");
                WriteParameterDeclarationList(value.Parameters, formatter, null);
                MethodCallingConvention callingConvention = value.CallingConvention;
                formatter.Write(")");
                return formatter.ToString();
            }

            private string GetPropertyReferenceDescription(IPropertyReference propertyReference)
            {
                IFormatter formatter = new TextFormatter();
                WriteType(propertyReference.PropertyType, formatter);
                formatter.Write(" ");
                string text = propertyReference.Name;
                if (text == "Item")
                {
                    text = "self";
                }
                formatter.Write(GetTypeReferenceDescription(propertyReference.DeclaringType as ITypeReference));
                formatter.Write(".");
                WriteDeclaration(text, formatter);
                IParameterDeclarationCollection parameters = propertyReference.Parameters;
                if (parameters.Count > 0)
                {
                    formatter.Write("[");
                    WriteParameterDeclarationList(parameters, formatter, null);
                    formatter.Write("]");
                }
                formatter.Write(" ");
                formatter.Write("{ ... }");
                return formatter.ToString();
            }

            private string GetEventReferenceDescription(IEventReference eventReference)
            {
                IFormatter formatter = new TextFormatter();
                formatter.WriteComment("event");
                formatter.Write(" ");
                WriteType(eventReference.EventType, formatter);
                formatter.Write(" ");
                formatter.Write(GetTypeReferenceDescription(eventReference.DeclaringType as ITypeReference));
                formatter.Write(".");
                WriteDeclaration(eventReference.Name, formatter);
                return formatter.ToString();
            }

            private static bool IsType(IType value, string namespaceName, string name)
            {
                return IsType(value, namespaceName, name, "mscorlib") || IsType(value, namespaceName, name, "sscorlib");
            }

            private static bool IsType(IType value, string namespaceName, string name, string assemblyName)
            {
                if (value is ITypeReference typeReference)
                {
                    return typeReference.Name == name && typeReference.Namespace == namespaceName && IsAssemblyReference(typeReference, assemblyName);
                }

                if (value is IRequiredModifier requiredModifier)
                {
                    return IsType(requiredModifier.ElementType, namespaceName, name);
                }

                return value is IOptionalModifier optionalModifier && IsType(optionalModifier.ElementType, namespaceName, name);
            }

            private static bool IsAssemblyReference(ITypeReference value, string assemblyName)
            {
                return Helper.GetAssemblyReference(value).Name == assemblyName;
            }

            private ICustomAttribute GetCustomAttribute(ICustomAttributeProvider value, string namespaceName, string name)
            {
                ICustomAttribute customAttribute = GetCustomAttribute(value, namespaceName, name, "mscorlib");
                if (customAttribute == null)
                {
                    customAttribute = GetCustomAttribute(value, namespaceName, name, "sscorlib");
                }
                return customAttribute;
            }

            private ICustomAttribute GetCustomAttribute(ICustomAttributeProvider value, string namespaceName, string name, string assemblyName)
            {
                foreach (object obj in value.Attributes)
                {
                    ICustomAttribute customAttribute = (ICustomAttribute)obj;
                    if (IsType(customAttribute.Constructor.DeclaringType, namespaceName, name, assemblyName))
                    {
                        return customAttribute;
                    }
                }
                return null;
            }

            private ILiteralExpression GetDefaultParameterValue(IParameterDeclaration value)
            {
                ICustomAttribute customAttribute = GetCustomAttribute(value, "System.Runtime.InteropServices", "DefaultParameterValueAttribute", "System");
                if (customAttribute != null && customAttribute.Arguments.Count == 1)
                {
                    return customAttribute.Arguments[0] as ILiteralExpression;
                }
                return null;
            }

            private bool IsConstructor(IMethodReference value)
            {
                return value.Name == ".ctor" || value.Name == ".cctor";
            }

            private bool IsEnumerationElement(IFieldDeclaration value)
            {
                IType fieldType = value.FieldType;
                IType declaringType = value.DeclaringType;
                if (fieldType.Equals(declaringType))
                {
                    if (fieldType is ITypeReference typeReference)
                    {
                        return Helper.IsEnumeration(typeReference);
                    }
                }
                return false;
            }

            private string QuoteLiteralExpression(string text)
            {
                string result;
                using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
                {
                    foreach (char c in text)
                    {
                        ushort num = (ushort)c;
                        if (num > 255)
                        {
                            stringWriter.Write("#$" + num.ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            char c2 = c;
                            if (c2 != '\0')
                            {
                                switch (c2)
                                {
                                    case '\t':
                                        stringWriter.Write("#9");
                                        goto IL_BD;
                                    case '\n':
                                        stringWriter.Write("#10");
                                        goto IL_BD;
                                    case '\v':
                                    case '\f':
                                        break;
                                    case '\r':
                                        stringWriter.Write("#13");
                                        goto IL_BD;
                                    default:
                                        if (c2 == '\'')
                                        {
                                            stringWriter.Write("''");
                                            goto IL_BD;
                                        }
                                        break;
                                }
                                stringWriter.Write(c);
                            }
                            else
                            {
                                stringWriter.Write("#0");
                            }
                        }
                    IL_BD:;
                    }
                    result = stringWriter.ToString();
                }
                return result;
            }

            private void WriteDeclaration(string name, IFormatter formatter)
            {
                formatter.WriteDeclaration((Array.IndexOf<string>(keywords, name) != -1) ? ("&" + name) : name);
            }

            private void WriteDeclaration(string name, object target, IFormatter formatter)
            {
                formatter.WriteDeclaration((Array.IndexOf<string>(keywords, name) != -1) ? ("&" + name) : name, target);
            }

            private void WriteReference(string name, IFormatter formatter, string toolTip, object reference)
            {
                string value = name;
                //name.Equals(".ctor");
                //name.Equals("..ctor");
                if (Array.IndexOf<string>(keywords, name) != -1)
                {
                    value = name;
                }
                formatter.WriteReference(value, toolTip, reference);
            }

            private IFormatter _formatter;

            private ILanguageWriterConfiguration configuration;

            private static Hashtable specialMethodNames;

            private static Hashtable specialTypeNames;

            private bool forLoop;

            private bool firstStmt;

            private int pendingOutdent;

            private int blockStatementLevel;

            private NumberFormat _numberFormat;

            private string[] keywords = new string[]
            {
                "and",
                "as",
                "assert",
                "break",
                "class",
                "continue",
                "def",
                "del",
                "elif",
                "else",
                "except",
                "exec",
                "finally",
                "for",
                "from",
                "global",
                "if",
                "import",
                "in",
                "is",
                "lambda",
                "not",
                "or",
                "pass",
                "print",
                "raise",
                "return",
                "try",
                "while",
                "yield",
                "None",
                "True",
                "False",
                "__import__",
                "abs",
                "callable",
                "chr",
                "classmethod",
                "cmp",
                "coerce",
                "compile",
                "delattr",
                "dir",
                "divmod",
                "enumerate",
                "eval",
                "execfile",
                "filter",
                "getattr",
                "globals",
                "hasattr",
                "hash",
                "help",
                "hex",
                "id",
                "input",
                "isinstance",
                "issubclass",
                "iter",
                "len",
                "locals",
                "map",
                "max",
                "min",
                "oct",
                "open",
                "ord",
                "pow",
                "property",
                "range",
                "raw_input",
                "reduce",
                "reload",
                "repr",
                "reversed",
                "round",
                "set",
                "setattr",
                "sorted",
                "staticmethod",
                "sum",
                "super",
                "unichr",
                "vars",
                "zip",
                "basestring",
                "bool",
                "buffer",
                "complex",
                "dict",
                "exception",
                "file",
                "float",
                "frozenset",
                "int",
                "list",
                "long",
                "object",
                "set",
                "slice",
                "str",
                "tuple",
                "type",
                "unicode",
                "xrange",
                "__future__",
                "with"
            };

            private enum NumberFormat
            {
                Auto,
                Hexadecimal,
                Decimal
            }

            private class TextFormatter : IFormatter
            {
                public override string ToString()
                {
                    return _writer.ToString();
                }

                public void WriteCustom(string value)
                {
                    //TODO:
                    Write(value);
                }

                public void Write(string text)
                {
                    ApplyIndent();
                    _writer.Write(text);
                }

                public void WriteDeclaration(string text)
                {
                    WriteBold(text);
                }

                public void WriteDeclaration(string text, object target)
                {
                    WriteBold(text);
                }

                public void WriteComment(string text)
                {
                    WriteColor(text, 0x808080);
                }

                public void WriteLiteral(string text)
                {
                    WriteColor(text, 0x800000);
                }

                public void WriteKeyword(string text)
                {
                    WriteColor(text, 0x80);
                }

                public void WriteIndent()
                {
                    indent++;
                }

                public void WriteLine()
                {
                    _writer.WriteLine();
                    newLine = true;
                }

                public void WriteOutdent()
                {
                    indent--;
                }

                public void WriteReference(string text, string toolTip, object reference)
                {
                    ApplyIndent();
                    _writer.Write(text);
                }

                public void WriteProperty(string propertyName, string propertyValue)
                {
                }

                private void WriteBold(string text)
                {
                    ApplyIndent();
                    _writer.Write(text);
                }

                private void WriteColor(string text, int color)
                {
                    ApplyIndent();
                    _writer.Write(text);
                }

                private void ApplyIndent()
                {
                    if (newLine)
                    {
                        for (int i = 0; i < indent; i++)
                        {
                            _writer.Write("    ");
                        }
                        newLine = false;
                    }
                }

                private StringWriter _writer = new StringWriter(CultureInfo.InvariantCulture);

                private bool newLine;

                private int indent;
            }
        }
    }
}
