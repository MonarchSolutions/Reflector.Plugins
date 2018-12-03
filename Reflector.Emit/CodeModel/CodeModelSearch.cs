using System;
using System.Collections;
using System.Reflection;
using Reflector.CodeModel;

namespace Reflector.Emit.CodeModel
{
    internal sealed class CodeModelSearch
    {
        public CodeModelSearch(IServiceProvider serviceProvider)
        {
            assemblyManager = (IAssemblyManager)serviceProvider.GetService(typeof(IAssemblyManager));
        }

        public ITypeDeclaration FindType(Type type)
        {
            return FindType(type.FullName);
        }

        public ITypeDeclaration FindType(string typeFullName)
        {
            ITypeDeclaration typeDeclaration = (ITypeDeclaration)cachedTypes[typeFullName];
            ITypeDeclaration result;
            if (typeDeclaration != null)
            {
                result = typeDeclaration;
            }
            else
            {
                foreach (object obj in assemblyManager.Assemblies)
                {
                    IAssembly assembly = (IAssembly)obj;
                    foreach (object obj2 in assembly.Modules)
                    {
                        IModule module = (IModule)obj2;
                        foreach (object obj3 in module.Types)
                        {
                            ITypeDeclaration typeDeclaration2 = (ITypeDeclaration)obj3;
                            if (Helper.GetNameWithResolutionScope(typeDeclaration2) == typeFullName)
                            {
                                cachedTypes.Add(typeFullName, typeDeclaration2);
                                return typeDeclaration2;
                            }
                        }
                    }
                }
                result = null;
            }
            return result;
        }

        public IPropertyDeclaration FindProperty(ITypeDeclaration type, string propertyName)
        {
            propertyName = propertyName.ToLower();
            if (type != null)
            {
                foreach (object obj in type.Properties)
                {
                    IPropertyDeclaration propertyDeclaration = (IPropertyDeclaration)obj;
                    if (propertyDeclaration.Name.ToLower() == propertyName)
                    {
                        return propertyDeclaration;
                    }
                }
            }
            return null;
        }

        public IFieldDeclaration FindField(ITypeDeclaration type, string fieldName)
        {
            fieldName = fieldName.ToLower();
            if (type != null)
            {
                foreach (object obj in type.Fields)
                {
                    IFieldDeclaration fieldDeclaration = (IFieldDeclaration)obj;
                    if (fieldDeclaration.Name.ToLower() == fieldName)
                    {
                        return fieldDeclaration;
                    }
                }
            }
            return null;
        }

        public int FindToken(IMethodDeclaration method)
        {
            object obj = methodTokens[method];
            int num;
            if (obj != null)
            {
                num = (int)obj;
            }
            else
            {
                if (tokenField == null)
                {
                    foreach (FieldInfo fieldInfo in method.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        if (fieldInfo.FieldType == typeof(int))
                        {
                            tokenField = fieldInfo;
                            break;
                        }
                    }
                }
                num = (int)tokenField.GetValue(method);
                methodTokens.Add(method, num);
            }
            return num;
        }

        private bool AreParameterMatching(ParameterInfo[] parameters, IParameterDeclarationCollection iparameters)
        {
            bool result;
            if (parameters.Length != iparameters.Count)
            {
                result = false;
            }
            else
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo parameterInfo = parameters[i];
                    IParameterDeclaration parameterDeclaration = iparameters[i].Resolve();
                    if (parameterInfo.Name != iparameters[i].Name)
                    {
                        return false;
                    }
                    if (!AreMatching(parameterInfo.ParameterType, parameterDeclaration.ParameterType))
                    {
                        return false;
                    }
                }
                result = true;
            }
            return result;
        }

        private bool AreMatching(Type type, IType iType)
        {
            IArrayType arrayType = iType as IArrayType;
            bool result;
            if (arrayType != null)
            {
                result = (type.IsArray && AreMatching(type.GetElementType(), arrayType.ElementType));
            }
            else
            {
                if (iType is IReferenceType referenceType)
                {
                    result = (type.IsByRef && AreMatching(type, referenceType.ElementType));
                }
                else
                {
                    result = (type.Name == Helper.GetName(iType as ITypeReference));
                }
            }
            return result;
        }

        private readonly IAssemblyManager assemblyManager;

        private readonly Hashtable cachedTypes = new Hashtable();

        private readonly Hashtable methodTokens = new Hashtable();

        private FieldInfo tokenField = null;
    }
}
