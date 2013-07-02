using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Feeder.Common.Timber;

namespace Feeder.Common.Controls
{
    public class ArrowRepeatButton : RepeatButton
    {
        public static readonly DependencyProperty ButtonArrowTypeProperty =
            DependencyProperty.Register("ButtonArrowType", typeof (ButtonArrowType), typeof (ArrowRepeatButton),
                new FrameworkPropertyMetadata(ButtonArrowType.Down));

        public static readonly DependencyProperty IsCornerCtrlCornerProperty =
            DependencyProperty.Register("IsCornerCtrlCorner", typeof (IsCornerCtrlCorner), typeof (ArrowRepeatButton),
                new FrameworkPropertyMetadata(new IsCornerCtrlCorner(false, true, true, false)));

        static ArrowRepeatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (ArrowRepeatButton),
                new FrameworkPropertyMetadata(typeof (ArrowRepeatButton)));
        }

        public ButtonArrowType ButtonArrowType
        {
            get { return (ButtonArrowType) GetValue(ButtonArrowTypeProperty); }
            set { SetValue(ButtonArrowTypeProperty, value); }
        }

        public IsCornerCtrlCorner IsCornerCtrlCorner
        {
            get { return (IsCornerCtrlCorner) GetValue(IsCornerCtrlCornerProperty); }
            set { SetValue(IsCornerCtrlCornerProperty, value); }
        }
    }

    public enum ButtonArrowType : byte
    {
        Down,
        Up,
        Left,
        Right
    }

    /// <summary>
    ///     IsCornerCtrlCorner is used to indicate which corners of the arrow button are also on the corner of the container
    ///     control
    ///     in which it is inserted. If for instance the arrow button is placed on right hand side as with a combo box, then
    ///     both
    ///     right hand sides corners of IsCornerCtrlCorner will be set to true, while both left hand sides will be set to
    ///     false.
    ///     Order is same as with Border.CornerRadius: topleft, topright, bottomright, bottomleft.
    ///     Reason for this is because with some themes (example: Aero), button has slightly rounded corners when these are on
    ///     edge of control.
    /// </summary>
    [TypeConverter(typeof (IsCornerCtrlCornerConverter))]
    public struct IsCornerCtrlCorner : IEquatable<IsCornerCtrlCorner>
    {
        private bool _bottomLeft;
        private bool _bottomRight;
        private bool _topLeft, _topRight;

        public IsCornerCtrlCorner(bool uniformCtrlCorner)
        {
            _topLeft = _topRight = _bottomRight = _bottomLeft = uniformCtrlCorner;
        }

        public IsCornerCtrlCorner(bool topLeft, bool topRight, bool bottomRight, bool bottomLeft)
        {
            _topLeft = topLeft;
            _topRight = topRight;
            _bottomRight = bottomRight;
            _bottomLeft = bottomLeft;
        }

        public bool BottomLeft
        {
            get { return _bottomLeft; }
            set { _bottomLeft = value; }
        }

        public bool BottomRight
        {
            get { return _bottomRight; }
            set { _bottomRight = value; }
        }

        public bool TopLeft
        {
            get { return _topLeft; }
            set { _topLeft = value; }
        }

        public bool TopRight
        {
            get { return _topRight; }
            set { _topRight = value; }
        }

        #region IEquatable<IsCornerCtrlCorner> Members

        public bool Equals(IsCornerCtrlCorner cornerCtrlCorner)
        {
            return this == cornerCtrlCorner;
        }

        #endregion

        public static bool operator !=(IsCornerCtrlCorner ccc1, IsCornerCtrlCorner ccc2)
        {
            return ccc1._topLeft != ccc2._topLeft || ccc1._topRight != ccc2._topRight ||
                   ccc1._bottomRight != ccc2._bottomRight || ccc1._bottomLeft != ccc2._topLeft;
        }

        public static bool operator ==(IsCornerCtrlCorner ccc1, IsCornerCtrlCorner ccc2)
        {
            return ccc1._topLeft == ccc2._topLeft && ccc1._topRight == ccc2._topRight &&
                   ccc1._bottomRight == ccc2._bottomRight && ccc1._bottomLeft == ccc2._topLeft;
        }

        public override bool Equals(object obj)
        {
            if (obj is IsCornerCtrlCorner)
                return this == (IsCornerCtrlCorner) obj;
            return false;
        }

        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyFieldInGetHashCode
            return (_topLeft ? 0x00001000 : 0x00000000) | (_topRight ? 0x00000100 : 0x00000000) |
                   (_bottomRight ? 0x00000010 : 0x00000000) | (_bottomLeft ? 0x00000001 : 0x00000000);
            // ReSharper restore NonReadonlyFieldInGetHashCode
        }

        public override string ToString()
        {
            return _topLeft.ToString() + "," + _topRight.ToString() + "," + _bottomRight.ToString() + "," +
                   _bottomLeft.ToString();
        }
    }

    [ValueConversion(typeof (IsCornerCtrlCorner), typeof (CornerRadius))]
    public class IsCornerCtrlCornerToRadiusConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter">
        ///     Must be a string that can be converted into an int, either directly as a decimal or as an hexadecimal
        ///     prefixed with either 0x or 0X. The first 8 bits will give the CornerRadius rounding next to edge of control. The
        ///     following bits will
        ///     give rounding of a corner not adjoining an edge. Example: 0x305: inner rounding: 0x3, outer rounding: 0x5. Inner
        ///     rounding of a
        ///     corner not adjoining an edge currently not used and set to 0.
        /// </param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                throw new ArgumentNullException();

            if (!(value is IsCornerCtrlCorner))
                throw new ArgumentException();

            var _str = (string) parameter;
            var _ccc = (IsCornerCtrlCorner) value;
            int _rounding;

            if (!int.TryParse(_str, out _rounding))
            {
                if (!(_str[0] == '0' && (_str[1] == 'x' || _str[1] == 'X')))
                    throw new ArgumentException();

                if (!Int32.TryParse(_str.Substring(2), NumberStyles.HexNumber, null, out _rounding))
                    throw new ArgumentException();
            }
            int _notEdgeRounding = _rounding >> 8;
            int _edgeRounding = _rounding & 0x000000FF;

            return new CornerRadius(_ccc.TopLeft ? _edgeRounding : _notEdgeRounding,
                _ccc.TopRight ? _edgeRounding : _notEdgeRounding, _ccc.BottomRight ? _edgeRounding : _notEdgeRounding,
                _ccc.BottomLeft ? _edgeRounding : _notEdgeRounding);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Not Implemented.");
        }

        #endregion
    }

    public class IsCornerCtrlCornerConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            return sourceType == Type.GetType("System.String");
        }

        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            return destinationType == Type.GetType("System.String");
        }

        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo,
            object source)
        {
            if (source == null)
                throw new ArgumentNullException();

            String[] _strArr = ((String) source).Split(',');

            if (_strArr.Count() != 4)
                throw new ArgumentException();

            var _cornerstates = new bool[4];

            for (var _i = 0; _i < _strArr.Count(); _i++)
            {
                if (!bool.TryParse(_strArr[_i], out _cornerstates[_i]))
                    throw new ArgumentException();
            }
            return new IsCornerCtrlCorner(_cornerstates[0], _cornerstates[1], _cornerstates[2], _cornerstates[3]);
        }

        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo,
            object value, Type destinationType)
        {
            if (value == null)
                throw new ArgumentNullException();
            if (!(value is IsCornerCtrlCorner))
                throw new ArgumentException();

            var _ccc = (IsCornerCtrlCorner) (value);

            return _ccc.TopLeft.ToString() + "," + _ccc.TopRight.ToString() + "," + _ccc.BottomRight.ToString() + "," +
                   _ccc.BottomLeft.ToString();
        }
    }

    public enum DecimalSeparatorType
    {
        SystemDefined,
        Point,
        Comma
    }

    public enum NegativeSignType
    {
        SystemDefined,
        Minus
    }

    public enum NegativeSignSide
    {
        SystemDefined,
        Prefix,
        Suffix
    }

    internal interface IFrameTxtBoxCtrl
    {
        TextBox TextBox { get; }
    }

    internal static class Coercer
    {
        public static void Initialize<T>() where T : UserControl
        {
            try
            {
                var _borderThicknessMetaData = new FrameworkPropertyMetadata
                                               {
                                                   CoerceValueCallback =
                                                       CoerceBorderThickness
                                               };
                Control.BorderThicknessProperty.OverrideMetadata(typeof (T), _borderThicknessMetaData);

                // For Background, do not do in XAML part something like:
                // Background="{Binding Background, ElementName=Root}" in TextBoxCtrl settings.
                // Reason: although this will indeed set the Background values as expected, problems arise when user
                // of control does not explicitly not set a value.
                // In this case, Background of TextBoxCtrl get defaulted to values in UserControl, which is null
                // and not what we want.
                // We want to keep the default values of a standard TextBox, which may differ according to themes.
                // Have to treat similarly as with BorderThickness...

                var _backgroundMetaData = new FrameworkPropertyMetadata {CoerceValueCallback = CoerceBackground};
                Control.BackgroundProperty.OverrideMetadata(typeof (T), _backgroundMetaData);

                // ... Same for BorderBrush
                var _borderBrushMetaData = new FrameworkPropertyMetadata {CoerceValueCallback = CoerceBorderBrush};
                Control.BorderBrushProperty.OverrideMetadata(typeof (T), _borderBrushMetaData);
            }
            catch (Exception _ex)
            {
                Logger.GetInstance()
                    .GetLogger()
                    .Add(new LogEntry {DateTime = DateTime.UtcNow, Module = "Error UI", Message = _ex.Message});
            }
        }

        private static void commonCoerce(DependencyObject d, object value, FuncCoerce funco)
        {
            var _frameTxtBox = d as IFrameTxtBoxCtrl;

            if (_frameTxtBox != null)
                funco(_frameTxtBox, value);
        }

        private static void funcCoerceBorderThickness(IFrameTxtBoxCtrl frameTxtBox, object value)
        {
            if (value is Thickness)
                frameTxtBox.TextBox.BorderThickness = (Thickness) value;
        }

        private static void funcCoerceBackground(IFrameTxtBoxCtrl frameTxtBox, object value)
        {
            var _brush = value as Brush;
            if (_brush != null)
                frameTxtBox.TextBox.Background = _brush;
        }

        private static void funcCoerceBorderBrush(IFrameTxtBoxCtrl frameTxtBox, object value)
        {
            var _brush = value as Brush;
            if (_brush != null)
                frameTxtBox.TextBox.BorderBrush = _brush;
        }

        public static object CoerceBorderThickness(DependencyObject d, object value)
        {
            commonCoerce(d, value, funcCoerceBorderThickness);
            return new Thickness(0.0);
        }

        public static object CoerceBackground(DependencyObject d, object value)
        {
            commonCoerce(d, value, funcCoerceBackground);
            return value;
        }

        public static object CoerceBorderBrush(DependencyObject d, object value)
        {
            commonCoerce(d, value, funcCoerceBorderBrush);
            return value;
        }

        #region Nested type: FuncCoerce

        private delegate void FuncCoerce(IFrameTxtBoxCtrl frameTxtBox, object value);

        #endregion
    }

    internal static class SystemNumberInfo
    {
        private static readonly NumberFormatInfo _nfi;

        static SystemNumberInfo()
        {
            var _ci = CultureInfo.CurrentCulture;
            _nfi = _ci.NumberFormat;
        }

        public static string DecimalSeparator
        {
            get { return _nfi.NumberDecimalSeparator; }
        }

        public static string NegativeSign
        {
            get { return _nfi.NegativeSign; }
        }

        public static bool IsNegativePrefix
        {
            // for values, see: http://msdn.microsoft.com/en-us/library/system.globalization.numberformatinfo.numbernegativepattern.aspx
            // Assume if negative number format is (xxx), number is prefixed.
            get { return _nfi.NumberNegativePattern < 3; }
        }
    }

    internal static class SystemDateInfo
    {
        private static readonly DateTimeFormatInfo _dtfi;

        static SystemDateInfo()
        {
            CultureInfo _ci = CultureInfo.CurrentCulture;
            _dtfi = _ci.DateTimeFormat;
        }

        // Pattern tags given in http://msdn.microsoft.com/en-us/library/system.globalization.datetimeformatinfo.aspx
        public static string LongTimePattern
        {
            get { return _dtfi.LongTimePattern; }
        }

        public static string AmDesignator
        {
            get { return _dtfi.AMDesignator; }
        }

        public static string PmDesignator
        {
            get { return _dtfi.PMDesignator; }
            set { _dtfi.PMDesignator = value; }
        }
    }

    internal enum HmsType : byte
    {
        Hour,
        HHour,
        Hour12,
        Hhour12,
        Minute,
        Mminute,
        Second,
        Ssecond,
        T,
        Tt,
        Unknown
    }

    internal static class TimeCtrlExtensions
    {
        // Extension properties not allowed unfortunately, so can't write HMSType hmstype = tb.HMSType; for instance. 
        public static HmsType get_HMSType(this FrameworkElement ctrl)
        {
            HmsType _hmsType;

            if (!Enum.TryParse(ctrl.Name, out _hmsType))
                _hmsType = HmsType.Unknown;

            return _hmsType;
        }

        public static void set_HMSType(this FrameworkElement ctrl, HmsType hmsType)
        {
            ctrl.Name = hmsType.ToString();
        }

        public static int Get12Hour(this int hour24)
        {
            return (hour24 == 0) ? 12 : ((hour24 <= 12) ? hour24 : hour24%12);
        }

        public static bool IsHalfDayHour(this TextBox tb)
        {
            return tb.get_HMSType() == HmsType.Hour12 || tb.get_HMSType() == HmsType.Hhour12;
        }

        public static int Get_t_Idx()
        {
            int _idx = 0;

            if (SystemDateInfo.PmDesignator[0] == SystemDateInfo.AmDesignator[0]) // case Japan.
                _idx++;

            return _idx;
        }

        public static bool IsAM_PM(this FrameworkElement ctrl)
        {
            return ctrl.get_HMSType() == HmsType.Tt || ctrl.get_HMSType() == HmsType.T;
        }

        public static bool IsAlways2CharInt(this FrameworkElement ctrl)
        {
            return ((get_HMSType(ctrl) == HmsType.HHour || get_HMSType(ctrl) == HmsType.Hhour12 ||
                     get_HMSType(ctrl) == HmsType.Mminute || get_HMSType(ctrl) == HmsType.Ssecond));
        }

        private static string getHmsText(FrameworkElement ctrl, DateTime dt)
        {
            string _hmsText;

            string _strFormat = ctrl.IsAlways2CharInt() ? "{0:D2}" : "{0:D}";

            switch (get_HMSType(ctrl))
            {
                case HmsType.Hour:
                case HmsType.HHour:
                    _hmsText = string.Format(_strFormat, dt.Hour);
                    break;
                case HmsType.Hour12:
                case HmsType.Hhour12:
                    _hmsText = string.Format(_strFormat, dt.Hour.Get12Hour());
                    break;
                case HmsType.Minute:
                case HmsType.Mminute:
                    _hmsText = string.Format(_strFormat, dt.Minute);
                    break;
                case HmsType.Second:
                case HmsType.Ssecond:
                    _hmsText = string.Format(_strFormat, dt.Second);
                    break;
                case HmsType.T:
                    _hmsText = (dt.Hour/12 == 0)
                        ? SystemDateInfo.AmDesignator[Get_t_Idx()].ToString(CultureInfo.InvariantCulture)
                        : SystemDateInfo.PmDesignator[Get_t_Idx()].ToString(CultureInfo.InvariantCulture);
                    break;
                case HmsType.Tt:
                    _hmsText = (dt.Hour/12 == 0) ? SystemDateInfo.AmDesignator : SystemDateInfo.PmDesignator;
                    break;
                default:
                    _hmsText = "";
                    break;
            }
            return _hmsText;
        }

        public static void set_HMSText(this TextBox tb, DateTime dt)
        {
            tb.Text = getHmsText(tb, dt);
        }

        public static int get_Max(this TextBox tb)
        {
            switch (tb.get_HMSType())
            {
                case HmsType.Hour:
                case HmsType.HHour:
                    return 24;
                case HmsType.Hour12:
                case HmsType.Hhour12:
                    return 13;
                default:
                    return 60;
            }
        }

        public static int get_Min(this TextBox tb)
        {
            switch (tb.get_HMSType())
            {
                case HmsType.Hour12:
                case HmsType.Hhour12:
                    return 1;
                default:
                    return 0;
            }
        }

        public static DateTime ResetTime(this DateTime dt, int hms, HmsType hmsType)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day,
                (hmsType == HmsType.Hour) || (hmsType == HmsType.HHour || hmsType == HmsType.Hour12) ||
                (hmsType == HmsType.Hhour12)
                    ? hms
                    : dt.Hour, (hmsType == HmsType.Minute) || (hmsType == HmsType.Mminute) ? hms : dt.Minute,
                (hmsType == HmsType.Second) || (hmsType == HmsType.Ssecond) ? hms : dt.Second, dt.Millisecond, dt.Kind);
        }

        public static DateTime Reset_AM_PM_Time(this DateTime dt, bool isAm)
        {
            int _hour = dt.Hour;

            if (isAm && _hour >= 12)
                _hour -= 12;
            else if (!isAm && _hour < 12)
                _hour += 12;

            return new DateTime(dt.Year, dt.Month, dt.Day, _hour, dt.Minute, dt.Second, dt.Millisecond, dt.Kind);
        }

        public static string get_TextFormat(this FrameworkElement fe)
        {
            var _fe = fe as TextBlock;
            if (_fe != null)
                return _fe.Text;
            if (get_HMSType(fe) == HmsType.Tt)
                return "{4}";
            if (get_HMSType(fe) == HmsType.T)
                return "{5}";

            string _strFormat = "{";

            switch (get_HMSType(fe))
            {
                case HmsType.HHour:
                case HmsType.Hour:
                    _strFormat += "0";
                    break;
                case HmsType.Hhour12:
                case HmsType.Hour12:
                    _strFormat += "1";
                    break;
                case HmsType.Mminute:
                case HmsType.Minute:
                    _strFormat += "2";
                    break;
                case HmsType.Ssecond:
                case HmsType.Second:
                    _strFormat += "3";
                    break;
            }
            _strFormat += ":D";

            if ((get_HMSType(fe) == HmsType.HHour || get_HMSType(fe) == HmsType.Hhour12 ||
                 get_HMSType(fe) == HmsType.Mminute || get_HMSType(fe) == HmsType.Ssecond))
                _strFormat += "2";

            _strFormat += "}";

            return _strFormat;
        }

        public static bool IsValidTime(string strTime, string strPattern, out int hour, out int minute, out int second)
        {
            // Loose validation: if for instance have an hour entry as '23' or '02', this still gets validated for an h tag.
            // Rule: if a number entry can possibly be interpreted, then it is. Allow AM/PM tag to go missing too.
            // ...but outside of this, must have a valid time and separators such as ':', '.' must be present and must match.  
            hour = 0;
            minute = 0;
            second = 0;

            if (string.IsNullOrEmpty(strTime) || string.IsNullOrEmpty(strPattern))
                return false;

            bool _isValid = true;
            int _idx = 0, _patIdx = 0;

            while (_isValid && _idx < strTime.Length && _patIdx < strPattern.Length)
            {
                switch (strPattern[_patIdx])
                {
                    case 's':
                    case 'm':
                    case 'H':
                    case 'h':
                        if (char.IsDigit(strTime, _idx))
                        {
                            string _strInt = strTime[_idx].ToString(CultureInfo.InvariantCulture);

                            if (++_idx < strTime.Length && char.IsDigit(strTime, _idx))
                                _strInt += strTime[_idx++].ToString(CultureInfo.InvariantCulture);

                            int _value = int.Parse(_strInt);

                            if (((strPattern[_patIdx] == 'h' || strPattern[_patIdx] == 'H') && _value >= 24) || _value >= 60)
                                _isValid = false;
                            else
                            {
                                switch (strPattern[_patIdx])
                                {
                                    case 'H':
                                    case 'h':
                                        hour = _value;
                                        break;
                                    case 'm':
                                        minute = _value;
                                        break;
                                    default:
                                        second = _value;
                                        break;
                                }

                                if (_patIdx + 1 < strPattern.Length && (strPattern[_patIdx] == strPattern[_patIdx + 1]))
                                    _patIdx += 2;
                                else
                                    _patIdx++;
                            }
                        }
                        else
                            _isValid = false;
                        break;
                    case 't':
                    {
                        bool _isPm = (strTime[_idx] == SystemDateInfo.PmDesignator[0]);

                        if (strTime[_idx] == SystemDateInfo.AmDesignator[0] ||
                            strTime[_idx] == SystemDateInfo.PmDesignator[0])
                            _idx++;

                        if (_patIdx + 1 < strPattern.Length && (strPattern[_patIdx] == strPattern[_patIdx + 1]))
                        {
                            if (_idx < strTime.Length &&
                                (strTime[_idx] == SystemDateInfo.AmDesignator[1] ||
                                 strTime[_idx] == SystemDateInfo.PmDesignator[1]))
                            {
                                if (_isPm && strTime[_idx] != SystemDateInfo.PmDesignator[1])
                                    _isPm = false;

                                _idx++;
                            }
                            else
                                _isValid = false;

                            _patIdx += 2;
                        }
                        else
                            _patIdx++;

                        if (_isPm && hour < 12)
                            hour += 12;
                    }
                        break;
                    default:
                        if (strTime[_idx] == strPattern[_patIdx])
                        {
                            _idx++;
                            _patIdx++;
                        }
                        else
                            _isValid = false;
                        break;
                }
            }
            if (_idx < strTime.Length || _patIdx < strPattern.Length)
                _isValid = false;

            return _isValid;
        }
    }

    public class ThicknessToMarginConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                throw new ArgumentNullException();

            if (!(value is Thickness) || !(parameter is string))
                throw new ArgumentException();

            bool _incrRightThickness;
            bool.TryParse((string) parameter, out _incrRightThickness);
            var _thickness = (Thickness) value;
            return new Thickness(_thickness.Left + 1.0, _thickness.Top + 1.0,
                _incrRightThickness ? _thickness.Right + 18.0 : 18.0, _thickness.Bottom);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Not Implemented.");
        }

        #endregion
    }

    internal static class LanguageStrings
    {
        private static readonly CultureInfo _ci = CultureInfo.CurrentCulture;
        /*static LanguageStrings() // For testing purposes
        {
            //ci = CultureInfo.CreateSpecificCulture("de-DE");
            ci = CultureInfo.CreateSpecificCulture("fr-FR");
        }*/

        public static string CopyTime
        {
            get { return getLangStr("COPY_TIME"); }
        }

        public static string PasteTime
        {
            get { return getLangStr("PASTE_TIME"); }
        }

        public static string ValidTimes
        {
            get { return getLangStr("VALID_TIMES"); }
        }

        public static string From
        {
            get { return getLangStr("FROM"); }
        }

        public static string To
        {
            get { return getLangStr("TO"); }
        }

        public static string None
        {
            get { return getLangStr("NONE"); }
        }

        private static string getLangStr(string strTag)
        {
            return Properties.Resources.ResourceManager.GetString(strTag, _ci);
        }
    }

    public static class TimeCtrlCustomCommands
    {
        static TimeCtrlCustomCommands()
        {
            CopyTime = new RoutedUICommand(LanguageStrings.CopyTime, "CopyTime", typeof (TimeCtrlCustomCommands));
            PasteTime = new RoutedUICommand(LanguageStrings.PasteTime, "PasteTime", typeof (TimeCtrlCustomCommands));
            ShowValidTimes = new RoutedUICommand("", "ShowValidTimes", typeof (TimeCtrlCustomCommands));
        }

        public static RoutedUICommand CopyTime { get; private set; }
        public static RoutedUICommand PasteTime { get; private set; }
        public static RoutedUICommand ShowValidTimes { get; private set; }
    }
}
