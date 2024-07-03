using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using SFML.System;
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
