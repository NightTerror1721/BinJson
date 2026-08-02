#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Krampus.BinJson.SourceGenerators.Utilities
{
    /// <summary>
    /// Builder for generating C# code with proper indentation and scope management
    /// </summary>
    internal sealed class CodeBuilder
    {
        private readonly StringBuilder _builder;
        private readonly List<string> _usings;
        private int _indentLevel;
        private bool _needsIndent;

        public CodeBuilder()
        {
            _builder = new StringBuilder();
            _usings = new List<string>();
            _indentLevel = 0;
            _needsIndent = true;
        }

        /// <summary>
        /// Add a using directive
        /// </summary>
        public void AddUsing(string namespaceName)
        {
            if (!_usings.Contains(namespaceName))
                _usings.Add(namespaceName);
        }

        /// <summary>
        /// Write using directives at the top of the file
        /// </summary>
        public void WriteUsings()
        {
            foreach (var ns in _usings.OrderBy(u => u))
            {
                _builder.Append("using ").Append(ns).AppendLine(";");
            }

            if (_usings.Count > 0)
                _builder.AppendLine();
        }

        /// <summary>
        /// Write a line of code with current indentation
        /// </summary>
        public void AppendLine(string text)
        {
            if (_needsIndent && !string.IsNullOrWhiteSpace(text))
            {
                Indent();
                _needsIndent = false;
            }

            _builder.AppendLine(text);
            _needsIndent = true;
        }

        /// <summary>
        /// Write text without newline
        /// </summary>
        public void Append(string text)
        {
            if (_needsIndent && !string.IsNullOrWhiteSpace(text))
            {
                Indent();
                _needsIndent = false;
            }

            _builder.Append(text);
        }

        /// <summary>
        /// Write an empty line
        /// </summary>
        public void AppendLine()
        {
            _builder.AppendLine();
            _needsIndent = true;
        }

        /// <summary>
        /// Write current indentation
        /// </summary>
        private void Indent()
        {
            for (int i = 0; i < _indentLevel; i++)
            {
                _builder.Append("    ");
            }
        }

        /// <summary>
        /// Increase indentation level
        /// </summary>
        public void IncreaseIndent()
        {
            _indentLevel++;
        }

        /// <summary>
        /// Decrease indentation level
        /// </summary>
        public void DecreaseIndent()
        {
            if (_indentLevel > 0)
                _indentLevel--;
        }

        /// <summary>
        /// Write opening brace and increase indent
        /// </summary>
        public void OpenBrace()
        {
            AppendLine("{");
            IncreaseIndent();
        }

        /// <summary>
        /// Decrease indent and write closing brace
        /// </summary>
        public void CloseBrace()
        {
            DecreaseIndent();
            AppendLine("}");
        }

        /// <summary>
        /// Create a scope block (automatically adds braces and manages indentation)
        /// </summary>
        public IDisposable Scope(string? declaration = null)
        {
            if (declaration != null)
                AppendLine(declaration);

            OpenBrace();
            return new ScopeHelper(this);
        }

        /// <summary>
        /// Write a namespace declaration with scope
        /// </summary>
        public IDisposable Namespace(string namespaceName)
        {
            return Scope($"namespace {namespaceName}");
        }

        /// <summary>
        /// Write a class declaration with scope
        /// </summary>
        public IDisposable Class(string modifiers, string className, string? baseType = null)
        {
            var declaration = $"{modifiers} class {className}";
            if (baseType != null)
                declaration += $" : {baseType}";

            return Scope(declaration);
        }

        /// <summary>
        /// Write a method declaration with scope
        /// </summary>
        public IDisposable Method(string signature)
        {
            return Scope(signature);
        }

        /// <summary>
        /// Write a conditional block
        /// </summary>
        public IDisposable If(string condition)
        {
            return Scope($"if ({condition})");
        }

        /// <summary>
        /// Write an else block
        /// </summary>
        public IDisposable Else()
        {
            DecreaseIndent();
            AppendLine("}");
            AppendLine("else");
            OpenBrace();
            return new ScopeHelper(this);
        }

        /// <summary>
        /// Write a foreach block
        /// </summary>
        public IDisposable ForEach(string variable, string collection)
        {
            return Scope($"foreach ({variable} in {collection})");
        }

        /// <summary>
        /// Write a comment line
        /// </summary>
        public void Comment(string text)
        {
            AppendLine($"// {text}");
        }

        /// <summary>
        /// Write XML documentation comment
        /// </summary>
        public void XmlComment(string tag, string content)
        {
            AppendLine($"/// <{tag}>{content}</{tag}>");
        }

        /// <summary>
        /// Get the generated code
        /// </summary>
        public override string ToString()
        {
            return _builder.ToString();
        }

        /// <summary>
        /// Helper class for automatic scope management with using statements
        /// </summary>
        private sealed class ScopeHelper : IDisposable
        {
            private readonly CodeBuilder _builder;

            public ScopeHelper(CodeBuilder builder)
            {
                _builder = builder;
            }

            public void Dispose()
            {
                _builder.CloseBrace();
            }
        }
    }

    /// <summary>
    /// Extension methods for CodeBuilder
    /// </summary>
    internal static class CodeBuilderExtensions
    {
        /// <summary>
        /// Format a type name for code generation (simplify common types)
        /// </summary>
        public static string FormatTypeName(this CodeBuilder builder, string typeName)
        {
            // Already handled by TypeRegistry.GetSimplifiedTypeName, but can be extended here
            return typeName switch
            {
                "System.Boolean" => "bool",
                "System.Byte" => "byte",
                "System.SByte" => "sbyte",
                "System.Int16" => "short",
                "System.UInt16" => "ushort",
                "System.Int32" => "int",
                "System.UInt32" => "uint",
                "System.Int64" => "long",
                "System.UInt64" => "ulong",
                "System.Single" => "float",
                "System.Double" => "double",
                "System.Decimal" => "decimal",
                "System.String" => "string",
                "System.Char" => "char",
                "System.Object" => "object",
                _ => typeName
            };
        }

        /// <summary>
        /// Write multiple lines at once
        /// </summary>
        public static void AppendLines(this CodeBuilder builder, params string[] lines)
        {
            foreach (var line in lines)
            {
                builder.AppendLine(line);
            }
        }
    }
}
