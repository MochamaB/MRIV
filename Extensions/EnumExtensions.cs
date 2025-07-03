using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Enums;
using MRIV.Models;
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

        public static FunctionalStatus GetFunctionalStatus(this Condition condition)
        {
            return condition switch
            {
                Condition.GoodCondition => FunctionalStatus.FullyFunctional,
                Condition.MinorDamage => FunctionalStatus.FullyFunctional,
                Condition.MajorDamage => FunctionalStatus.PartiallyFunctional,
                Condition.Faulty => FunctionalStatus.PartiallyFunctional,
                Condition.UnderMaintenance => FunctionalStatus.NonFunctional,
                Condition.LostOrStolen => FunctionalStatus.NonFunctional,
                Condition.Disposed => FunctionalStatus.NonFunctional,
                _ => FunctionalStatus.FullyFunctional
            };
        }
        public static CosmeticStatus GetCosmeticStatus(this Condition condition)
        {
            return condition switch
            {
                Condition.GoodCondition => CosmeticStatus.Excellent,
                Condition.MinorDamage => CosmeticStatus.Good,
                Condition.MajorDamage => CosmeticStatus.Fair,
                Condition.Faulty => CosmeticStatus.Fair,
                Condition.UnderMaintenance => CosmeticStatus.Poor,
                Condition.LostOrStolen => CosmeticStatus.Poor,
                Condition.Disposed => CosmeticStatus.Poor,
                _ => CosmeticStatus.Excellent
            };
        }
        public static SelectList GetFilteredRequisitionTypes(int ticketId)
        {
            if (ticketId == 0)
            {
                var interFactoryOnly = new[] { RequisitionType.InterFactory };
                return new SelectList(
                    interFactoryOnly.Select(rt => new SelectListItem
                    {
                        Value = ((int)rt).ToString(),
                        Text = rt.GetDescription()
                    }), "Value", "Text");
            }
            else
            {
                var allowedTypes = Enum.GetValues<RequisitionType>()
                    .Where(rt => rt != RequisitionType.InterFactory);

                return new SelectList(
                    allowedTypes.Select(rt => new SelectListItem
                    {
                        Value = ((int)rt).ToString(),
                        Text = rt.GetDescription()
                    }), "Value", "Text");
            }
        }
    }
}
