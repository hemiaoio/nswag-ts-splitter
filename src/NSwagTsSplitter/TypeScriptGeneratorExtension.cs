using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.TypeScript;
using NJsonSchema.CodeGeneration.TypeScript.Models;

namespace NSwagTsSplitter
{
    internal static class TypeScriptGeneratorExtension
    {
        internal static CodeArtifactCollection GenerateTypes(TypeScriptTypeResolver _resolver, TypeScriptExtensionCode extensionCode)
        {
            var collection = GenerateTypes(_resolver);
            var artifacts = collection.Artifacts.ToList();

            foreach (var artifact in collection.Artifacts)
            {
                if (extensionCode?.ExtensionClasses.ContainsKey(artifact.TypeName) == true)
                {
                    var classCode = artifact.Code;

                    var index = classCode.IndexOf("constructor(", StringComparison.Ordinal);
                    if (index != -1)
                    {
                        artifact.Code = classCode.Insert(index, extensionCode.GetExtensionClassBody(artifact.TypeName).Trim() + "\n\n    ");
                    }
                    else
                    {
                        index = classCode.IndexOf("class", StringComparison.Ordinal);
                        index = classCode.IndexOf("{", index, StringComparison.Ordinal) + 1;

                        artifact.Code = classCode.Insert(index, "\n    " + extensionCode.GetExtensionClassBody(artifact.TypeName).Trim() + "\n");
                    }
                }
            }

            if (artifacts.Any(r => r.Code.Contains("formatDate(")))
            {
                var template = _resolver.Settings.TemplateFactory.CreateTemplate("TypeScript", "File.FormatDate", null);
                artifacts.Add(new CodeArtifact("formatDate", CodeArtifactType.Function, CodeArtifactLanguage.CSharp, template));
            }

            if (_resolver.Settings.HandleReferences)
            {
                var template = _resolver.Settings.TemplateFactory.CreateTemplate("TypeScript", "File.ReferenceHandling", null);
                artifacts.Add(new CodeArtifact("jsonParse", CodeArtifactType.Function, CodeArtifactLanguage.CSharp, template));
            }

            return new CodeArtifactCollection(artifacts, extensionCode);
        }


        internal static CodeArtifactCollection GenerateTypes(TypeScriptTypeResolver _resolver)
        {
            var processedTypes = new List<string>();
            var types = new Dictionary<string, CodeArtifact>();
            while (_resolver.Types.Any(t => !processedTypes.Contains(t.Value)))
            {
                foreach (var pair in _resolver.Types.ToList())
                {
                    processedTypes.Add(pair.Value);
                    var result = GenerateType(_resolver, pair.Key, pair.Value);
                    types[result.TypeName] = result;
                }
            }

            var artifacts = types.Values.Where(p =>
                !_resolver.Settings.ExcludedTypeNames.Contains(p.TypeName));

            return new CodeArtifactCollection(artifacts, null);
        }

        internal static CodeArtifact GenerateType(TypeScriptTypeResolver resolver, JsonSchema4 schema, string typeNameHint)
        {

            var typeName = resolver.GetOrGenerateTypeName(schema, typeNameHint);

            if (schema.IsEnumeration)
            {
                var model = new EnumTemplateModel(typeName, schema, resolver.Settings);
                var template = resolver.Settings.TemplateFactory.CreateTemplate("TypeScript", "Enum", model);
                return new CodeArtifact(typeName, CodeArtifactType.Enum, CodeArtifactLanguage.TypeScript, template);
            }
            else
            {
                var model = new ClassTemplateModel(typeName, typeNameHint, resolver.Settings, resolver, schema, schema);
                var template = resolver.Settings.CreateTemplate(typeName, model);

                var type = resolver.Settings.TypeStyle == TypeScriptTypeStyle.Interface
                    ? CodeArtifactType.Interface
                    : CodeArtifactType.Class;

                var codeArtifact = new CodeArtifact(typeName, model.BaseClass, type, CodeArtifactLanguage.TypeScript, template);

                List<string> typeNames = new List<string>();
                StringBuilder builder = new StringBuilder();
                foreach (var property in model.Properties)
                {
                    var propertyType = property.Type.IndexOf("[") > 0 ? property.Type.Replace("[]", "") : property.Type;
                    if (!typeNames.Contains(propertyType) && !Constant.TsBaseType.Contains(propertyType) && !property.IsDictionary)
                    {
                        typeNames.Add(propertyType);
                    }
                    var propertyDictionaryItemType = property.DictionaryItemType.IndexOf("[") > 0 ? property.DictionaryItemType.Replace("[]", "") : property.DictionaryItemType;
                    if (!typeNames.Contains(propertyDictionaryItemType) && !Constant.TsBaseType.Contains(propertyDictionaryItemType))
                    {
                        typeNames.Add(propertyDictionaryItemType);
                    }
                    var propertyArrayItemType = property.ArrayItemType.IndexOf("[") > 0 ? property.ArrayItemType.Replace("[]", "") : property.ArrayItemType;
                    if (!typeNames.Contains(propertyArrayItemType) && !Constant.TsBaseType.Contains(propertyArrayItemType))
                    {
                        typeNames.Add(propertyArrayItemType);
                    }
                }
                typeNames = typeNames.Where(c => c != typeName).ToList();
                typeNames.ForEach(c => builder.AppendLine($"import {{{c}}} from './{c}';"));
                builder.AppendLine();

                codeArtifact.Code = builder.ToString() + codeArtifact.Code;
                return codeArtifact;
            }

        }
    }
}
