using System.Windows;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Controls;

namespace CustomMediaPlayerUltimate.Elements;

public partial class NumberInputBox : UserControl
{
    private bool updateDisplayedValue = true;

    public float NumericValue
    {
        get { return (float)GetValue(NumericValueProperty); }
        set
        {
            SetValue(NumericValueProperty, value);
            if (updateDisplayedValue)
                UpdateValue(value);
        }
    }
    public static readonly DependencyProperty NumericValueProperty =
        DependencyProperty.Register("NumericValue", typeof(float), typeof(Control), new PropertyMetadata(0f));

    public float Minimum
    {
        get { return (float)GetValue(MinimumProperty); }
        set { SetValue(MinimumProperty, value); }
    }
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register("Minimum", typeof(float), typeof(Control), new PropertyMetadata(0f));

    public float Maximum
    {
        get { return (float)GetValue(MaximumProperty); }
        set { SetValue(MaximumProperty, value); }
    }
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(float), typeof(Control), new PropertyMetadata(100f));

    public NumberInputBoxNumberType NumberType
    {
        get { return (NumberInputBoxNumberType)GetValue(NumberTypeProperty); }
        set { SetValue(NumberTypeProperty, value); }
    }
    public static readonly DependencyProperty NumberTypeProperty =
        DependencyProperty.Register("NumberType", typeof(NumberInputBoxNumberType), typeof(Control), new PropertyMetadata(NumberInputBoxNumberType.Integer));

    public static readonly RoutedEvent FinishedEditingEvent = EventManager.RegisterRoutedEvent(
        "FinishedEditing", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TabItem));
    public event RoutedEventHandler FinishedEditing
    {
        add { AddHandler(FinishedEditingEvent, value); }
        remove { RemoveHandler(FinishedEditingEvent, value); }
    }

    public NumberInputBox()
    {
        InitializeComponent();

        UpdateValue(NumericValue);
    }

    private (bool Allowed, float Value) IsValueAllowed(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c < '0' || c > '9')
            {
                if (i == 0 && Minimum < 0 && c == '-')
                {
                    continue;
                }
                else if (NumberType == NumberInputBoxNumberType.Decimal && c == '.')
                {
                    continue;
                }
                return (false, 0f);
            }
        }

        if (value.Length == 1 && value[0] == '-') return (true, NumericValue);

        float val;
        if (!float.TryParse(value, NumberType switch
        {
            NumberInputBoxNumberType.Integer => NumberStyles.Integer,
            _ => NumberStyles.AllowDecimalPoint
        },
            CultureInfo.InvariantCulture.NumberFormat, out val)) return (false, 0f);
        if (value.StartsWith("-")) val = -val;
        if (val < Minimum || val > Maximum) return (false, 0f);

        return (true, val);
    }

    private void TextBoxPreviewInput(object sender, TextCompositionEventArgs e)
    {
        (bool valid, float val) = IsValueAllowed($"{InputTextBox.Text}{e.Text}");
        if (valid)
        {
            updateDisplayedValue = false;
            NumericValue = val;
        }
        e.Handled = !valid;
    }

    private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        (bool valid, float val) = IsValueAllowed((string)e.DataObject.GetData(typeof(string)));

        if (!valid)
        {
            e.CancelCommand();
        }

        updateDisplayedValue = false;
        NumericValue = val;
    }

    private void TextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        EditingEnded();
    }

    private void TextBoxKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            EditingEnded();
        }
    }

    private void TextBoxTooltipOpening(object sender, RoutedEventArgs e)
    {
        InputTextBoxTooltipLabel.Content = $"{Minimum} - {Maximum}";
    }

    private void PlusButtonClick(object sender, RoutedEventArgs e)
    {
        if (NumericValue + 1f > Maximum)
        {
            updateDisplayedValue = true;
            NumericValue = Maximum;
        }
        else if (NumericValue < Maximum)
        {
            updateDisplayedValue = true;
            NumericValue++;
        }
        EditingEnded();
    }

    private void MinusButtonClick(object sender, RoutedEventArgs e)
    {
        if (NumericValue - 1f < Minimum)
        {
            updateDisplayedValue = true;
            NumericValue = Minimum;
        }
        else if (NumericValue > Minimum)
        {
            updateDisplayedValue = true;
            NumericValue--;
        }
        EditingEnded();
    }

    private void EditingEnded()
    {
        EnsureMinimumValue();
        RoutedEventArgs newEventArgs = new RoutedEventArgs(FinishedEditingEvent);
        RaiseEvent(newEventArgs);
    }

    private void EnsureMinimumValue()
    {
        if (InputTextBox.Text.Length == 0)
        {
            NumericValue = Minimum;
            UpdateValue(Minimum);
        }
    }

    private void UpdateValue(float val)
    {
        switch (NumberType)
        {
            case NumberInputBoxNumberType.Decimal:
                InputTextBox.Text = $"{val:0.00}";
                break;
            case NumberInputBoxNumberType.Integer:
            default:
                InputTextBox.Text = $"{val:0}";
                break;
        }
    }

    public void SetValue(float val)
    {
        if (IsValueAllowed($"{val}").Allowed)
        {
            NumericValue = val;
            UpdateValue(val);
        }
    }
}

public enum NumberInputBoxNumberType
{
    Integer,
    Decimal
}