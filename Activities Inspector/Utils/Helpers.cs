﻿using System;
using System.ComponentModel;
using System.Linq;

namespace ProgettoInformaticaForense_Argentieri.Utility
{
    internal class Helpers
    {
        public static string GetDescriptionFromEnumValue(Enum value)
        {
            var attribute = value.GetType()
                .GetField(value.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .SingleOrDefault() as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }
}
