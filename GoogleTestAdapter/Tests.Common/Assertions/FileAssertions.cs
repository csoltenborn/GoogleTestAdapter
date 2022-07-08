using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace GoogleTestAdapter.Tests.Common.Assertions
{
    public static class FileAssertionsExtensions
    {
        public static FileAssertions Should(this FileInfo file)
        {
            return new FileAssertions(file);
        }

        public static FileInfo AsFileInfo(this string path)
        {
            return new FileInfo(path);
        }

        public class FileAssertions : ReferenceTypeAssertions<FileInfo, FileAssertions>
        {
            private const string ReasonTag = "{reason}";

            protected override string Identifier { get; } = "files";

            public FileAssertions(FileInfo file) : base(file)
            {
            }

            public AndConstraint<FileAssertions> Exist(string because = "", params object[] becauseArgs)
            {
                string message = $"Expected '{Subject.FullName}' to exist{ReasonTag}, but it does not.{Environment.NewLine}";
                message += GetBaseDirInfos();

                Execute.Assertion
                    .ForCondition(Subject.Exists)
                    .BecauseOf(because, becauseArgs)
                    .FailWith(message);
                return new AndConstraint<FileAssertions>(this);
            }

            public AndConstraint<FileAssertions> NotExist(string because = "", params object[] becauseArgs)
            {
                string message = $"Expected '{Subject.FullName}' to not exist{ReasonTag}, but it does.{Environment.NewLine}";
                message += GetBaseDirInfos();

                Execute.Assertion
                    .ForCondition(!Subject.Exists)
                    .BecauseOf(because, becauseArgs)
                    .FailWith(message);
                return new AndConstraint<FileAssertions>(this);
            }

            private string GetBaseDirInfos()
            {
                string dir = Subject.Directory?.FullName;
                if (dir == null || !Directory.Exists(dir))
                    return $"Base dir '{Path.GetDirectoryName(Subject.FullName)}' does not exist.";

                string details = $"Content of base dir '{dir}':{Environment.NewLine}";
                details += string.Join(Environment.NewLine, Directory.GetFiles(dir).Select(Path.GetFileName).OrderBy(f => f));
                return details;
            }

        }

    }

}