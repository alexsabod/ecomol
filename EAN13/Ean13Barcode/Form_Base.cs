using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Ean13Barcode
{
    public partial class Form_Base : Form
    {
        public Ean13 ean13 = new Ean13();

        public string name_add;
        public int ves_add = 1;

        public Form_Base()
        {
            InitializeComponent();
        }
    }
}
