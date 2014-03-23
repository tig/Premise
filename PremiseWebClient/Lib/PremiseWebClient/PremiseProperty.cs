// Copyright 2013 Kindel Systems
//   
// This file is part of PremiseLib
//   

using System;

namespace PremiseWebClient {
    /// <summary>
    ///     This class supports mapping Premise's type system to .NET
    ///     Premise has a complete type system that mostly maps, but
    ///     with some exceptions.
    /// 
    ///     A property (e.g. 'Trigger') of type TypeTrigger will cause the holding object
    ///     to automatically expose another property that exposes ICommand (named 'TriggerCommand').
    ///     This is useful for Premise properties that are momentary such as keypad buttons and
    ///     allows MVVM support for Commands in XAML.
    /// 
    ///     TODO: Support additional type mappings
    /// </summary>
    public class PremiseProperty {
        public enum PremiseType {
            TypeNone = 0,
            TypeObjectRef,
            TypeText,
            TypeBoolean,
            TypeInteger,
            TypeFloat,
            TypeColor,
            TypeStaticText,
            TypeDateTime,
            TypeDate,
            TypeTime,
            TypeFileFolder,
            TypeCurrency,
            TypePercent,
            TypeMultiValue,
            TypeComponentRef,
            TypeObjectArray,
            TypeDataSource,
            TypeFont,
            TypeStream,
            TypeObjectFlag,
            TypeControlCode,
            TypeStatusCode,
            TypeTimeSpan,
            TypeUnit,
            TimespanProperty,
            UnitProperty,
            TypeImage,
            TypeHtmlColor,
            TypeByteArray,
            TypeClassRef,
            TypeFile,
            TypeDynamicType,
            TypePicture,
            Picture
        }

        private object _value;

        public PremiseProperty(String propertyName, PremiseType type = PremiseType.TypeNone, bool persistent = false, bool ignoreServer = false) {
            Name = propertyName;
            PropertyType = type;
            //UpdatedFromServer = false;
            Persistent = persistent;
            IgnoreServer = ignoreServer;
        }

        /// <summary>
        ///     The Premse type
        /// </summary>
        public PremiseType PropertyType { get; set; }

        /// <summary>
        ///     The property value.
        /// </summary>
        public object Value {
            get { return _value; }
            set {
                if (value == null) {
                    _value = null;
                    return;
                }

                // If the type is not set the set it based on the value
                if (PropertyType == PremiseType.TypeNone) {
                    if (value is string) {
                        string s = value.ToString().ToLower();
                        if (s.EndsWith("%"))
                            PropertyType = PremiseType.TypePercent;
                        else if (s == "on" || s == "off" || s == "yes" || s == "no")
                            PropertyType = PremiseType.TypeBoolean;
                        else
                            PropertyType = PremiseType.TypeText;
                    }
                    if (value is bool)
                        PropertyType = PremiseType.TypeBoolean;
                    if (value is int)
                        PropertyType = PremiseType.TypeInteger;
                    if (value is double || value is float)
                        PropertyType = PremiseType.TypeFloat;
                    if (value is DateTime)
                        PropertyType = PremiseType.TypeDateTime;
                    _value = value;
                    return;
                }

                // coerce new value to correct type
                switch (PropertyType) {
                    case PremiseType.TypeText:
                        _value = value.ToString();
                        break;
                    case PremiseType.TypeBoolean:
                        bool b;
                        if (value is bool)
                            _value = value;
                        else if (value is int) 
                            _value = ((int) value != 0);
                        else if (bool.TryParse(value.ToString(), out b))
                            _value = b;
                        else if (value is string) {
                            switch (value.ToString().ToLower()) {
                                case "yes":
                                case "on":
                                    _value = true;
                                    break;
                                case "no":
                                case "off":
                                    _value = false;
                                    break;
                                default:
                                    _value = false;
                                    break;
                            }
                        }
                        break;
                    case PremiseType.TypeInteger:
                        int i;
                        if (int.TryParse(value.ToString(), out i))
                            _value = i;
                        break;
                    case PremiseType.TypeFloat:
                        double d;
                        if (double.TryParse(value.ToString(), out d))
                            _value = d;
                        break;
                    case PremiseType.TypePercent:
                        double p;
                        if (value.ToString().EndsWith("%")) {
                            int n;
                            if (int.TryParse(value.ToString().Replace("%", ""), out n))
                                _value = (double) n/100;
                        }
                        else if (double.TryParse(value.ToString(), out p))
                            _value = p;
                        break;
                    case PremiseType.TypeDate:
                    case PremiseType.TypeTime:
                    case PremiseType.TypeDateTime:
                        DateTime dt;
                        if (value is DateTime)
                            _value = value;
                        else if (DateTime.TryParse(value.ToString(), out dt))
                            _value = dt;
                        break;
                    case PremiseType.TypeObjectRef:
                    case PremiseType.TypeColor:
                    case PremiseType.TypeStaticText:
                    case PremiseType.TypeFileFolder:
                    case PremiseType.TypeCurrency:
                    case PremiseType.TypeMultiValue:
                    case PremiseType.TypeComponentRef:
                    case PremiseType.TypeObjectArray:
                    case PremiseType.TypeDataSource:
                    case PremiseType.TypeFont:
                    case PremiseType.TypeStream:
                    case PremiseType.TypeObjectFlag:
                    case PremiseType.TypeControlCode:
                    case PremiseType.TypeStatusCode:
                    case PremiseType.TypeTimeSpan:
                    case PremiseType.TypeUnit:
                    case PremiseType.TimespanProperty:
                    case PremiseType.UnitProperty:
                    case PremiseType.TypeImage:
                    case PremiseType.TypeHtmlColor:
                    case PremiseType.TypeByteArray:
                    case PremiseType.TypeClassRef:
                    case PremiseType.TypeFile:
                    case PremiseType.TypeDynamicType:
                    case PremiseType.TypePicture:
                    case PremiseType.Picture:
                    default:
                        _value = value;
                        break;
                }
            }
        }

        // Dynamic binding in XAML requires that we use
        // array indexer syntax in XAML. To access a property use
        // <TextBlock Text="{Binding [Description]}"/>
        public static string NameFromItem(string item) {
            // "Item[PropertyName]"
            return item.Substring(5, item.Length - 6);
        }

        public static string ItemFromName(string name) {
            // "Item[PropertyName]"
            return "Item[" + name + "]";
        }

        /// <summary>
        ///     The name of the property. Matches the name on the server.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     If True then this property value should be persisted on the client
        ///     and never refreshed from the server (e.g. it never changes on the server, so don't
        ///     ask for it again).
        /// </summary>
        public bool Persistent { get; set; }

        /// <summary>
        ///     If True then this property value will never be retrieved from the server. This is useful
        ///     for property values on the server we want to ingore on the client such as DisplayName's that
        ///     change on the server for random reasons (e.g. IrrigationPro stores interal state in DisplayName that
        ///     we don't care about).
        /// </summary>
        public bool IgnoreServer { get; set; }

        ///// <summary>
        /////   True if the property value was retrieved from the server.
        ///// </summary>
        //public bool UpdatedFromServer { get; set; }

        //public bool CheckGetFromServer
        //{
        //    get
        //    {
        //        if (IgnoreServer) return false;

        //        if (Persistent && UpdatedFromServer) return false;

        //        return true;
        //    }
        //}
    }
}