using Avalonia.Controls;
using PhiliaContacts.Presentation.Base.Extensions;

namespace PhiliaContacts.Presentation.Views
{
    public partial class ContactsView : UserControl
    {
        public ContactsView()
        {
            InitializeComponent();
            this.SetDataContext(App.Current?.Services);
        }
    }
}
