using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Parsing;

namespace Framework.Sql
{
    class SQLGrammar<TValue> : Grammar<char>
    {
        public readonly NonTerminal QuerySpec = new NonTerminal();
        public readonly NonTerminal SetQuantifier = new NonTerminal();
        public readonly NonTerminal SelectList = new NonTerminal();
        public readonly NonTerminal TableExpression = new NonTerminal();
        public readonly GrammarRule QuerySpecRuleWithSetQuantifier;
        public readonly GrammarRule QuerySpecRule;
        private Terminal<char> Star;
        private GrammarRule SelectStarRule;
        private NonTerminal DerivedColumn;
        private Terminal<char> Comma;
        private GrammarRule SelectListRule;
        private GrammarRule SelectSingletonRule;
        private NonTerminal ValueExpression;
        private GrammarRule SimpleColumnRule;
        private Terminal<char> As;
        private GrammarRule ColumnAsRule;
        private NonTerminal ColumnName;
        private NonTerminal AsClause;
        private GrammarRule AsRule;
        private NonTerminal CommonValueExpression;
        private NonTerminal BooleanValueExpression;
        private NonTerminal RowValueExpression;
        private NonTerminal NumericValueExpression;
        private NonTerminal StringValueExpression;
        private NonTerminal DateTimeValueExpression;
        private NonTerminal IntervalValueExpression;
        private NonTerminal UserDefinedTypeValueExpression;
        private NonTerminal ReferenceValueExpression;
        private NonTerminal CollectionValueExpression;
        private NonTerminal ValueExpressionPrimary;
        private NonTerminal ArrayValueExpression;
        private NonTerminal MultisetValueExpression;
        private NonTerminal Term;
        private Terminal<char> PlusSign;
        private GrammarRule NumericAddRule;
        private Terminal<char> MinusSign;
        private GrammarRule NumericSubRule;
        private GrammarRule NumericMulRule;
        private GrammarRule NumericDivRule;
        private GrammarRule NumericSignedRule;
        private NonTerminal Factor;
        private Terminal<char> Asterisk;
        private Terminal<char> Slash;
        private NonTerminal NumericPrimary;
        private NonTerminal NumericValueFunction;
        private GrammarRule QuerySpecDistinctRule;
        private Terminal<char> Select;
        private Terminal<char> Distinct;
        private Terminal<char> LeftParen;
        private Terminal<char> RightParen;
        private NonTerminal NonparenthesizedValueExpressionPrimary;
        private GrammarRule QuerySpecAllRule;
        private Terminal<char> All;
        private NonTerminal UnsignedValueSpec;
        private NonTerminal ColumnReference;
        private NonTerminal SetFunctionSpec;
        private NonTerminal WindowFunction;
        private NonTerminal ScalarSubquery;
        private NonTerminal CaseExpression;
        private NonTerminal CastSpec;
        private NonTerminal FieldReference;
        private NonTerminal MethodInvocation;
        private NonTerminal StaticMethodInvocation;
        private NonTerminal NewSpecification;
        private NonTerminal AttributeOrMethodReference;
        private NonTerminal ReferenceResolution;
        private NonTerminal CollectionValueConstructor;
        private NonTerminal ArrayElementReference;
        private NonTerminal MultisetElementReference;
        private NonTerminal RoutineInvocation;
        private NonTerminal NextValueExpression;
        private NonTerminal Literal;
        private NonTerminal GeneralValueSpec;
        private NonTerminal HostParameterSpec;
        private NonTerminal SQLParameterReference;
        private NonTerminal DynamicParameterSpec;
        private NonTerminal EmbeddedVariableSpec;
        private NonTerminal CurrentCollationSpec;
        private NonTerminal CurrentDefaultTransformGroup;
        private NonTerminal CurrentPath;
        private NonTerminal CurrentRole;
        private NonTerminal CurrentUser;
        private NonTerminal SessionUser;
        private NonTerminal SystemUser;
        private Terminal<char> User;
        private Terminal<char> Value;
        private GrammarRule CurrentDefaultTransformGroupRule;
        private GrammarRule CurrentPathRule;
        private GrammarRule CurrentRoleRule;
        private GrammarRule CurrentUserRule;
        private GrammarRule SessionUserRule;
        private GrammarRule SystemUserRule;
        private GrammarRule UserRule;
        private GrammarRule ValueRule;
        private Terminal<char> Current;
        private Terminal<char> Default;
        private Terminal<char> Transform;
        private Terminal<char> Group;
        private Terminal<char> Path;
        private GrammarRule HostParameterSpecRule;
        private GrammarRule SqlParameterReferenceRule;
        private GrammarRule IdentifierChainSingletonRule;
        private GrammarRule IdentifierChainRule;
        private GrammarRule DynamicParameterSpecRule;
        private GrammarRule EmbeddedVariableNameRule;
        private GrammarRule CurrentCollationSpecRule;
        private Terminal<char> ColonOrAtSign;
        private NonTerminal HostParameterName;
        private NonTerminal BasicIdentifierChain;
        private NonTerminal IdentifierChain;
        private NonTerminal Identifier;
        private Terminal<char> Dot;
        private Terminal<char> QuestionMark;
        private NonTerminal EmbeddedVariableName;
        private Terminal<char> Colon;
        private Terminal<char> Collation;
        private Terminal<char> Role;
        private Terminal<char> Session;
        private Terminal<char> SystemKeyword;

        // Rules

        public SQLGrammar()
        {
            var rules = new GrammarRule[] {
                QuerySpecDistinctRule = new GrammarRule {LeftHandSide = QuerySpec, RightHandSide = new GrammarSymbol[] {Select, Distinct, SelectList, TableExpression}},
                QuerySpecAllRule = GrammarRule.Create(QuerySpec, Select, All, SelectList, TableExpression),
                QuerySpecRule = new GrammarRule {LeftHandSide = QuerySpec, RightHandSide = new GrammarSymbol[] {Select, SelectList, TableExpression}},
                SelectStarRule = new GrammarRule {LeftHandSide = SelectList, RightHandSide = new GrammarSymbol[] {Star}},
                SelectListRule = new GrammarRule {LeftHandSide = SelectList, RightHandSide = new GrammarSymbol[] {SelectList, Comma, DerivedColumn}},
                // NOTE: Skipping "derived asterisk" bit.
                SelectSingletonRule = new GrammarRule {LeftHandSide = SelectList, RightHandSide = new GrammarSymbol[] {DerivedColumn}},
                SimpleColumnRule = new GrammarRule {LeftHandSide = DerivedColumn, RightHandSide = new GrammarSymbol[] {ValueExpression}},
                ColumnAsRule = new GrammarRule {LeftHandSide = DerivedColumn, RightHandSide = new GrammarSymbol[] {ValueExpression, AsClause}},
                AsRule = new GrammarRule {LeftHandSide = AsClause, RightHandSide = new GrammarSymbol[] {As, ColumnName}},
                new GrammarRule {LeftHandSide = ValueExpression, RightHandSide = new GrammarSymbol[] {CommonValueExpression}},
                new GrammarRule {LeftHandSide = ValueExpression, RightHandSide = new GrammarSymbol[] {BooleanValueExpression}},
                new GrammarRule {LeftHandSide = ValueExpression, RightHandSide = new GrammarSymbol[] {RowValueExpression}},
                new GrammarRule {LeftHandSide = CommonValueExpression, RightHandSide = new GrammarSymbol[] {NumericValueExpression}},
                new GrammarRule {LeftHandSide = CommonValueExpression, RightHandSide = new GrammarSymbol[] {StringValueExpression}},
                new GrammarRule {LeftHandSide = CommonValueExpression, RightHandSide = new GrammarSymbol[] {DateTimeValueExpression}},
                new GrammarRule {LeftHandSide = CommonValueExpression, RightHandSide = new GrammarSymbol[] {IntervalValueExpression}},
                new GrammarRule {LeftHandSide = CommonValueExpression, RightHandSide = new GrammarSymbol[] {UserDefinedTypeValueExpression}},
                new GrammarRule {LeftHandSide = CommonValueExpression, RightHandSide = new GrammarSymbol[] {ReferenceValueExpression}},
                new GrammarRule {LeftHandSide = CommonValueExpression, RightHandSide = new GrammarSymbol[] {CollectionValueExpression}},
                GrammarRule.Create(UserDefinedTypeValueExpression, ValueExpressionPrimary),
                GrammarRule.Create(ReferenceValueExpression, ValueExpressionPrimary),
                GrammarRule.Create(CollectionValueExpression, ArrayValueExpression),
                GrammarRule.Create(CollectionValueExpression, MultisetValueExpression),
                GrammarRule.Create(NumericValueExpression, Term),
                NumericAddRule = GrammarRule.Create(NumericValueExpression, NumericValueExpression, PlusSign, Term),
                NumericSubRule = GrammarRule.Create(NumericValueExpression, NumericValueExpression, MinusSign, Term),
                GrammarRule.Create(Term, Factor),
                NumericMulRule = GrammarRule.Create(Term, Term, Asterisk, Factor),
                NumericDivRule = GrammarRule.Create(Term, Term, Slash, Factor),
                NumericSignedRule = GrammarRule.Create(Factor, MinusSign, NumericPrimary),
                GrammarRule.Create(Factor, PlusSign, NumericPrimary),
                GrammarRule.Create(Factor, NumericPrimary),
                GrammarRule.Create(NumericPrimary, ValueExpressionPrimary),
                GrammarRule.Create(NumericPrimary, NumericValueFunction),
                GrammarRule.Create(ValueExpressionPrimary, LeftParen, ValueExpression, RightParen),
                GrammarRule.Create(ValueExpressionPrimary, NonparenthesizedValueExpressionPrimary),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, UnsignedValueSpec),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, ColumnReference),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, SetFunctionSpec),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, WindowFunction),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, ScalarSubquery),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, CaseExpression),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, CastSpec),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, FieldReference),
                // NOTE: Skipping SubtypeTreatment
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, MethodInvocation),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, StaticMethodInvocation),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, NewSpecification),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, AttributeOrMethodReference),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, ReferenceResolution),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, CollectionValueConstructor),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, ArrayElementReference),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, MultisetElementReference),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, RoutineInvocation),
                GrammarRule.Create(NonparenthesizedValueExpressionPrimary, NextValueExpression),
                GrammarRule.Create(UnsignedValueSpec, Literal),
                GrammarRule.Create(UnsignedValueSpec, GeneralValueSpec),
                GrammarRule.Create(GeneralValueSpec, HostParameterSpec),
                GrammarRule.Create(GeneralValueSpec, SQLParameterReference),
                GrammarRule.Create(GeneralValueSpec, DynamicParameterSpec),
                GrammarRule.Create(GeneralValueSpec, EmbeddedVariableSpec),
                GrammarRule.Create(GeneralValueSpec, CurrentCollationSpec),
                CurrentDefaultTransformGroupRule = GrammarRule.Create(GeneralValueSpec, CurrentDefaultTransformGroup),
                CurrentPathRule = GrammarRule.Create(GeneralValueSpec, CurrentPath),
                CurrentRoleRule = GrammarRule.Create(GeneralValueSpec, CurrentRole),
                // NOTE: Skipping CurrentTrandformGroupForType
                CurrentUserRule = GrammarRule.Create(GeneralValueSpec, CurrentUser),
                SessionUserRule = GrammarRule.Create(GeneralValueSpec, SessionUser),
                SystemUserRule = GrammarRule.Create(GeneralValueSpec, SystemUser),
                UserRule = GrammarRule.Create(GeneralValueSpec, User),
                ValueRule = GrammarRule.Create(GeneralValueSpec, Value),
                HostParameterSpecRule = GrammarRule.Create(HostParameterSpec, ColonOrAtSign, HostParameterName),
                SqlParameterReferenceRule = GrammarRule.Create(SQLParameterReference, BasicIdentifierChain),
                GrammarRule.Create(BasicIdentifierChain, IdentifierChain),
                IdentifierChainSingletonRule = GrammarRule.Create(IdentifierChain, Identifier),
                IdentifierChainRule = GrammarRule.Create(IdentifierChain, IdentifierChain, Dot, Identifier),
                DynamicParameterSpecRule = GrammarRule.Create(DynamicParameterSpec, QuestionMark),
                GrammarRule.Create(EmbeddedVariableSpec, EmbeddedVariableName),
                EmbeddedVariableNameRule = GrammarRule.Create(EmbeddedVariableName, Colon, Identifier),
                CurrentCollationSpecRule = GrammarRule.Create(CurrentCollationSpec, Current, Collation, LeftParen, StringValueExpression, RightParen),
                GrammarRule.Create(CurrentDefaultTransformGroup, Current, Default, Transform, Group),
                GrammarRule.Create(CurrentPath, Current, Path),
                GrammarRule.Create(CurrentRole, Current, Role),
                GrammarRule.Create(CurrentUser, Current, User),
                GrammarRule.Create(SessionUser, Session, User),
                GrammarRule.Create(SystemUser, SystemKeyword, User),
                GrammarRule.Create(HostParameterName, Identifier),


                
            };

        }


    }
}
