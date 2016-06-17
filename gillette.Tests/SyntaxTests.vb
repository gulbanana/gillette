Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports The.Reports

<TestClass> Public Class SyntaxTests
    Public Class SmallModel
        Property Foo As Integer
    End Class

    Public Class LargeModel
        Property Foo As String
        Property Bar As SmallModel
    End Class

    Public Class RTFModel
        Property SimpleProp As String
        Property CollectionProp As RTFCollection

        Public Class RTFCollection
            Property SimpleProp As String
            Property Groups As List(Of RTFGroup)
        End Class

        Public Class RTFGroup
            Property SimpleProp As String
            Property Rows As List(Of RTFRow)
        End Class

        Public Class RTFRow
            Property FiscalYear As String
            Property FileNumber As String
            Property Applicant As String
            Property ProjectTitle As String
            Property Amount As String
            Property PaymentDate As String
            Property GranteeCategory As String
            Property TotalOutstanding As String
            Property ApprovedAmount As String
            Property VariationDate As String
        End Class
    End Class

    <TestMethod> Public Sub SyntaxEmpty()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}
        Dim report = Razor.Generate(<xml></xml>.Value, model)
        Assert.AreEqual("", report)
    End Sub

    <TestMethod> Public Sub SyntaxRaw()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}
        Dim report = Razor.Generate(<xml>\rtf\rtf</xml>.Value, model)
        Assert.AreEqual("\rtf\rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxLiteralAt()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}
        Dim report = Razor.Generate(<xml>@</xml>.Value, model)
        Assert.AreEqual("@", report)
    End Sub

    <TestMethod> Public Sub SyntaxEscapedAt()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>@@</xml>.Value, model)
        Assert.AreEqual("@", report)
    End Sub

    <TestMethod> Public Sub SyntaxEscapedAtExpression()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>\rtf @@Model.Foo = @Model.Foo \rtf</xml>.Value, model)
        Assert.AreEqual("\rtf @Model.Foo = 0 \rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxExpressionBare()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}
        Dim report = Razor.Generate(<xml>@Model.Bar.Foo.ToString()</xml>.Value, model)
        Assert.AreEqual("1", report)
    End Sub

    <TestMethod> Public Sub SyntaxExpressionPrefix()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>@Model.Bar.Foo.ToString()\rtf</xml>.Value, model)
        Assert.AreEqual("1\rtf", report)

        report = Razor.Generate(<xml>@Model.Bar.Foo.ToString() \rtf</xml>.Value, model)
        Assert.AreEqual("1 \rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxExpressionPostfix()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>\rtf@Model.Bar.Foo.ToString()</xml>.Value, model)
        Assert.AreEqual("\rtf1", report)

        report = Razor.Generate(<xml>\rtf @Model.Bar.Foo.ToString()</xml>.Value, model)
        Assert.AreEqual("\rtf 1", report)
    End Sub

    <TestMethod> Public Sub SyntaxExpressionInfix()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>\rtf@Model.Bar.Foo.ToString()\rtf</xml>.Value, model)
        Assert.AreEqual("\rtf1\rtf", report)

        report = Razor.Generate(<xml>\rtf @Model.Bar.Foo.ToString() \rtf</xml>.Value, model)
        Assert.AreEqual("\rtf 1 \rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxMultipleExpressions()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>@Model.Foo@Model.Bar.Foo.ToString()</xml>.Value, model)
        Assert.AreEqual("01", report)

        report = Razor.Generate(<xml>\rtf @Model.Foo \rtf @Model.Bar.Foo.ToString() \rtf</xml>.Value, model)
        Assert.AreEqual("\rtf 0 \rtf 1 \rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxExpressionAmbiguated()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>\rtf @Model.Foo + Model.Foo \rtf</xml>.Value, model)
        Assert.AreEqual("\rtf 0 + Model.Foo \rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxExpressionDisambiguated()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>\rtf @(Model.Foo + Model.Foo) \rtf</xml>.Value, model)
        Assert.AreEqual("\rtf 00 \rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxExpressionDisambiguatedLazily()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>\rtf @(Model.Foo + Model.Foo)() \rtf</xml>.Value, model)
        Assert.AreEqual("\rtf 00() \rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxExpressionDisambiguatedBalanced()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}

        Dim report = Razor.Generate(<xml>\rtf @(Model.Foo + Model.Bar.Foo.ToString()) \rtf</xml>.Value, model)
        Assert.AreEqual("\rtf 01 \rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxBlock()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}
        Dim report = Razor.Generate(<xml>@{
            var x = Model.Foo;
        }@x</xml>.Value, model)
        Assert.AreEqual("0", report)
    End Sub

    <TestMethod> Public Sub SyntaxBlockBalanced()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}
        Dim report = Razor.Generate(<xml>@{
            string x = "";
            if (true) {
                x = Model.Foo;
            }
        }@x</xml>.Value, model)
        Assert.AreEqual("0", report)
    End Sub

    <TestMethod> Public Sub SyntaxForeach()
        Dim model = {1, 2, 3}
        Dim report = Razor.Generate(<xml>@foreach (var i in Model) {x}</xml>.Value, model)
        Assert.AreEqual("xxx", report)
    End Sub

    <TestMethod> Public Sub SyntaxForeachEmbeddedCode()
        Dim model = {1, 2, 3}
        Dim report = Razor.Generate(<xml>@foreach (var i in Model) {@i}</xml>.Value, model)
        Assert.AreEqual("123", report)
    End Sub

    <TestMethod> Public Sub SyntaxForeachEmbeddedContent()
        Dim model = {1, 2, 3}
        Dim report = Razor.Generate(<xml>@foreach (var i in Model) {\rtf}</xml>.Value, model)
        Assert.AreEqual("\rtf\rtf\rtf", report)
    End Sub

    <TestMethod> Public Sub SyntaxIf()
        Dim model = True

        Dim report = Razor.Generate(<xml>@if (Model) {yes}</xml>.Value, model)
        Assert.AreEqual("yes", report)
    End Sub

    <TestMethod> Public Sub SyntaxIfElse()
        Dim report = Razor.Generate(<xml>@if (Model) {yes} else {no}</xml>.Value, True)
        Assert.AreEqual("yes", report)

        report = Razor.Generate(<xml>@if (Model) {yes} else {no}</xml>.Value, False)
        Assert.AreEqual("no", report)
    End Sub

    <TestMethod> Public Sub SyntaxIfElseIf()
        Dim report = Razor.Generate(<xml>@if (Model) {yes} else if (!Model) {no}</xml>.Value, False)
        Assert.AreEqual("no", report)
    End Sub

    <TestMethod> Public Sub SyntaxIfElseIfElse()
        Dim report = Razor.Generate(<xml>@if (Model) {yes} else if (Model) {YES} else {no}</xml>.Value, False)
        Assert.AreEqual("no", report)
    End Sub

    <TestMethod> Public Sub SyntaxIfElseEtCetera()
        Dim report = Razor.Generate(<xml>
@if (Model) {
    no
} else if (Model) {
    no
} else if (Model) {
    no
} else if (Model) {
    no
} else if (Model) {
    NO
} else if (!Model) {
    OKAY WHATEVER
} else {
    yes
}</xml>.Value, False)
        Assert.AreEqual(<xml>

    OKAY WHATEVER
</xml>.Value, report)
    End Sub

    <TestMethod> Public Sub SyntaxUsing()
        Dim model = {1, 2, 3}
        Dim report = Razor.Generate(<xml>@using (var s = new System.IO.MemoryStream()) {@s.ToString()}</xml>.Value, model)
        Assert.AreEqual("System.IO.MemoryStream", report)
    End Sub

    <TestMethod> Public Sub SyntaxTryCatch()
        Dim model = {1, 2, 3}
        Dim report = Razor.Generate(<xml>@try {foo} catch (Exception e) {bar}</xml>.Value, model)
        Assert.AreEqual("foo", report)

        report = Razor.Generate(<xml>@try {@{throw new Exception();}} catch (Exception e) {bar}</xml>.Value, model)
        Assert.AreEqual("bar", report)
    End Sub

    <TestMethod> Public Sub SyntaxTryFinally()
        Dim model = {1, 2, 3}
        Dim report = Razor.Generate(<xml>@try {foo} finally {bar}</xml>.Value, model)
        Assert.AreEqual("foobar", report)
    End Sub

    <TestMethod> Public Sub SyntaxTryCatchFinally()
        Dim model = {1, 2, 3}
        Dim report = Razor.Generate(<xml>@try {foo} catch (Exception e) {bar} finally {baz}</xml>.Value, model)
        Assert.AreEqual("foobaz", report)
    End Sub

    <TestMethod> Public Sub SyntaxFunction()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}
        Dim report = Razor.Generate(<xml>@String.Format("{0}",Model.Bar.Foo)</xml>.Value, model)
        Assert.AreEqual("1", report)
    End Sub

    <TestMethod> Public Sub SyntaxFunctionWithWhitespace()
        Dim model = New LargeModel With {.Foo = "0", .Bar = New SmallModel With {.Foo = 1}}
        Dim report = Razor.Generate(<xml>@String.Format("{0}", Model.Bar.Foo)</xml>.Value, model)
        Assert.AreEqual("1", report)
    End Sub

    <TestMethod> Public Sub SyntaxDirectNesting()
        Dim report = Razor.Generate(<xml>@foreach (var i in Enumerable.Range(0, 3)) {@foreach (var j in Enumerable.Range(0, 3)) {@Model} }</xml>.Value, ".")
        Assert.AreEqual(".........", report)
    End Sub

    <TestMethod> Public Sub SyntaxDirectNestingRTF()
        Dim report = Razor.Generate(<xml>{\rtf@foreach (var i in Enumerable.Range(0, 3)) \{@foreach (var j in Enumerable.Range(0, 3)) \{@Model\} \par\}</xml>.Value, ".")
        Assert.AreEqual("{\rtf.........", report)
    End Sub

    <TestMethod> Public Sub SyntaxRTFControlFlow()
        Dim model = True

        Dim report = Razor.Generate(<xml>{\rtf @if (Model) \{yes\}</xml>.Value, model)
        Assert.AreEqual("{\rtf yes", report)
    End Sub

    <TestMethod> Public Sub SyntaxRTFStatements()
        Dim model = "1"

        Dim report = Razor.Generate(<xml>{\rtf @\{ var x = "1"; \}@x</xml>.Value, model)
        Assert.AreEqual("{\rtf 1", report)
    End Sub

    <TestMethod> Public Sub SyntaxRTFNewline()
        Dim report = Razor.Generate(<xml>{\rtf @\{ \par var x = "ye"; \par var y = x + "s"; \}@y</xml>.Value, True)
        Assert.AreEqual("{\rtf yes", report)
    End Sub

    <TestMethod> Public Sub SyntaxRTFNewlineIf()
        Dim report = Razor.Generate(<xml>{\rtf @if (Model) \{ \par yes\}</xml>.Value, True)
        Assert.AreEqual("{\rtf yes", report)
    End Sub

    <TestMethod> Public Sub SyntaxEmbeddedString()
        Dim model = {1, 2, 3}
        Dim report = Razor.Generate(<xml>"string"</xml>.Value, model)
        Assert.AreEqual(<xml>"string"</xml>.Value, report)
    End Sub

    <TestMethod> Public Sub SyntaxNestedLoops()
        Dim model = New X() With {.List = New List(Of List(Of Integer))() From {New List(Of Integer)() From {1, 2, 3}, New List(Of Integer)() From {2, 3, 4}, New List(Of Integer)() From {3, 4, 5}}}

        Dim actual = Razor.Generate("{\rtf 
\cltxlrtb
@foreach (var x in Model.List)
\{
\par
@foreach (var y in x)
\{
\par \ltrrow
\rtlch @y \rtlch
\insrsid15163223
\}
\par
\}
\par", model)
        Dim expected = "{\rtf 
\cltxlrtb
\ltrrow
\rtlch 1 \rtlch
\insrsid15163223
\ltrrow
\rtlch 2 \rtlch
\insrsid15163223
\ltrrow
\rtlch 3 \rtlch
\insrsid15163223
\ltrrow
\rtlch 2 \rtlch
\insrsid15163223
\ltrrow
\rtlch 3 \rtlch
\insrsid15163223
\ltrrow
\rtlch 4 \rtlch
\insrsid15163223
\ltrrow
\rtlch 3 \rtlch
\insrsid15163223
\ltrrow
\rtlch 4 \rtlch
\insrsid15163223
\ltrrow
\rtlch 5 \rtlch
\insrsid15163223
"
        Dim expectedLines = expected.Split({vbCrLf, vbLf}, StringSplitOptions.None)
        Dim actualLines = actual.Split({vbCrLf, vbLf}, StringSplitOptions.None)
        For i = 0 To Math.Max(expectedLines.Length - 1, actualLines.Length - 1)
            Assert.IsTrue(i < expectedLines.Length)
            Assert.IsTrue(i < actualLines.Length)
            Assert.AreEqual(expectedLines(i), actualLines(i))
        Next
    End Sub

    Public Class X
        Public List As List(Of List(Of Integer))
    End Class
End Class