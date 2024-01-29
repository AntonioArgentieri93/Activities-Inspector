namespace Activities_Inspector.Messages
{
    public class OnSortColumnMessage
    {
        public object NewPropertyType { get; }
        public bool NewIsAscending { get; }

        public OnSortColumnMessage(object newPropertyType, bool newIsAscending)
        {
            NewPropertyType = newPropertyType;
            NewIsAscending = newIsAscending;
        }
    }
}
