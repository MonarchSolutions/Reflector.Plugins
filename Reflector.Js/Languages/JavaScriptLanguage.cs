// -----------------------------------------------------------
// JavaScript Language view for Lutz Roeder's .NET Reflector
// Copyright (C) 2011 Frank A. Krueger. All rights reserved.
// fak@praeclarum.org
//
// based on DelphiLanguage from Lutz Roeder
// ----------------------------------------

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Reflector.Application;
using Reflector.CodeModel;
using Reflector.CodeModel.Memory;
using Reflector.Disassembler;
// ReSharper disable StringLiteralTypo

namespace Reflector.Js.Languages
{
    internal class JavaScriptLanguage : ILanguage
    {
        private bool addInMode;

        public JavaScriptLanguage()
        {
            addInMode = false;
        }

        public JavaScriptLanguage(bool addInMode)
        {
            this.addInMode = addInMode;
        }

        public string Name => "JavaScript";

        public string FileExtension => ".js";

        public bool Translate => true;

        public ILanguageWriter GetWriter(IFormatter formatter, ILanguageWriterConfiguration configuration)
        {
            return new LanguageWriter(formatter, configuration);
        }

        public Language LanguageType => Language.CSharpNet;

        internal class LanguageWriter : ILanguageWriter
        {
            private IFormatter _formatter;
            private ILanguageWriterConfiguration _configuration;

            private static Hashtable specialMethodNames;
            private static Hashtable specialTypeNames;
            private bool forLoop = false;
            private bool firstStmt = false;
            private int pendingOutdent = 0;
            private int blockStatementLevel = 0;
            private NumberFormat numberFormat;

            private enum NumberFormat
            {
                Auto,
                Hexadecimal,
                Decimal
            }

            public LanguageWriter(IFormatter formatter, ILanguageWriterConfiguration configuration)
            {
                _formatter = formatter;
                _configuration = configuration;

                if (specialTypeNames == null)
                {
                    specialTypeNames = new Hashtable
                    {
                        ["Void"] = " ",
                        ["Object"] = "TObject",
                        ["String"] = "string",
                        ["SByte"] = "Shortint",
                        ["Byte"] = "Byte",
                        ["Int16"] = "Smallint",
                        ["UInt16"] = "Word",
                        ["Int32"] = "Integer",
                        ["UInt32"] = "Cardinal",
                        ["Int64"] = "Int64",
                        ["UInt64"] = "UInt64",
                        ["Char"] = "Char",
                        ["Boolean"] = "boolean",
                        ["Single"] = "Single",
                        ["Double"] = "Double",
                        ["Decimal"] = "Decimal"
                    };
                }

                if (specialMethodNames == null)
                {
                    specialMethodNames = new Hashtable
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

                switch (configuration["NumberFormat"])
                {
                    case "Hexadecimal":
                        numberFormat = NumberFormat.Hexadecimal;
                        break;

                    case "Decimal":
                        numberFormat = NumberFormat.Decimal;
                        break;

                    default:
                        numberFormat = NumberFormat.Auto;
                        break;
                }
            }

            public void WriteAssembly(IAssembly value)
            {
                _formatter.Write("// JS Assembly");
                _formatter.Write(" ");
                _formatter.WriteDeclaration(value.Name);

                if (value.Version != null)
                {
                    _formatter.Write(", ");
                    _formatter.Write("Version");
                    _formatter.Write(" ");
                    _formatter.Write(value.Version.ToString());
                }

                _formatter.WriteLine();

                if ((_configuration["ShowCustomAttributes"] == "true") && (value.Attributes.Count != 0))
                {
                    _formatter.WriteLine();
                    WriteCustomAttributeList(value, _formatter);
                    _formatter.WriteLine();
                }

                _formatter.WriteProperty("Location", value.Location);
                _formatter.WriteProperty("Name", value.ToString());

                switch (value.Type)
                {
                    case AssemblyType.Application:
                        _formatter.WriteProperty("Type", "Windows Application");
                        break;

                    case AssemblyType.Console:
                        _formatter.WriteProperty("Type", "Console Application");
                        break;

                    case AssemblyType.Library:
                        _formatter.WriteProperty("Type", "Library");
                        break;
                }
            }

            public void WriteAssemblyReference(IAssemblyReference value)
            {
                _formatter.Write("// Assembly Reference");
                _formatter.Write(" ");
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteLine();

                _formatter.WriteProperty("Version", value.Version.ToString());
                _formatter.WriteProperty("Name", value.ToString());
            }

            public void WriteModule(IModule value)
            {
                _formatter.Write("// Module");
                _formatter.Write(" ");
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteLine();

                if ((_configuration["ShowCustomAttributes"] == "true") && (value.Attributes.Count != 0))
                {
                    _formatter.WriteLine();
                    WriteCustomAttributeList(value, _formatter);
                    _formatter.WriteLine();
                }

                _formatter.WriteProperty("Version", value.Version.ToString());
                _formatter.WriteProperty("Location", value.Location);

                string location = Environment.ExpandEnvironmentVariables(value.Location);
                if (File.Exists(location))
                {
                    _formatter.WriteProperty("Size", new FileInfo(location).Length + " Bytes");
                }
            }

            public void WriteModuleReference(IModuleReference value)
            {
                _formatter.Write("// Module Reference");
                _formatter.Write(" ");
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteLine();
            }

            public void WriteResource(IResource value)
            {
                _formatter.Write("// ");

                switch (value.Visibility)
                {
                    case ResourceVisibility.Public:
                        _formatter.WriteKeyword("public");
                        break;

                    case ResourceVisibility.Private:
                        _formatter.WriteKeyword("private");
                        break;
                }

                _formatter.Write(" ");
                _formatter.WriteKeyword("resource");
                _formatter.Write(" ");
                _formatter.WriteDeclaration(value.Name, value);
                _formatter.WriteLine();

                if ((value is IEmbeddedResource embeddedResource) && (embeddedResource.Value != null))
                {
                    _formatter.WriteProperty("Size", embeddedResource.Value.Length.ToString(CultureInfo.InvariantCulture) + " bytes");
                }

                if (value is IFileResource fileResource)
                {
                    _formatter.WriteProperty("Location", fileResource.Location);
                }
            }

            public void WriteNamespace(INamespace value)
            {
                _formatter.WriteKeyword("unit ");
                if (value.Name.Length != 0)
                {
                    _formatter.Write(" ");
                    WriteDeclaration(value.Name, _formatter);
                }

                _formatter.Write(";");

                if (_configuration["ShowNamespaceBody"] == "true")
                {
                    _formatter.WriteLine();
                    _formatter.WriteKeyword("interface");
                    _formatter.WriteLine();
                    _formatter.WriteKeyword("type");
                    _formatter.WriteLine();
                    // formatter.WriteIndent();

                    ArrayList types = new ArrayList();
                    foreach (ITypeDeclaration typeDeclaration in value.Types)
                    {
                        if (Helper.IsVisible(typeDeclaration, _configuration.Visibility))
                        {
                            types.Add(typeDeclaration);
                        }
                    }

                    types.Sort();

                    for (int i = 0; i < types.Count; i++)
                    {
                        if (i != 0)
                        {
                            _formatter.WriteLine();
                        }

                        WriteTypeDeclaration((ITypeDeclaration)types[i]);
                    }

                    _formatter.WriteOutdent();
                    _formatter.WriteLine();
                    _formatter.WriteLine();
                    _formatter.WriteKeyword("implementation");
                    _formatter.WriteLine();
                    _formatter.WriteLine();
                    _formatter.WriteComment("  ");
                    _formatter.WriteComment("{...}");
                    _formatter.WriteLine();
                    _formatter.WriteLine();
                    _formatter.WriteKeyword("end.");
                    _formatter.WriteLine();
                    _formatter.WriteLine();
                }
            }

            public void WriteTypeDeclaration(ITypeDeclaration value)
            {
                if ((_configuration["ShowCustomAttributes"] == "true") && (value.Attributes.Count != 0))
                {
                    //this.WriteCustomAttributeList(value, formatter);
                    //formatter.WriteLine();
                }

                //WriteTypeVisibility(value.Visibility, formatter);

                _formatter.Write(value.Namespace);
                _formatter.Write(".");
                WriteDeclaration(value.Name, value, _formatter);

                if (Helper.IsDelegate(value))
                {
                    IMethodDeclaration methodDeclaration = Helper.GetMethod(value, "Invoke");
                    string method = "procedure";
                    bool isFunction = false;
                    if (!IsType(methodDeclaration.ReturnType.Type, "System", "Void"))
                    {
                        method = "function";
                        isFunction = true;
                    }

                    _formatter.WriteKeyword(method);
                    _formatter.Write(" ");
                    WriteDeclaration(methodDeclaration.Name, value, _formatter);

                    // Generic Parameters
                    WriteGenericArgumentList(methodDeclaration.GenericArguments, _formatter);

                    // Method Parameters
                    if ((methodDeclaration.Parameters.Count > 0) || (methodDeclaration.CallingConvention == MethodCallingConvention.VariableArguments))
                    {
                        _formatter.Write("(");
                        WriteParameterDeclarationList(methodDeclaration.Parameters, _formatter, _configuration);
                        _formatter.Write(")");
                    }
                    WriteGenericParameterConstraintList(methodDeclaration, _formatter);

                    if (isFunction)
                    {
                        _formatter.Write(": ");
                        WriteType(methodDeclaration.ReturnType.Type, _formatter);
                    }
                    _formatter.Write(";");
                }
                else
                    if (Helper.IsEnumeration(value))
                {
                    bool first = true;
                    _formatter.Write("(");
                    foreach (IFieldDeclaration fieldDeclaration in Helper.GetFields(value, _configuration.Visibility))
                    {
                        // Do not render underlying "value__" field
                        if ((!fieldDeclaration.SpecialName) || (!fieldDeclaration.RuntimeSpecialName) || (fieldDeclaration.FieldType.Equals(value)))
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                _formatter.Write(", ");
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
                    _formatter.Write(");");
                }
                else
                {
                    if (Helper.IsValueType(value))
                    {
                    }
                    else if (value.Interface)
                    {
                        //formatter.WriteKeyword("interface");
                        //this.WriteGenericArgumentList(value.GenericArguments, formatter);
                    }
                    else
                    {
                        _formatter.Write(" = ");
                        _formatter.WriteKeyword("function");
                        _formatter.Write("() { };");
                        _formatter.WriteLine();

                        if (value.Abstract)
                        {
                            //formatter.Write(" ");
                            //formatter.WriteKeyword("abstract");
                        }

                        if (value.Sealed)
                        {
                            //formatter.Write(" ");
                            //formatter.WriteKeyword("sealed");
                        }
                        //this.WriteGenericArgumentList(value.GenericArguments, formatter);

                        ITypeReference baseType = value.BaseType;
                        if ((baseType != null) && (!IsType(baseType, "System", "Object")))
                        {

                            _formatter.Write(value.Namespace);
                            _formatter.Write(".");
                            WriteDeclaration(value.Name, value, _formatter);
                            _formatter.Write(".");
                            _formatter.WriteKeyword("prototype");
                            _formatter.Write(" = ");
                            _formatter.WriteKeyword("new");
                            _formatter.Write(" ");
                            WriteType(baseType, _formatter);
                            _formatter.Write("();");
                        }
                    }

                    // TODO filter interfaces
                    foreach (ITypeReference interfaceType in value.Interfaces)
                    {
                        //formatter.Write(bracketPrinted ? ", " : " (");
                        //this.WriteType(interfaceType, formatter);
                        //bracketPrinted = true;
                    }

                    //this.WriteGenericParameterConstraintList(value, formatter);
                }

                _formatter.WriteProperty("Name", GetDelphiStyleResolutionScope(value));
                WriteDeclaringAssembly(Helper.GetAssemblyReference(value), _formatter);

                if ((_configuration["ShowTypeDeclarationBody"] == "true") && (!Helper.IsEnumeration(value)) && (!Helper.IsDelegate(value)))
                {
                    _formatter.WriteLine();

                    bool newLine = false;
                    ICollection events = Helper.GetEvents(value, _configuration.Visibility);
                    if (events.Count > 0)
                    {
                        if (newLine)
                            _formatter.WriteLine();
                        newLine = true;
                        _formatter.WriteComment("// Events");
                        _formatter.WriteLine();

                        foreach (IEventDeclaration eventDeclaration in events)
                        {
                            WriteEventDeclaration(eventDeclaration);
                            _formatter.WriteLine();
                        }
                    }

                    ICollection methods = Helper.GetMethods(value, _configuration.Visibility);
                    if (methods.Count > 0)
                    {
                        if (newLine)
                            _formatter.WriteLine();
                        newLine = true;
                        _formatter.WriteComment("// Methods");
                        _formatter.WriteLine();

                        foreach (IMethodDeclaration methodDeclaration in methods)
                        {
                            WriteMethodDeclaration(methodDeclaration);
                            _formatter.WriteLine();
                        }
                    }

                    ICollection properties = Helper.GetProperties(value, _configuration.Visibility);
                    if (properties.Count > 0)
                    {
                        if (newLine)
                            _formatter.WriteLine();
                        newLine = true;
                        _formatter.WriteComment("// Properties");
                        _formatter.WriteLine();

                        foreach (IPropertyDeclaration propertyDeclaration in properties)
                        {
                            WritePropertyDeclaration(propertyDeclaration);
                            _formatter.WriteLine();
                        }
                    }

                    ICollection fields = Helper.GetFields(value, _configuration.Visibility);
                    if (fields.Count > 0)
                    {
                        if (newLine)
                            _formatter.WriteLine();
                        newLine = true;
                        _formatter.WriteComment("// Fields");
                        _formatter.WriteLine();

                        foreach (IFieldDeclaration fieldDeclaration in fields)
                            if ((!fieldDeclaration.SpecialName) || (fieldDeclaration.Name != "value__"))
                            {
                                WriteFieldDeclaration(fieldDeclaration);
                                _formatter.WriteLine();
                            }
                    }

                    ICollection nestedTypes = Helper.GetNestedTypes(value, _configuration.Visibility);
                    if (nestedTypes.Count > 0)
                    {
                        if (newLine)
                            _formatter.WriteLine();
                        newLine = true;

                        _formatter.WriteKeyword("type");
                        _formatter.Write(" ");
                        _formatter.WriteComment("// Nested Types");
                        _formatter.WriteLine();
                        _formatter.WriteIndent();
                        foreach (ITypeDeclaration nestedTypeDeclaration in nestedTypes)
                        {
                            WriteTypeDeclaration(nestedTypeDeclaration);
                            _formatter.WriteLine();
                        }
                        _formatter.WriteOutdent();
                    }

                    _formatter.WriteLine();
                    _formatter.WriteOutdent();
                    _formatter.WriteKeyword("end");
                    _formatter.Write(";");
                    _formatter.WriteLine();
                }
            }

            public void WriteTypeVisibility(TypeVisibility visibility, IFormatter formatter)
            {
                switch (visibility)
                {
                    case TypeVisibility.Public: formatter.WriteKeyword("public"); break;
                    case TypeVisibility.NestedPublic: formatter.WriteKeyword("public"); break;
                    case TypeVisibility.Private: formatter.WriteKeyword("strict private"); break;
                    case TypeVisibility.NestedAssembly: formatter.WriteKeyword("private"); break;
                    case TypeVisibility.NestedPrivate: formatter.WriteKeyword("strict private"); break;
                    case TypeVisibility.NestedFamily: formatter.WriteKeyword("strict protected"); break;
                    case TypeVisibility.NestedFamilyAndAssembly: formatter.WriteKeyword("protected"); break;
                    case TypeVisibility.NestedFamilyOrAssembly:
                        formatter.WriteKeyword("protected");
                        formatter.Write(" ");
                        formatter.WriteComment("{internal}"); break;
                    default: throw new NotSupportedException();
                }
                formatter.Write(" ");
            }

            public void WriteFieldVisibility(FieldVisibility visibility, IFormatter formatter)
            {
                switch (visibility)
                {
                    case FieldVisibility.Public: formatter.WriteKeyword("public"); break;
                    case FieldVisibility.Private: formatter.WriteKeyword("strict private"); break;
                    case FieldVisibility.PrivateScope:
                        formatter.WriteKeyword("private");
                        formatter.Write(" ");
                        formatter.WriteComment("{scope}"); break;
                    case FieldVisibility.Family: formatter.WriteKeyword("strict protected"); break;
                    case FieldVisibility.Assembly: formatter.WriteKeyword("private"); break;
                    case FieldVisibility.FamilyOrAssembly: formatter.WriteKeyword("protected"); break;
                    case FieldVisibility.FamilyAndAssembly:
                        formatter.WriteKeyword("protected");
                        formatter.Write(" ");
                        formatter.WriteComment("{internal}"); break;
                    default: throw new NotSupportedException();
                }
                formatter.Write(" ");
            }

            public void WriteMethodVisibility(MethodVisibility visibility, IFormatter formatter)
            {
                switch (visibility)
                {
                    case MethodVisibility.Public: formatter.WriteKeyword("public"); break;
                    case MethodVisibility.Private: formatter.WriteKeyword("strict private"); break;
                    case MethodVisibility.PrivateScope:
                        formatter.WriteKeyword("private");
                        formatter.Write(" ");
                        formatter.WriteComment("{scope}"); break;
                    case MethodVisibility.Family: formatter.WriteKeyword("strict protected"); break;
                    case MethodVisibility.Assembly: formatter.WriteKeyword("private"); break;
                    case MethodVisibility.FamilyOrAssembly: formatter.WriteKeyword("protected"); break;
                    case MethodVisibility.FamilyAndAssembly:
                        formatter.WriteKeyword("protected");
                        formatter.Write(" ");
                        formatter.WriteComment("{internal}"); break;
                    default: throw new NotSupportedException();
                }
                formatter.Write(" ");
            }

            public void WriteFieldDeclaration(IFieldDeclaration value)
            {
                if ((_configuration["ShowCustomAttributes"] == "true") && (value.Attributes.Count != 0))
                {
                    WriteCustomAttributeList(value, _formatter);
                    _formatter.WriteLine();
                }

                if (!IsEnumerationElement(value))
                {
                    WriteFieldVisibility(value.Visibility, _formatter);
                    if ((value.Static) && (value.Literal))
                    {
                        _formatter.WriteKeyword("const");
                        _formatter.Write(" ");
                    }
                    else
                    {
                        if (value.Static)
                        {
                            _formatter.WriteKeyword("class var");
                            _formatter.Write(" ");
                        }
                        if (value.ReadOnly)
                        {
                            _formatter.WriteKeyword("{readonly}");
                            _formatter.Write(" ");
                        }
                    }

                    WriteDeclaration(value.Name, value, _formatter);
                    _formatter.Write(": ");
                    WriteType(value.FieldType, _formatter);
                }
                else
                {
                    WriteDeclaration(value.Name, value, _formatter);
                }

                byte[] data = null;

                IExpression initializer = value.Initializer;
                if (initializer != null)
                {
                    if ((initializer is ILiteralExpression literalExpression) && (literalExpression.Value != null) && (literalExpression.Value is byte[]))
                    {
                        data = (byte[])literalExpression.Value;
                    }
                    else
                    {
                        _formatter.Write(" = ");
                        WriteExpression(initializer, _formatter);
                    }
                }

                if (!IsEnumerationElement(value))
                {
                    _formatter.Write(";");
                }

                if (data != null)
                {
                    _formatter.WriteComment(" // data size: " + data.Length.ToString(CultureInfo.InvariantCulture) + " bytes");
                }

                WriteDeclaringType(value.DeclaringType as ITypeReference, _formatter);
            }

            public void WriteMethodDeclaration(IMethodDeclaration value)
            {
                if (value.Body == null)
                {
                    if ((_configuration["ShowCustomAttributes"] == "true") && (value.ReturnType.Attributes.Count != 0))
                    {
                        WriteCustomAttributeList(value.ReturnType, _formatter);
                        _formatter.WriteLine();
                    }

                    if ((_configuration["ShowCustomAttributes"] == "true") && (value.Attributes.Count != 0))
                    {
                        WriteCustomAttributeList(value, _formatter);
                        _formatter.WriteLine();
                    }

                    WriteMethodAttributes(value, _formatter);

                    if (GetCustomAttribute(value, "System.Runtime.InteropServices", "DllImportAttribute") != null)
                    {
                        _formatter.WriteKeyword("extern");
                        _formatter.Write(" ");
                    }
                }

                string methodName = value.Name;

                if (IsConstructor(value))
                {
                    methodName = "Create";
                }
                else
                    if ((value.SpecialName) && (specialMethodNames.Contains(methodName)))
                {
                }
                else
                {
                    if (!IsType(value.ReturnType.Type, "System", "Void"))
                    {
                    }
                }

                if (value.Body != null)
                {
                    WriteDeclaringTypeReference(value.DeclaringType as ITypeReference, _formatter);
                    _formatter.Write("prototype.");
                }

                WriteDeclaration(methodName, value, _formatter);

                _formatter.Write(" = ");
                _formatter.WriteKeyword("function");

                // Generic Parameters
                //this.WriteGenericArgumentList(value.GenericArguments, formatter);

                // Method Parameters
                _formatter.Write("(");
                WriteParameterDeclarationList(value.Parameters, _formatter, _configuration);
                //if (value.CallingConvention == MethodCallingConvention.VariableArguments)
                //{
                //	formatter.Write(" {; __arglist}");
                //}
                _formatter.Write(")");

                //this.WriteGenericParameterConstraintList(value, formatter);

                _formatter.Write(" {");
                _formatter.WriteLine();
                _formatter.WriteIndent();

                IBlockStatement body = value.Body as IBlockStatement;
                if (body == null)
                {
                    //this.WriteMethodDirectives(value, formatter);
                }
                else
                {
                    // Method Body

                    // we need to dump the Delphi Variable list first
                    bool hasvar = false;
                    WriteVariableList(body.Statements, _formatter, ref hasvar);
                    blockStatementLevel = 0; // to optimize exit() for Delphi

                    WriteStatement(body, _formatter);
                    WritePendingOutdent(_formatter);

                    _formatter.WriteLine();
                }
                _formatter.WriteOutdent();
                _formatter.Write("}");
                _formatter.WriteLine();

                //this.WriteDeclaringType(value.DeclaringType as ITypeReference, formatter);			
            }

            public void WritePropertyDeclaration(IPropertyDeclaration value)
            {
                if ((_configuration["ShowCustomAttributes"] == "true") && (value.Attributes.Count != 0))
                {
                    WriteCustomAttributeList(value, _formatter);
                    _formatter.WriteLine();
                }

                IMethodDeclaration getMethod = null;
                if (value.GetMethod != null)
                {
                    getMethod = value.GetMethod.Resolve();
                }

                IMethodDeclaration setMethod = null;
                if (value.SetMethod != null)
                {
                    setMethod = value.SetMethod.Resolve();
                }

                bool hasSameAttributes = true;
                if ((getMethod != null) && (setMethod != null))
                {
                    hasSameAttributes &= (getMethod.Visibility == setMethod.Visibility);
                    hasSameAttributes &= (getMethod.Static == setMethod.Static);
                    hasSameAttributes &= (getMethod.Final == setMethod.Final);
                    hasSameAttributes &= (getMethod.Virtual == setMethod.Virtual);
                    hasSameAttributes &= (getMethod.Abstract == setMethod.Abstract);
                    hasSameAttributes &= (getMethod.NewSlot == setMethod.NewSlot);
                }

                if (hasSameAttributes)
                {
                    if (getMethod != null)
                    {
                        WriteMethodAttributes(getMethod, _formatter);
                    }
                    else if (setMethod != null)
                    {
                        WriteMethodAttributes(setMethod, _formatter);
                    }
                }

                _formatter.WriteKeyword("property");
                _formatter.Write(" ");

                // Name
                string propertyName = value.Name;
                //if (propertyName == "Item")
                //	propertyName = "Item";

                WriteDeclaration(propertyName, value, _formatter);

                IParameterDeclarationCollection parameters = value.Parameters;
                if (parameters.Count > 0)
                {
                    _formatter.Write("(");
                    WriteParameterDeclarationList(parameters, _formatter, _configuration);
                    _formatter.Write(")");
                }
                _formatter.Write(": ");

                // PropertyType
                WriteType(value.PropertyType, _formatter);

                if (getMethod != null)
                {
                    _formatter.Write(" ");
                    if (!hasSameAttributes)
                    {
                        _formatter.Write("{");
                        WriteMethodAttributes(getMethod, _formatter);
                        _formatter.Write("}");
                        _formatter.Write(" ");
                    }

                    _formatter.WriteKeyword("read");
                    _formatter.Write(" ");
                    WriteMethodReference(getMethod, _formatter);
                }

                if (setMethod != null)
                {
                    _formatter.Write(" ");
                    if (!hasSameAttributes)
                    {
                        _formatter.Write("{");
                        WriteMethodAttributes(setMethod, _formatter);
                        _formatter.Write("}");
                        _formatter.Write(" ");
                    }

                    _formatter.WriteKeyword("write");
                    _formatter.Write(" ");
                    WriteMethodReference(setMethod, _formatter);
                }

                if (value.Initializer != null)
                { // in Delphi we do not have a property initializer. Or do we ?
                    // PS
                    _formatter.Write("{(pseudo) := ");
                    WriteExpression(value.Initializer, _formatter);
                    _formatter.Write(" }");
                }


                _formatter.Write(";");
                WriteDeclaringType(value.DeclaringType as ITypeReference, _formatter);
            }

            public void WriteEventDeclaration(IEventDeclaration value)
            {
                if ((_configuration["ShowCustomAttributes"] == "true") && (value.Attributes.Count != 0))
                {
                    WriteCustomAttributeList(value, _formatter);
                    _formatter.WriteLine();
                }

                ITypeDeclaration declaringType = (value.DeclaringType as ITypeReference)?.Resolve();
                if (!declaringType.Interface)
                {
                    WriteMethodVisibility(Helper.GetVisibility(value), _formatter);
                }

                if (Helper.IsStatic(value))
                {
                    _formatter.WriteKeyword("static");
                    _formatter.Write(" ");
                }

                _formatter.Write("event");
                _formatter.Write(" ");
                WriteType(value.EventType, _formatter);
                _formatter.Write(" ");
                _formatter.WriteKeyword(value.Name);
                _formatter.Write(";");
                WriteDeclaringType(value.DeclaringType as ITypeReference, _formatter);

            }

            private void WriteDeclaringTypeReference(ITypeReference value, IFormatter formatter)
            {
                if (value.Owner is ITypeReference owner)
                {
                    WriteDeclaringTypeReference(owner, formatter);
                }
                WriteType(value, formatter);
                formatter.Write(".");
            }

            private string GetDelphiStyleResolutionScope(ITypeReference reference)
            {
                string result = reference.ToString();
                while (true)
                {
                    if (!(reference.Owner is ITypeReference ownerRef))
                    {
                        string ns = reference.Namespace;
                        if (ns.Length == 0)
                            return result;
                        else
                            return ns + "." + result;
                    }
                    reference = ownerRef;
                    result = reference.ToString() + "." + result;
                }
            }




            private void WriteType(IType type, IFormatter formatter)
            {
                if (type is ITypeReference typeReference)
                {
                    string description = Helper.GetNameWithResolutionScope(typeReference);
                    WriteTypeReference(typeReference, formatter, description, typeReference);
                    return;
                }

                if (type is IArrayType arrayType)
                {
                    WriteType(arrayType.ElementType, formatter);
                    formatter.Write("[");

                    IArrayDimensionCollection dimensions = arrayType.Dimensions;
                    for (int i = 0; i < dimensions.Count; i++)
                    {
                        if (i != 0)
                        {
                            formatter.Write(",");
                        }

                        if ((dimensions[i].LowerBound != 0) && (dimensions[i].UpperBound != -1))
                        {
                            if ((dimensions[i].LowerBound != -1) || (dimensions[i].UpperBound != -1))
                            {
                                formatter.Write((dimensions[i].LowerBound != -1) ? dimensions[i].LowerBound.ToString(CultureInfo.InvariantCulture) : ".");
                                formatter.Write("..");
                                formatter.Write((dimensions[i].UpperBound != -1) ? dimensions[i].UpperBound.ToString(CultureInfo.InvariantCulture) : ".");
                            }
                        }
                    }

                    formatter.Write("]");
                    return;
                }

                if (type is IPointerType pointerType)
                {
                    WriteType(pointerType.ElementType, formatter);
                    formatter.Write("*");
                    return;
                }

                if (type is IReferenceType referenceType)
                {
                    // formatter.WriteKeyword ("var"); // already done before the param name - HV
                    // formatter.Write (" ");
                    WriteType(referenceType.ElementType, formatter);
                    return;
                }

                if (type is IOptionalModifier optionalModifier)
                {
                    WriteType(optionalModifier.ElementType, formatter);
                    formatter.Write(" ");
                    formatter.WriteKeyword("modopt");
                    formatter.Write("(");
                    WriteType(optionalModifier.Modifier, formatter);
                    formatter.Write(")");
                    return;
                }

                if (type is IRequiredModifier requiredModifier)
                {
                    WriteType(requiredModifier.ElementType, formatter);
                    formatter.Write(" ");
                    formatter.WriteKeyword("modreq");
                    formatter.Write("(");
                    WriteType(requiredModifier.Modifier, formatter);
                    formatter.Write(")");
                    return;
                }

                if (type is IFunctionPointer functionPointer)
                {
                    WriteType(functionPointer.ReturnType.Type, formatter);
                    formatter.Write(" *(");
                    for (int i = 0; i < functionPointer.Parameters.Count; i++)
                    {
                        if (i != 0)
                        {
                            formatter.Write(", ");
                        }

                        WriteType(functionPointer.Parameters[i].ParameterType, formatter);
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

            private void WriteMethodAttributes(IMethodDeclaration methodDeclaration, IFormatter formatter)
            {
                ITypeDeclaration declaringType = (methodDeclaration.DeclaringType as ITypeReference)?.Resolve();
                if (!declaringType.Interface)
                {
                    WriteMethodVisibility(methodDeclaration.Visibility, formatter);

                    if (methodDeclaration.Static)
                    {
                        formatter.WriteKeyword("class");
                        formatter.Write(" ");
                    }
                }
            }

            private void WriteMethodDirectives(IMethodDeclaration methodDeclaration, IFormatter formatter)
            {
                ITypeDeclaration declaringType = (methodDeclaration.DeclaringType as ITypeReference)?.Resolve();
                if (!declaringType.Interface)
                {
                    formatter.Write(" ");

                    if (methodDeclaration.Static)
                    {
                        formatter.Write("static;");
                        formatter.Write(" ");
                    }

                    if ((methodDeclaration.Final) && (!methodDeclaration.NewSlot))
                    {
                        formatter.WriteKeyword("final;");
                        formatter.Write(" ");
                    }

                    if (methodDeclaration.Virtual)
                    {
                        if (methodDeclaration.Abstract)
                        {
                            formatter.WriteKeyword("abstract;");
                            formatter.Write(" ");
                        }
                        else if ((methodDeclaration.NewSlot) && (!methodDeclaration.Final))
                        {
                            formatter.WriteKeyword("virtual;");
                            formatter.Write(" ");
                        }

                        if (!methodDeclaration.NewSlot)
                        {
                            formatter.WriteKeyword("override;");
                            formatter.Write(" ");
                        }
                    }
                }
            }

            private void WriteParameterDeclaration(IParameterDeclaration value, IFormatter formatter, ILanguageWriterConfiguration configuration)
            {
                if ((configuration != null) && (configuration["ShowCustomAttributes"] == "true") && (value.Attributes.Count != 0))
                {
                    WriteCustomAttributeList(value, formatter);
                    formatter.Write(" ");
                }

                IType parameterType = value.ParameterType;

                IReferenceType referenceType = parameterType as IReferenceType;
                if (referenceType != null)
                {
                }

                if (!string.IsNullOrEmpty(value.Name))
                {
                    formatter.Write(value.Name);
                }
                else
                {
                    formatter.Write("A");
                }
            }

            private void WriteParameterDeclarationList(IParameterDeclarationCollection parameters, IFormatter formatter, ILanguageWriterConfiguration configuration)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    IParameterDeclaration parameter = parameters[i];
                    IType parameterType = parameter.ParameterType;
                    if ((parameterType != null) || ((i + 1) != parameters.Count))
                    {
                        if (i != 0)
                        {
                            formatter.Write(", ");
                        }

                        WriteParameterDeclaration(parameter, formatter, configuration);
                    }
                }
            }

            private void WriteCustomAttribute(ICustomAttribute customAttribute, IFormatter formatter)
            {
                ITypeReference type = (customAttribute.Constructor.DeclaringType as ITypeReference);
                string name = type.Name;

                if (name.EndsWith("Attribute"))
                {
                    name = name.Substring(0, name.Length - 9);
                }

                WriteReference(name, formatter, GetMethodReferenceDescription(customAttribute.Constructor), customAttribute.Constructor);

                ExpressionCollection expression = customAttribute.Arguments;
                if (expression.Count != 0)
                {
                    formatter.Write("(");
                    for (int i = 0; i < expression.Count; i++)
                    {
                        if (i != 0)
                        {
                            formatter.Write(", ");
                        }

                        WriteExpression(expression[i], formatter);
                    }

                    formatter.Write(")");
                }
            }

            private void WriteCustomAttributeList(ICustomAttributeProvider provider, IFormatter formatter)
            {
                ArrayList attributes = new ArrayList();
                for (int i = 0; i < provider.Attributes.Count; i++)
                {
                    ICustomAttribute attribute = provider.Attributes[i];
                    if (IsType(attribute.Constructor.DeclaringType, "System.Runtime.InteropServices", "DefaultParameterValueAttribute", "System"))
                    {
                        continue;
                    }

                    attributes.Add(attribute);
                }

                if (attributes.Count > 0)
                {
                    string prefix = null;

                    IAssembly assembly = provider as IAssembly;
                    if (assembly != null)
                    {
                        prefix = "assembly:";
                    }

                    IModule module = provider as IModule;
                    if (module != null)
                    {
                        prefix = "module:";
                    }

                    if (provider is IMethodReturnType methodReturnType)
                    {
                        prefix = "return:";
                    }

                    if ((assembly != null) || (module != null))
                    {
                        for (int i = 0; i < attributes.Count; i++)
                        {
                            ICustomAttribute attribute = (ICustomAttribute)attributes[i];
                            formatter.Write("[");
                            formatter.WriteKeyword(prefix);
                            formatter.Write(" ");
                            WriteCustomAttribute(attribute, formatter);
                            formatter.Write("]");

                            if (i != (attributes.Count - 1))
                            {
                                formatter.WriteLine();
                            }
                        }
                    }
                    else
                    {
                        formatter.Write("[");
                        if (prefix != null)
                        {
                            formatter.WriteKeyword(prefix);
                            formatter.Write(" ");
                        }

                        for (int i = 0; i < attributes.Count; i++)
                        {
                            if (i != 0)
                            {
                                formatter.Write(", ");
                            }

                            ICustomAttribute attribute = (ICustomAttribute)attributes[i];
                            WriteCustomAttribute(attribute, formatter);
                        }

                        formatter.Write("]");
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
                            formatter.Write("; ");
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
                    formatter.WriteKeyword("new");
                    formatter.Write("()");
                    return;
                }

                if (value is IReferenceTypeConstraint referenceTypeConstraint)
                {
                    formatter.WriteKeyword("class");
                    return;
                }

                if (value is IValueTypeConstraint valueTypeConstraint)
                {
                    formatter.WriteKeyword("struct");
                    return;
                }

                WriteType(value, formatter);
            }

            private void WriteGenericParameterConstraintList(IGenericArgumentProvider provider, IFormatter formatter)
            {
                ITypeCollection genericArguments = provider.GenericArguments;
                if (genericArguments.Count > 0)
                {
                    for (int i = 0; i < genericArguments.Count; i++)
                    {
                        if ((genericArguments[i] is IGenericParameter parameter) && (parameter.Constraints.Count > 0))
                        {
                            formatter.Write(" ");
                            formatter.WriteKeyword("where");
                            formatter.Write(" ");
                            formatter.Write(parameter.Name);
                            formatter.Write(":");
                            formatter.Write(" ");

                            for (int j = 0; j < parameter.Constraints.Count; j++)
                            {
                                if (j != 0)
                                {
                                    formatter.Write(", ");
                                }

                                IType constraint = (IType)parameter.Constraints[j];
                                WriteGenericParameterConstraint(constraint, formatter);
                            }
                        }
                    }
                }
            }

            #region Expression

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

                if (value is ILiteralExpression literalExpression)
                {
                    WriteLiteralExpression(literalExpression, formatter);
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

                if (value is IBaseReferenceExpression baseReferenceExpression)
                {
                    WriteBaseReferenceExpression(baseReferenceExpression, formatter);
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

                if (value is ITypedReferenceCreateExpression typedReferenceCreateExpression)
                {
                    WriteTypedReferenceCreateExpression(typedReferenceCreateExpression, formatter);
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
                // Indent++;
                for (int i = 0; i < expressions.Count; i++)
                {
                    if (i != 0)
                    {
                        formatter.Write(", ");
                    }

                    WriteExpression(expressions[i], formatter);
                }
                // Indent--;
            }

            private void WriteGenericDefaultExpression(IGenericDefaultExpression value, IFormatter formatter)
            {
                formatter.WriteKeyword("default");
                formatter.Write("(");
                WriteType(value.GenericArgument, formatter);
                formatter.Write(")");
            }

            private void WriteTypeOfTypedReferenceExpression(ITypeOfTypedReferenceExpression value, IFormatter formatter)
            {
                formatter.WriteKeyword("__reftype");
                formatter.Write("(");
                WriteExpression(value.Expression, formatter);
                formatter.Write(")");
            }

            private void WriteValueOfTypedReferenceExpression(IValueOfTypedReferenceExpression value, IFormatter formatter)
            {
                formatter.WriteKeyword("__refvalue");
                formatter.Write("(");
                WriteExpression(value.Expression, formatter);
                formatter.Write(")");
            }

            private void WriteTypedReferenceCreateExpression(ITypedReferenceCreateExpression value, IFormatter formatter)
            {
                formatter.WriteKeyword("__makeref");
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
                formatter.WriteKeyword("typeof");
                formatter.Write("(");
                WriteType(expression.Type, formatter);
                formatter.Write(")");
            }

            private void WriteFieldOfExpression(IFieldOfExpression value, IFormatter formatter)
            {
                formatter.WriteKeyword("fieldof");
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
                formatter.WriteKeyword("methodof");
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
                }
                else
                {
                    WriteType(type, formatter);
                }
            }

            private void WriteArrayCreateExpression(IArrayCreateExpression expression, IFormatter formatter)
            {
                if (expression.Initializer != null)
                {
                    WriteExpression(expression.Initializer, formatter);
                }
                else
                {
                    if (expression.Dimensions.Count == 1 && (expression.Dimensions[0] is ILiteralExpression) && ((ILiteralExpression)expression.Dimensions[0]).Value.Equals(0))
                    {
                        formatter.Write("[]");
                    }
                    else
                    {
                        formatter.Write("Array");
                        formatter.Write("(");
                        WriteExpressionList(expression.Dimensions, formatter);
                        formatter.Write(")");
                    }
                }
            }

            private void WriteBlockExpression(IBlockExpression expression, IFormatter formatter)
            {
                formatter.Write("[");

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

                        if ((i % 16) == 0)
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

                formatter.Write("]");
            }

            private void WriteBaseReferenceExpression(IBaseReferenceExpression expression, IFormatter formatter)
            {
                formatter.WriteKeyword("this");
            }

            private void WriteTryCastExpression(ITryCastExpression expression, IFormatter formatter)
            {
                formatter.Write("(");
                WriteExpression(expression.Expression, formatter);
                formatter.WriteKeyword(" as ");
                WriteType(expression.TargetType, formatter);
                formatter.Write(")");
            }

            private void WriteCanCastExpression(ICanCastExpression expression, IFormatter formatter)
            {
                formatter.Write("(");
                WriteExpression(expression.Expression, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("is");
                formatter.Write(" ");
                WriteType(expression.TargetType, formatter);
                formatter.Write(")");
            }

            private void WriteCastExpression(ICastExpression expression, IFormatter formatter)
            {
                //formatter.Write("(");
                WriteExpression(expression.Expression, formatter);
                //formatter.Write(" ");
                //formatter.WriteKeyword("as");
                //formatter.Write(" ");
                //this.WriteType(expression.TargetType, formatter);
                //formatter.Write(")");
            }

            private void WriteConditionExpression(IConditionExpression expression, IFormatter formatter)
            {
                formatter.Write("(");
                WriteExpression(expression.Condition, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("?");
                formatter.Write(" ");
                WriteExpression(expression.Then, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword(":");
                formatter.Write(" ");
                WriteExpression(expression.Else, formatter);
                formatter.Write(")");
            }

            private void WriteNullCoalescingExpression(INullCoalescingExpression value, IFormatter formatter)
            {
                formatter.Write("(");
                WriteExpression(value.Condition, formatter);
                formatter.Write("!!");
                formatter.Write(" ");
                formatter.WriteKeyword("?");
                formatter.Write(" ");
                WriteExpression(value.Condition, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword(":");
                formatter.Write(" ");
                WriteExpression(value.Expression, formatter);
                formatter.Write(")");
            }


            private void WriteDelegateCreateExpression(IDelegateCreateExpression expression, IFormatter formatter)
            {
                WriteTypeReference(expression.DelegateType, formatter);
                formatter.Write(".");
                formatter.Write("Create");
                formatter.Write("(");
                WriteTargetExpression(expression.Target, formatter);
                formatter.Write(",");
                WriteMethodReference(expression.Method, formatter); // TODO Escape = true
                formatter.Write(")");
            }

            private void WriteAnonymousMethodExpression(IAnonymousMethodExpression value, IFormatter formatter)
            {
                bool parameters = false;

                for (int i = 0; i < value.Parameters.Count; i++)
                {
                    if (!string.IsNullOrEmpty(value.Parameters[i].Name))
                    {
                        parameters = true;
                    }
                }

                formatter.WriteKeyword("function");
                formatter.Write("(");
                if (parameters)
                {
                    WriteParameterDeclarationList(value.Parameters, formatter, _configuration);
                }
                formatter.Write(") {");

                formatter.WriteLine();
                formatter.WriteIndent();
                WriteBlockStatement(value.Body, formatter);
                formatter.WriteOutdent();
                formatter.WriteLine();
                formatter.Write("}");
            }

            private void WriteTypeReferenceExpression(ITypeReferenceExpression expression, IFormatter formatter)
            {
                WriteTypeReference(expression.Type, formatter);
            }

            private void WriteFieldReferenceExpression(IFieldReferenceExpression expression, IFormatter formatter)
            { // TODO bool escape = true;
                if (expression.Target != null)
                {
                    WriteTargetExpression(expression.Target, formatter);
                    formatter.Write(".");
                    // TODO escape = false;
                }
                WriteFieldReference(expression.Field, formatter);
            }

            private void WriteArgumentReferenceExpression(IArgumentReferenceExpression expression, IFormatter formatter)
            {
                // TODO Escape name?
                // TODO Should there be a Resolve() mechanism

                TextFormatter textFormatter = new TextFormatter();
                WriteParameterDeclaration(expression.Parameter.Resolve(), textFormatter, null);
                textFormatter.Write("; // Parameter");
                if (expression.Parameter.Name != null)
                {
                    WriteReference(expression.Parameter.Name, formatter, textFormatter.ToString(), null);
                }
            }

            private void WriteArgumentListExpression(IArgumentListExpression expression, IFormatter formatter)
            {
                formatter.WriteKeyword("__arglist");
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
                textFormatter.Write(" // Local Variable");

                formatter.WriteReference(variableDeclaration.Name, textFormatter.ToString(), null);
            }

            private void WritePropertyIndexerExpression(IPropertyIndexerExpression expression, IFormatter formatter)
            {
                WriteTargetExpression(expression.Target, formatter);
                formatter.Write("(");

                bool first = true;

                foreach (IExpression index in expression.Indices)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        formatter.Write(", ");
                    }

                    WriteExpression(index, formatter);
                }

                formatter.Write(")");
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
                if (expression.Method is IMethodReferenceExpression methodReferenceExpression)
                    WriteMethodReferenceExpression(methodReferenceExpression, formatter);
                else
                {
                    formatter.Write("(");
                    WriteExpression(expression.Method, formatter);
                    formatter.Write("^");
                    formatter.Write(")");
                }

                formatter.Write("(");
                WriteExpressionList(expression.Arguments, formatter);
                formatter.Write(")");
            }


            private void WriteMethodReferenceExpression(IMethodReferenceExpression expression, IFormatter formatter)
            { // TODO bool escape = true;
                if (expression.Target != null)
                { // TODO escape = false;
                    if (expression.Target is IBinaryExpression)
                    {
                        formatter.Write("(");
                        WriteExpression(expression.Target, formatter);
                        formatter.Write(")");
                    }
                    else
                    {
                        //formatter.WriteComment("/* " + expression.Target.GetType() + " */");
                        WriteTargetExpression(expression.Target, formatter);
                    }

                    formatter.Write(".");

                }
                WriteMethodReference(expression.Method, formatter);
            }

            private void WriteEventReferenceExpression(IEventReferenceExpression expression, IFormatter formatter)
            { // TODO bool escape = true;
                if (expression.Target != null)
                { // TODO escape = false;
                    WriteTargetExpression(expression.Target, formatter);
                    formatter.Write(".");
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
                formatter.Write("(");
                formatter.WriteKeyword("new");
                formatter.Write(" ");

                if (value.Constructor != null)
                {
                    WriteTypeReference((ITypeReference)value.Type, formatter, GetMethodReferenceDescription(value.Constructor), value.Constructor);
                }
                else
                {
                    WriteType(value.Type, formatter);
                }

                formatter.Write("()).ctor");

                formatter.Write("(");
                WriteExpressionList(value.Arguments, formatter);
                formatter.Write(")");

                if ((value.Initializer is IBlockExpression initializer) && (initializer.Expressions.Count > 0))
                {
                    formatter.Write(" ");
                    WriteExpression(initializer, formatter);
                }
            }

            private void WritePropertyReferenceExpression(IPropertyReferenceExpression expression, IFormatter formatter)
            { // TODO bool escape = true;
                if (expression.Target != null)
                { // TODO escape = false;
                    WriteTargetExpression(expression.Target, formatter);
                    formatter.Write(".");
                }
                var g = expression.Property.Resolve().GetMethod;
                WriteMethodReference(g, formatter);
                formatter.Write("()");
            }

            private void WriteThisReferenceExpression(IThisReferenceExpression expression, IFormatter formatter)
            {
                formatter.WriteKeyword("this");
            }

            private void WriteAddressOfExpression(IAddressOfExpression expression, IFormatter formatter)
            {
                formatter.Write("[");
                WriteExpression(expression.Expression, formatter);
                formatter.Write("]");
            }

            private void WriteAddressReferenceExpression(IAddressReferenceExpression expression, IFormatter formatter)
            {
                formatter.Write("[");
                WriteExpression(expression.Expression, formatter);
                formatter.Write("]");
            }

            private void WriteAddressOutExpression(IAddressOutExpression expression, IFormatter formatter)
            {
                formatter.Write("[");
                WriteExpression(expression.Expression, formatter);
                formatter.Write("]");
            }

            private void WriteAddressDereferenceExpression(IAddressDereferenceExpression expression, IFormatter formatter)
            {
                if (expression.Expression is IAddressOfExpression addressOf)
                {
                    WriteExpression(addressOf.Expression, formatter);
                }
                else
                {
                    // formatter.Write("*(");
                    WriteExpression(expression.Expression, formatter);
                    // formatter.Write(")");
                }
            }

            private void WriteSizeOfExpression(ISizeOfExpression expression, IFormatter formatter)
            {
                formatter.WriteKeyword("sizeof");
                formatter.Write("(");
                WriteType(expression.Type, formatter);
                formatter.Write(")");
            }

            private void WriteStackAllocateExpression(IStackAllocateExpression expression, IFormatter formatter)
            {
                formatter.WriteKeyword("stackalloc");
                formatter.Write(" ");
                WriteType(expression.Type, formatter);
                formatter.Write("[");
                WriteExpression(expression.Expression, formatter);
                formatter.Write("]");
            }

            private void WriteLambdaExpression(ILambdaExpression value, IFormatter formatter)
            {
                formatter.WriteKeyword("function");
                formatter.Write("(");

                for (int i = 0; i < value.Parameters.Count; i++)
                {
                    if (i != 0)
                    {
                        formatter.Write(", ");
                    }

                    // this.WriteVariableIdentifier(value.Parameters[i].Variable.Identifier, formatter);
                    WriteDeclaration(value.Parameters[i].Name, formatter);
                }

                formatter.Write(")");

                formatter.Write(" { ");

                formatter.WriteKeyword("return");

                formatter.Write(" ");

                WriteExpression(value.Body, formatter);

                formatter.Write("; }");
            }

            private void WriteQueryExpression(IQueryExpression value, IFormatter formatter)
            {
                formatter.Write("(");

                WriteFromClause(value.From, formatter);

                if ((value.Body.Clauses.Count > 0) || (value.Body.Continuation != null))
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

                if ((value.Body.Clauses.Count > 0) || (value.Body.Continuation != null))
                {
                    formatter.WriteOutdent();
                }
            }

            private void WriteQueryBody(IQueryBody value, IFormatter formatter)
            {
                // from | where | let | join | orderby
                for (int i = 0; i < value.Clauses.Count; i++)
                {
                    WriteQueryClause(value.Clauses[i], formatter);
                    formatter.WriteLine();
                }

                // select | group
                WriteQueryOperation(value.Operation, formatter);

                // into
                if (value.Continuation != null)
                {
                    formatter.Write(" ");
                    WriteQueryContinuation(value.Continuation, formatter);
                }
            }

            private void WriteQueryContinuation(IQueryContinuation value, IFormatter formatter)
            {
                formatter.WriteKeyword("into");
                formatter.Write(" ");
                WriteDeclaration(value.Variable.Name, formatter);
                formatter.WriteLine();
                WriteQueryBody(value.Body, formatter);
            }

            private void WriteQueryClause(IQueryClause value, IFormatter formatter)
            {
                if (value is IWhereClause whereClause)
                {
                    WriteWhereClause(whereClause, formatter);
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

                if (value is IOrderClause clause)
                {
                    WriteOrderClause(clause, formatter);
                    return;
                }

                throw new NotSupportedException();
            }

            private void WriteQueryOperation(IQueryOperation value, IFormatter formatter)
            {
                if (value is ISelectOperation selectOperation)
                {
                    WriteSelectOperation(selectOperation, formatter);
                    return;
                }

                if (value is IGroupOperation operation)
                {
                    WriteGroupOperation(operation, formatter);
                    return;
                }

                throw new NotSupportedException();
            }

            private void WriteFromClause(IFromClause value, IFormatter formatter)
            {
                formatter.WriteKeyword("from");
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
                formatter.WriteKeyword("let");
                formatter.Write(" ");
                WriteDeclaration(value.Variable.Name, formatter);
                formatter.Write(" = ");
                WriteExpression(value.Expression, formatter);
            }

            private void WriteJoinClause(IJoinClause value, IFormatter formatter)
            {
                formatter.WriteKeyword("join");
                formatter.Write(" ");
                WriteDeclaration(value.Variable.Name, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("in");
                formatter.Write(" ");
                WriteExpression(value.In, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("on");
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
                formatter.WriteKeyword("orderby");
                formatter.Write(" ");

                var ed = value.ExpressionAndDirections[0];

                WriteExpression(ed.Expression, formatter);

                if (ed.Direction == OrderDirection.Descending)
                {
                    formatter.Write(" ");
                    formatter.WriteKeyword("descending");
                }
            }

            private void WriteSelectOperation(ISelectOperation value, IFormatter formatter)
            {
                formatter.WriteKeyword("select");
                formatter.Write(" ");
                WriteExpression(value.Expression, formatter);
            }

            private void WriteGroupOperation(IGroupOperation value, IFormatter formatter)
            {
                formatter.WriteKeyword("group");
                formatter.Write(" ");
                WriteExpression(value.Item, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("by");
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
                    case UnaryOperator.BitwiseNot:
                        formatter.WriteKeyword("!");
                        WriteExpression(expression.Expression, formatter);
                        break;

                    case UnaryOperator.BooleanNot:
                        formatter.WriteKeyword("!");
                        WriteExpression(expression.Expression, formatter);
                        break;

                    case UnaryOperator.Negate:
                        formatter.Write("-");
                        WriteExpression(expression.Expression, formatter);
                        break;

                    case UnaryOperator.PreIncrement:
                        formatter.Write("++");
                        WriteExpression(expression.Expression, formatter);
                        break;

                    case UnaryOperator.PreDecrement:
                        formatter.Write("--");
                        WriteExpression(expression.Expression, formatter);
                        break;

                    case UnaryOperator.PostIncrement:
                        WriteExpression(expression.Expression, formatter);
                        formatter.Write("++");
                        break;

                    case UnaryOperator.PostDecrement:
                        WriteExpression(expression.Expression, formatter);
                        formatter.Write("--");
                        break;

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
                        break;

                    case BinaryOperator.Subtract:
                        formatter.Write("-");
                        break;

                    case BinaryOperator.Multiply:
                        formatter.Write("*");
                        break;

                    case BinaryOperator.Divide:
                        formatter.WriteKeyword("/");
                        break;

                    case BinaryOperator.Modulus:
                        formatter.WriteKeyword("%");
                        break;

                    case BinaryOperator.ShiftLeft:
                        formatter.WriteKeyword("<<");
                        break;

                    case BinaryOperator.ShiftRight:
                        formatter.WriteKeyword(">>");
                        break;

                    case BinaryOperator.ValueInequality:
                    case BinaryOperator.IdentityInequality:
                        formatter.Write("!==");
                        break;

                    case BinaryOperator.ValueEquality:
                    case BinaryOperator.IdentityEquality:
                        formatter.Write("===");
                        break;

                    case BinaryOperator.BitwiseOr:
                        formatter.WriteKeyword("|");
                        break;

                    case BinaryOperator.BitwiseAnd:
                        formatter.WriteKeyword("&");
                        break;

                    case BinaryOperator.BitwiseExclusiveOr:
                        formatter.WriteKeyword("^");
                        break;

                    case BinaryOperator.BooleanOr:
                        formatter.WriteKeyword("||");
                        break;

                    case BinaryOperator.BooleanAnd:
                        formatter.WriteKeyword("&&");
                        break;

                    case BinaryOperator.LessThan:
                        formatter.Write("<");
                        break;

                    case BinaryOperator.LessThanOrEqual:
                        formatter.Write("<=");
                        break;

                    case BinaryOperator.GreaterThan:
                        formatter.Write(">");
                        break;

                    case BinaryOperator.GreaterThanOrEqual:
                        formatter.Write(">=");
                        break;

                    default:
                        throw new NotSupportedException(operatorType.ToString());
                }
            }

            private void WriteLiteralExpression(ILiteralExpression value, IFormatter formatter)
            {
                if (value.Value == null)
                {
                    formatter.WriteLiteral("null");
                }
                else if (value.Value is char c)
                {
                    string text = new string(new[] { c });
                    text = QuoteLiteralExpression(text);
                    formatter.WriteLiteral("\"" + text + "\"");
                }
                else if (value.Value is string text)
                {
                    text = QuoteLiteralExpression(text);
                    formatter.WriteLiteral("\"" + text + "\"");
                }
                else if (value.Value is byte bt)
                {
                    WriteNumber(bt, formatter);
                }
                else if (value.Value is sbyte sbt)
                {
                    WriteNumber(sbt, formatter);
                }
                else if (value.Value is short s)
                {
                    WriteNumber(s, formatter);
                }
                else if (value.Value is ushort us)
                {
                    WriteNumber(us, formatter);
                }
                else if (value.Value is int i)
                {
                    WriteNumber(i, formatter);
                }
                else if (value.Value is uint u)
                {
                    WriteNumber(u, formatter);
                }
                else if (value.Value is long l)
                {
                    WriteNumber(l, formatter);
                }
                else if (value.Value is ulong ul)
                {
                    WriteNumber(ul, formatter);
                }
                else if (value.Value is float f)
                {
                    // TODO
                    formatter.WriteLiteral(f.ToString(CultureInfo.InvariantCulture));
                }
                else if (value.Value is double d)
                {
                    // TODO
                    formatter.WriteLiteral(d.ToString("R", CultureInfo.InvariantCulture));
                }
                else if (value.Value is decimal dec)
                {
                    formatter.WriteLiteral(dec.ToString(CultureInfo.InvariantCulture));
                }
                else if (value.Value is bool b)
                {
                    formatter.WriteLiteral(b ? "true" : "false");
                }
                /*
                else if (expression.Value is byte[])
                {
                    formatter.WriteComment("{ ");

                    byte[] bytes = (byte[])expression.Value;
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        if (i != 0)
                        {
                            formatter.Write(", ");
                        }

                        formatter.WriteComment("0x" + bytes[i].ToString("X2", CultureInfo.InvariantCulture));
                    }

                    formatter.WriteComment(" }");
                }
                */
                else
                {
                    throw new ArgumentException("expression");
                }
            }

            private void WriteNumber(IConvertible value, IFormatter formatter)
            {
                IFormattable formattable = (IFormattable)value;

                switch (GetNumberFormat(value))
                {
                    case NumberFormat.Decimal:
                        formatter.WriteLiteral(formattable.ToString(null, CultureInfo.InvariantCulture));
                        break;

                    case NumberFormat.Hexadecimal:
                        formatter.WriteLiteral("0x" + formattable.ToString("x", CultureInfo.InvariantCulture));
                        break;
                }
            }

            private NumberFormat GetNumberFormat(IConvertible value)
            {
                NumberFormat format = numberFormat;
                if (format == NumberFormat.Auto)
                {
                    long number = (value is ulong) ? (long)(ulong)value : value.ToInt64(CultureInfo.InvariantCulture);

                    if (number < 16)
                    {
                        return NumberFormat.Decimal;
                    }

                    if (((number % 10) == 0) && (number < 1000))
                    {
                        return NumberFormat.Decimal;
                    }

                    return NumberFormat.Hexadecimal;
                }

                return format;
            }

            private void WriteTypeReference(ITypeReference typeReference, IFormatter formatter)
            {
                WriteType(typeReference, formatter);
            }

            private void WriteTypeReference(ITypeReference typeReference, IFormatter formatter, string description, object target)
            {
                string name = typeReference.Namespace + "." + typeReference.Name;

                // TODO mscorlib test
                if (typeReference.Namespace == "System")
                {
                    if (specialTypeNames.Contains(name))
                    {
                        name = (string)specialTypeNames[name];
                    }
                }

                ITypeReference genericType = typeReference.GenericType;
                if (genericType != null)
                {
                    WriteReference(name, formatter, description, genericType);
                    //this.WriteGenericArgumentList(typeReference.GenericArguments, formatter);
                }
                else
                {
                    WriteReference(name, formatter, description, target);
                }
            }

            private void WriteFieldReference(IFieldReference fieldReference, IFormatter formatter)
            {
                // TODO Escape?
                WriteReference(fieldReference.Name, formatter, GetFieldReferenceDescription(fieldReference), fieldReference);
            }

            private void WriteMethodReference(IMethodReference methodReference, IFormatter formatter)
            {
                // TODO Escape?

                IMethodReference genericMethod = methodReference.GenericMethod;
                if (genericMethod != null)
                {
                    WriteReference(methodReference.Name, formatter, GetMethodReferenceDescription(methodReference), genericMethod);
                    //this.WriteGenericArgumentList(methodReference.GenericArguments, formatter);
                }
                else
                {
                    WriteReference(methodReference.Name, formatter, GetMethodReferenceDescription(methodReference), methodReference);
                }
            }


            private void WritePropertyReference(IPropertyReference propertyReference, IFormatter formatter)
            {
                // TODO Escape?
                WriteReference(propertyReference.Name, formatter, GetPropertyReferenceDescription(propertyReference), propertyReference);
            }

            private void WriteEventReference(IEventReference eventReference, IFormatter formatter)
            {
                // TODO Escape?
                WriteReference(eventReference.Name, formatter, GetEventReferenceDescription(eventReference), eventReference);
            }

            #endregion

            #region Statement

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

                if (value is IBlockStatement blockStatement)
                {
                    WriteBlockStatement(blockStatement, formatter);
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

                if (value is IAttachEventStatement attachEventStatement)
                {
                    WriteAttachEventStatement(attachEventStatement, formatter);
                    return;
                }

                if (value is IRemoveEventStatement eventStatement)
                {
                    WriteRemoveEventStatement(eventStatement, formatter);
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

                if (value is IMemoryInitializeStatement statement)
                {
                    WriteMemoryInitializeStatement(statement, formatter);
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

                throw new ArgumentException("Invalid statement type `" + value.GetType() + "`.", "value");
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
                    firstStmt = false;
                else
                    if (!forLoop)
                {
                    formatter.Write(";");
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
                    WriteStatementSeparator(formatter);
                }
                blockStatementLevel++;
            }

            private void WriteStatementList(StatementCollection statements, IFormatter formatter)
            {
                firstStmt = true;
                // put Delphi Loop detection here for now
                //			DetectDelphiIterationStatement1(statements);
                //			DetectDelphiIterationStatement2(statements);
                //
                for (int i = 0; i < statements.Count; i++)
                {
                    WriteStatement(statements[i], formatter, (i == statements.Count - 1));
                }
            }


            private void WriteMemoryCopyStatement(IMemoryCopyStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);

                formatter.WriteKeyword("memcpy");
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

                formatter.WriteKeyword("meminit");
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

                formatter.WriteKeyword("debug");
            }

            private void WriteLockStatement(ILockStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);

                formatter.WriteKeyword("lock");
                formatter.Write(" ");
                formatter.Write("(");
                WriteExpression(statement.Expression, formatter);
                formatter.Write(")");
                formatter.WriteLine();

                formatter.WriteKeyword("begin");
                formatter.WriteIndent();

                if (statement.Body != null)
                {
                    WriteStatement(statement.Body, formatter);
                }

                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.WriteKeyword("end");
            }


            /*
            //-------------------------------------------------
                private void DetectDelphiIterationStatement1(IStatementCollection statements)
                {
        // Delphi-style dynamic for-loop:
        //			for i := k to j do
        //				Debug.Writeline('For expr');

        //			 num2 = num5; 									 // init1
        //			 num1 = num6; 									 // init2
        //			 if (num2 < num1) 							 // condTop
        //			 {
        //					goto L_0027;								 // gotoBottom
        //			 }
        //			 num2 = (num2 + 1); 						 // incr1
        //			 _0015: 												 // labelTop
        //			 Debug.WriteLine("For expr");
        //			 num1 = (num1 + 1); 						 // incr2
        //			 if (num1 != num2)							 // condBottom
        //			 {
        //					goto L_0015;								 // gotoTop
        //			 }
        //			 _0027: 												 // labelBottom
        //}
                    for (int i = 0; i < statements.Count-4; i++)
                    {
                        IAssignStatement init1 = statements[i] as IAssignStatement; 					 if (init1==null) continue;
                        IAssignStatement init2 = statements[i+1] as IAssignStatement; 				 if (init2==null) continue;
                        IConditionStatement condTop = statements[i+2] as IConditionStatement;  if (condTop==null) continue;
                        IAssignStatement incr1 = statements[i+3] as IAssignStatement; 				 if (incr1==null) continue;
                        ILabeledStatement labelTop = statements[i+4] as ILabeledStatement;		 if (labelTop==null) continue;

                        if ((init1 != null) && (init2 != null) && (incr1 != null) && (condTop != null) && (labelTop != null)
                        // && (this.blockTable[labelTop].Length == 1)
                        && (condTop.Then.Statements.Count == 1) && (condTop.Else.Statements.Count == 0))
                        {
                            IBinaryExpression condTopExpr = condTop.Condition as IBinaryExpression;
                            IGotoStatement gotoBottom = condTop.Then.Statements[0] as IGotoStatement;
                            if ((condTopExpr != null) && (gotoBottom != null))
                            {
                                IVariableReferenceExpression condTopLeftVar = condTopExpr.Left as IVariableReferenceExpression;
                                IVariableReferenceExpression condTopRightVar = condTopExpr.Right as IVariableReferenceExpression;
                                IVariableReferenceExpression init1Var = init1.Target as IVariableReferenceExpression;
                                IVariableReferenceExpression init2Var = init2.Target as IVariableReferenceExpression;
                                IVariableReferenceExpression incr1Var = incr1.Target as IVariableReferenceExpression;
                                if ((condTopLeftVar != null)	&& (condTopRightVar != null)	&& (init1Var != null) && (init1Var != null) &&
                                        (condTopLeftVar.Variable == init1Var.Variable) && (condTopRightVar.Variable == init2Var.Variable) &&
                                        (incr1Var != null) &&(incr1Var.Variable == init1Var.Variable))
                                {
                                    // search for the loop-back test, goto top
                                    for (int j = i + 5; j < statements.Count-2; j++)
                                    {
                                        IAssignStatement incr2 = statements[j] as IAssignStatement;
                                        IConditionStatement condBottom = statements[j+1] as IConditionStatement;
                                        ILabeledStatement labelBottom = statements[j+2] as ILabeledStatement;
                                        if ((incr2 != null) && (condBottom != null) && (labelBottom != null)
                                        //&& (this.blockTable[labelBottom].Length == 1)
                                        && (condBottom.Then.Statements.Count == 1) && (condBottom.Else.Statements.Count == 0))
                                        {
                                            IGotoStatement gotoTop = condBottom.Then.Statements[0] as IGotoStatement;
                                            IVariableReferenceExpression incr2Var = incr2.Target as IVariableReferenceExpression;
                                            IBinaryExpression condBottomExpr = condBottom.Condition as IBinaryExpression;
                                            // TODO: check condBottom.Operator vs condTop.Operator
                                            if ((gotoTop != null) && (gotoTop.Name == labelTop.Name) &&
                                                    (condBottomExpr != null) && (incr2Var != null) && (incr2Var.Variable == init2Var.Variable))
                                            {
                                                IVariableReferenceExpression condBottomLeftVar = condBottomExpr.Left as IVariableReferenceExpression;
                                                IVariableReferenceExpression condBottomRightVar = condBottomExpr.Right as IVariableReferenceExpression;
                                                if ((condBottomLeftVar != null)  && (condBottomRightVar != null) &&
                                                        (condBottomLeftVar.Variable == init2Var.Variable) && (condBottomRightVar.Variable == init1Var.Variable))
                                                {
        // don't know how to do this yet
        //											this.blockTable.RemoveGotoStatement(gotoBottom);
        //											this.blockTable.RemoveGotoStatement(gotoTop);

                                                    // Replace RHS of condition with full, pre-computed expression
                                                    condBottomExpr.Right = incr1.Expression;

                                                    IWhileStatement whileStatement = new WhileStatement();
                                                    whileStatement.Condition = this.InverseBooleanExpression(condTop.Condition); // condBottom.Condition;
                                                    whileStatement.Body = new BlockStatement();
                                                    statements.RemoveAt(j+1);  // Remove condBottom
                                                    statements.RemoveAt(i+3);  // Remove incr1
                                                    statements.RemoveAt(i+2);  // Remove condTop

                                                    for (int k = j - 2; k > i+1; k--)
                                                    {
                                                        whileStatement.Body.Statements.Insert(0, statements[k]);
                                                        statements.RemoveAt(k);
                                                    }
                                                    statements.Insert(i+2, whileStatement, formatter);

                                                    // this.OptimizeStatementList(whileStatement.Block.Statements);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            */




            /*
                private void DetectDelphiIterationStatement2(IStatementCollection statements)
                {
        // Delphi-style constant for-loop:
        //	for i := 0 to 10 do
        //		Use(i + j);
        //		-----------
        //		int num1;
        //			 num1 = 0;								 // init
        //			 label _0005: 						 // labelTop
        //			 Unit.Use((num1 + num2));
        //			 num1 = (num1 + 1);
        //			 if (num1 != 11)					 // condition
        //			 {
        //							 goto L_0005; 		 // gotoTop
        //			 }
                    for (int i = 0; i < statements.Count-1; i++)
                    {
                        IAssignStatement init = statements[i] as IAssignStatement;
                        ILabeledStatement labelTop = statements[i+1] as ILabeledStatement;
                        if ((init != null) && (labelTop != null)
                        // && (this.blockTable[labelTop].Length == 1)
                        )
                        { // search for the loop-back test, goto top
                            for (int j = i + 2; j < statements.Count; j++)
                            { IConditionStatement condition = statements[j] as IConditionStatement;
                                if ((condition != null) && (condition.Then.Statements.Count == 1) && (condition.Else.Statements.Count == 0))
                                { IBinaryExpression condExpr = condition.Condition as IBinaryExpression;
                                    if ((condExpr != null) )
                                    { IVariableReferenceExpression checkVar = condExpr.Left as IVariableReferenceExpression;
                                        IVariableReferenceExpression initVar = init.Target as IVariableReferenceExpression;
                                        if ((checkVar != null)	&& (initVar != null) && (checkVar.Variable == initVar.Variable))
                                        { IGotoStatement gotoTop = condition.Then.Statements[0] as IGotoStatement;
                                            if ((gotoTop != null) && (gotoTop.Name == labelTop.Name))
                                            { // this.blockTable.RemoveGotoStatement(gotoTop);
                                                IWhileStatement whileStatement = new WhileStatement();
                                                whileStatement.Condition = condition.Condition;
                                                whileStatement.Body = new BlockStatement();
                                                statements.RemoveAt(j);
                                                for (int k = j - 1; k > i; k--)
                                                { whileStatement.Body.Statements.Insert(0, statements[k]);
                                                    statements.RemoveAt(k);
                                                }
                                                statements.Insert(i+1, whileStatement, formatter);
                                                // this.OptimizeStatementList(whileStatement.Block.Statements);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            */






















































            internal static IExpression InverseBooleanExpression(IExpression expression)
            {
                if (expression is IBinaryExpression binaryExpression)
                {
                    switch (binaryExpression.Operator)
                    {
                        case BinaryOperator.GreaterThan:
                            {
                                IBinaryExpression target = new BinaryExpression
                                {
                                    Left = binaryExpression.Left,
                                    Operator = BinaryOperator.LessThanOrEqual,
                                    Right = binaryExpression.Right
                                };
                                return target;
                            }

                        case BinaryOperator.GreaterThanOrEqual:
                            {
                                IBinaryExpression target = new BinaryExpression
                                {
                                    Left = binaryExpression.Left,
                                    Operator = BinaryOperator.LessThan,
                                    Right = binaryExpression.Right
                                };
                                return target;
                            }

                        case BinaryOperator.LessThan:
                            {
                                IBinaryExpression target = new BinaryExpression
                                {
                                    Left = binaryExpression.Left,
                                    Operator = BinaryOperator.GreaterThanOrEqual,
                                    Right = binaryExpression.Right
                                };
                                return target;
                            }

                        case BinaryOperator.LessThanOrEqual:
                            {
                                IBinaryExpression target = new BinaryExpression
                                {
                                    Left = binaryExpression.Left,
                                    Operator = BinaryOperator.GreaterThan,
                                    Right = binaryExpression.Right
                                };
                                return target;
                            }

                        case BinaryOperator.IdentityEquality:
                            {
                                IBinaryExpression target = new BinaryExpression
                                {
                                    Left = binaryExpression.Left,
                                    Operator = BinaryOperator.IdentityInequality,
                                    Right = binaryExpression.Right
                                };
                                return target;
                            }

                        case BinaryOperator.IdentityInequality:
                            {
                                IBinaryExpression target = new BinaryExpression
                                {
                                    Left = binaryExpression.Left,
                                    Operator = BinaryOperator.IdentityEquality,
                                    Right = binaryExpression.Right
                                };
                                return target;
                            }

                        case BinaryOperator.ValueInequality:
                            {
                                IBinaryExpression target = new BinaryExpression
                                {
                                    Left = binaryExpression.Left,
                                    Operator = BinaryOperator.ValueEquality,
                                    Right = binaryExpression.Right
                                };
                                return target;
                            }
                        case BinaryOperator.ValueEquality:
                            {
                                IBinaryExpression target = new BinaryExpression
                                {
                                    Left = binaryExpression.Left,
                                    Operator = BinaryOperator.ValueInequality,
                                    Right = binaryExpression.Right
                                };
                                return target;
                            }

                        case BinaryOperator.BooleanAnd: // De Morgan
                            {
                                IExpression left = InverseBooleanExpression(binaryExpression.Left);
                                IExpression right = InverseBooleanExpression(binaryExpression.Right);
                                if ((left != null) && (right != null))
                                {
                                    IBinaryExpression target = new BinaryExpression
                                    {
                                        Left = left,
                                        Operator = BinaryOperator.BooleanOr,
                                        Right = right
                                    };
                                    return target;
                                }
                            }
                            break;


                        case BinaryOperator.BooleanOr: // De Morgan
                            {
                                IExpression left = InverseBooleanExpression(binaryExpression.Left);
                                IExpression right = InverseBooleanExpression(binaryExpression.Right);
                                if ((left != null) && (right != null))
                                {
                                    IBinaryExpression target = new BinaryExpression
                                    {
                                        Left = left,
                                        Operator = BinaryOperator.BooleanAnd,
                                        Right = right
                                    };
                                    return target;
                                }
                            }
                            break;
                    }
                }

                IUnaryExpression unaryExpression = expression as IUnaryExpression;
                if (unaryExpression != null)
                {
                    if (unaryExpression.Operator == UnaryOperator.BooleanNot)
                    {
                        return unaryExpression.Expression;
                    }
                }

                IUnaryExpression unaryOperator = new UnaryExpression
                {
                    Operator = UnaryOperator.BooleanNot,
                    Expression = expression
                };
                return unaryOperator;
            }

            //-------------------------------------------
            // this writes one line of variable declaration and sets the hasVar flag to true
            //  if it was false, put out the "var" definition line
            private void WriteVariableListEntry(IVariableDeclaration variable, IFormatter formatter, ref bool hasvar)
            {
                if (variable != null)
                    WriteVariableDeclaration(variable, formatter);
            }

            private void WriteVariableList(IVariableDeclarationExpression expression, IFormatter formatter, ref bool hasVar)
            {
                if (expression != null)
                    WriteVariableListEntry(expression.Variable, formatter, ref hasVar);
            }

            private void WriteVariableList(IStatement statement, IFormatter formatter, ref bool hasVar)
            {
                if (statement is IBlockStatement blockStatement)
                {
                    WriteVariableList(blockStatement.Statements, formatter, ref hasVar);
                    return;
                }

                if (statement is ILabeledStatement labeledStatement)
                {
                    WriteVariableList(labeledStatement.Statement, formatter, ref hasVar);
                    return;
                }

                if (statement is IForEachStatement forEachStatement)
                {
                    WriteVariableListEntry(forEachStatement.Variable, formatter, ref hasVar);
                    WriteVariableList(forEachStatement.Body, formatter, ref hasVar);
                    return;
                }

                if (statement is IConditionStatement conditionStatement)
                {
                    WriteVariableList(conditionStatement.Then, formatter, ref hasVar);
                    WriteVariableList(conditionStatement.Else, formatter, ref hasVar);
                    return;
                }

                if (statement is IForStatement forStatement)
                {
                    WriteVariableList(forStatement.Initializer, formatter, ref hasVar);
                    WriteVariableList(forStatement.Body, formatter, ref hasVar);
                    return;
                }

                if (statement is ISwitchStatement switchStatement)
                {
                    foreach (ISwitchCase switchCase in switchStatement.Cases)
                        WriteVariableList(switchCase.Body, formatter, ref hasVar);
                    return;
                }

                if (statement is IDoStatement doStatement)
                {
                    WriteVariableList(doStatement.Body, formatter, ref hasVar);
                    return;
                }

                if (statement is ILockStatement lockStatement)
                {
                    WriteVariableList(lockStatement.Body, formatter, ref hasVar);
                    return;
                }

                if (statement is IWhileStatement whileStatement)
                {
                    WriteVariableList(whileStatement.Body, formatter, ref hasVar);
                    return;
                }

                if (statement is IFixedStatement fixedStatement)
                {
                    WriteVariableListEntry(fixedStatement.Variable, formatter, ref hasVar);
                    WriteVariableList(fixedStatement.Body, formatter, ref hasVar);
                    return;
                }

                if (statement is IUsingStatement usingStatement)
                {
                    if (usingStatement.Expression is IAssignExpression assignExpression)
                    {
                        if (assignExpression.Target is IVariableDeclarationExpression variableDeclarationExpression)
                        {
                            WriteVariableListEntry(variableDeclarationExpression.Variable, formatter, ref hasVar);
                        }
                    }

                    return;
                }

                if (statement is ITryCatchFinallyStatement tryCatchFinallyStatement)
                {
                    WriteVariableList(tryCatchFinallyStatement.Try, formatter, ref hasVar);
                    foreach (ICatchClause catchClause in tryCatchFinallyStatement.CatchClauses)
                        WriteVariableList(catchClause.Body, formatter, ref hasVar);
                    WriteVariableList(tryCatchFinallyStatement.Fault, formatter, ref hasVar);
                    WriteVariableList(tryCatchFinallyStatement.Finally, formatter, ref hasVar);
                    return;
                }

                if (statement is IExpressionStatement expressionStatement)
                {
                    WriteVariableList(expressionStatement.Expression as IVariableDeclarationExpression, formatter, ref hasVar);
                    return;
                }

            }

            // write a list of variable definitions by recursing through the statements and define
            //  the corresponding variable names
            private void WriteVariableList(StatementCollection statements, IFormatter formatter, ref bool hasVar)
            {
                foreach (IStatement statement in statements)
                    WriteVariableList(statement, formatter, ref hasVar);
            }

            private void WriteCommentStatement(ICommentStatement statement, IFormatter formatter)
            {
                WriteComment(statement.Comment, formatter);
            }

            private void WriteComment(IComment comment, IFormatter formatter)
            {
                string[] parts = comment.Text.Split(new char[] { '\n' });
                if (parts.Length <= 1)
                {
                    foreach (string part in parts)
                    {
                        formatter.WriteComment("// ");
                        formatter.WriteComment(part);
                        formatter.WriteLine();
                    }
                }
                else
                {
                    formatter.WriteComment("/* ");
                    formatter.WriteLine();

                    foreach (string part in parts)
                    {
                        formatter.WriteComment(part);
                        formatter.WriteLine();
                    }

                    formatter.WriteComment("*/");
                    formatter.WriteLine();
                }
            }

            private void WriteMethodReturnStatement(IMethodReturnStatement statement, IFormatter formatter, bool lastStatement)
            {
                WriteStatementSeparator(formatter);
                if (statement.Expression == null)
                {
                    formatter.WriteKeyword("return");
                    formatter.Write(";");
                }
                else
                {
                    formatter.WriteKeyword("return");
                    formatter.Write(" ");
                    WriteExpression(statement.Expression, formatter);
                    formatter.Write(";");
                }
            }

            private void WriteConditionStatement(IConditionStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("if");
                formatter.Write(" ");
                if (statement.Condition is IBinaryExpression)
                    WriteExpression(statement.Condition, formatter);
                else
                {
                    formatter.Write("(");
                    WriteExpression(statement.Condition, formatter);
                    formatter.Write(")");
                }

                formatter.Write(" {");
                formatter.WriteLine();
                formatter.WriteIndent();

                if (statement.Then != null)
                    WriteStatement(statement.Then, formatter);
                else
                    formatter.WriteLine();

                formatter.WriteOutdent();
                formatter.Write("}");

                if ((statement.Else != null) && (statement.Else.Statements.Count > 0))
                {
                    WritePendingOutdent(formatter);
                    formatter.WriteLine();
                    formatter.WriteKeyword("else");
                    formatter.Write(" {");
                    formatter.WriteLine();
                    formatter.WriteIndent();
                    if (statement.Else != null)
                    {
                        WriteStatement(statement.Else, formatter);
                        WritePendingOutdent(formatter);
                    }
                    else
                    {
                        formatter.WriteLine();
                    }
                    formatter.WriteOutdent();
                    formatter.Write("}");
                }
            }

            private void WriteTryCatchFinallyStatement(ITryCatchFinallyStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);

                formatter.WriteKeyword("try");
                formatter.Write(" {");
                formatter.WriteLine();
                formatter.WriteIndent();

                if (statement.Try != null)
                {
                    WriteStatement(statement.Try, formatter);
                    WritePendingOutdent(formatter);
                }
                else
                {
                    formatter.WriteLine();
                }
                formatter.WriteOutdent();
                formatter.Write("}");

                firstStmt = true;
                foreach (ICatchClause catchClause in statement.CatchClauses)
                {
                    formatter.WriteLine();
                    formatter.WriteKeyword("catch");
                    formatter.Write(" (");
                    formatter.WriteDeclaration(catchClause.Variable.Name);
                    formatter.Write(")");
                    formatter.Write(" {");
                    formatter.WriteLine();
                    formatter.WriteIndent();

                    if (catchClause.Condition != null)
                    {
                        formatter.Write(" ");
                        formatter.WriteKeyword("if");
                        formatter.Write(" ");
                        WriteExpression(catchClause.Condition, formatter);
                        formatter.Write(" ");
                        formatter.WriteKeyword("then");
                    }

                    if (catchClause.Body != null)
                    {
                        WriteStatement(catchClause.Body, formatter);
                    }
                    else
                    {
                        formatter.WriteLine();
                    }

                    formatter.WriteOutdent();
                    formatter.Write("}");
                }

                if ((statement.Finally != null) && (statement.Finally.Statements.Count > 0))
                {
                    formatter.WriteLine();
                    formatter.WriteKeyword("finally");
                    formatter.Write(" {");
                    formatter.WriteLine();
                    formatter.WriteIndent();
                    if (statement.Finally != null)
                    {
                        WriteStatement(statement.Finally, formatter);
                        WritePendingOutdent(formatter);
                    }
                    else
                    {
                        formatter.WriteLine();
                    }
                    formatter.WriteOutdent();
                    formatter.Write("}");
                }
            }

            private void WriteAssignExpression(IAssignExpression value, IFormatter formatter)
            {
                if (value.Expression is IBinaryExpression binaryExpression)
                {
                    if (value.Target.Equals(binaryExpression.Left))
                    {
                        string operatorText = string.Empty;
                        switch (binaryExpression.Operator)
                        {
                            case BinaryOperator.Add:
                                operatorText = "inc";
                                break;

                            case BinaryOperator.Subtract:
                                operatorText = "dec";
                                break;
                        }

                        if (operatorText.Length != 0)
                        {
                            // Op(a,b)
                            formatter.Write(operatorText);
                            formatter.Write("(");
                            WriteExpression(value.Target, formatter);
                            formatter.Write(",");
                            formatter.Write(" ");
                            WriteExpression(binaryExpression.Right, formatter);
                            formatter.Write(")");

                            return;
                        }
                    }
                }

                if (value.Target is IPropertyReferenceExpression propExpression)
                {
                    if (propExpression.Target != null)
                    {
                        WriteTargetExpression(propExpression.Target, formatter);
                        formatter.Write(".");
                    }
                    var s = propExpression.Property.Resolve().SetMethod;
                    WriteMethodReference(s, formatter);
                    formatter.Write("(");
                    WriteExpression(value.Expression, formatter);
                    formatter.Write(")");
                }
                else
                {
                    // x := y + z
                    WriteExpression(value.Target, formatter);
                    formatter.Write(" = ");
                    WriteExpression(value.Expression, formatter);
                }
            }

            private void WriteExpressionStatement(IExpressionStatement statement, IFormatter formatter)
            { // in Delphi we have to filter the IExpressionStatement that is a IVariableDeclarationExpression
                // as this is defined/dumped in the method's var section by WriteVariableList
                if (!(statement.Expression is IVariableDeclarationExpression))
                {
                    WriteStatementSeparator(formatter);
                    IUnaryExpression unaryExpression = statement.Expression as IUnaryExpression;
                    if (unaryExpression != null && unaryExpression.Operator == UnaryOperator.PostIncrement)
                    {
                        WriteExpression(unaryExpression.Expression, formatter);
                        formatter.Write("++");
                    }
                    else if (unaryExpression != null && unaryExpression.Operator == UnaryOperator.PostDecrement)
                    {
                        WriteExpression(unaryExpression.Expression, formatter);
                        formatter.Write("--");
                    }
                    else
                    {
                        WriteExpression(statement.Expression, formatter);
                    }
                }
            }

            private void WriteForStatement(IForStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);


                formatter.WriteKeyword("for");
                formatter.Write(" (");
                forLoop = true;
                WriteStatement(statement.Initializer, formatter);
                formatter.Write("; ");
                WriteExpression(statement.Condition, formatter);
                formatter.Write("; ");
                WriteStatement(statement.Increment, formatter);
                formatter.Write(")");
                forLoop = false;
                formatter.Write(" {");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteStatement(statement.Body, formatter);
                }
                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.WriteKeyword("}");


            }

            private void WriteForEachStatement(IForEachStatement value, IFormatter formatter)
            {
                // TODO statement.Variable declaration needs to be rendered some where

                WriteStatementSeparator(formatter);

                TextFormatter description = new TextFormatter();
                WriteVariableDeclaration(value.Variable, description);

                formatter.WriteLine();
                formatter.WriteKeyword("foreach");
                formatter.Write(" (");
                formatter.WriteReference(value.Variable.Name, description.ToString(), null);
                formatter.WriteKeyword(" in ");
                WriteExpression(value.Expression, formatter);
                formatter.Write(") {");
                formatter.WriteLine();
                formatter.WriteIndent();

                if (value.Body != null)
                {
                    WriteStatement(value.Body, formatter);
                }

                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.WriteKeyword("}");
            }

            private void WriteUsingStatement(IUsingStatement statement, IFormatter formatter)
            {
                IVariableReference variable = null;

                IAssignExpression assignExpression = statement.Expression as IAssignExpression;
                if (assignExpression != null)
                {
                    if (assignExpression.Target is IVariableDeclarationExpression variableDeclarationExpression)
                    {
                        variable = variableDeclarationExpression.Variable;
                    }

                    if (assignExpression.Target is IVariableReferenceExpression variableReferenceExpression)
                    {
                        variable = variableReferenceExpression.Variable;
                    }
                }

                WriteStatementSeparator(formatter);
                // make a comment that Reflector detected this as a using statement
                //formatter.Write("{using");

                if (variable != null)
                {
                    //formatter.Write(" ");
                    WriteVariableReference(variable, formatter);
                }

                formatter.Write("}");
                formatter.WriteLine();

                // and replace this with
                // - create obj
                // - try ... finally obj.Dispose end

                formatter.WriteKeyword("begin");
                formatter.WriteLine();
                formatter.WriteIndent();

                if (variable != null)
                {
                    WriteVariableReference(variable, formatter);
                    formatter.Write(" ");
                    formatter.WriteKeyword(":=");
                    formatter.Write(" ");
                    WriteExpression(assignExpression.Expression, formatter);
                    WriteStatementSeparator(formatter);
                }

                formatter.WriteKeyword("try");
                formatter.WriteLine();
                formatter.WriteIndent();

                if (statement.Body != null)
                {
                    WriteBlockStatement(statement.Body, formatter);
                }

                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.WriteKeyword("finally");
                formatter.WriteLine();
                formatter.WriteIndent();

                if (variable != null)
                {
                    firstStmt = true;
                    WriteVariableReference(variable, formatter);
                    formatter.Write(".");
                    formatter.Write("Dispose");
                    formatter.WriteLine();
                }
                else
                {
                    firstStmt = true;
                    WriteExpression(statement.Expression);
                    formatter.Write(".");
                    formatter.Write("Dispose");
                    formatter.WriteLine();
                }

                formatter.WriteOutdent();
                formatter.WriteKeyword("end");
                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.WriteKeyword("end");
            }

            private void WriteFixedStatement(IFixedStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);

                formatter.WriteKeyword("fixed");
                formatter.Write(" ");
                formatter.Write("(");
                WriteVariableDeclaration(statement.Variable, formatter);
                formatter.Write(" ");
                formatter.WriteKeyword("=");
                formatter.Write(" ");
                WriteExpression(statement.Expression, formatter);
                formatter.Write(")");

                formatter.WriteLine();
                formatter.WriteKeyword("begin");
                formatter.WriteLine();
                formatter.WriteIndent();

                if (statement.Body != null)
                {
                    WriteBlockStatement(statement.Body, formatter);
                }

                formatter.WriteOutdent();
                formatter.WriteKeyword("end ");
            }

            private void WriteWhileStatement(IWhileStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("while");
                formatter.Write(" ");
                formatter.Write("(");
                if (statement.Condition != null)
                {

                    WriteExpression(statement.Condition, formatter);

                }
                else
                    formatter.WriteLiteral("true");
                formatter.Write(")");

                formatter.Write(" {");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteStatement(statement.Body, formatter);
                }
                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.Write("}");
            }

            private void WriteDoStatement(IDoStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("do");
                formatter.Write(" {");
                formatter.WriteLine();
                formatter.WriteIndent();
                if (statement.Body != null)
                {
                    WriteStatement(statement.Body, formatter);
                }
                formatter.WriteLine();
                formatter.WriteOutdent();
                formatter.Write("} ");
                formatter.WriteKeyword("while");
                formatter.Write(" (");

                if (statement.Condition != null)
                {
                    WriteExpression(statement.Condition, formatter);
                }
                else
                {
                    formatter.WriteLiteral("true");
                }
                formatter.Write(" );");
            }

            private void WriteBreakStatement(IBreakStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("break");
                formatter.Write(";");
                formatter.WriteLine();
            }

            private void WriteContinueStatement(IContinueStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("continue");
                formatter.Write(";");
                formatter.WriteLine();
            }

            private void WriteThrowExceptionStatement(IThrowExceptionStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("raise");
                formatter.Write(" ");
                if (statement.Expression != null)
                    WriteExpression(statement.Expression, formatter);
                else
                {
                    WriteDeclaration("Exception", formatter);
                    formatter.Write(".");
                    formatter.WriteKeyword("Create");
                }
            }

            private void WriteVariableDeclarationExpression(IVariableDeclarationExpression expression, IFormatter formatter)
            { // this.WriteVariableDeclaration(formatter, expression.Variable); // this is for C#
                //
                // no variable declaration expression in Delphi. Convert this to a variable reference only!
                WriteVariableReference(expression.Variable, formatter);
            }

            private void WriteVariableDeclaration(IVariableDeclaration variableDeclaration, IFormatter formatter)
            {
                formatter.WriteKeyword("var ");
                WriteDeclaration(variableDeclaration.Name, formatter); // TODO Escape = true

                if (!forLoop)
                {
                    formatter.Write(";");
                    formatter.WriteLine();
                }
            }

            private void WriteAttachEventStatement(IAttachEventStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                WriteEventReferenceExpression(statement.Event, formatter);
                formatter.Write(" += ");
                WriteExpression(statement.Listener, formatter);
                formatter.Write(";");
                formatter.WriteLine();
            }

            private void WriteRemoveEventStatement(IRemoveEventStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                WriteEventReferenceExpression(statement.Event, formatter);
                formatter.Write(" -= ");
                WriteExpression(statement.Listener, formatter);
                formatter.Write(";");
                formatter.WriteLine();
            }

            private void WriteSwitchStatement(ISwitchStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);

                formatter.WriteKeyword("switch");
                formatter.Write(" (");
                WriteExpression(statement.Expression, formatter);
                formatter.Write(") ");
                formatter.Write("{");
                formatter.WriteLine();
                foreach (ISwitchCase switchCase in statement.Cases)
                {
                    if (switchCase is IConditionCase conditionCase)
                    {
                        WriteSwitchCaseCondition(conditionCase.Condition, formatter);
                    }

                    if (switchCase is IDefaultCase defaultCase)
                    {
                        formatter.WriteKeyword("default");
                        formatter.Write(":");
                    }

                    formatter.WriteIndent();

                    if (switchCase.Body != null)
                    {
                        WriteStatement(switchCase.Body, formatter);
                        WritePendingOutdent(formatter);
                    }
                    else
                    {
                        formatter.WriteLine();
                    }

                    formatter.WriteOutdent();

                }
                formatter.WriteKeyword("}");
            }

            private void WriteSwitchCaseCondition(IExpression condition, IFormatter formatter)
            {
                if ((condition is IBinaryExpression binaryExpression) && (binaryExpression.Operator == BinaryOperator.BooleanOr))
                {
                    WriteSwitchCaseCondition(binaryExpression.Left, formatter);
                    WriteSwitchCaseCondition(binaryExpression.Right, formatter);
                }
                else
                {
                    formatter.WriteKeyword("case ");
                    WriteExpression(condition, formatter);
                    formatter.Write(":");
                    formatter.WriteLine();
                }
            }

            private void WriteGotoStatement(IGotoStatement statement, IFormatter formatter)
            {
                WriteStatementSeparator(formatter);
                formatter.WriteKeyword("goto");
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
            #endregion

            private void WriteDeclaringType(ITypeReference value, IFormatter formatter)
            {
                formatter.WriteProperty("Declaring Type", GetDelphiStyleResolutionScope(value));
                WriteDeclaringAssembly(Helper.GetAssemblyReference(value), formatter);
            }

            private void WriteDeclaringAssembly(IAssemblyReference value, IFormatter formatter)
            {
                if (value != null)
                {
                    string text = ((value.Name != null) && (value.Version != null)) ? (value.Name + ", Version=" + value.Version.ToString()) : value.ToString();
                    formatter.WriteProperty("Assembly", text);
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
                formatter.Write(";");

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
                    // TODO custom attributes [return: ...]
                    WriteType(value.ReturnType.Type, formatter);
                    formatter.Write(" ");
                    formatter.Write(Helper.GetNameWithResolutionScope(value.DeclaringType as ITypeReference));
                    formatter.Write(".");
                    formatter.Write(value.Name);
                }

                WriteGenericArgumentList(value.GenericArguments, formatter);

                formatter.Write("(");

                WriteParameterDeclarationList(value.Parameters, formatter, null);

                if (value.CallingConvention == MethodCallingConvention.VariableArguments)
                {
                    formatter.WriteKeyword(", __arglist");
                }

                formatter.Write(")");
                formatter.Write(";");

                return formatter.ToString();
            }

            private string GetPropertyReferenceDescription(IPropertyReference propertyReference)
            {
                IFormatter formatter = new TextFormatter();

                WriteType(propertyReference.PropertyType, formatter);
                formatter.Write(" ");

                // Name
                string propertyName = propertyReference.Name;
                if (propertyName == "Item")
                {
                    propertyName = "this";
                }

                formatter.Write(GetTypeReferenceDescription(propertyReference.DeclaringType as ITypeReference));
                formatter.Write(".");
                WriteDeclaration(propertyName, formatter);

                // Parameters
                IParameterDeclarationCollection parameters = propertyReference.Parameters;
                if (parameters.Count > 0)
                {
                    formatter.Write("(");
                    WriteParameterDeclarationList(parameters, formatter, null);
                    formatter.Write(")");
                }

                formatter.Write(" ");
                formatter.Write("{ ... }");

                return formatter.ToString();
            }

            private string GetEventReferenceDescription(IEventReference eventReference)
            {
                IFormatter formatter = new TextFormatter();

                formatter.WriteKeyword("event");
                formatter.Write(" ");
                WriteType(eventReference.EventType, formatter);
                formatter.Write(" ");
                formatter.Write(GetTypeReferenceDescription(eventReference.DeclaringType as ITypeReference));
                formatter.Write(".");
                WriteDeclaration(eventReference.Name, formatter);
                formatter.Write(";");

                return formatter.ToString();
            }

            private static bool IsType(IType value, string namespaceName, string name)
            {
                return (IsType(value, namespaceName, name, "mscorlib") || IsType(value, namespaceName, name, "sscorlib"));
            }

            private static bool IsType(IType value, string namespaceName, string name, string assemblyName)
            {
                if (value is ITypeReference typeReference)
                {
                    return ((typeReference.Name == name) && (typeReference.Namespace == namespaceName) && (IsAssemblyReference(typeReference, assemblyName)));
                }

                if (value is IRequiredModifier requiredModifier)
                {
                    return IsType(requiredModifier.ElementType, namespaceName, name);
                }

                if (value is IOptionalModifier optionalModifier)
                {
                    return IsType(optionalModifier.ElementType, namespaceName, name);
                }

                return false;
            }

            private static bool IsAssemblyReference(ITypeReference value, string assemblyName)
            {
                return (Helper.GetAssemblyReference(value).Name == assemblyName);
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
                foreach (ICustomAttribute customAttribute in value.Attributes)
                {
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
                if ((customAttribute != null) && (customAttribute.Arguments.Count == 1))
                {
                    return customAttribute.Arguments[0] as ILiteralExpression;
                }

                return null;
            }

            private bool IsConstructor(IMethodReference value)
            {
                return ((value.Name == ".ctor") || (value.Name == ".cctor"));
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
                using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        char character = text[i];
                        ushort value = (ushort)character;
                        if (value > 0x00ff)
                        {
                            writer.Write("\\u" + value.ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            switch (character)
                            {
                                case '\r': writer.Write("\\r"); break;
                                case '\t': writer.Write("\\t"); break;
                                case '\'': writer.Write("\\\'"); break;
                                case '\0': writer.Write("\\0"); break;
                                case '\n': writer.Write("\\n"); break;
                                default: writer.Write(character); break;
                            }
                        }
                    }
                    return writer.ToString();
                }
            }

            private void WriteDeclaration(string name, IFormatter formatter)
            {
                formatter.WriteDeclaration((Array.IndexOf(keywords, name) != -1) ? ("@" + name) : name);
            }

            private void WriteDeclaration(string name, object target, IFormatter formatter)
            {
                formatter.WriteDeclaration((Array.IndexOf(keywords, name) != -1) ? ("&" + name) : name, target);
            }

            private void WriteReference(string name, IFormatter formatter, string toolTip, object reference)
            {
                string text = name;
                if (name.Equals(".ctor"))
                {
                    text = "Create";
                }
                if (name.Equals("..ctor"))
                {
                    text = "Create";
                }
                if (Array.IndexOf(keywords, name) != -1)
                {
                    text = "&" + name;
                }
                formatter.WriteReference(text, toolTip, reference);
            }

            private string[] keywords = new string[] {
                    "and",            "array",         "as",           "asm",
                    "begin",          "case",          "class",        "const",
                    "constructor",    "destructor",    "dispinterface","div",
                    "do",             "downto",        "else",         "end",
                    "except",         "exports",       "file",         "finalization",
                    "finally",        "for",           "function",     "goto",
                    "if",             "implementation","in",           "inherited",
                    "initialization", "inline",        "interface",    "is",
                    "label",          "library",       "mod",          "nil",
                    "not",            "object",        "of",           "or",
                    "out",            "packed",        "procedure",    "program",
                    "property",       "raise",         "record",       "repeat",
                    "resourcestring", "set",           "shl",          "shr",
					/*"string", */    "then",          "threadvar",    "to",
                    "try",            "type",          "unit",         "until",
                    "uses",           "var",           "while",        "with",
                    "xor"
                };

            private class TextFormatter : IFormatter
            {
                private StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
                private bool newLine;
                private int indent = 0;

                public override string ToString()
                {
                    return writer.ToString();
                }

                public void WriteCustom(string value)
                {
                    //TODO:
                    Write(value);
                }

                public void Write(string text)
                {
                    ApplyIndent();
                    writer.Write(text);
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
                    WriteColor(text, (int)0x808080);
                }

                public void WriteLiteral(string text)
                {
                    WriteColor(text, (int)0x800000);
                }

                public void WriteKeyword(string text)
                {
                    WriteColor(text, (int)0x000080);
                }

                public void WriteIndent()
                {
                    indent++;
                }

                public void WriteLine()
                {
                    writer.WriteLine();
                    newLine = true;
                }

                public void WriteOutdent()
                {
                    indent--;
                }

                public void WriteReference(string text, string toolTip, Object reference)
                {
                    ApplyIndent();
                    writer.Write(text);
                }

                public void WriteProperty(string propertyName, string propertyValue)
                {
                }

                private void WriteBold(string text)
                {
                    ApplyIndent();
                    writer.Write(text);
                }

                private void WriteColor(string text, int color)
                {
                    ApplyIndent();
                    writer.Write(text);
                }

                private void ApplyIndent()
                {
                    if (newLine)
                    {
                        for (int i = 0; i < indent; i++)
                        {
                            writer.Write("    ");
                        }

                        newLine = false;
                    }
                }
            }
        }
    }
}
