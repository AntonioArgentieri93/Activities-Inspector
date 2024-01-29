using System;

namespace ProgettoInformaticaForense_Argentieri.Exceptions
{
    public class OverrunBufferException : Exception
    {
        public int ext_offset { get; set; }
        public int size { get; set; }

        public OverrunBufferException(int ext_offset, int size)
        {
            this.ext_offset = ext_offset;
            this.size = size;
        }
    }
}
