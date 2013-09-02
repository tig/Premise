// Copyright 2013 Kindel Systems
//   
// This file is part of pr
//   

using System;

namespace Premise {
    /// <summary>
    ///     This class supports mapping Premise's type system to .NET
    ///     Premise has a complete type system that mostly maps, but
    ///     with some exceptions.
    ///     TODO: Support additional type mappings
    /// </summary>
    public class PremiseProperty {
        public enum PremiseType {
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
            TypeHTMLColor,
            TypeByteArray,
            TypeClassRef,
            TypeFile,
            TypeDynamicType,
            TypePicture,
            Picture
        }

        private object _value;

        public PremiseProperty(String propertyName, PremiseType type, bool persistent = false, bool ignoreServer = false) {
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
                // coerce new value to correct type
                switch (PropertyType) {
                    case PremiseType.TypeText:
                        _value = value.ToString();
                        break;
                    case PremiseType.TypeBoolean:
                        bool b;
                        if (bool.TryParse(value.ToString(), out b))
                            _value = b;
                        else if (value is int) {
                            _value = ((int) value != 0);
                        }
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
                    case PremiseType.TypeHTMLColor:
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