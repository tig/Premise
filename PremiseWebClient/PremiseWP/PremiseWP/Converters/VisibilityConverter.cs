// Copyright 2012 Charlie Kindel
//   
// This file is part of PremiseWP7
//   

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PremiseWP.Converters {
    public class VisibilityConverter : IValueConverter {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var val = (bool)value;
            var param = parameter as string;
            if (param != null && param == "Inverse") {
                val = !val;
            }
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
    }
}