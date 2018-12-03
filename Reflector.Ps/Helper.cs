using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Reflector.CodeModel;

namespace Reflector.Ps
{
    internal static class Helper
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
                    string ns = value.Namespace;
                    if (ns.Length == 0)
                    {
                        result = GetName(value);
                    }
                    else
                    {
                        result = ns + "." + GetName(value);
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
                        IAssemblyReference assemblyReference = typeReference.Owner as IAssemblyReference;
                        if (assemblyReference == null)
                        {
                            goto IL_7C;
                        }
                        result = assemblyReference;
                    }
                }
                return result;
            }
        IL_7C:
            throw new NotSupportedException();
        }

        public static bool IsVisible(IType value, IVisibilityConfiguration visibility)
        {
            if (value is ITypeReference typeReference)
            {
                if (typeReference.Owner is ITypeReference typeReference2)
                {
                    if (!IsVisible(typeReference2, visibility))
                    {
                        return false;
                    }
                }
                ITypeDeclaration typeDeclaration = typeReference.Resolve();
                bool result;
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
                return result;
            }
            throw new NotSupportedException();
        }

        public static IMethodDeclaration GetMethod(ITypeDeclaration value, string methodName)
        {
            IMethodDeclarationCollection methods = value.Methods;
            for (int i = 0; i < methods.Count; i++)
            {
                if (methodName == methods[i].Name)
                {
                    return methods[i];
                }
            }
            return null;
        }

        private static ICollection GetInterfaces(ITypeDeclaration value)
        {
            ArrayList arrayList = new ArrayList(value.Interfaces);
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
                ITypeReference typeReference = (ITypeReference)obj2;
                ITypeDeclaration typeDeclaration2 = typeReference.Resolve();
                foreach (object obj3 in typeDeclaration2.Interfaces)
                {
                    ITypeReference typeReference2 = (ITypeReference)obj3;
                    if (arrayList.Contains(typeReference2))
                    {
                        arrayList.Remove(typeReference2);
                    }
                }
            }
            ITypeReference[] array = new ITypeReference[arrayList.Count];
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
            IFieldDeclarationCollection fields = value.Fields;
            ICollection result;
            if (fields.Count > 0)
            {
                ArrayList arrayList = new ArrayList(0);
                foreach (object obj in fields)
                {
                    IFieldDeclaration value2 = (IFieldDeclaration)obj;
                    if (visibility == null || IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                arrayList.Sort();
                result = arrayList;
            }
            else
            {
                result = new IFieldDeclaration[0];
            }
            return result;
        }

        public static ICollection GetMethods(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            IMethodDeclarationCollection methods = value.Methods;
            ICollection result;
            if (methods.Count > 0)
            {
                ArrayList arrayList = new ArrayList(0);
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
                result = arrayList;
            }
            else
            {
                result = new IMethodDeclaration[0];
            }
            return result;
        }

        public static ICollection GetProperties(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            IPropertyDeclarationCollection properties = value.Properties;
            ICollection result;
            if (properties.Count > 0)
            {
                ArrayList arrayList = new ArrayList(0);
                foreach (object obj in properties)
                {
                    IPropertyDeclaration value2 = (IPropertyDeclaration)obj;
                    if (visibility == null || IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                arrayList.Sort();
                result = arrayList;
            }
            else
            {
                result = new IPropertyDeclaration[0];
            }
            return result;
        }

        public static ICollection GetEvents(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            IEventDeclarationCollection events = value.Events;
            ICollection result;
            if (events.Count > 0)
            {
                ArrayList arrayList = new ArrayList(0);
                foreach (object obj in events)
                {
                    IEventDeclaration value2 = (IEventDeclaration)obj;
                    if (visibility == null || IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                arrayList.Sort();
                result = arrayList;
            }
            else
            {
                result = new IEventDeclaration[0];
            }
            return result;
        }

        public static ICollection GetNestedTypes(ITypeDeclaration value, IVisibilityConfiguration visibility)
        {
            ITypeDeclarationCollection nestedTypes = value.NestedTypes;
            ICollection result;
            if (nestedTypes.Count > 0)
            {
                ArrayList arrayList = new ArrayList(0);
                foreach (object obj in nestedTypes)
                {
                    ITypeDeclaration value2 = (ITypeDeclaration)obj;
                    if (IsVisible(value2, visibility))
                    {
                        arrayList.Add(value2);
                    }
                }
                arrayList.Sort();
                result = arrayList;
            }
            else
            {
                result = new ITypeDeclaration[0];
            }
            return result;
        }

        public static string GetName(IFieldReference value)
        {
            IType fieldType = value.FieldType;
            IType declaringType = value.DeclaringType;
            if (fieldType.Equals(declaringType))
            {
                if (fieldType is ITypeReference typeReference)
                {
                    if (IsEnumeration(typeReference))
                    {
                        return value.Name;
                    }
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
                IArrayType arrayType = value.DeclaringType as IArrayType;
                if (arrayType == null)
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

        public static MethodVisibility GetVisibility(IPropertyReference value)
        {
            MethodVisibility result = MethodVisibility.Public;
            IPropertyDeclaration propertyDeclaration = value.Resolve();
            if (propertyDeclaration != null)
            {
                IMethodReference setMethod = propertyDeclaration.SetMethod;
                IMethodDeclaration methodDeclaration = setMethod?.Resolve();
                IMethodReference getMethod = propertyDeclaration.GetMethod;
                IMethodDeclaration methodDeclaration2 = getMethod?.Resolve();
                if (methodDeclaration != null && methodDeclaration2 != null)
                {
                    if (methodDeclaration2.Visibility == methodDeclaration.Visibility)
                    {
                        result = methodDeclaration2.Visibility;
                    }
                }
                else if (methodDeclaration != null)
                {
                    result = methodDeclaration.Visibility;
                }
                else if (methodDeclaration2 != null)
                {
                    result = methodDeclaration2.Visibility;
                }
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

        public static bool IsBaseMethod(IMethodReference value, IMethodReference baseMethod)
        {
            bool result;
            if (value.Name != baseMethod.Name)
            {
                result = false;
            }
            else
            {
                if (value.GenericMethod != null)
                {
                    value = value.GenericMethod;
                }
                if (baseMethod.GenericMethod != null)
                {
                    baseMethod = baseMethod.GenericMethod;
                }
                if (!value.ReturnType.Type.Equals(baseMethod.ReturnType.Type))
                {
                    result = false;
                }
                else if (value.HasThis != baseMethod.HasThis && value.ExplicitThis != baseMethod.ExplicitThis && value.CallingConvention != baseMethod.CallingConvention)
                {
                    result = false;
                }
                else if (value.Parameters.Count != baseMethod.Parameters.Count)
                {
                    result = false;
                }
                else
                {
                    for (int i = 0; i < value.Parameters.Count; i++)
                    {
                        if (!value.Parameters[i].ParameterType.Equals(baseMethod.Parameters[i].ParameterType))
                        {
                            return false;
                        }
                    }
                    if (value.GenericArguments.Count != baseMethod.GenericArguments.Count)
                    {
                        result = false;
                    }
                    else
                    {
                        IMethodDeclaration methodDeclaration = value.Resolve();
                        IMethodDeclaration methodDeclaration2 = baseMethod.Resolve();
                        result = (methodDeclaration != null && methodDeclaration2 != null && methodDeclaration.Virtual && methodDeclaration2.Virtual && IsBaseType(methodDeclaration.DeclaringType, methodDeclaration2.DeclaringType));
                    }
                }
            }
            return result;
        }

        private static bool IsBaseType(IType type, IType baseType)
        {
            if (type is ITypeReference typeReference && baseType is ITypeReference typeReference2)
            {
                ITypeDeclaration typeDeclaration = typeReference.Resolve();
                if (typeDeclaration != null)
                {
                    if (typeDeclaration.BaseType != null && typeDeclaration.BaseType.Equals(baseType))
                    {
                        return true;
                    }
                    foreach (object obj in typeDeclaration.Interfaces)
                    {
                        ITypeReference typeReference3 = (ITypeReference)obj;
                        if (typeReference3.Equals(baseType))
                        {
                            return true;
                        }
                    }
                    if (typeDeclaration.BaseType != null && IsBaseType(typeDeclaration.BaseType, baseType))
                    {
                        return true;
                    }
                    foreach (object obj2 in typeDeclaration.Interfaces)
                    {
                        ITypeReference typeReference3 = (ITypeReference)obj2;
                        if (IsBaseType(typeReference3, baseType))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
