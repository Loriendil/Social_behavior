using System.Globalization;
using System.Windows.Controls;

namespace RelationshipSimulator;

/// <summary>
/// Отклоняет значение вне [Min..Max] ДО того, как оно попадёт в модель (ValidationStep=
/// ConvertedProposedValue — уже после парсинга строки в число, но до записи в источник
/// привязки). В отличие от кламп в ядре, здесь ошибочный ввод не искажается молча, а просто
/// не проходит — подсветка ячейки берёт на себя Validation.HasError (см. стиль
/// ValidatingTextBoxStyle в MainWindow.xaml).
/// </summary>
public sealed class RangeValidationRule : ValidationRule
{
    public double Min { get; set; } = double.MinValue;

    public double Max { get; set; } = double.MaxValue;

    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        double number;
        try
        {
            number = System.Convert.ToDouble(value, cultureInfo);
        }
        catch
        {
            return new ValidationResult(false, "Ожидалось число.");
        }

        if (number < Min || number > Max)
        {
            return new ValidationResult(false, $"Значение должно быть в диапазоне [{Min:0.##}..{Max:0.##}].");
        }

        return ValidationResult.ValidResult;
    }
}
