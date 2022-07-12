using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DEHPSTEPTAS.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for DstEtractPreview.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class DstExtractPreview :Window
    {
        public DstExtractPreview()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
