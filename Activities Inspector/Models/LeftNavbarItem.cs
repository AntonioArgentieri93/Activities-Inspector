namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class LeftNavbarItem
    {
        public ViewerMode ViewerMode { get; set; }
        public bool RequiresAdminPrivileges { get; set; }

        public LeftNavbarItem(ViewerMode viewerMode, bool requiresAdminPrivileges)
        {
            ViewerMode = viewerMode;
            RequiresAdminPrivileges = requiresAdminPrivileges;
        }
    }
}
