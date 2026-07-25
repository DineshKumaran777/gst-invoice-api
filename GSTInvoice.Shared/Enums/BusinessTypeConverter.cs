// =============================================================================
// Copyright © 2024 DK (Freelancer)
// All rights reserved.
//
// Product:     DK GST Billing Platform
// Company:     DK (Freelancer)
// Website:     www.dkgstbilling.com
// Email:       support@dkgstbilling.com
//
// NOTICE: All information contained herein is, and remains the property of
// DK (Freelancer). The intellectual and technical
// concepts contained herein are proprietary to DK (Freelancer)
// and may be covered by Indian and International Patents,
// patents in process, and are protected by trade secret or copyright law.
//
// Unauthorized copying, modification, distribution, or use of this software,
// via any medium, is strictly prohibited without the prior written permission
// of DK (Freelancer).
// =============================================================================
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GSTInvoice.Shared.Enums;

/// <summary>
/// Custom JSON converter for <see cref="BusinessType"/> that accepts both
/// string values (e.g., "Individual", "Company") and integer values (1, 2, 3, 4).
/// </summary>
public class BusinessTypeConverter : JsonConverter<BusinessType>
{
    public override BusinessType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("BusinessType value cannot be null or empty.");
            }

            // Try parsing as the enum name (case-insensitive)
            if (Enum.TryParse<BusinessType>(value, ignoreCase: true, out var result))
            {
                return result;
            }

            // Try parsing as a numeric string (e.g., "1")
            if (int.TryParse(value, out var numericValue) && Enum.IsDefined(typeof(BusinessType), numericValue))
            {
                return (BusinessType)numericValue;
            }

            throw new JsonException($"Value '{value}' is not a valid BusinessType. Valid values are: Individual (1), Company (2), LLP (3), Partnership (4).");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            var numericValue = reader.GetInt32();
            if (Enum.IsDefined(typeof(BusinessType), numericValue))
            {
                return (BusinessType)numericValue;
            }

            throw new JsonException($"Value {numericValue} is not a valid BusinessType. Valid values are: Individual (1), Company (2), LLP (3), Partnership (4).");
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing BusinessType.");
    }

    public override void Write(Utf8JsonWriter writer, BusinessType value, JsonSerializerOptions options)
    {
        // Serialize as integer to maintain backward compatibility
        writer.WriteNumberValue((int)value);
    }
}
