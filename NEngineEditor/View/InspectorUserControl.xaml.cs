using System.Windows.Controls;

using NEngineEditor.ViewModel;

namespace NEngineEditor.View
{
    public partial class InspectorUserControl : UserControl
    {
        public InspectorUserControl()
        {
            InitializeComponent();
            DataContext = new InspectorViewModel();
        }
    }
}
