using System;
using System.IO;
using Reflector.CodeModel;
using Reflector.CodeModel.Memory;
using Reflector.Ps.CodeModel;

namespace Reflector.Ps.Languages
{
    internal sealed class VisitorWriter : Visitor
    {
        public VisitorWriter(IFormatter formatter, ILanguageWriterConfiguration configuration)
        {
            this._formatter = formatter;
            this._configuration = configuration;
        }

        private void WriteWhitespace()
        {
            _formatter.Write(" ");
        }

        public override void VisitAssembly(IAssembly value)
        {
            if (_configuration["ShowNamespaceBody"] == "true")
            {
                base.VisitAssembly(value);
            }
            else
            {
                _formatter.Write("# Assembly ");
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteProperty("Name", value.ToString());
                _formatter.WriteProperty("Location", value.Location);
            }
        }

        public override void VisitNamespace(INamespace value)
        {
            if (_configuration["ShowNamespaceBody"] == "true")
            {
                base.VisitNamespace(value);
            }
            else
            {
                _formatter.Write("# Namespace ");
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteProperty("Assembly", Helper.GetAssemblyReference(value.Types[0]).ToString());
                _formatter.WriteProperty("Location", Helper.GetAssemblyReference(value.Types[0]).Resolve().Location);
            }
        }

        public override void VisitModule(IModule value)
        {
            if (_configuration["ShowNamespaceBody"] == "true")
            {
                base.VisitModule(value);
            }
            else
            {
                _formatter.Write("# Namespace ");
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteProperty("Location", value.Location);
                long length = new FileInfo(Environment.ExpandEnvironmentVariables(value.Location)).Length;
                if (length > 1024000L)
                {
                    _formatter.WriteProperty("Size", (length / 1024000L).ToString("F") + " Mb");
                }
                else if (length > 1024L)
                {
                    _formatter.WriteProperty("Size", (length / 1024L).ToString("F") + " Kb");
                }
                else
                {
                    _formatter.WriteProperty("Size", length + " Bytes");
                }
                _formatter.WriteProperty("Runtime", value.TargetRuntimeVersion);
            }
        }

        public override void VisitTypeDeclaration(ITypeDeclaration value)
        {
            if (_configuration["ShowTypeDeclarationBody"] == "true")
            {
                base.VisitTypeDeclaration(value);
            }
            else
            {
                _formatter.Write("# Type ");
                _formatter.WriteDeclaration(value.Name);
                _formatter.WriteProperty("Name", value.Namespace + "." + value.Name);
                _formatter.WriteProperty("Assembly", Helper.GetAssemblyReference(value).ToString());
                _formatter.WriteProperty("Location", Helper.GetAssemblyReference(value).Resolve().Location);
            }
        }

        public override void VisitStatementCollection(StatementCollection value)
        {
            foreach (IStatement statement in value)
            {
                VisitStatement(statement);
                _formatter.WriteLine();
            }
        }

        public override void VisitMethodDeclarationCollection(IMethodDeclarationCollection value)
        {
            foreach (object obj in value)
            {
                IMethodDeclaration value2 = (IMethodDeclaration)obj;
                VisitMethodDeclaration(value2);
                _formatter.WriteLine();
            }
        }

        public override void VisitMethodDeclaration(IMethodDeclaration value)
        {
            if (value.HasThis)
            {
                _formatter.WriteLiteral("# Instance methods are not supported at the moment.");
                _formatter.WriteLine();
                WriteUnsupported(value.ToString());
                _formatter.WriteLiteral("# Rendering as static function. Access to 'this' and 'base' will not work.");
                _formatter.WriteLine();
            }
            if (value.GenericArguments.Count != 0)
            {
                WriteUnsupported(value.ToString());
            }
            _formatter.WriteKeyword("function");
            WriteWhitespace();
            _formatter.WriteDeclaration(value.Name);
            _formatter.WriteLine();
            using (new IndentedCodeBlock(_formatter))
            {
                VisitParameterDeclarationCollection(value.Parameters);
                if (value.Body is IBlockStatement blockStatement)
                {
                    VisitBlockStatement(blockStatement);
                }
            }
        }

        public override void VisitMethodReturnStatement(IMethodReturnStatement value)
        {
            _formatter.WriteKeyword("return");
            WriteWhitespace();
            VisitExpression(value.Expression);
        }

        public override void VisitMethodReturnType(IMethodReturnType value)
        {
        }

        public override void VisitParameterDeclarationCollection(IParameterDeclarationCollection value)
        {
            if (value.Count > 0)
            {
                _formatter.WriteKeyword("param");
                _formatter.Write("(");
                foreach (object obj in value)
                {
                    IParameterDeclaration value2 = (IParameterDeclaration)obj;
                    if (value.IndexOf(value2) != 0)
                    {
                        _formatter.Write(", ");
                    }
                    VisitParameterDeclaration(value2);
                }
                _formatter.Write(")");
                _formatter.WriteLine();
                _formatter.WriteLine();
            }
        }

        public override void VisitParameterDeclaration(IParameterDeclaration value)
        {
            _formatter.Write("[");
            VisitType(value.ParameterType);
            _formatter.Write("] $");
            _formatter.Write(value.Name);
        }

        public override void VisitAssignExpression(IAssignExpression value)
        {
            VisitExpression(value.Target);
            _formatter.Write(" = ");
            VisitExpression(value.Expression);
        }

        public override void VisitForStatement(IForStatement value)
        {
            _formatter.WriteLine();
            _formatter.WriteKeyword("for");
            _formatter.Write("(");
            VisitStatement(value.Initializer);
            _formatter.Write("; ");
            VisitExpression(value.Condition);
            _formatter.Write("; ");
            VisitStatement(value.Increment);
            _formatter.Write(")");
            _formatter.WriteLine();
            using (new IndentedCodeBlock(_formatter))
            {
                VisitStatement(value.Body);
            }
        }

        public override void VisitLiteralExpression(ILiteralExpression value)
        {
            if (value.Value is string)
            {
                _formatter.WriteLiteral("\"" + value.Value + "\"");
            }
            else if (value.Value is bool)
            {
                _formatter.WriteKeyword("$" + value.Value.ToString().ToLower());
            }
            else if (value.Value == null)
            {
                _formatter.WriteLiteral("$null");
            }
            else
            {
                _formatter.Write(value.Value.ToString());
            }
        }

        public override void VisitVariableDeclaration(IVariableDeclaration value)
        {
            _formatter.WriteDeclaration("$" + value.Name);
        }

        public override void VisitArrayType(IArrayType type)
        {
            base.VisitArrayType(type);
            _formatter.Write("[]");
            for (int i = 0; i < type.Dimensions.Count; i++)
            {
                _formatter.Write("[]");
            }
        }

        public override void VisitTypeReference(ITypeReference type)
        {
            base.VisitTypeReference(type);
            if (type.Owner is ITypeReference typeReference)
            {
                VisitTypeReference(typeReference);
            }
            _formatter.WriteReference(type.Namespace + "." + type.Name, string.Empty, type);
        }

        public override void VisitArgumentReferenceExpression(IArgumentReferenceExpression value)
        {
            _formatter.Write("$" + value.Parameter.Name);
        }

        public override void VisitBinaryExpression(IBinaryExpression value)
        {
            _formatter.Write("(");
            VisitExpression(value.Left);
            switch (value.Operator)
            {
                case BinaryOperator.Add:
                    _formatter.Write(" + ");
                    break;
                case BinaryOperator.Subtract:
                    _formatter.Write(" - ");
                    break;
                case BinaryOperator.Divide:
                    _formatter.Write(" % ");
                    break;
                case BinaryOperator.Modulus:
                    _formatter.WriteLiteral(" # BinaryOperator.Modulus # ");
                    break;
                case BinaryOperator.ShiftLeft:
                    _formatter.WriteLiteral(" # BinaryOperator.ShiftLeft # ");
                    break;
                case BinaryOperator.ShiftRight:
                    _formatter.WriteLiteral(" # BinaryOperator.ShiftRight # ");
                    break;
                case BinaryOperator.IdentityEquality:
                case BinaryOperator.ValueEquality:
                    _formatter.Write(" -eq ");
                    break;
                case BinaryOperator.IdentityInequality:
                case BinaryOperator.ValueInequality:
                    _formatter.Write(" -ne ");
                    break;
                case BinaryOperator.BitwiseOr:
                    _formatter.Write(" -bor ");
                    break;
                case BinaryOperator.BitwiseAnd:
                    _formatter.Write(" -band ");
                    break;
                case BinaryOperator.BitwiseExclusiveOr:
                    _formatter.WriteLiteral(" # BinaryOperator.BitwiseExclusiveOr # ");
                    break;
                case BinaryOperator.BooleanOr:
                    _formatter.Write(" -or ");
                    break;
                case BinaryOperator.BooleanAnd:
                    _formatter.Write(" -and ");
                    break;
                case BinaryOperator.LessThan:
                    _formatter.Write(" -lt ");
                    break;
                case BinaryOperator.LessThanOrEqual:
                    _formatter.Write(" -le ");
                    break;
                case BinaryOperator.GreaterThan:
                    _formatter.Write(" -gt ");
                    break;
                case BinaryOperator.GreaterThanOrEqual:
                    _formatter.Write(" -ge ");
                    break;
            }
            VisitExpression(value.Right);
            _formatter.Write(")");
        }

        public override void VisitUnaryExpression(IUnaryExpression value)
        {
            switch (value.Operator)
            {
                case UnaryOperator.Negate:
                case UnaryOperator.BooleanNot:
                    _formatter.Write("!");
                    VisitExpression(value.Expression);
                    break;
                case UnaryOperator.BitwiseNot:
                    _formatter.Write("-bnot ");
                    VisitExpression(value.Expression);
                    break;
                case UnaryOperator.PreIncrement:
                    _formatter.Write("++");
                    VisitExpression(value.Expression);
                    break;
                case UnaryOperator.PreDecrement:
                    _formatter.Write("--");
                    VisitExpression(value.Expression);
                    break;
                case UnaryOperator.PostIncrement:
                    VisitExpression(value.Expression);
                    _formatter.Write("++");
                    break;
                case UnaryOperator.PostDecrement:
                    VisitExpression(value.Expression);
                    _formatter.Write("--");
                    break;
            }
        }

        public override void VisitVariableReference(IVariableReference value)
        {
            _formatter.Write("$" + value.Resolve().Name);
        }

        public override void VisitFieldReference(IFieldReference value)
        {
            _formatter.Write(value.Name);
        }

        public override void VisitFieldReferenceExpression(IFieldReferenceExpression value)
        {
            if (value.Field.Resolve().Static)
            {
                _formatter.Write("[");
                VisitExpression(value.Target);
                _formatter.Write("]");
                _formatter.Write("::");
                VisitFieldReference(value.Field);
            }
            else
            {
                _formatter.WriteLiteral("# Instance fields are not supported yet.");
                _formatter.WriteLine();
                WriteUnsupported(value);
            }
        }

        public override void VisitMethodReferenceExpression(IMethodReferenceExpression value)
        {
            bool hasThis = !value.Method.HasThis;
            if (value.Target is IBinaryExpression)
            {
                _formatter.Write("(");
                VisitExpression(value.Target);
                _formatter.Write(")");
            }
            else if (hasThis)
            {
                _formatter.Write("[");
                VisitExpression(value.Target);
                _formatter.Write("]");
            }
            else
            {
                VisitExpression(value.Target);
            }

            _formatter.Write(hasThis ? "::" : ".");
            VisitMethodReference(value.Method);
        }

        public override void VisitMethodReference(IMethodReference value)
        {
            if (value.GenericArguments.Count > 0)
            {
                WriteUnsupported(new MethodReferenceExpression
                {
                    Method = value
                });
            }
            else
            {
                TextFormatter textFormatter = new TextFormatter();
                VisitorWriter visitorWriter = new VisitorWriter(textFormatter, _configuration);
                textFormatter.WriteKeyword("function");
                visitorWriter.WriteWhitespace();
                textFormatter.WriteDeclaration(value.Name);
                textFormatter.WriteLine();
                using (new IndentedCodeBlock(textFormatter))
                {
                    visitorWriter.VisitParameterDeclarationCollection(value.Resolve().Parameters);
                }
                _formatter.WriteReference(value.Name, textFormatter.ToString(), value);
            }
        }

        public override void VisitMethodInvokeExpression(IMethodInvokeExpression value)
        {
            VisitExpression(value.Method);
            _formatter.Write("(");
            foreach (IExpression expression in value.Arguments)
            {
                if (value.Arguments.IndexOf(expression) != 0)
                {
                    _formatter.Write(", ");
                }
                VisitExpression(expression);
            }
            _formatter.Write(")");
        }

        public override void VisitThisReferenceExpression(IThisReferenceExpression value)
        {
            _formatter.Write("$this");
        }

        public override void VisitConditionExpression(IConditionExpression value)
        {
            _formatter.Write("$(");
            _formatter.WriteKeyword("if");
            _formatter.Write(" (");
            VisitExpression(value.Condition);
            _formatter.Write(") { ");
            VisitExpression(value.Then);
            _formatter.Write(" } ");
            if (value.Else != null)
            {
                _formatter.WriteKeyword("else");
                _formatter.Write(" { ");
                VisitExpression(value.Else);
                _formatter.Write(" }");
            }
            _formatter.Write(")");
        }

        public override void VisitConditionStatement(IConditionStatement value)
        {
            _formatter.WriteKeyword("if");
            WriteWhitespace();
            _formatter.Write("(");
            VisitExpression(value.Condition);
            _formatter.Write(")");
            _formatter.WriteLine();
            using (new IndentedCodeBlock(_formatter))
            {
                VisitStatement(value.Then);
            }
            if (value.Else.Statements.Count != 0)
            {
                _formatter.WriteKeyword("else");
                _formatter.WriteLine();
                using (new IndentedCodeBlock(_formatter))
                {
                    VisitStatement(value.Else);
                }
            }
        }

        public override void VisitContinueStatement(IContinueStatement value)
        {
            _formatter.WriteKeyword("continue");
        }

        public override void VisitBreakStatement(IBreakStatement value)
        {
            _formatter.WriteKeyword("break");
        }

        public override void VisitExpressionCollection(ExpressionCollection value)
        {
            VisitExpressionCollection(value, false);
        }

        private void VisitExpressionCollection(ExpressionCollection collection, bool useColon)
        {
            foreach (IExpression expression in collection)
            {
                if (useColon && collection.IndexOf(expression) != 0)
                {
                    _formatter.Write(", ");
                }
                VisitExpression(expression);
            }
        }

        public override void VisitArrayCreateExpression(IArrayCreateExpression value)
        {
            if (value.Initializer != null)
            {
                _formatter.Write("$(");
                VisitExpressionCollection(value.Initializer.Expressions, true);
                _formatter.Write(")");
            }
            else
            {
                _formatter.Write("@(");
                VisitExpressionCollection(value.Dimensions, true);
                _formatter.Write(")");
            }
        }

        public override void VisitArrayIndexerExpression(IArrayIndexerExpression value)
        {
            VisitExpression(value.Target);
            _formatter.Write("[");
            VisitExpressionCollection(value.Indices, true);
            _formatter.Write("]");
        }

        public override void VisitCastExpression(ICastExpression value)
        {
            VisitExpression(value.Expression);
        }

        public override void VisitSwitchStatement(ISwitchStatement value)
        {
            _formatter.WriteLine();
            _formatter.WriteKeyword("switch");
            WriteWhitespace();
            _formatter.Write("(");
            VisitExpression(value.Expression);
            _formatter.Write(")");
            _formatter.WriteLine();
            using (new IndentedCodeBlock(_formatter))
            {
                foreach (ISwitchCase switchCase in value.Cases)
                {
                    VisitSwitchCase(switchCase);
                }
            }
            _formatter.WriteLine();
        }

        public override void VisitConditionCase(IConditionCase value)
        {
            VisitExpression(value.Condition);
            _formatter.WriteLine();
            using (new IndentedCodeBlock(_formatter))
            {
                VisitBlockStatement(value.Body);
            }
        }

        public override void VisitDefaultCase(IDefaultCase value)
        {
            _formatter.WriteKeyword("default");
            _formatter.WriteLine();
            using (new IndentedCodeBlock(_formatter))
            {
                VisitBlockStatement(value.Body);
            }
        }

        public override void VisitThrowExceptionStatement(IThrowExceptionStatement value)
        {
            _formatter.WriteKeyword("throw");
            WriteWhitespace();
            VisitExpression(value.Expression);
        }

        public override void VisitObjectCreateExpression(IObjectCreateExpression value)
        {
            _formatter.WriteKeyword("new-object");
            WriteWhitespace();
            VisitType(value.Constructor.DeclaringType);
            if (value.Arguments.Count > 0)
            {
                _formatter.Write("(");
                VisitExpressionCollection(value.Arguments, true);
                _formatter.Write(")");
            }
        }

        public override void VisitForEachStatement(IForEachStatement value)
        {
            _formatter.WriteLine();
            _formatter.WriteKeyword("foreach");
            WriteWhitespace();
            _formatter.Write("(");
            VisitVariableDeclaration(value.Variable);
            WriteWhitespace();
            _formatter.WriteKeyword("in");
            WriteWhitespace();
            VisitExpression(value.Expression);
            _formatter.Write(")");
            _formatter.WriteLine();
            using (new IndentedCodeBlock(_formatter))
            {
                VisitBlockStatement(value.Body);
            }
        }

        public override void VisitPropertyReferenceExpression(IPropertyReferenceExpression value)
        {
            VisitExpression(value.Target);
            VisitPropertyReference(value.Property);
        }

        public override void VisitPropertyReference(IPropertyReference value)
        {
            _formatter.Write(".");
            _formatter.WriteReference(value.Name, value.Name, value);
        }

        public override void VisitPropertyIndexerExpression(IPropertyIndexerExpression value)
        {
            VisitExpression(value.Target);
            _formatter.Write("[");
            VisitExpressionCollection(value.Indices, true);
            _formatter.Write("]");
        }

        public override void VisitDoStatement(IDoStatement value)
        {
            _formatter.WriteKeyword("do");
            _formatter.WriteLine();
            _formatter.Write("{");
            _formatter.WriteLine();
            _formatter.WriteIndent();
            VisitBlockStatement(value.Body);
            _formatter.WriteOutdent();
            _formatter.Write("} ");
            _formatter.WriteKeyword("until");
            WriteWhitespace();
            _formatter.Write("(");
            VisitExpression(value.Condition);
            _formatter.Write(")");
            _formatter.WriteLine();
        }

        public override void VisitCommentStatement(ICommentStatement value)
        {
            _formatter.WriteLiteral("#" + value.Comment.Text);
        }

        public override void VisitTypeOfExpression(ITypeOfExpression value)
        {
            _formatter.Write("(");
            _formatter.WriteKeyword("typeof");
            _formatter.Write(" ");
            VisitType(value.Type);
            _formatter.Write(")");
        }

        public override void VisitNullCoalescingExpression(INullCoalescingExpression value)
        {
            VisitExpression(new ConditionExpression
            {
                Condition = new BinaryExpression
                {
                    Left = value.Condition,
                    Operator = BinaryOperator.ValueInequality,
                    Right = new LiteralExpression()
                },
                Then = value.Condition,
                Else = value.Expression
            });
        }

        public override void VisitAddressDereferenceExpression(IAddressDereferenceExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitAddressOfExpression(IAddressOfExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitAddressOutExpression(IAddressOutExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitAddressReferenceExpression(IAddressReferenceExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitArgumentListExpression(IArgumentListExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitAttachEventStatement(IAttachEventStatement value)
        {
            WriteUnsupported(value);
        }

        public override void VisitBaseReferenceExpression(IBaseReferenceExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitCanCastExpression(ICanCastExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitCatchClause(ICatchClause value)
        {
            WriteUnsupported(value.ToString());
        }

        public override void VisitDelegateCreateExpression(IDelegateCreateExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitDelegateInvokeExpression(IDelegateInvokeExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitEventReference(IEventReference value)
        {
            WriteUnsupported(value.ToString());
        }

        public override void VisitEventReferenceExpression(IEventReferenceExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitGenericDefaultExpression(IGenericDefaultExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitGotoStatement(IGotoStatement value)
        {
            WriteUnsupported(value);
        }

        public override void VisitLabeledStatement(ILabeledStatement value)
        {
            WriteUnsupported(value);
        }

        public override void VisitOptionalModifier(IOptionalModifier type)
        {
            WriteUnsupported(type.ToString());
        }

        public override void VisitPointerType(IPointerType type)
        {
            WriteUnsupported(type.ToString());
        }

        public override void VisitReferenceType(IReferenceType type)
        {
            WriteUnsupported(type.ToString());
        }

        public override void VisitRemoveEventStatement(IRemoveEventStatement value)
        {
            WriteUnsupported(value);
        }

        public override void VisitRequiredModifier(IRequiredModifier type)
        {
            WriteUnsupported(type.ToString());
        }

        public override void VisitSizeOfExpression(ISizeOfExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitSnippetExpression(ISnippetExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitStackAllocateExpression(IStackAllocateExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitTryCastExpression(ITryCastExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitTryCatchFinallyStatement(ITryCatchFinallyStatement value)
        {
            WriteUnsupported(value);
        }

        public override void VisitTypedReferenceCreateExpression(ITypedReferenceCreateExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitTypeOfTypedReferenceExpression(ITypeOfTypedReferenceExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitUsingStatement(IUsingStatement value)
        {
            WriteUnsupported(value);
        }

        public override void VisitValueOfTypedReferenceExpression(IValueOfTypedReferenceExpression value)
        {
            WriteUnsupported(value);
        }

        public override void VisitWhileStatement(IWhileStatement value)
        {
            WriteUnsupported(value);
        }

        private void WriteUnsupported(string value)
        {
            _formatter.WriteLiteral("# Unsupported expression:");
            _formatter.WriteLine();
            _formatter.WriteLiteral("#" + value);
            _formatter.WriteLine();
        }

        private void WriteUnsupported(IExpression value)
        {
            _formatter.WriteLiteral("# Unsupported expression " + value.GetType().Name + ":");
            _formatter.WriteLine();
            _formatter.WriteLiteral("#" + value.ToString());
            _formatter.WriteLine();
        }

        private void WriteUnsupported(IStatement value)
        {
            _formatter.WriteLiteral("# Unsupported statement " + value.GetType().Name + ":");
            _formatter.WriteLine();
            _formatter.WriteLiteral("#" + value.ToString());
            _formatter.WriteLine();
        }

        private IFormatter _formatter;

        private ILanguageWriterConfiguration _configuration;

        private class IndentedCodeBlock : IDisposable
        {
            public IndentedCodeBlock(IFormatter formatter)
            {
                this._formatter = formatter;
                formatter.Write("{");
                formatter.WriteLine();
                formatter.WriteIndent();
            }

            public void Dispose()
            {
                _formatter.WriteOutdent();
                _formatter.Write("}");
                _formatter.WriteLine();
            }

            private IFormatter _formatter;
        }
    }
}
