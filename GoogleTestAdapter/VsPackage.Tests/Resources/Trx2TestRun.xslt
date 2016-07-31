<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt"
	xmlns:ms="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
  xmlns:regex="urn:my-scripts"
  exclude-result-prefixes="msxsl ms regex">
  <xsl:output method="xml" indent="yes" />
  <msxsl:script language="C#" implements-prefix="regex">
    <msxsl:assembly name="System.Text.RegularExpressions" />
    <msxsl:using namespace="System.Text.RegularExpressions" />
    <![CDATA[
      public string replace(string input, string pattern, string replacement)
      {
        return Regex.Replace(input, pattern, replacement);
      }
    ]]>
  </msxsl:script>

  <xsl:template match="ms:TestRun">
    <TestRun>
      <xsl:apply-templates />
    </TestRun>
  </xsl:template>

  <xsl:template match="ms:Results">
    <Results>
      <xsl:apply-templates />
    </Results>
  </xsl:template>

  <xsl:template match="ms:UnitTestResult">
    <UnitTestResult>
      <xsl:attribute name="testName">
        <xsl:value-of select="regex:replace(@testName, '([0-9A-F]{8}){1,2} pointing to', '${MemoryLocation} pointing to')" />
      </xsl:attribute>
      <xsl:attribute name="outcome">
        <xsl:value-of select="@outcome" />
      </xsl:attribute>
      <xsl:apply-templates />
    </UnitTestResult>
  </xsl:template>

  <xsl:template match="ms:Output">
    <Output>
      <xsl:apply-templates />
    </Output>
  </xsl:template>

  <xsl:template match="ms:ErrorInfo">
    <ErrorInfo>
      <xsl:apply-templates />
    </ErrorInfo>
  </xsl:template>

  <xsl:template match="ms:Message">
    <Message>
      <xsl:value-of select="." />
    </Message>
  </xsl:template>

  <xsl:template match="ms:ResultSummary">
    <ResultSummary>
      <xsl:attribute name="outcome">
        <xsl:value-of select="@outcome" />
      </xsl:attribute>
      <xsl:apply-templates />
    </ResultSummary>
  </xsl:template>

  <xsl:template match="ms:Counters">
    <Counters>
      <xsl:attribute name="total">
        <xsl:value-of select="@total" />
      </xsl:attribute>
      <xsl:attribute name="executed">
        <xsl:value-of select="@executed" />
      </xsl:attribute>
      <xsl:attribute name="passed">
        <xsl:value-of select="@passed" />
      </xsl:attribute>
      <xsl:attribute name="failed">
        <xsl:value-of select="@failed" />
      </xsl:attribute>
      <xsl:attribute name="error">
        <xsl:value-of select="@error" />
      </xsl:attribute>
      <xsl:attribute name="timeout">
        <xsl:value-of select="@timeout" />
      </xsl:attribute>
      <xsl:attribute name="aborted">
        <xsl:value-of select="@aborted" />
      </xsl:attribute>
      <xsl:attribute name="inconclusive">
        <xsl:value-of select="@inconclusive" />
      </xsl:attribute>
      <xsl:attribute name="passedButRunAborted">
        <xsl:value-of select="@passedButRunAborted" />
      </xsl:attribute>
      <xsl:attribute name="notRunnable">
        <xsl:value-of select="@notRunnable" />
      </xsl:attribute>
      <xsl:attribute name="notExecuted">
        <xsl:value-of select="@notExecuted" />
      </xsl:attribute>
      <xsl:attribute name="disconnected">
        <xsl:value-of select="@disconnected" />
      </xsl:attribute>
      <xsl:attribute name="warning">
        <xsl:value-of select="@warning" />
      </xsl:attribute>
      <xsl:attribute name="completed">
        <xsl:value-of select="@completed" />
      </xsl:attribute>
      <xsl:attribute name="inProgress">
        <xsl:value-of select="@inProgress" />
      </xsl:attribute>
      <xsl:attribute name="pending">
        <xsl:value-of select="@pending" />
      </xsl:attribute>
    </Counters>
  </xsl:template>


  <xsl:template match="text()|@*" />

</xsl:stylesheet>