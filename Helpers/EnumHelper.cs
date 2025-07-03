using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.Reflection;

namespace MRIV.Helpers
{
    public class EnumHelper
    {
        public static SelectList GetEnumDescriptionSelectList<TEnum>() where TEnum : struct
        {
            var values = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = GetEnumDescription(e)
                });

            return new SelectList(values, "Value", "Text");
        }

        public static string GetEnumDescription<TEnum>(TEnum value)
        {
            if (value == null) return string.Empty; // Handle null

            return value.GetType()
                       .GetMember(value.ToString())
                       .FirstOrDefault()
                       ?.GetCustomAttribute<DescriptionAttribute>()
                       ?.Description
                    ?? value.ToString();
        }

    }
}
