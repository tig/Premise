using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PremiseWebClient {
    /// <summary>
    /// Each .NET client has a different way of dispatching events to the
    /// 'ui thread'. We isolate these in an IPremiseNotify implementation
    /// that the caller to PremiseServer can give us. The default 
    /// implementation does no dispatching.
    /// Set PremiseServer.Notify to an implementation of IPremiseNotify
    /// to override. 
    /// </summary>
    public interface IPremiseNotify {
        void DispatchSetMember(PremiseObject obj, string propertyName, string value);

        void OnPropertyChanged(PremiseServer thisServer, PropertyChangedEventHandler handler, [CallerMemberName] string propertyName = null);
    }
}
