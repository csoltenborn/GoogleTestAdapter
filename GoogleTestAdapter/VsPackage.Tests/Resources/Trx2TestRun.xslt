<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt"
	xmlns:ms="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
  xmlns:regex="urn:my-scripts"
  exclude-result-prefixes="msxsl ms regex">
  <xsl:output method="xml" indent="yes" />
  <msxsl:script language="C#" implements-prefix="regex">
    <msxsl:assembly name="System.Text.RegularExpressions" />
    <msxsl:assembly name="System.IO" />
    <msxsl:using namespace="System.Text.RegularExpressions" />
    <msxsl:using namespace="System.IO" />
    <![CDATA[
        public string replace(string input, string pattern, string replacement)
        {
            string validFileCharsRegex = "[^" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]+";
            string validDirCharsRegex = "[^" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]+";
            pattern = pattern
                .Replace("_FILE_", validFileCharsRegex)
                .Replace("_DIR_", validDirCharsRegex);

            input = Regex.Replace(input, @"\\(Debug|Release)\\", @"\${ConfigurationName}\", RegexOptions.IgnoreCase);
            return Regex.Replace(input, pattern, replacement, RegexOptions.IgnoreCase);
        }
        
        public string replacePointer(string text)
        {
          return Regex.Replace(text, "([0-9A-F]{8}){1,2} pointing to", "${MemoryLocation} pointing to", RegexOptions.IgnoreCase);
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
        <xsl:value-of select="regex:replacePointer(@testName)" />
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

  <xsl:template match="ms:StackTrace">
    <StackTrace>
      <xsl:value-of select="regex:replace(., '[a-z]:\\+(?:_DIR_\\)*(sampletests\\(?:_DIR_\\)*_FILE_:line \d+)', '$(Directory)\$1')" />
      <xsl:apply-templates />
    </StackTrace>
  </xsl:template>

  <xsl:template match="ms:Message">
    <Message>
      <xsl:value-of select="." />
    </Message>
  </xsl:template>

  <xsl:template match="ms:TestDefinitions">
    <TestDefinitions>
      <xsl:apply-templates />
    </TestDefinitions>
  </xsl:template>

  <xsl:template match="ms:UnitTest">
    <UnitTest>
      <xsl:attribute name="name">
        <xsl:value-of select="regex:replacePointer(@name)" />
      </xsl:attribute>
      <xsl:attribute name="storage">
        <xsl:value-of select="regex:replace(@storage, '(?:[a-z]:\\+)?(?:_DIR_\\)*(sampletests\\(?:_DIR_\\)*_FILE_)', '$(Directory)\$1')" />
      </xsl:attribute>
      <xsl:apply-templates />
    </UnitTest>
  </xsl:template>

  <xsl:template match="ms:TestMethod">
    <TestMethod>
      <xsl:attribute name="name">
        <xsl:value-of select="regex:replacePointer(@name)" />
      </xsl:attribute>
      <xsl:attribute name="className">
        <xsl:value-of select="@className" />
      </xsl:attribute>
      <xsl:attribute name="adapterTypeName">
        <xsl:value-of select="@adapterTypeName" />
      </xsl:attribute>
      <xsl:attribute name="codeBase">
        <xsl:value-of select="regex:replace(@codeBase, '(?:[a-z]:\\+)?(?:_DIR_\\)*(sampletests\\(?:_DIR_\\)*_FILE_)', '$(Directory)\$1')" />
      </xsl:attribute>
    </TestMethod>
  </xsl:template>

  <xsl:template match="ms:TestLists">
    <TestLists>
      <xsl:apply-templates />
    </TestLists>
  </xsl:template>

  <xsl:template match="ms:TestList">
    <TestList>
      <xsl:attribute name="name">
        <xsl:value-of select="@name" />
      </xsl:attribute>
    </TestList>
  </xsl:template>

<!--
  <xsl:template match="ms:RunInfos">
    <RunInfos>
      <xsl:apply-templates />
    </RunInfos>
  </xsl:template>

  <xsl:template match="ms:RunInfo">
    <RunInfo>
      <xsl:attribute name="outcome">
        <xsl:value-of select="@outcome" />
      </xsl:attribute>
      <xsl:apply-templates />
    </RunInfo>
  </xsl:template>

  <xsl:template match="ms:Text">
    <Text>
      <xsl:value-of select="." />
    </Text>
  </xsl:template>
-->

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