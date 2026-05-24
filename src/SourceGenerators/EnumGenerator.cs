using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerators;


[Generator]
public class NativeEnumSourceGenerator : ISourceGenerator {
    public void Execute(GeneratorExecutionContext context) {
        StringBuilder sourceBuilder = new StringBuilder("namespace NativeGen;\n");

        foreach (var file in context.AdditionalFiles) {
            if (file.Path.EndsWith(".hpp")) {
                Generate(file.Path, sourceBuilder);
            }
        }

        context.AddSource("NativeEnumSourceGenerator", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }


    private void Generate(string file, StringBuilder builder) {
        const string ENUM_KEY = "enum class";
        string[] lines = File.ReadAllLines(file);
        bool insideEnum = false;

        foreach (var line in lines) {
            if (!insideEnum && line.Contains(ENUM_KEY)) {
                insideEnum = true;

                int start = line.IndexOf(ENUM_KEY) + ENUM_KEY.Length;
                int end = line.IndexOf("{");
                string name = line.Substring(start, end - start);

                builder.AppendLine($"public enum {name} : int {{");
            } else if (insideEnum && line.Contains("}")) {
                insideEnum = false;
                builder.AppendLine("}\n");
            } else if (insideEnum) {
                var pair = line.Split('=');
                if (pair.Length != 2) {
                    continue;
                }

                string name = pair[0].Trim();
                string value = pair[1].Replace(',', ' ').Trim();

                builder.AppendLine($"   {name} = {value},");
            }
        }
    }


    public void Initialize(GeneratorInitializationContext context) { }
}
