<TestClass, DeploymentItem("report.csrtf")>
Public Class IntegrationTests
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

    <TestMethod> Public Sub IntegrationValidateSuccess()
        Dim errors = Razor.Validate(Of LargeModel)(<xml>{\rtf@Model.Bar.Foo.ToString()\rtf</xml>.Value)
        Assert.IsFalse(errors.Any())
    End Sub

    <TestMethod> Public Sub IntegrationValidateFailure()
        Dim errors = Razor.Validate(Of LargeModel)(<xml>\rtf@Model.Foo.Bar.ToString()\rtf</xml>.Value)
        Assert.IsTrue(errors.Any())
    End Sub

    <TestMethod> Public Sub IntegrationValidateLanguage()
        Dim errors = Razor.Validate(Of Boolean)("@(Model && true)")
        Assert.IsFalse(errors.Any())

        errors = Razor.Validate(Of Boolean)("@(Model AndAlso True)")
        Assert.IsTrue(errors.Any())
    End Sub

    <TestMethod> Public Sub IntegrationValidateRTFFile()
        Dim template = IO.File.ReadAllText("report.csrtf")
        Dim errors = Razor.Validate(Of RTFModel)(template)
        Assert.AreEqual(0, errors.Count())
    End Sub

    <TestMethod> Public Sub IntegrationPrecompile()
        Dim code = Razor.Precompile(Of LargeModel)(<xml>@Model.Foo</xml>.Value)
        Assert.IsTrue(code.Contains("return builder"))
    End Sub

    <TestMethod> Public Sub IntegrationGenerate()
        Dim html = Razor.Generate(<xml>@Model.Foo</xml>.Value, New LargeModel With {.Foo = 5})
        Assert.IsTrue(html.Contains("5"))
    End Sub
End Class