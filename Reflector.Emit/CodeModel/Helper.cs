using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Reflector.CodeModel;

namespace Reflector.Emit.CodeModel
{
    public static class Helper
    {
        public static string GetName(ITypeReference value)
        {
            if (value != null)
            {
                ITypeCollection genericArguments = value.GenericArguments;
                if (genericArguments.Count > 0)
                {
                    using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        for (int i = 0; i < genericArguments.Count; i++)
                        {
                            if (i != 0)
                            {
                                stringWriter.Write(",");
                            }
                            IType type = genericArguments[i];
                            if (type != null)
                            {
                                stringWriter.Write(type.ToString());
                            }
                        }
                        return value.Name + "<" + stringWriter.ToString() + ">";
                    }
                }
                return value.Name;
            }
            throw new NotSupportedException();
        }

        public static string GetNameWithResolutionScope(ITypeReference value)
        {
            if (value != null)
            {
                ITypeReference typeReference = value.Owner as ITypeReference;
                string result;
                if (typeReference != null)
                {
                    result = GetNameWithResolutionScope(typeReference) + "+" + GetName(value);
                }
                else
                {
                    string @namespace = value.Namespace;
                    if (@namespace.Length == 0)
                    {
                        result = GetName(value);
                    }
                    else
                    {
                        result = @namespace + "." + GetName(value);
                    }
                }
                return result;
            }
            throw new NotSupportedException();
        }

        public static string GetResolutionScope(ITypeReference value)
        {
            IModule module = value.Owner as IModule;
            string result;
            if (module != null)
            {
                result = value.Namespace;
            }
            else
            {
                if (!(value.Owner is ITypeDeclaration typeDeclaration))
                {
                    throw new NotSupportedException();
                }
                result = GetResolutionScope(typeDeclaration) + "+" + GetName(typeDeclaration);
            }
            return result;
        }

        public static bool IsValueType(ITypeReference value)
        {
            bool result;
            if (value != null)
            {
                ITypeDeclaration typeDeclaration = value.Resolve();
                if (typeDeclaration == null)
                {
                    result = false;
                }
                else
                {
                    ITypeReference baseType = typeDeclaration.BaseType;
                    result = (baseType != null && (baseType.Name == "ValueType" || baseType.Name == "Enum") && baseType.Namespace == "System");
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public static bool IsDelegate(ITypeReference value)
        {
            bool result;
            if (value != null)
            {
                if (value.Name == "MulticastDelegate" && value.Namespace == "System")
                {
                    result = false;
                }
                else
                {
                    ITypeDeclaration typeDeclaration = value.Resolve();
                    if (typeDeclaration == null)
                    {
                        result = false;
                    }
                    else
                    {
                        ITypeReference baseType = typeDeclaration.BaseType;
                        result = (baseType != null && baseType.Namespace == "System" && (baseType.Name == "MulticastDelegate" || baseType.Name == "Delegate") && baseType.Namespace == "System");
                    }
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public static bool IsEnumeration(ITypeReference value)
        {
            bool result;
            if (value != null)
            {
                ITypeDeclaration typeDeclaration = value.Resolve();
                if (typeDeclaration == null)
                {
                    result = false;
                }
                else
                {
                    ITypeReference baseType = typeDeclaration.BaseType;
                    result = (baseType != null && baseType.Name == "Enum" && baseType.Namespace == "System");
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public static IAssemblyReference GetAssemblyReference(IType value)
        {
            if (value is ITypeReference typeReference)
            {
                ITypeReference typeReference2 = typeReference.Owner as ITypeReference;
                IAssemblyReference result;
                if (typeReference2 != null)
                {
                    result = GetAssemblyReference(typeReference2);
                }
                else
                {
                    if (typeReference.Owner is IModuleReference moduleReference)
                    {
                        IModule module = moduleReference.Resolve();
                        result = module.Assembly;
                    }
                    else
                    {
                        if (!(typeReference.Owner is IAssemblyReference assemblyReference))
                        {
                            throw new NotSupportedException();
                        }
                        result = assemblyReference;
                    }
                }
                return result;
            }
            throw new NotSupportedException();
        }

        public static bool IsVisible(IType value, IVisibilityConfiguration visibility)
        {
            if (value is ITypeReference typeReference)
            {
                ITypeReference typeReference2 = typeReference.Owner as ITypeReference;
                bool result;
                if (typeReference2 != null && !IsVisible(typeReference2, visibility))
                {
                    result = false;
                }
                else
                {
                    ITypeDeclaration typeDeclaration = typeReference.Resolve();
                    if (typeDeclaration == null)
                    {
                        result = true;
                    }
                    else
                    {
                        switch (typeDeclaration.Visibility)
                        {
                            case TypeVisibility.Private:
                            case TypeVisibility.NestedPrivate:
                                result = visibility.Private;
                                break;
                            case TypeVisibility.Public:
                            case TypeVisibility.NestedPublic:
                                result = visibility.Public;
                                break;
                            case TypeVisibility.NestedFamily:
                                result = visibility.Family;
                                break;
                            case TypeVisibility.NestedAssembly:
                                result = visibility.Assembly;
                                break;
                            case TypeVisibility.NestedFamilyAndAssembly:
                                result = visibility.FamilyAndAssembly;
                                break;
                            case TypeVisibility.NestedFamilyOrAssembly:
                                result = visibility.FamilyOrAssembly;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                }
                return result;
            }
            throw new NotSupportedException();
        }

        public static IMethodDeclaration GetMethod(ITypeDeclaration value, string methodName)
        {
            if (value != null)
            {
                IMethodDeclarationCollection methods = value.Methods;
                for (int i = 0; i < methods.Count; i++)
                {
                    if (methodName == methods[i].Name)
                    {
                        return methods[i];
                    }
                }
            }
            return null;
        }

        private static ICollection GetInterfaces(ITypeDeclaration value)
        {
            ArrayList arrayList = new ArrayList(0);
            arrayList.AddRange(value.Interfaces);
            if (value.BaseType != null)
            {
                ITypeDeclaration typeDeclaration = value.BaseType.Resolve();
                foreach (object obj in typeDeclaration.Interfaces)
                {
                    ITypeReference typeReference = (ITypeReference)obj;
                    if (arrayList.Contains(typeReference))
                    {
                        arrayList.Remove(typeReference);
                    }
                }
            }
            foreach (object obj2 in value.Interfaces)
            {
                ITypeReference typeReference2 = (ITypeReference)obj2;
                ITypeDeclaration typeDeclaration2 = typeReference2.Resolve();
                foreach (object obj3 in typeDeclaration2.Interfaces)
                {
                    ITypeReference typeReference3 = (ITypeReference)obj3;
                    if (arrayList.Contains(typeReference3))
                    {
                        arrayList.Remove(typeReference3);
                    }
                }
            }
            ITypeReference[] array = new ITypeReference[checked((uint)arrayList.Count)];
            arrayList.CopyTo(array, 0);
            return array;
        }

        public static ICollection GetInterfaces(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            ArrayList arrayList = new ArrayList(0);
            foreach (object obj in GetInterfaces(value))
            {
                ITypeReference value2 = (ITypeReference)obj;
                if (IsVisible(value2, visibility))
                {
                    arrayList.Add(value2);
                }
            }
            arrayList.Sort();
            return arrayList;
        }

        public static ICollection GetFields(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            ArrayList arrayList = new ArrayList(0);
            IFieldDeclarationCollection fields = value.Fields;
            if (fields.Count > 0)
            {
                foreach (object obj in fields)
                {
                    IFieldDeclaration value2 = (IFieldDeclaration)obj;
                    if (visibility == null || IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                arrayList.Sort();
            }
            return arrayList;
        }

        public static ICollection GetMethods(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            ArrayList arrayList = new ArrayList(0);
            IMethodDeclarationCollection methods = value.Methods;
            if (methods.Count > 0)
            {
                foreach (object obj in methods)
                {
                    IMethodDeclaration value2 = (IMethodDeclaration)obj;
                    if (visibility == null || IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                foreach (object obj2 in value.Properties)
                {
                    IPropertyDeclaration propertyDeclaration = (IPropertyDeclaration)obj2;
                    if (propertyDeclaration.SetMethod != null)
                    {
                        arrayList.Remove(propertyDeclaration.SetMethod.Resolve());
                    }
                    if (propertyDeclaration.GetMethod != null)
                    {
                        arrayList.Remove(propertyDeclaration.GetMethod.Resolve());
                    }
                }
                foreach (object obj3 in value.Events)
                {
                    IEventDeclaration eventDeclaration = (IEventDeclaration)obj3;
                    if (eventDeclaration.AddMethod != null)
                    {
                        arrayList.Remove(eventDeclaration.AddMethod.Resolve());
                    }
                    if (eventDeclaration.RemoveMethod != null)
                    {
                        arrayList.Remove(eventDeclaration.RemoveMethod.Resolve());
                    }
                    if (eventDeclaration.InvokeMethod != null)
                    {
                        arrayList.Remove(eventDeclaration.InvokeMethod.Resolve());
                    }
                }
                arrayList.Sort();
            }
            return arrayList;
        }

        public static ICollection GetProperties(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            ArrayList arrayList = new ArrayList(0);
            IPropertyDeclarationCollection properties = value.Properties;
            if (properties.Count > 0)
            {
                foreach (object obj in properties)
                {
                    IPropertyDeclaration value2 = (IPropertyDeclaration)obj;
                    if (visibility == null || IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                arrayList.Sort();
            }
            return arrayList;
        }

        public static ICollection GetEvents(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            ArrayList arrayList = new ArrayList(0);
            IEventDeclarationCollection events = value.Events;
            if (events.Count > 0)
            {
                foreach (object obj in events)
                {
                    IEventDeclaration value2 = (IEventDeclaration)obj;
                    if (visibility == null || IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                arrayList.Sort();
            }
            return arrayList;
        }

        public static ICollection GetNestedTypes(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            ArrayList arrayList = new ArrayList(0);
            ITypeDeclarationCollection nestedTypes = value.NestedTypes;
            if (nestedTypes.Count > 0)
            {
                foreach (object obj in nestedTypes)
                {
                    ITypeDeclaration value2 = (ITypeDeclaration)obj;
                    if (IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                arrayList.Sort();
            }
            return arrayList;
        }

        public static string GetName(IFieldReference value)
        {
            IType fieldType = value.FieldType;
            IType declaringType = value.DeclaringType;
            if (fieldType.Equals(declaringType))
            {
                if (fieldType is ITypeReference typeReference && IsEnumeration(typeReference))
                {
                    return value.Name;
                }
            }
            return value.Name + " : " + value.FieldType.ToString();
        }

        public static string GetNameWithDeclaringType(IFieldReference value)
        {
            return GetNameWithResolutionScope(value.DeclaringType as ITypeReference) + "." + GetName(value);
        }

        public static bool IsVisible(IFieldReference value, IVisibilityConfiguration visibility)
        {
            bool result;
            if (IsVisible(value.DeclaringType, visibility))
            {
                IFieldDeclaration fieldDeclaration = value.Resolve();
                if (fieldDeclaration == null)
                {
                    result = true;
                }
                else
                {
                    switch (fieldDeclaration.Visibility)
                    {
                        case FieldVisibility.PrivateScope:
                            result = visibility.Private;
                            break;
                        case FieldVisibility.Private:
                            result = visibility.Private;
                            break;
                        case FieldVisibility.FamilyAndAssembly:
                            result = visibility.FamilyAndAssembly;
                            break;
                        case FieldVisibility.Assembly:
                            result = visibility.Assembly;
                            break;
                        case FieldVisibility.Family:
                            result = visibility.Family;
                            break;
                        case FieldVisibility.FamilyOrAssembly:
                            result = visibility.FamilyOrAssembly;
                            break;
                        case FieldVisibility.Public:
                            result = visibility.Public;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public static string GetName(IMethodReference value)
        {
            ITypeCollection genericArguments = value.GenericArguments;
            if (genericArguments.Count > 0)
            {
                using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
                {
                    for (int i = 0; i < genericArguments.Count; i++)
                    {
                        if (i != 0)
                        {
                            stringWriter.Write(", ");
                        }
                        IType type = genericArguments[i];
                        if (type != null)
                        {
                            stringWriter.Write(type.ToString());
                        }
                        else
                        {
                            stringWriter.Write("???");
                        }
                    }
                    return value.Name + "<" + stringWriter.ToString() + ">";
                }
            }
            return value.Name;
        }

        public static string GetNameWithParameterList(IMethodReference value)
        {
            string result;
            using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                stringWriter.Write(GetName(value));
                stringWriter.Write("(");
                IParameterDeclarationCollection parameters = value.Parameters;
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (i != 0)
                    {
                        stringWriter.Write(", ");
                    }
                    stringWriter.Write(parameters[i].ParameterType.ToString());
                }
                if (value.CallingConvention == MethodCallingConvention.VariableArguments)
                {
                    if (value.Parameters.Count > 0)
                    {
                        stringWriter.Write(", ");
                    }
                    stringWriter.Write("...");
                }
                stringWriter.Write(")");
                if (value.Name != ".ctor" && value.Name != ".cctor")
                {
                    stringWriter.Write(" : ");
                    stringWriter.Write(value.ReturnType.Type.ToString());
                }
                result = stringWriter.ToString();
            }
            return result;
        }

        public static string GetNameWithDeclaringType(IMethodReference value)
        {
            ITypeReference typeReference = value.DeclaringType as ITypeReference;
            string result;
            if (typeReference != null)
            {
                result = GetNameWithResolutionScope(typeReference) + "." + GetNameWithParameterList(value);
            }
            else
            {
                if (!(value.DeclaringType is IArrayType arrayType))
                {
                    throw new NotSupportedException();
                }
                result = arrayType.ToString() + "." + GetNameWithParameterList(value);
            }
            return result;
        }

        public static bool IsVisible(IMethodReference value, IVisibilityConfiguration visibility)
        {
            bool result;
            if (IsVisible(value.DeclaringType, visibility))
            {
                IMethodDeclaration methodDeclaration = value.Resolve();
                switch (methodDeclaration.Visibility)
                {
                    case MethodVisibility.PrivateScope:
                    case MethodVisibility.Private:
                        result = visibility.Private;
                        break;
                    case MethodVisibility.FamilyAndAssembly:
                        result = visibility.FamilyAndAssembly;
                        break;
                    case MethodVisibility.Assembly:
                        result = visibility.Assembly;
                        break;
                    case MethodVisibility.Family:
                        result = visibility.Family;
                        break;
                    case MethodVisibility.FamilyOrAssembly:
                        result = visibility.FamilyOrAssembly;
                        break;
                    case MethodVisibility.Public:
                        result = visibility.Public;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public static string GetName(IPropertyReference value)
        {
            IParameterDeclarationCollection parameters = value.Parameters;
            if (parameters.Count > 0)
            {
                using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        if (i != 0)
                        {
                            stringWriter.Write(", ");
                        }
                        stringWriter.Write(parameters[i].ParameterType.ToString());
                    }
                    return string.Concat(new string[]
                    {
                        value.Name,
                        "[",
                        stringWriter.ToString(),
                        "] : ",
                        value.PropertyType.ToString()
                    });
                }
            }
            return value.Name + " : " + value.PropertyType.ToString();
        }

        public static string GetNameWithDeclaringType(IPropertyReference value)
        {
            return GetNameWithResolutionScope(value.DeclaringType as ITypeReference) + "." + GetName(value);
        }

        public static IMethodDeclaration GetSetMethod(IPropertyReference value)
        {
            IPropertyDeclaration propertyDeclaration = value.Resolve();
            IMethodDeclaration result;
            if (propertyDeclaration.SetMethod != null)
            {
                result = propertyDeclaration.SetMethod.Resolve();
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static IMethodDeclaration GetGetMethod(IPropertyReference value)
        {
            IPropertyDeclaration propertyDeclaration = value.Resolve();
            IMethodDeclaration result;
            if (propertyDeclaration.GetMethod != null)
            {
                result = propertyDeclaration.GetMethod.Resolve();
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static bool IsStatic(IPropertyReference value)
        {
            IMethodDeclaration setMethod = GetSetMethod(value);
            IMethodDeclaration getMethod = GetGetMethod(value);
            bool flag = false;
            flag |= (setMethod != null && setMethod.Static);
            return flag | (getMethod != null && getMethod.Static);
        }

        public static MethodVisibility GetVisibility(IPropertyReference value)
        {
            IMethodDeclaration getMethod = GetGetMethod(value);
            IMethodDeclaration setMethod = GetSetMethod(value);
            MethodVisibility result = MethodVisibility.Public;
            if (setMethod != null && getMethod != null)
            {
                if (getMethod.Visibility == setMethod.Visibility)
                {
                    result = getMethod.Visibility;
                }
            }
            else if (setMethod != null)
            {
                result = setMethod.Visibility;
            }
            else if (getMethod != null)
            {
                result = getMethod.Visibility;
            }
            return result;
        }

        public static bool IsVisible(IPropertyReference value, IVisibilityConfiguration visibility)
        {
            bool result;
            if (IsVisible(value.DeclaringType, visibility))
            {
                switch (GetVisibility(value))
                {
                    case MethodVisibility.PrivateScope:
                    case MethodVisibility.Private:
                        result = visibility.Private;
                        break;
                    case MethodVisibility.FamilyAndAssembly:
                        result = visibility.FamilyAndAssembly;
                        break;
                    case MethodVisibility.Assembly:
                        result = visibility.Assembly;
                        break;
                    case MethodVisibility.Family:
                        result = visibility.Family;
                        break;
                    case MethodVisibility.FamilyOrAssembly:
                        result = visibility.FamilyOrAssembly;
                        break;
                    case MethodVisibility.Public:
                        result = visibility.Public;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public static string GetName(IEventReference value)
        {
            return value.Name;
        }

        public static string GetNameWithDeclaringType(IEventReference value)
        {
            return GetNameWithResolutionScope(value.DeclaringType as ITypeReference) + "." + GetName(value);
        }

        public static IMethodDeclaration GetAddMethod(IEventReference value)
        {
            IEventDeclaration eventDeclaration = value.Resolve();
            IMethodDeclaration result;
            if (eventDeclaration.AddMethod != null)
            {
                result = eventDeclaration.AddMethod.Resolve();
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static IMethodDeclaration GetRemoveMethod(IEventReference value)
        {
            IEventDeclaration eventDeclaration = value.Resolve();
            IMethodDeclaration result;
            if (eventDeclaration.RemoveMethod != null)
            {
                result = eventDeclaration.RemoveMethod.Resolve();
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static IMethodDeclaration GetInvokeMethod(IEventReference value)
        {
            IEventDeclaration eventDeclaration = value.Resolve();
            IMethodDeclaration result;
            if (eventDeclaration.InvokeMethod != null)
            {
                result = eventDeclaration.InvokeMethod.Resolve();
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static MethodVisibility GetVisibility(IEventReference value)
        {
            IMethodDeclaration addMethod = GetAddMethod(value);
            IMethodDeclaration removeMethod = GetRemoveMethod(value);
            IMethodDeclaration invokeMethod = GetInvokeMethod(value);
            if (addMethod != null && removeMethod != null && invokeMethod != null)
            {
                if (addMethod.Visibility == removeMethod.Visibility && addMethod.Visibility == invokeMethod.Visibility)
                {
                    return addMethod.Visibility;
                }
            }
            else if (addMethod != null && removeMethod != null)
            {
                if (addMethod.Visibility == removeMethod.Visibility)
                {
                    return addMethod.Visibility;
                }
            }
            else if (addMethod != null && invokeMethod != null)
            {
                if (addMethod.Visibility == invokeMethod.Visibility)
                {
                    return addMethod.Visibility;
                }
            }
            else if (removeMethod != null && invokeMethod != null)
            {
                if (removeMethod.Visibility == invokeMethod.Visibility)
                {
                    return removeMethod.Visibility;
                }
            }
            else
            {
                if (addMethod != null)
                {
                    return addMethod.Visibility;
                }
                if (removeMethod != null)
                {
                    return removeMethod.Visibility;
                }
                if (invokeMethod != null)
                {
                    return invokeMethod.Visibility;
                }
            }
            return MethodVisibility.Public;
        }

        public static bool IsVisible(IEventReference value, IVisibilityConfiguration visibility)
        {
            bool result;
            if (IsVisible(value.DeclaringType, visibility))
            {
                switch (GetVisibility(value))
                {
                    case MethodVisibility.PrivateScope:
                    case MethodVisibility.Private:
                        result = visibility.Private;
                        break;
                    case MethodVisibility.FamilyAndAssembly:
                        result = visibility.FamilyAndAssembly;
                        break;
                    case MethodVisibility.Assembly:
                        result = visibility.Assembly;
                        break;
                    case MethodVisibility.Family:
                        result = visibility.Family;
                        break;
                    case MethodVisibility.FamilyOrAssembly:
                        result = visibility.FamilyOrAssembly;
                        break;
                    case MethodVisibility.Public:
                        result = visibility.Public;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                result = false;
            }
            return result;
        }

        public static bool IsStatic(IEventReference value)
        {
            bool flag = false;
            if (GetAddMethod(value) != null)
            {
                flag |= GetAddMethod(value).Static;
            }
            if (GetRemoveMethod(value) != null)
            {
                flag |= GetRemoveMethod(value).Static;
            }
            if (GetInvokeMethod(value) != null)
            {
                flag |= GetInvokeMethod(value).Static;
            }
            return flag;
        }

        public static int GetInstructionSize(IInstruction value)
        {
            int num = 0;
            if (value.Code < 256)
            {
                num++;
            }
            else if (value.Code < 65536)
            {
                num += 2;
            }
            switch (GetOperandType(value.Code))
            {
                case OperandType.BranchTarget:
                case OperandType.Field:
                case OperandType.Int32:
                case OperandType.Method:
                case OperandType.Signature:
                case OperandType.String:
                case OperandType.Token:
                case OperandType.Type:
                case OperandType.Single:
                    return num + 4;
                case OperandType.Int64:
                case OperandType.Double:
                    return num + 8;
                case OperandType.None:
                    return num;
                case OperandType.Switch:
                    {
                        num += 4;
                        int[] array = (int[])value.Value;
                        return num + array.Length * 4;
                    }
                case OperandType.Variable:
                    return num + 2;
                case OperandType.ShortBranchTarget:
                case OperandType.SByte:
                case OperandType.ShortVariable:
                    return num + 1;
            }
            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Unknown operand type for operator '{0}'.", new object[]
            {
                value.Code.ToString("x4")
            }));
        }

        private static OperandType GetOperandType(int code)
        {
            switch (code)
            {
                case 0:
                    return OperandType.None;
                case 1:
                    return OperandType.None;
                case 2:
                    return OperandType.None;
                case 3:
                    return OperandType.None;
                case 4:
                    return OperandType.None;
                case 5:
                    return OperandType.None;
                case 6:
                    return OperandType.None;
                case 7:
                    return OperandType.None;
                case 8:
                    return OperandType.None;
                case 9:
                    return OperandType.None;
                case 10:
                    return OperandType.None;
                case 11:
                    return OperandType.None;
                case 12:
                    return OperandType.None;
                case 13:
                    return OperandType.None;
                case 14:
                    return OperandType.ShortVariable;
                case 15:
                    return OperandType.ShortVariable;
                case 16:
                    return OperandType.ShortVariable;
                case 17:
                    return OperandType.ShortVariable;
                case 18:
                    return OperandType.ShortVariable;
                case 19:
                    return OperandType.ShortVariable;
                case 20:
                    return OperandType.None;
                case 21:
                    return OperandType.None;
                case 22:
                    return OperandType.None;
                case 23:
                    return OperandType.None;
                case 24:
                    return OperandType.None;
                case 25:
                    return OperandType.None;
                case 26:
                    return OperandType.None;
                case 27:
                    return OperandType.None;
                case 28:
                    return OperandType.None;
                case 29:
                    return OperandType.None;
                case 30:
                    return OperandType.None;
                case 31:
                    return OperandType.SByte;
                case 32:
                    return OperandType.Int32;
                case 33:
                    return OperandType.Int64;
                case 34:
                    return OperandType.Single;
                case 35:
                    return OperandType.Double;
                case 37:
                    return OperandType.None;
                case 38:
                    return OperandType.None;
                case 39:
                    return OperandType.Method;
                case 40:
                    return OperandType.Method;
                case 41:
                    return OperandType.Signature;
                case 42:
                    return OperandType.None;
                case 43:
                    return OperandType.ShortBranchTarget;
                case 44:
                    return OperandType.ShortBranchTarget;
                case 45:
                    return OperandType.ShortBranchTarget;
                case 46:
                    return OperandType.ShortBranchTarget;
                case 47:
                    return OperandType.ShortBranchTarget;
                case 48:
                    return OperandType.ShortBranchTarget;
                case 49:
                    return OperandType.ShortBranchTarget;
                case 50:
                    return OperandType.ShortBranchTarget;
                case 51:
                    return OperandType.ShortBranchTarget;
                case 52:
                    return OperandType.ShortBranchTarget;
                case 53:
                    return OperandType.ShortBranchTarget;
                case 54:
                    return OperandType.ShortBranchTarget;
                case 55:
                    return OperandType.ShortBranchTarget;
                case 56:
                    return OperandType.BranchTarget;
                case 57:
                    return OperandType.BranchTarget;
                case 58:
                    return OperandType.BranchTarget;
                case 59:
                    return OperandType.BranchTarget;
                case 60:
                    return OperandType.BranchTarget;
                case 61:
                    return OperandType.BranchTarget;
                case 62:
                    return OperandType.BranchTarget;
                case 63:
                    return OperandType.BranchTarget;
                case 64:
                    return OperandType.BranchTarget;
                case 65:
                    return OperandType.BranchTarget;
                case 66:
                    return OperandType.BranchTarget;
                case 67:
                    return OperandType.BranchTarget;
                case 68:
                    return OperandType.BranchTarget;
                case 69:
                    return OperandType.Switch;
                case 70:
                    return OperandType.None;
                case 71:
                    return OperandType.None;
                case 72:
                    return OperandType.None;
                case 73:
                    return OperandType.None;
                case 74:
                    return OperandType.None;
                case 75:
                    return OperandType.None;
                case 76:
                    return OperandType.None;
                case 77:
                    return OperandType.None;
                case 78:
                    return OperandType.None;
                case 79:
                    return OperandType.None;
                case 80:
                    return OperandType.None;
                case 81:
                    return OperandType.None;
                case 82:
                    return OperandType.None;
                case 83:
                    return OperandType.None;
                case 84:
                    return OperandType.None;
                case 85:
                    return OperandType.None;
                case 86:
                    return OperandType.None;
                case 87:
                    return OperandType.None;
                case 88:
                    return OperandType.None;
                case 89:
                    return OperandType.None;
                case 90:
                    return OperandType.None;
                case 91:
                    return OperandType.None;
                case 92:
                    return OperandType.None;
                case 93:
                    return OperandType.None;
                case 94:
                    return OperandType.None;
                case 95:
                    return OperandType.None;
                case 96:
                    return OperandType.None;
                case 97:
                    return OperandType.None;
                case 98:
                    return OperandType.None;
                case 99:
                    return OperandType.None;
                case 100:
                    return OperandType.None;
                case 101:
                    return OperandType.None;
                case 102:
                    return OperandType.None;
                case 103:
                    return OperandType.None;
                case 104:
                    return OperandType.None;
                case 105:
                    return OperandType.None;
                case 106:
                    return OperandType.None;
                case 107:
                    return OperandType.None;
                case 108:
                    return OperandType.None;
                case 109:
                    return OperandType.None;
                case 110:
                    return OperandType.None;
                case 111:
                    return OperandType.Method;
                case 112:
                    return OperandType.Type;
                case 113:
                    return OperandType.Type;
                case 114:
                    return OperandType.String;
                case 115:
                    return OperandType.Method;
                case 116:
                    return OperandType.Type;
                case 117:
                    return OperandType.Type;
                case 118:
                    return OperandType.None;
                case 121:
                    return OperandType.Type;
                case 122:
                    return OperandType.None;
                case 123:
                    return OperandType.Field;
                case 124:
                    return OperandType.Field;
                case 125:
                    return OperandType.Field;
                case 126:
                    return OperandType.Field;
                case 127:
                    return OperandType.Field;
                case 128:
                    return OperandType.Field;
                case 129:
                    return OperandType.Type;
                case 130:
                    return OperandType.None;
                case 131:
                    return OperandType.None;
                case 132:
                    return OperandType.None;
                case 133:
                    return OperandType.None;
                case 134:
                    return OperandType.None;
                case 135:
                    return OperandType.None;
                case 136:
                    return OperandType.None;
                case 137:
                    return OperandType.None;
                case 138:
                    return OperandType.None;
                case 139:
                    return OperandType.None;
                case 140:
                    return OperandType.Type;
                case 141:
                    return OperandType.Type;
                case 142:
                    return OperandType.None;
                case 143:
                    return OperandType.Type;
                case 144:
                    return OperandType.None;
                case 145:
                    return OperandType.None;
                case 146:
                    return OperandType.None;
                case 147:
                    return OperandType.None;
                case 148:
                    return OperandType.None;
                case 149:
                    return OperandType.None;
                case 150:
                    return OperandType.None;
                case 151:
                    return OperandType.None;
                case 152:
                    return OperandType.None;
                case 153:
                    return OperandType.None;
                case 154:
                    return OperandType.None;
                case 155:
                    return OperandType.None;
                case 156:
                    return OperandType.None;
                case 157:
                    return OperandType.None;
                case 158:
                    return OperandType.None;
                case 159:
                    return OperandType.None;
                case 160:
                    return OperandType.None;
                case 161:
                    return OperandType.None;
                case 162:
                    return OperandType.None;
                case 163:
                    return OperandType.Type;
                case 164:
                    return OperandType.Type;
                case 165:
                    return OperandType.Type;
                case 179:
                    return OperandType.None;
                case 180:
                    return OperandType.None;
                case 181:
                    return OperandType.None;
                case 182:
                    return OperandType.None;
                case 183:
                    return OperandType.None;
                case 184:
                    return OperandType.None;
                case 185:
                    return OperandType.None;
                case 186:
                    return OperandType.None;
                case 194:
                    return OperandType.Type;
                case 195:
                    return OperandType.None;
                case 198:
                    return OperandType.Type;
                case 208:
                    return OperandType.Token;
                case 209:
                    return OperandType.None;
                case 210:
                    return OperandType.None;
                case 211:
                    return OperandType.None;
                case 212:
                    return OperandType.None;
                case 213:
                    return OperandType.None;
                case 214:
                    return OperandType.None;
                case 215:
                    return OperandType.None;
                case 216:
                    return OperandType.None;
                case 217:
                    return OperandType.None;
                case 218:
                    return OperandType.None;
                case 219:
                    return OperandType.None;
                case 220:
                    return OperandType.None;
                case 221:
                    return OperandType.BranchTarget;
                case 222:
                    return OperandType.ShortBranchTarget;
                case 223:
                    return OperandType.None;
                case 224:
                    return OperandType.None;
                case 248:
                    return OperandType.None;
                case 249:
                    return OperandType.None;
                case 250:
                    return OperandType.None;
                case 251:
                    return OperandType.None;
                case 252:
                    return OperandType.None;
                case 253:
                    return OperandType.None;
                case 254:
                    return OperandType.None;
                case 255:
                    return OperandType.None;
            }
            switch (code)
            {
                case 65024:
                    return OperandType.None;
                case 65025:
                    return OperandType.None;
                case 65026:
                    return OperandType.None;
                case 65027:
                    return OperandType.None;
                case 65028:
                    return OperandType.None;
                case 65029:
                    return OperandType.None;
                case 65030:
                    return OperandType.Method;
                case 65031:
                    return OperandType.Method;
                case 65033:
                    return OperandType.Variable;
                case 65034:
                    return OperandType.Variable;
                case 65035:
                    return OperandType.Variable;
                case 65036:
                    return OperandType.Variable;
                case 65037:
                    return OperandType.Variable;
                case 65038:
                    return OperandType.Variable;
                case 65039:
                    return OperandType.None;
                case 65041:
                    return OperandType.None;
                case 65042:
                    return OperandType.SByte;
                case 65043:
                    return OperandType.None;
                case 65044:
                    return OperandType.None;
                case 65045:
                    return OperandType.Type;
                case 65046:
                    return OperandType.Type;
                case 65047:
                    return OperandType.None;
                case 65048:
                    return OperandType.None;
                case 65050:
                    return OperandType.None;
                case 65052:
                    return OperandType.Type;
                case 65053:
                    return OperandType.None;
                case 65054:
                    return OperandType.None;
            }
            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Unknown IL instruction '{0}'.", new object[]
            {
                code.ToString("X4", CultureInfo.InvariantCulture)
            }));
        }

        public static int GetMethodBodySize(IMethodDeclaration value)
        {
            int num = 0;
            if (value.Body is IMethodBody methodBody)
            {
                IInstructionCollection instructions = methodBody.Instructions;
                if (instructions.Count != 0)
                {
                    IInstruction instruction = instructions[instructions.Count - 1];
                    num = num + instruction.Offset + GetInstructionSize(instruction);
                }
            }
            return num;
        }

        private enum OperandType
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
}
