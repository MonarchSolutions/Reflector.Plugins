using System;
using System.Collections.Generic;
using System.Globalization;
using Reflector.CodeModel;
using Reflector.CodeModel.Memory;

namespace Reflector.Ps.CodeModel
{
    public class Visitor
    {
        public virtual void VisitAssembly(IAssembly value)
        {
            VisitCustomAttributeCollection(value.Attributes);
            VisitModuleCollection(value.Modules);
        }

        public virtual void VisitAssemblyReference(IAssemblyReference value)
        {
        }

        public virtual void VisitModule(IModule value)
        {
            VisitCustomAttributeCollection(value.Attributes);
        }

        public virtual void VisitModuleReference(IModuleReference value)
        {
        }

        public virtual void VisitNamespace(INamespace value)
        {
            VisitTypeDeclarationCollection(value.Types);
        }

        public virtual void VisitTypeDeclaration(ITypeDeclaration value)
        {
            VisitCustomAttributeCollection(value.Attributes);
            VisitMethodDeclarationCollection(value.Methods);
            VisitFieldDeclarationCollection(value.Fields);
            VisitPropertyDeclarationCollection(value.Properties);
            VisitEventDeclarationCollection(value.Events);
            VisitTypeDeclarationCollection(value.NestedTypes);
        }

        public virtual void VisitTypeReference(ITypeReference value)
        {
            VisitTypeCollection(value.GenericArguments);
        }

        public virtual void VisitMethodDeclaration(IMethodDeclaration value)
        {
            VisitCustomAttributeCollection(value.Attributes);
            VisitParameterDeclarationCollection(value.Parameters);
            VisitMethodReferenceCollection(value.Overrides);
            VisitMethodReturnType(value.ReturnType);
            if (value.Body is IBlockStatement blockStatement)
            {
                VisitStatement(blockStatement);
            }
        }

        public virtual void VisitFieldDeclaration(IFieldDeclaration value)
        {
            VisitCustomAttributeCollection(value.Attributes);
            VisitType(value.FieldType);
        }

        public virtual void VisitPropertyDeclaration(IPropertyDeclaration value)
        {
            VisitCustomAttributeCollection(value.Attributes);
            VisitType(value.PropertyType);
        }

        public virtual void VisitEventDeclaration(IEventDeclaration value)
        {
            VisitCustomAttributeCollection(value.Attributes);
            VisitType(value.EventType);
        }

        public virtual void VisitMethodReturnType(IMethodReturnType value)
        {
            VisitCustomAttributeCollection(value.Attributes);
            VisitType(value.Type);
        }

        public virtual void VisitParameterDeclaration(IParameterDeclaration value)
        {
            VisitCustomAttributeCollection(value.Attributes);
            VisitType(value.ParameterType);
        }

        public virtual void VisitResource(IResource value)
        {
            if (value is IEmbeddedResource embeddedResource)
            {
                VisitEmbeddedResource(embeddedResource);
            }

            if (value is IFileResource fileResource)
            {
                VisitFileResource(fileResource);
            }
        }

        public virtual void VisitEmbeddedResource(IEmbeddedResource value)
        {
        }

        public virtual void VisitFileResource(IFileResource value)
        {
        }

        public virtual void VisitType(IType value)
        {
            if (value != null)
            {
                if (value is ITypeReference typeReference)
                {
                    VisitTypeReference(typeReference);
                }
                else
                {
                    if (value is IArrayType arrayType)
                    {
                        VisitArrayType(arrayType);
                    }
                    else
                    {
                        if (value is IPointerType pointerType)
                        {
                            VisitPointerType(pointerType);
                        }
                        else
                        {
                            if (value is IReferenceType referenceType)
                            {
                                VisitReferenceType(referenceType);
                            }
                            else
                            {
                                if (value is IOptionalModifier optionalModifier)
                                {
                                    VisitOptionalModifier(optionalModifier);
                                }
                                else
                                {
                                    if (value is IRequiredModifier requiredModifier)
                                    {
                                        VisitRequiredModifier(requiredModifier);
                                    }
                                    else
                                    {
                                        if (value is IFunctionPointer functionPointer)
                                        {
                                            VisitFunctionPointer(functionPointer);
                                        }
                                        else
                                        {
                                            if (value is IGenericParameter genericParameter)
                                            {
                                                VisitGenericParameter(genericParameter);
                                            }
                                            else
                                            {
                                                IGenericArgument genericArgument = value as IGenericArgument;
                                                if (genericArgument == null)
                                                {
                                                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Invalid type '{0}'.", new object[]
                                                    {
                                                        value.GetType().Name
                                                    }));
                                                }
                                                VisitGenericArgument(genericArgument);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual void VisitArrayType(IArrayType value)
        {
            VisitType(value.ElementType);
            VisitArrayDimensionCollection(value.Dimensions);
        }

        public virtual void VisitPointerType(IPointerType value)
        {
            VisitType(value.ElementType);
        }

        public virtual void VisitReferenceType(IReferenceType value)
        {
            VisitType(value.ElementType);
        }

        public virtual void VisitOptionalModifier(IOptionalModifier type)
        {
            VisitType(type.Modifier);
            VisitType(type.ElementType);
        }

        public virtual void VisitRequiredModifier(IRequiredModifier type)
        {
            VisitType(type.Modifier);
            VisitType(type.ElementType);
        }

        public virtual void VisitFunctionPointer(IFunctionPointer type)
        {
        }

        public virtual void VisitGenericParameter(IGenericParameter type)
        {
        }

        public virtual void VisitGenericArgument(IGenericArgument type)
        {
        }

        public virtual void VisitCustomAttribute(ICustomAttribute customAttribute)
        {
            VisitExpressionCollection(customAttribute.Arguments);
        }

        public virtual void VisitStatement(IStatement value)
        {
            if (value != null)
            {
                if (value is IExpressionStatement expressionStatement)
                {
                    VisitExpressionStatement(expressionStatement);
                }
                else if (value is IBlockStatement blockStatement)
                {
                    VisitBlockStatement(blockStatement);
                }
                else if (value is IConditionStatement conditionStatement)
                {
                    VisitConditionStatement(conditionStatement);
                }
                else if (value is IMethodReturnStatement returnStatement)
                {
                    VisitMethodReturnStatement(returnStatement);
                }
                else if (value is ILabeledStatement labeledStatement)
                {
                    VisitLabeledStatement(labeledStatement);
                }
                else if (value is IGotoStatement gotoStatement)
                {
                    VisitGotoStatement(gotoStatement);
                }
                else if (value is IForStatement forStatement)
                {
                    VisitForStatement(forStatement);
                }
                else if (value is IForEachStatement eachStatement)
                {
                    VisitForEachStatement(eachStatement);
                }
                else if (value is IWhileStatement whileStatement)
                {
                    VisitWhileStatement(whileStatement);
                }
                else if (value is IDoStatement doStatement)
                {
                    VisitDoStatement(doStatement);
                }
                else if (value is ITryCatchFinallyStatement finallyStatement)
                {
                    VisitTryCatchFinallyStatement(finallyStatement);
                }
                else if (value is IThrowExceptionStatement exceptionStatement)
                {
                    VisitThrowExceptionStatement(exceptionStatement);
                }
                else if (value is IAttachEventStatement eventStatement)
                {
                    VisitAttachEventStatement(eventStatement);
                }
                else if (value is IRemoveEventStatement removeEventStatement)
                {
                    VisitRemoveEventStatement(removeEventStatement);
                }
                else if (value is ISwitchStatement switchStatement)
                {
                    VisitSwitchStatement(switchStatement);
                }
                else if (value is IBreakStatement breakStatement)
                {
                    VisitBreakStatement(breakStatement);
                }
                else if (value is IContinueStatement continueStatement)
                {
                    VisitContinueStatement(continueStatement);
                }
                else if (value is ICommentStatement commentStatement)
                {
                    VisitCommentStatement(commentStatement);
                }
                else if (value is IUsingStatement statement)
                {
                    VisitUsingStatement(statement);
                }
                else if (value is IFixedStatement fixedStatement)
                {
                    VisitFixedStatement(fixedStatement);
                }
                else if (value is ILockStatement lockStatement)
                {
                    VisitLockStatement(lockStatement);
                }
                else if (value is IMemoryCopyStatement copyStatement)
                {
                    VisitMemoryCopyStatement(copyStatement);
                }
                else if (value is IMemoryInitializeStatement initializeStatement)
                {
                    VisitMemoryInitializeStatement(initializeStatement);
                }
                else if (value is IDebugBreakStatement debugBreakStatement)
                {
                    VisitDebugBreakStatement(debugBreakStatement);
                }
                else
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture,
                        "Invalid statement type '{0}'.", new object[]
                        {
                                value.GetType().Name
                        }));
            }
        }

        public virtual void VisitBlockStatement(IBlockStatement value)
        {
            VisitStatementCollection(value.Statements);
        }

        public virtual void VisitCommentStatement(ICommentStatement value)
        {
        }

        public virtual void VisitMethodReturnStatement(IMethodReturnStatement value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitMemoryCopyStatement(IMemoryCopyStatement value)
        {
            VisitExpression(value.Source);
            VisitExpression(value.Destination);
            VisitExpression(value.Length);
        }

        public virtual void VisitMemoryInitializeStatement(IMemoryInitializeStatement value)
        {
            VisitExpression(value.Offset);
            VisitExpression(value.Value);
            VisitExpression(value.Length);
        }

        public virtual void VisitDebugBreakStatement(IDebugBreakStatement value)
        {
        }

        public virtual void VisitConditionStatement(IConditionStatement value)
        {
            VisitExpression(value.Condition);
            VisitStatement(value.Then);
            VisitStatement(value.Else);
        }

        public virtual void VisitTryCatchFinallyStatement(ITryCatchFinallyStatement value)
        {
            VisitStatement(value.Try);
            VisitCatchClauseCollection(value.CatchClauses);
            VisitStatement(value.Finally);
            VisitStatement(value.Fault);
        }

        public virtual void VisitAssignExpression(IAssignExpression value)
        {
            VisitExpression(value.Target);
            VisitExpression(value.Expression);
        }

        public virtual void VisitExpressionStatement(IExpressionStatement value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitForStatement(IForStatement value)
        {
            VisitStatement(value.Initializer);
            VisitExpression(value.Condition);
            VisitStatement(value.Increment);
            VisitStatement(value.Body);
        }

        public virtual void VisitForEachStatement(IForEachStatement value)
        {
            VisitVariableDeclaration(value.Variable);
            VisitExpression(value.Expression);
            VisitStatement(value.Body);
        }

        public virtual void VisitUsingStatement(IUsingStatement value)
        {
            //TODO: remove
            //this.VisitExpression(value.Variable);
            VisitExpression(value.Expression);
            VisitStatement(value.Body);
        }

        public virtual void VisitFixedStatement(IFixedStatement value)
        {
            VisitVariableDeclaration(value.Variable);
            VisitExpression(value.Expression);
            VisitStatement(value.Body);
        }

        public virtual void VisitLockStatement(ILockStatement value)
        {
            VisitExpression(value.Expression);
            VisitStatement(value.Body);
        }

        public virtual void VisitWhileStatement(IWhileStatement value)
        {
            VisitExpression(value.Condition);
            VisitStatement(value.Body);
        }

        public virtual void VisitDoStatement(IDoStatement value)
        {
            VisitExpression(value.Condition);
            VisitStatement(value.Body);
        }

        public virtual void VisitBreakStatement(IBreakStatement value)
        {
        }

        public virtual void VisitContinueStatement(IContinueStatement value)
        {
        }

        public virtual void VisitThrowExceptionStatement(IThrowExceptionStatement value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitAttachEventStatement(IAttachEventStatement value)
        {
            VisitExpression(value.Event);
            VisitExpression(value.Listener);
        }

        public virtual void VisitRemoveEventStatement(IRemoveEventStatement value)
        {
            VisitExpression(value.Event);
            VisitExpression(value.Listener);
        }

        public virtual void VisitSwitchStatement(ISwitchStatement value)
        {
            VisitExpression(value.Expression);
            VisitSwitchCaseCollection(value.Cases);
        }

        public virtual void VisitGotoStatement(IGotoStatement value)
        {
        }

        public virtual void VisitLabeledStatement(ILabeledStatement value)
        {
            VisitStatement(value.Statement);
        }

        public virtual void VisitExpression(IExpression value)
        {
            if (value != null)
            {
                if (value is IVariableReferenceExpression expression)
                {
                    VisitVariableReferenceExpression(expression);
                }
                else if (value is ILiteralExpression literalExpression)
                {
                    VisitLiteralExpression(literalExpression);
                }
                else if (value is IFieldReferenceExpression referenceExpression)
                {
                    VisitFieldReferenceExpression(referenceExpression);
                }
                else if (value is IPropertyReferenceExpression propertyReferenceExpression)
                {
                    VisitPropertyReferenceExpression(propertyReferenceExpression);
                }
                else if (value is IAssignExpression assignExpression)
                {
                    VisitAssignExpression(assignExpression);
                }
                else if (value is IBinaryExpression binaryExpression)
                {
                    VisitBinaryExpression(binaryExpression);
                }
                else if (value is IThisReferenceExpression thisReferenceExpression)
                {
                    VisitThisReferenceExpression(thisReferenceExpression);
                }
                else if (value is IMethodInvokeExpression invokeExpression)
                {
                    VisitMethodInvokeExpression(invokeExpression);
                }
                else if (value is IMethodReferenceExpression methodReferenceExpression)
                {
                    VisitMethodReferenceExpression(methodReferenceExpression);
                }
                else if (value is IArgumentReferenceExpression argumentReferenceExpression)
                {
                    VisitArgumentReferenceExpression(argumentReferenceExpression);
                }
                else if (value is IVariableDeclarationExpression declarationExpression)
                {
                    VisitVariableDeclarationExpression(declarationExpression);
                }
                else if (value is ITypeReferenceExpression typeReferenceExpression)
                {
                    VisitTypeReferenceExpression(typeReferenceExpression);
                }
                else if (value is IBaseReferenceExpression baseReferenceExpression)
                {
                    VisitBaseReferenceExpression(baseReferenceExpression);
                }
                else if (value is IUnaryExpression unaryExpression)
                {
                    VisitUnaryExpression(unaryExpression);
                }
                else if (value is ITryCastExpression castExpression)
                {
                    VisitTryCastExpression(castExpression);
                }
                else if (value is ICanCastExpression canCastExpression)
                {
                    VisitCanCastExpression(canCastExpression);
                }
                else if (value is ICastExpression expression1)
                {
                    VisitCastExpression(expression1);
                }
                else if (value is ITypeOfExpression ofExpression)
                {
                    VisitTypeOfExpression(ofExpression);
                }
                else if (value is IFieldOfExpression fieldOfExpression)
                {
                    VisitFieldOfExpression(fieldOfExpression);
                }
                else if (value is IMethodOfExpression methodOfExpression)
                {
                    VisitMethodOfExpression(methodOfExpression);
                }
                else if (value is IMemberInitializerExpression initializerExpression)
                {
                    VisitMemberInitializerExpression(initializerExpression);
                }
                else if (value is IEventReferenceExpression eventReferenceExpression)
                {
                    VisitEventReferenceExpression(eventReferenceExpression);
                }
                else if (value is IArgumentListExpression listExpression)
                {
                    VisitArgumentListExpression(listExpression);
                }
                else if (value is IArrayCreateExpression createExpression)
                {
                    VisitArrayCreateExpression(createExpression);
                }
                else if (value is IBlockExpression blockExpression)
                {
                    VisitBlockExpression(blockExpression);
                }
                else if (value is IConditionExpression conditionExpression)
                {
                    VisitConditionExpression(conditionExpression);
                }
                else if (value is INullCoalescingExpression coalescingExpression)
                {
                    VisitNullCoalescingExpression(coalescingExpression);
                }
                else if (value is IDelegateCreateExpression delegateCreateExpression)
                {
                    VisitDelegateCreateExpression(delegateCreateExpression);
                }
                else if (value is IAnonymousMethodExpression methodExpression)
                {
                    VisitAnonymousMethodExpression(methodExpression);
                }
                else if (value is IPropertyIndexerExpression propertyIndexerExpression)
                {
                    VisitPropertyIndexerExpression(propertyIndexerExpression);
                }
                else if (value is IArrayIndexerExpression indexerExpression)
                {
                    VisitArrayIndexerExpression(indexerExpression);
                }
                else if (value is IDelegateInvokeExpression delegateInvokeExpression)
                {
                    VisitDelegateInvokeExpression(delegateInvokeExpression);
                }
                else if (value is IObjectCreateExpression objectCreateExpression)
                {
                    VisitObjectCreateExpression(objectCreateExpression);
                }
                else if (value is IAddressOfExpression addressOfExpression)
                {
                    VisitAddressOfExpression(addressOfExpression);
                }
                else if (value is IAddressReferenceExpression addressReferenceExpression)
                {
                    VisitAddressReferenceExpression(addressReferenceExpression);
                }
                else if (value is IAddressOutExpression outExpression)
                {
                    VisitAddressOutExpression(outExpression);
                }
                else if (value is IAddressDereferenceExpression dereferenceExpression)
                {
                    VisitAddressDereferenceExpression(dereferenceExpression);
                }
                else if (value is ISizeOfExpression sizeOfExpression)
                {
                    VisitSizeOfExpression(sizeOfExpression);
                }
                else if (value is ITypedReferenceCreateExpression referenceCreateExpression)
                {
                    VisitTypedReferenceCreateExpression(referenceCreateExpression);
                }
                else if (value is ITypeOfTypedReferenceExpression ofTypedReferenceExpression)
                {
                    VisitTypeOfTypedReferenceExpression(ofTypedReferenceExpression);
                }
                else if (value is IValueOfTypedReferenceExpression typedReferenceExpression)
                {
                    VisitValueOfTypedReferenceExpression(typedReferenceExpression);
                }
                else if (value is IStackAllocateExpression allocateExpression)
                {
                    VisitStackAllocateExpression(allocateExpression);
                }
                else if (value is IGenericDefaultExpression defaultExpression)
                {
                    VisitGenericDefaultExpression(defaultExpression);
                }
                else if (value is IQueryExpression queryExpression)
                {
                    VisitQueryExpression(queryExpression);
                }
                else if (value is ILambdaExpression lambdaExpression)
                {
                    VisitLambdaExpression(lambdaExpression);
                }
                else if (value is ISnippetExpression snippetExpression)
                {
                    VisitSnippetExpression(snippetExpression);
                }
                else
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture,
                        "Invalid expression type '{0}'.", new object[]
                        {
                                value.GetType().Name
                        }));

            }
        }

        public virtual void VisitVariableDeclarationExpression(IVariableDeclarationExpression value)
        {
            VisitVariableDeclaration(value.Variable);
        }

        public virtual void VisitMemberInitializerExpression(IMemberInitializerExpression value)
        {
            VisitExpression(value.Value);
        }

        public virtual void VisitTypeOfExpression(ITypeOfExpression value)
        {
            VisitType(value.Type);
        }

        public virtual void VisitFieldOfExpression(IFieldOfExpression value)
        {
            VisitFieldReference(value.Field);
        }

        public virtual void VisitMethodOfExpression(IMethodOfExpression value)
        {
            VisitMethodReference(value.Method);
            if (value.Type != null)
            {
                VisitTypeReference(value.Type);
            }
        }

        public virtual void VisitArrayCreateExpression(IArrayCreateExpression value)
        {
            VisitType(value.Type);
            VisitExpression(value.Initializer);
            VisitExpressionCollection(value.Dimensions);
        }

        public virtual void VisitBlockExpression(IBlockExpression value)
        {
            VisitExpressionCollection(value.Expressions);
        }

        public virtual void VisitBaseReferenceExpression(IBaseReferenceExpression value)
        {
        }

        public virtual void VisitTryCastExpression(ITryCastExpression value)
        {
            VisitType(value.TargetType);
            VisitExpression(value.Expression);
        }

        public virtual void VisitCanCastExpression(ICanCastExpression value)
        {
            VisitType(value.TargetType);
            VisitExpression(value.Expression);
        }

        public virtual void VisitCastExpression(ICastExpression value)
        {
            VisitType(value.TargetType);
            VisitExpression(value.Expression);
        }

        public virtual void VisitConditionExpression(IConditionExpression value)
        {
            VisitExpression(value.Condition);
            VisitExpression(value.Then);
            VisitExpression(value.Else);
        }

        public virtual void VisitNullCoalescingExpression(INullCoalescingExpression value)
        {
            VisitExpression(value.Condition);
            VisitExpression(value.Expression);
        }

        public virtual void VisitDelegateCreateExpression(IDelegateCreateExpression value)
        {
            VisitType(value.DelegateType);
            VisitExpression(value.Target);
        }

        public virtual void VisitAnonymousMethodExpression(IAnonymousMethodExpression value)
        {
            VisitType(value.DelegateType);
            VisitParameterDeclarationCollection(value.Parameters);
            VisitMethodReturnType(value.ReturnType);
            VisitStatement(value.Body);
        }

        public virtual void VisitQueryExpression(IQueryExpression value)
        {
            VisitFromClause(value.From);
            VisitQueryBody(value.Body);
        }

        public virtual void VisitQueryBody(IQueryBody value)
        {
            VisitQueryOperation(value.Operation);
            VisitQueryClauseCollection(value.Clauses);
            if (value.Continuation != null)
            {
                VisitQueryContinuation(value.Continuation);
            }
        }

        public virtual void VisitQueryClause(IQueryClause value)
        {
            if (value is IFromClause fromClause)
            {
                VisitFromClause(fromClause);
            }
            else if (value is IWhereClause whereClause)
            {
                VisitWhereClause(whereClause);
            }
            else if (value is ILetClause letClause)
            {
                VisitLetClause(letClause);
            }
            else if (value is IJoinClause clause)
            {
                VisitJoinClause(clause);
            }
            else if (value is IOrderClause orderClause)
            {
                VisitOrderClause(orderClause);
            }
            else
                throw new NotSupportedException();

        }

        public virtual void VisitQueryOperation(IQueryOperation value)
        {
            if (value is ISelectOperation operation)
            {
                VisitSelectOperation(operation);
            }
            else if (value is IGroupOperation groupOperation)
            {
                VisitGroupOperation(groupOperation);
            }
            else
                throw new NotSupportedException();
        }

        public virtual void VisitQueryContinuation(IQueryContinuation value)
        {
            VisitVariableDeclaration(value.Variable);
            VisitQueryBody(value.Body);
        }

        public virtual void VisitSelectOperation(ISelectOperation value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitGroupOperation(IGroupOperation value)
        {
            VisitExpression(value.Item);
            VisitExpression(value.Key);
        }

        public virtual void VisitFromClause(IFromClause value)
        {
            VisitVariableDeclaration(value.Variable);
            VisitExpression(value.Expression);
        }

        public virtual void VisitWhereClause(IWhereClause value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitLetClause(ILetClause value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitJoinClause(IJoinClause value)
        {
            VisitVariableDeclaration(value.Variable);
            VisitExpression(value.In);
            VisitExpression(value.On);
            VisitExpression(value.Equality);
            if (value.Into != null)
            {
                VisitVariableDeclaration(value.Into);
            }
        }

        public virtual void VisitOrderClause(IOrderClause value)
        {
            VisitExpression(value.ExpressionAndDirections[0].Expression);
        }

        public virtual void VisitLambdaExpression(ILambdaExpression value)
        {
            VisitVariableDeclarationCollection(value.Parameters);
            VisitExpression(value.Body);
        }

        public virtual void VisitTypeReferenceExpression(ITypeReferenceExpression value)
        {
            VisitType(value.Type);
        }

        public virtual void VisitFieldReferenceExpression(IFieldReferenceExpression value)
        {
            VisitFieldReference(value.Field);
            VisitExpression(value.Target);
        }

        public virtual void VisitArgumentReferenceExpression(IArgumentReferenceExpression value)
        {
        }

        public virtual void VisitArgumentListExpression(IArgumentListExpression value)
        {
        }

        public virtual void VisitVariableReferenceExpression(IVariableReferenceExpression value)
        {
            VisitVariableReference(value.Variable);
        }

        public virtual void VisitPropertyIndexerExpression(IPropertyIndexerExpression value)
        {
            VisitExpressionCollection(value.Indices);
            VisitExpression(value.Target);
        }

        public virtual void VisitArrayIndexerExpression(IArrayIndexerExpression value)
        {
            VisitExpressionCollection(value.Indices);
            VisitExpression(value.Target);
        }

        public virtual void VisitMethodInvokeExpression(IMethodInvokeExpression value)
        {
            VisitExpressionCollection(value.Arguments);
            VisitExpression(value.Method);
        }

        public virtual void VisitMethodReferenceExpression(IMethodReferenceExpression value)
        {
            VisitExpression(value.Target);
        }

        public virtual void VisitEventReferenceExpression(IEventReferenceExpression value)
        {
            VisitEventReference(value.Event);
            VisitExpression(value.Target);
        }

        public virtual void VisitDelegateInvokeExpression(IDelegateInvokeExpression value)
        {
            VisitExpressionCollection(value.Arguments);
            VisitExpression(value.Target);
        }

        public virtual void VisitObjectCreateExpression(IObjectCreateExpression value)
        {
            VisitType(value.Type);
            if (value.Constructor != null)
            {
                VisitMethodReference(value.Constructor);
            }
            VisitExpressionCollection(value.Arguments);
            if (value.Initializer != null)
            {
                VisitBlockExpression(value.Initializer);
            }
        }

        public virtual void VisitPropertyReferenceExpression(IPropertyReferenceExpression value)
        {
            VisitPropertyReference(value.Property);
            VisitExpression(value.Target);
        }

        public virtual void VisitThisReferenceExpression(IThisReferenceExpression value)
        {
        }

        public virtual void VisitAddressOfExpression(IAddressOfExpression value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitAddressReferenceExpression(IAddressReferenceExpression value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitAddressOutExpression(IAddressOutExpression value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitAddressDereferenceExpression(IAddressDereferenceExpression value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitSizeOfExpression(ISizeOfExpression value)
        {
            VisitType(value.Type);
        }

        public virtual void VisitStackAllocateExpression(IStackAllocateExpression value)
        {
            VisitType(value.Type);
            VisitExpression(value.Expression);
        }

        public virtual void VisitSnippetExpression(ISnippetExpression value)
        {
        }

        public virtual void VisitUnaryExpression(IUnaryExpression value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitBinaryExpression(IBinaryExpression value)
        {
            VisitExpression(value.Left);
            VisitExpression(value.Right);
        }

        public virtual void VisitLiteralExpression(ILiteralExpression value)
        {
        }

        public virtual void VisitGenericDefaultExpression(IGenericDefaultExpression value)
        {
        }

        public virtual void VisitTypeOfTypedReferenceExpression(ITypeOfTypedReferenceExpression value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitValueOfTypedReferenceExpression(IValueOfTypedReferenceExpression value)
        {
            VisitType(value.TargetType);
            VisitExpression(value.Expression);
        }

        public virtual void VisitTypedReferenceCreateExpression(ITypedReferenceCreateExpression value)
        {
            VisitExpression(value.Expression);
        }

        public virtual void VisitArrayDimension(IArrayDimension value)
        {
        }

        public virtual void VisitSwitchCase(ISwitchCase value)
        {
            if (value != null)
            {
                IConditionCase conditionCase = value as IConditionCase;
                if (conditionCase != null)
                {
                    VisitConditionCase(conditionCase);
                }
                else
                {
                    IDefaultCase defaultCase = value as IDefaultCase;
                    if (defaultCase == null)
                    {
                        throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Invalid switch case type '{0}'.", new object[]
                        {
                            value.GetType().Name
                        }));
                    }
                    VisitDefaultCase(defaultCase);
                }
            }
        }

        public virtual void VisitDefaultCase(IDefaultCase value)
        {
            VisitStatement(value.Body);
        }

        public virtual void VisitConditionCase(IConditionCase value)
        {
            VisitExpression(value.Condition);
            VisitStatement(value.Body);
        }

        public virtual void VisitCatchClause(ICatchClause value)
        {
            VisitVariableDeclaration(value.Variable);
            VisitExpression(value.Condition);
            VisitStatement(value.Body);
        }

        public virtual void VisitVariableDeclaration(IVariableDeclaration value)
        {
            VisitType(value.VariableType);
        }

        public virtual void VisitVariableReference(IVariableReference value)
        {
        }

        public virtual void VisitMethodReference(IMethodReference value)
        {
            VisitMethodReturnType(value.ReturnType);
        }

        public virtual void VisitFieldReference(IFieldReference value)
        {
            VisitType(value.FieldType);
        }

        public virtual void VisitPropertyReference(IPropertyReference value)
        {
            VisitType(value.PropertyType);
        }

        public virtual void VisitEventReference(IEventReference value)
        {
            VisitType(value.EventType);
        }

        public virtual void VisitModuleCollection(IModuleCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitModule(value[i]);
            }
        }

        public virtual void VisitResourceCollection(IResourceCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitResource(value[i]);
            }
        }

        public virtual void VisitTypeCollection(ITypeCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitType(value[i]);
            }
        }

        public virtual void VisitTypeDeclarationCollection(ITypeDeclarationCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitTypeDeclaration(value[i]);
            }
        }

        public virtual void VisitFieldDeclarationCollection(IFieldDeclarationCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitFieldDeclaration(value[i]);
            }
        }

        public virtual void VisitMethodDeclarationCollection(IMethodDeclarationCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitMethodDeclaration(value[i]);
            }
        }

        public virtual void VisitPropertyDeclarationCollection(IPropertyDeclarationCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitPropertyDeclaration(value[i]);
            }
        }

        public virtual void VisitEventDeclarationCollection(IEventDeclarationCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitEventDeclaration(value[i]);
            }
        }

        public virtual void VisitCustomAttributeCollection(ICustomAttributeCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitCustomAttribute(value[i]);
            }
        }

        public virtual void VisitParameterDeclarationCollection(IParameterDeclarationCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitParameterDeclaration(value[i]);
            }
        }

        public virtual void VisitVariableDeclarationCollection(IVariableDeclarationCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitVariableDeclaration(value[i]);
            }
        }

        public virtual void VisitMethodReferenceCollection(IMethodReferenceCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitMethodReference(value[i]);
            }
        }

        public virtual void VisitStatementCollection(StatementCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitStatement(value[i]);
            }
        }

        public virtual void VisitExpressionCollection(ExpressionCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitExpression(value[i]);
            }
        }

        public virtual void VisitQueryClauseCollection(IQueryClauseCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitQueryClause(value[i]);
            }
        }

        public virtual void VisitCatchClauseCollection(ICatchClauseCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitCatchClause(value[i]);
            }
        }

        public virtual void VisitSwitchCaseCollection(List<ISwitchCase> value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitSwitchCase(value[i]);
            }
        }

        public virtual void VisitArrayDimensionCollection(IArrayDimensionCollection value)
        {
            for (int i = 0; i < value.Count; i++)
            {
                VisitArrayDimension(value[i]);
            }
        }
    }
}
