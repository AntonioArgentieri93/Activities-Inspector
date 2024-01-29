using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    //https://www.usb.org/defined-class-codes

    public class UsbClass
    {
        public static Dictionary<string, string> UsbClasses = new Dictionary<string, string>
        {
            { "00", "Use class information in the Interface Descriptors" },
            { "01", "Audio" },
            { "02", "Communications and CDC Control" },
            { "03", "HID (Human Interface Device)" },
            { "05", "Physical" },
            { "06", "Image" },
            { "07", "Printer" },
            { "08", "Mass Storage" },
            { "09", "Hub" },
            { "0a", "CDC-Data" },
            { "0b", "Smart Card" },
            { "0d", "Content Security" },
            { "0e", "Video" },
            { "0f", "Personal Healthcare" },
            { "10", "Audio/Video Devices" },
            { "11", "Billboard Device Class" },
            { "12", "USB Type-C Bridge Class" },
            { "3c", "I3C Device Class" },
            { "dc", "Diagnostic Device" },
            { "e0", "Wireless Controller" },
            { "ef", "Miscellaneous" },
            { "fe", "Application Specific" },
            { "ff", "Vendor Specific" },
        };
    }
}
