// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Relational.Utilities;

namespace Microsoft.Data.Entity.Relational.Model
{
    public class RelationalTypeMapper
    {
        // This dictionary is for invariant mappings from a sealed CLR type to a single
        // store type. If the CLR type is unsealed or if the mapping varies based on how the
        // type is used (e.g. in keys), then add custom mapping below.
        // TODO: Linear lookup is probably faster than dictionary
        private readonly IDictionary<Type, RelationalTypeMapping> _simpleMappings = new Dictionary<Type, RelationalTypeMapping>()
            {
                { typeof(int), new RelationalTypeMapping("integer", DbType.Int32) },
                { typeof(DateTime), new RelationalTypeMapping("timestamp", DbType.DateTime) },
                { typeof(bool), new RelationalTypeMapping("boolean", DbType.Boolean) },
                { typeof(double), new RelationalTypeMapping("double precision", DbType.Double) },
                { typeof(short), new RelationalTypeMapping("smallint", DbType.Int16) },
                { typeof(long), new RelationalTypeMapping("bigint", DbType.Int64) },
                { typeof(float), new RelationalTypeMapping("real", DbType.Single) },
                { typeof(DateTimeOffset), new RelationalTypeMapping("timestamp with time zone", DbType.DateTimeOffset) },
            };

        // TODO: What is the best size value for the base provider mapping?
        private readonly RelationalTypeMapping _nonKeyStringMapping
            = new RelationalSizedTypeMapping("varchar(4000)", DbType.AnsiString, 4000);

        // TODO: What is the best size value for the base provider mapping?
        private readonly RelationalTypeMapping _keyStringMapping
            = new RelationalSizedTypeMapping("varchar(128)", DbType.AnsiString, 128);

        private readonly RelationalTypeMapping _rowVersionMapping
            = new RelationalSizedTypeMapping("rowversion", DbType.Binary, 8);

        private readonly RelationalDecimalTypeMapping _decimalMapping = new RelationalDecimalTypeMapping(18, 2);

        // TODO: It would be nice to just pass IProperty into this method, but Migrations uses its own
        // store model for which there is no easy way to get an IProperty.
        public virtual RelationalTypeMapping GetTypeMapping(
            [CanBeNull] string specifiedType,
            [NotNull] string storageName,
            [NotNull] Type propertyType,
            bool isKey,
            bool isConcurrencyToken)
        {
            Check.NotNull(storageName, "storageName");
            Check.NotNull(propertyType, "propertyType");

            // TODO: if specifiedType is non-null then parse it to create a type mapping
            // TODO: Consider allowing Code First to specify an actual type mapping instead of just the string
            // type since that would remove the need to parse the string.

            RelationalTypeMapping mapping;
            if (_simpleMappings.TryGetValue(propertyType, out mapping))
            {
                return mapping;
            }

            if (propertyType == typeof(decimal))
            {
                // TODO: If scale/precision have been configured for the property, then create parameter appropriately
                return _decimalMapping;
            }

            if (propertyType == typeof(string))
            {
                if (isKey)
                {
                    return _keyStringMapping;
                }
                return _nonKeyStringMapping;
            }

            if (propertyType == typeof(byte[]) && isConcurrencyToken)
            {
                return _rowVersionMapping;
            }

            // TODO: Consider TimeSpan mapping

            throw new NotSupportedException(Strings.FormatUnsupportedType(storageName, propertyType.Name));
        }
    }
}