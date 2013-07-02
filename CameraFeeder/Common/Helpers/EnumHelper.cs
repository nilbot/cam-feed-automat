using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Feeder.Common.Helpers
{
    public static class EnumHelper
    {
        /// <summary>
        ///     Gets the description of a specific enum value.
        /// </summary>
        public static string Description(this Enum eValue)
        {
            var _nAttributes =
                eValue.GetType().GetField(eValue.ToString()).GetCustomAttributes(typeof (DescriptionAttribute), false);

            if (!_nAttributes.Any())
            {
                TextInfo _oTi = CultureInfo.CurrentCulture.TextInfo;
                return _oTi.ToTitleCase(_oTi.ToLower(eValue.ToString().Replace("_", " ")));
            }

            return ((DescriptionAttribute) _nAttributes.First()).Description;
        }

        /// <summary>
        ///     Returns an enumerable collection of all values and descriptions for an enum type.
        /// </summary>
        public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions<TEnum>()
            where TEnum : struct, IConvertible, IComparable, IFormattable
        {
            if (!typeof (TEnum).IsEnum)
                throw new ArgumentException("TEnum must be an Enumeration type");

            return
                Enum.GetValues(typeof (TEnum))
                    .Cast<Enum>()
                    .Select(e => new ValueDescription {Value = e, Description = e.Description()})
                    .ToList();
        }
    }

    public class ValueDescription
    {
        public Enum Value { get; set; }

        public string Description { get; set; }
    }
}
