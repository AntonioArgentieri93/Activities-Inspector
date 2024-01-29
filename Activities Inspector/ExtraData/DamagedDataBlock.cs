using System;

namespace ProgettoInformaticaForense_Argentieri.ExtraData.ExtraData
{
    public class DamagedDataBlock : ExtraDataBase
    {
        public DamagedDataBlock(byte[] rawBytes, string errMsg)
        {
            Signature = ExtraDataTypes.DamagedDataBlock;
            
            Size = (uint)rawBytes.Length;
            ErrorMessage = errMsg;

            if (rawBytes.Length < 4)
            {
                return;
            }

            OriginalSignature  = (ExtraDataTypes)BitConverter.ToInt32(rawBytes, 4);
            OriginalData = rawBytes;
        }

        public ExtraDataTypes OriginalSignature { get;  }
        public byte[] OriginalData { get; }
        public string ErrorMessage { get; }

        public override string ToString()
        {
            return
                $"Damaged data block" +
                $"\r\nOriginal Signature: {OriginalSignature}" +
                $"\r\nErrorMessage: {ErrorMessage}" ;
        }
    }
}
