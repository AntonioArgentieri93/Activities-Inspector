namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class ShellItem0x20 : ShellItem
    {
        public override string TypeName { get => "Volume"; }
        public ShellItem0x20(byte[] buf) : base(buf) { }
    }
}
