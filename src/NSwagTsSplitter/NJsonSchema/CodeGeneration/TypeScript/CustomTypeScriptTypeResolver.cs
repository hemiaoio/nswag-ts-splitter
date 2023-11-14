
using System.Linq;
using System;
using System.Runtime;
using NSwagTsSplitter.Contants;

// ReSharper disable once CheckNamespace
namespace NJsonSchema.CodeGeneration.TypeScript;

public class CustomTypeScriptTypeResolver : TypeScriptTypeResolver
{
    private const string UnionPipe = " | ";

    /// <summary>Initializes a new instance of the <see cref="TypeScriptTypeResolver" /> class.</summary>
    /// <param name="settings">The settings.</param>
    public CustomTypeScriptTypeResolver(TypeScriptGeneratorSettings settings)
        : base(settings)
    {
    }

    /// <summary>Resolves and possibly generates the specified schema. Returns the type name with a 'I' prefix if the feature is supported for the given schema.</summary>
    /// <param name="schema">The schema.</param>
    /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
    /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
    /// <returns>The type name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
    public new string ResolveConstructorInterfaceName(JsonSchema schema, bool isNullable, string typeNameHint)
    {
        return Resolve(schema, typeNameHint, true);
    }

    /// <summary>Resolves and possibly generates the specified schema.</summary>
    /// <param name="schema">The schema.</param>
    /// <param name="isNullable">Specifies whether the given type usage is nullable.</param>
    /// <param name="typeNameHint">The type name hint to use when generating the type and the type name is missing.</param>
    /// <returns>The type name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null" />.</exception>
    public override string Resolve(JsonSchema schema, bool isNullable, string typeNameHint)
    {
        return Resolve(schema, typeNameHint, false);
    }

    /// <summary>Gets a value indicating whether the schema supports constructor conversion.</summary>
    /// <param name="schema">The schema.</param>
    /// <returns>The result.</returns>
    public bool CustomSupportsConstructorConversion(JsonSchema schema)
    {
        return schema?.ActualSchema.ResponsibleDiscriminatorObject == null;
    }

    /// <summary>Checks whether the given schema should generate a type.</summary>
    /// <param name="schema">The schema.</param>
    /// <returns>True if the schema should generate a type.</returns>
    protected override bool IsDefinitionTypeSchema(JsonSchema schema)
    {
        if (schema.IsDictionary && !Settings.InlineNamedDictionaries)
        {
            return true;
        }

        return base.IsDefinitionTypeSchema(schema);
    }

    public string ResolveDictionaryKeyType(JsonSchema schema, string fallbackType, bool addInterfacePrefix)
    {
        if (schema.DictionaryKey != null)
        {
            var type = Resolve(schema.DictionaryKey, schema.DictionaryKey.ActualSchema.IsNullable(Settings.SchemaType), null);
            if (addInterfacePrefix && !Constant.TsBaseType.Contains(type) && !schema.DictionaryKey.ActualSchema.IsEnumeration)
            {
                return $"I{type}";
            }

            return type;
        }

        return fallbackType;
    }

    public string ResolveDictionaryValueType(JsonSchema schema, string fallbackType, bool addInterfacePrefix)
    {
        if (schema.AdditionalPropertiesSchema != null)
        {
            var type = Resolve(schema.AdditionalPropertiesSchema, schema.AdditionalPropertiesSchema.ActualSchema.IsNullable(Settings.SchemaType), null);
            if (addInterfacePrefix && !Constant.TsBaseType.Contains(type) && !schema.AdditionalPropertiesSchema.IsEnumeration)
            {
                return $"I{type}";
            }

            return type;
        }

        if (schema.AllowAdditionalProperties == false && schema.PatternProperties.Any())
        {
            var valueTypes = schema.PatternProperties
                .Select(p => Resolve(p.Value, p.Value.IsNullable(Settings.SchemaType), null))
                .Distinct()
                .ToList();

            if (valueTypes.Count == 1)
            {
                var type = valueTypes.First();
                if (addInterfacePrefix && !Constant.TsBaseType.Contains(type))
                {
                    return $"I{type}";
                }

                return type;
            }
        }

        return fallbackType;
    }

    private string Resolve(JsonSchema schema, string typeNameHint, bool addInterfacePrefix)
    {
        if (schema == null)
        {
            throw new ArgumentNullException(nameof(schema));
        }

        schema = GetResolvableSchema(schema);

        // Primitive schemas (no new type)

        if (schema.ActualTypeSchema.IsAnyType &&
            schema.InheritedSchema == null && // not in inheritance hierarchy
            schema.AllOf.Count == 0 &&
            !Types.ContainsKey(schema) &&
            !schema.HasReference)
        {
            return "any";
        }

        var type = schema.ActualTypeSchema.Type;
        if (type == JsonObjectType.None && schema.ActualTypeSchema.IsEnumeration)
        {
            type = schema.ActualTypeSchema.Enumeration.All(v => v is int) ?
                JsonObjectType.Integer :
                JsonObjectType.String;
        }

        if (type.IsNumber())
        {
            return "number";
        }

        if (type.IsInteger() && !schema.ActualTypeSchema.IsEnumeration)
        {
            return ResolveInteger(schema.ActualTypeSchema, typeNameHint);
        }

        if (type.IsBoolean())
        {
            return "boolean";
        }

        if (type.IsString() && !schema.ActualTypeSchema.IsEnumeration)
        {
            return ResolveString(schema.ActualTypeSchema, typeNameHint);
        }

        if (schema.IsBinary)
        {
            return "any";
        }

        // Type generating schemas

        if (schema.ActualTypeSchema.IsEnumeration)
        {
            return GetOrGenerateTypeName(schema, typeNameHint);
        }

        if (schema.Type.IsArray())
        {
            return ResolveArrayOrTuple(schema, typeNameHint, addInterfacePrefix, false);
        }

        if (schema.IsDictionary)
        {
            var valueType = ResolveDictionaryValueType(schema, "any", addInterfacePrefix);
            var defaultType = "string";
            var resolvedType = ResolveDictionaryKeyType(schema, defaultType, addInterfacePrefix);
            if (resolvedType != defaultType)
            {
                var keyType = Settings.TypeScriptVersion >= 2.1m ? resolvedType : defaultType;
                if (keyType != defaultType && schema.DictionaryKey.ActualTypeSchema.IsEnumeration)
                {
                    if (Settings.EnumStyle == TypeScriptEnumStyle.Enum)
                    {
                        return $"{{ [key in keyof typeof {keyType}]?: {valueType}; }}";
                    }
                    else if (Settings.EnumStyle == TypeScriptEnumStyle.StringLiteral)
                    {
                        return $"{{ [key in {keyType}]?: {valueType}; }}";
                    }

                    throw new ArgumentOutOfRangeException(nameof(Settings.EnumStyle), Settings.EnumStyle, "Unknown enum style");
                }

                return $"{{ [key: {keyType}]: {valueType}; }}";
            }

            return $"{{ [key: {resolvedType}]: {valueType}; }}";
        }

        if (Settings.UseLeafType &&
            schema.DiscriminatorObject == null &&
            schema.ActualTypeSchema.DiscriminatorObject != null)
        {
            var types = schema.ActualTypeSchema.ActualDiscriminatorObject.Mapping
                .Select(m => Resolve(
                    m.Value,
                    typeNameHint,
                    addInterfacePrefix
                ));

            return string.Join(UnionPipe, types);
        }

        return (addInterfacePrefix && !schema.ActualTypeSchema.IsEnumeration && CustomSupportsConstructorConversion(schema) ? "I" : "") +
            GetOrGenerateTypeName(schema, typeNameHint);
    }

    private string ResolveString(JsonSchema schema, string typeNameHint)
    {
        // TODO: Make this more generic (see DataConversionGenerator.IsDate)
        if (Settings.DateTimeType == TypeScriptDateTimeType.Date)
        {
            if (schema.Format == JsonFormatStrings.Date)
            {
                return "Date";
            }

            if (schema.Format == JsonFormatStrings.DateTime)
            {
                return "Date";
            }

            if (schema.Format == JsonFormatStrings.Time)
            {
                return "string";
            }

            if (schema.Format is JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan)
            {
                return "string";
            }
        }
        else if (Settings.DateTimeType == TypeScriptDateTimeType.MomentJS ||
                 Settings.DateTimeType == TypeScriptDateTimeType.OffsetMomentJS)
        {
            if (schema.Format == JsonFormatStrings.Date)
            {
                return "moment.Moment";
            }

            if (schema.Format == JsonFormatStrings.DateTime)
            {
                return "moment.Moment";
            }

            if (schema.Format == JsonFormatStrings.Time)
            {
                return "moment.Moment";
            }

            if (schema.Format is JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan)
            {
                return "moment.Duration";
            }
        }
        else if (Settings.DateTimeType == TypeScriptDateTimeType.Luxon)
        {
            if (schema.Format == JsonFormatStrings.Date)
            {
                return "DateTime";
            }

            if (schema.Format == JsonFormatStrings.DateTime)
            {
                return "DateTime";
            }

            if (schema.Format == JsonFormatStrings.Time)
            {
                return "DateTime";
            }

            if (schema.Format is JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan)
            {
                return "Duration";
            }
        }
        else if (Settings.DateTimeType == TypeScriptDateTimeType.DayJS)
        {
            if (schema.Format == JsonFormatStrings.Date)
            {
                return "dayjs.Dayjs";
            }

            if (schema.Format == JsonFormatStrings.DateTime)
            {
                return "dayjs.Dayjs";
            }

            if (schema.Format == JsonFormatStrings.Time)
            {
                return "dayjs.Dayjs";
            }

            if (schema.Format is JsonFormatStrings.Duration or JsonFormatStrings.TimeSpan)
            {
                return "dayjs.Dayjs";
            }
        }

        return "string";
    }

    private string ResolveInteger(JsonSchema schema, string typeNameHint)
    {
        return "number";
    }

    public string ResolveArrayOrTuple(JsonSchema schema, string typeNameHint, bool addInterfacePrefix, bool onlyType)
    {
        if (schema.Item != null)
        {
            var isObject = schema.Item?.ActualSchema.Type.IsObject() == true;
            var isDictionary = schema.Item?.ActualSchema.IsDictionary == true;
            var prefix = addInterfacePrefix && CustomSupportsConstructorConversion(schema.Item) && isObject && !isDictionary ? "I" : "";

            if (Settings.UseLeafType)
            {
                var itemTypes = Resolve(schema.Item, true, typeNameHint) // TODO: Make typeNameHint singular if possible
                    .Split(new[] { UnionPipe }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => GetNullableItemType(schema, prefix + x))
                    .ToList();

                var itemType = string.Join(UnionPipe, itemTypes);
                if (onlyType)
                {
                    return itemType;
                }
                // is TypeUnion
                if (itemTypes.Count > 1)
                {
                    itemType = string.Format("({0})", itemType);
                }

                return string.Format("{0}[]", itemType);
            }
            else
            {
                var itemType = prefix + Resolve(schema.Item, true, typeNameHint);
                var nullableTypeName = GetNullableItemType(schema, itemType);
                if (onlyType)
                {
                    return nullableTypeName;
                }
                return string.Format("{0}[]", nullableTypeName); // TODO: Make typeNameHint singular if possible
            }
        }

        if (schema.Items != null && schema.Items.Count > 0)
        {
            var tupleTypes = schema.Items
                .Select(s => GetNullableItemType(s, Resolve(s, false, null)))
                .ToArray();
            if (onlyType)
            {
                return string.Join(",", tupleTypes);
            }
            return string.Format("[" + string.Join(", ", tupleTypes) + "]");
        }

        if (onlyType)
        {
            return "any";
        }

        return "any[]";
    }

    private string GetNullableItemType(JsonSchema schema, string itemType)
    {
        if (Settings.SupportsStrictNullChecks && schema.Item.IsNullable(Settings.SchemaType))
        {
            return string.Format("({0} | {1})", itemType, Settings.NullValue.ToString().ToLowerInvariant());
        }

        return itemType;
    }

}