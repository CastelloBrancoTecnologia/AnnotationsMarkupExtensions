﻿using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Input;
using System.ComponentModel;

namespace CastelloBranco.AnnotationsMarkupExtensions;

public class AnnotateExtension : MarkupExtension
{
    public string PropertyName { get; set; }
    public MarkupAnnotationEnum Annotation { get; set; }

    public AnnotateExtension(string propertyName, MarkupAnnotationEnum annotation)
    {
        PropertyName = propertyName;
        Annotation = annotation;
    }

    protected override object? ProvideValue(IXamlServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget provideValueTarget)
        {
            if (provideValueTarget.TargetObject is FrameworkElement targetElement)
            {
                var dataContext = targetElement.DataContext;

                if (dataContext != null)
                {
                    Type type = dataContext.GetType();

                    PropertyInfo prop = type.GetProperty(PropertyName) ?? throw new Exception($"Propriedade {PropertyName} nao encontrada");
                    
                    string fieldName = char.ToLower(PropertyName[0]) + PropertyName.[1..]);

                    FieldInfo? field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field == null)
                    {
                        field = type.GetField($"_{PropertyName}", BindingFlags.NonPublic | BindingFlags.Instance);
                    }

                    return ExtractAnnotation(prop, field);
                }
            }
        }

        return null;
    }

    private object? ExtractAnnotation(PropertyInfo prop, FieldInfo? field)
    {
        switch (Annotation)
        {
            case MarkupAnnotationEnum.DisplayName:
            case MarkupAnnotationEnum.Description:
            case MarkupAnnotationEnum.ShortDisplayName:
                var displayAttr = prop?.GetCustomAttributes<DisplayAttribute>(true).FirstOrDefault()
                                ?? field?.GetCustomAttributes<DisplayAttribute>(true).FirstOrDefault();

                return Annotation == MarkupAnnotationEnum.DisplayName ? displayAttr?.Name :
                       Annotation == MarkupAnnotationEnum.ShortDisplayName ? displayAttr?.ShortName :
                                     displayAttr?.Description;

            case MarkupAnnotationEnum.Prompt:
                var placeholderAttr = prop?.GetCustomAttributes<DisplayAttribute>(true).FirstOrDefault()
                                    ?? field?.GetCustomAttributes<DisplayAttribute>(true).FirstOrDefault();

                return placeholderAttr?.Prompt;

            case MarkupAnnotationEnum.MaxLength:
                return GetMaxLength(prop, field);

            case MarkupAnnotationEnum.MinLength:
                return GetMinLength(prop, field);

            case MarkupAnnotationEnum.MaxValue:
                var rangeAttrMax = prop?.GetCustomAttributes<RangeAttribute>(true).FirstOrDefault()
                                 ?? field?.GetCustomAttributes<RangeAttribute>(true).FirstOrDefault();

                return rangeAttrMax?.Maximum ?? double.MaxValue;

            case MarkupAnnotationEnum.MinValue:
                var rangeAttrMin = prop?.GetCustomAttributes<RangeAttribute>(true).FirstOrDefault()
                                 ?? field?.GetCustomAttributes<RangeAttribute>(true).FirstOrDefault();

                return rangeAttrMin?.Minimum ?? double.MinValue;

            case MarkupAnnotationEnum.DisplayFormat:
                var displayFormatAttr = prop?.GetCustomAttributes<DisplayFormatAttribute>(true).FirstOrDefault()
                                      ?? field?.GetCustomAttributes<DisplayFormatAttribute>(true).FirstOrDefault();

                return displayFormatAttr?.DataFormatString;

            case MarkupAnnotationEnum.CharacterCasing:
                bool isUpperCase = prop?.GetCustomAttributes(true).Any(x => x.GetType().Name.Contains("UpperCase", StringComparison.InvariantCultureIgnoreCase))
                                   ?? field?.GetCustomAttributes(true).Any(x => x.GetType().Name.Contains("UpperCase", StringComparison.InvariantCultureIgnoreCase)) ?? false;

                bool isLowerCase =     prop?.GetCustomAttributes(true).Any(x => x.GetType().Name.Contains("LowerCase", StringComparison.InvariantCultureIgnoreCase))
                                   ?? field?.GetCustomAttributes(true).Any(x => x.GetType().Name.Contains("LowerCase", StringComparison.InvariantCultureIgnoreCase)) ?? false;

                if (isUpperCase) return CharacterCasing.Upper;
                if (isLowerCase) return CharacterCasing.Lower;
                return CharacterCasing.Normal;

            case MarkupAnnotationEnum.IsReadOnly:
                var readOnlyAttr = prop?.GetCustomAttributes<ReadOnlyAttribute>(true).FirstOrDefault()
                                 ?? field?.GetCustomAttributes<ReadOnlyAttribute>(true).FirstOrDefault();
                return readOnlyAttr?.IsReadOnly ?? false;

            case MarkupAnnotationEnum.BestInputScope:
                    var dataTypeAttr = prop?.GetCustomAttributes<DataTypeAttribute>(true).FirstOrDefault()
                                       ?? field?.GetCustomAttributes<DataTypeAttribute>(true).FirstOrDefault();

                    var dfAttr = prop?.GetCustomAttributes<DisplayFormatAttribute>(true).FirstOrDefault()
                                 ?? field?.GetCustomAttributes<DisplayFormatAttribute>(true).FirstOrDefault();

                    return GetBestInputScope(prop, dataTypeAttr, dfAttr?.DataFormatString);

            default:
                return null;
        }
    }

    private int GetMaxLength(PropertyInfo prop, FieldInfo? field)
    {
        var stringLengthAttr = prop?.GetCustomAttributes<StringLengthAttribute>(true).FirstOrDefault()
                            ?? field?.GetCustomAttributes<StringLengthAttribute>(true).FirstOrDefault();

        var maxLengthAttr = prop?.GetCustomAttributes<MaxLengthAttribute>(true).FirstOrDefault()
                          ?? field?.GetCustomAttributes<MaxLengthAttribute>(true).FirstOrDefault();

        var lengthAttr = prop?.GetCustomAttributes<LengthAttribute>(true).FirstOrDefault()
                  ?? field?.GetCustomAttributes<LengthAttribute>(true).FirstOrDefault();

        return (int) (stringLengthAttr?.MaximumLength ?? 
                      maxLengthAttr?.Length ?? 
                      lengthAttr?.MaximumLength ?? 
                      0);
    }

    private int GetMinLength(PropertyInfo prop, FieldInfo? field)
    {
        var stringLengthAttr = prop?.GetCustomAttributes<StringLengthAttribute>(true).FirstOrDefault()
                            ?? field?.GetCustomAttributes<StringLengthAttribute>(true).FirstOrDefault();

        var minLengthAttr = prop?.GetCustomAttributes<MinLengthAttribute>(true).FirstOrDefault()
                          ?? field?.GetCustomAttributes<MinLengthAttribute>(true).FirstOrDefault();

        var lengthAttr = prop?.GetCustomAttributes<LengthAttribute>(true).FirstOrDefault()
                          ?? field?.GetCustomAttributes<LengthAttribute>(true).FirstOrDefault();

        return  (int) (stringLengthAttr?.MinimumLength ?? 
                       minLengthAttr?.Length ?? 
                       lengthAttr?.MinimumLength ??
                       0);
    }

    private static Dictionary<Type, InputScopeNameValue> InputScopesOfValidators { get; } = new Dictionary<Type, InputScopeNameValue>
    {
        { typeof(EmailAddressAttribute), InputScopeNameValue.EmailNameOrAddress },
        { typeof(UrlAttribute), InputScopeNameValue.Url },
        { typeof(CreditCardAttribute), InputScopeNameValue.NumberFullWidth },
        { typeof(PhoneAttribute), InputScopeNameValue.NumberFullWidth },
        // { typeof(FullNameAttribute), InputScopeNameValue.PersonalFullName },
    };

    private static Dictionary<DataType, InputScopeNameValue> InputScopesOfDataTypes { get; } = new Dictionary<DataType, InputScopeNameValue>
    {
        { DataType.CreditCard, InputScopeNameValue.NumberFullWidth },
        { DataType.Currency, InputScopeNameValue.CurrencyAmount },
        { DataType.Date, InputScopeNameValue.NumberFullWidth },
        { DataType.DateTime, InputScopeNameValue.NumberFullWidth },
        { DataType.Duration, InputScopeNameValue.NumberFullWidth },
        { DataType.EmailAddress, InputScopeNameValue.EmailNameOrAddress },
        { DataType.Html, InputScopeNameValue.Text },
        { DataType.ImageUrl, InputScopeNameValue.Url },
        { DataType.MultilineText, InputScopeNameValue.Text },
        { DataType.Password, InputScopeNameValue.Password },
        { DataType.PhoneNumber, InputScopeNameValue.NumberFullWidth },
        { DataType.PostalCode, InputScopeNameValue.NumberFullWidth },
        { DataType.Text, InputScopeNameValue.Text },
        { DataType.Time, InputScopeNameValue.NumberFullWidth },
        { DataType.Upload, InputScopeNameValue.Url },
        { DataType.Url, InputScopeNameValue.Url }
    };

    private static InputScope GetBestInputScope(PropertyInfo? prop, DataTypeAttribute? dataTypeAttr, string? dataFormat)
    {
        InputScope scope = new InputScope();

        if (dataTypeAttr != null && InputScopesOfDataTypes.TryGetValue(dataTypeAttr.DataType, out var inputScopeValue))
        {
            scope.Names.Add(new InputScopeName { NameValue = inputScopeValue });

            return scope;
        }

        if (prop != null)
        {
            foreach (var validatorType in InputScopesOfValidators.Keys)
            {
                if (prop.GetCustomAttributes(validatorType, true).Any())
                {
                    scope.Names.Add(new InputScopeName { NameValue = InputScopesOfValidators[validatorType] });

                    return scope;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(dataFormat))
        {
            bool containsDigits = dataFormat.Any(char.IsDigit);
            bool containsAlpha = dataFormat.Any(char.IsLetter);

            scope.Names.Add(new InputScopeName
            {
                NameValue = containsAlpha ? InputScopeNameValue.Default : InputScopeNameValue.CurrencyAmount
            });

            return scope;
        }

        scope.Names.Add(new InputScopeName { NameValue = InputScopeNameValue.Default });

        return scope;
    }
}

public enum MarkupAnnotationEnum
{
    DisplayName,
    ShortDisplayName,
    Description,
    MaxLength,
    MinLength,
    MaxValue,
    MinValue,
    Prompt,
    DisplayFormat,
    IsReadOnly,
    CharacterCasing,
    BestInputScope,
}

