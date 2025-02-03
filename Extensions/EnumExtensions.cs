using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;

namespace MRIV.Extensions
{
    public static class EnumExtensions
    {
        public static SelectList ToSelectListWithDescriptions<TEnum>(this TEnum enumObj)
            where TEnum : struct, IConvertible
        {
            var values = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Select(e => new SelectListItem
                {
                    Value = e.ToString(),
                    Text = e.GetDescription(),
                });

            return new SelectList(values, "Value", "Text");
        }

        public static string GetDescription<TEnum>(this TEnum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attributes = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                as DescriptionAttribute[];
            return attributes?.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }
}
