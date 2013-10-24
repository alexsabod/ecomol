using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace Ean13Barcode
{
    public partial class add_EAN13 : Form
    {

        public add_EAN13()
        {
            CallBackMy.callbackEventHandler = new CallBackMy.callbackEvent(this.Reload);
            InitializeComponent();           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!code_pars(textBox2.Text))
            {
                MessageBox.Show("Заполните правильно все данные!");
                return;
            }
            string sCxn = "Data Source=w2k3\\ADIFO;Initial Catalog=Bunkers;Persist Security Info=True;User ID=sa;Password=Adifosa1";
          //  string insertQuery = "insert into ean13bar (code,name,ves) values ("+int.Parse(textBox1.Text) + textBox2.Text + ",'" + textBox3.Text.TrimStart(' ') + "'," + textBox4.Text + ")";
            string insertQuery = "insert into ean13ZAO (code,name,ves) values (" + int.Parse(textBox1.Text) + int.Parse(textBox2.Text) + ",'" + textBox3.Text.TrimStart(' ') + "'," + textBox4.Text + ")";
            SqlConnection myConnection = new SqlConnection(sCxn);

            try
            {
                SqlCommand myCommand = new SqlCommand(insertQuery, myConnection);
                myConnection.Open();
                myCommand.ExecuteNonQuery();
            }
            catch (Exception et)
            {
                MessageBox.Show(et.ToString());
                
            }
            finally
            {
                myConnection.Close();
                MessageBox.Show("Код добавлен");
                Close();
            }
        }
        
        //PARSING SYMBOL
        bool code_pars(string code)
        {
            int i = 0;
            char[] tmp_mas = code.ToCharArray();//koll tonn
            if (code == "")
                return false;
            foreach (char temp in tmp_mas)
            {
                if (!char.IsNumber(temp)) return false;
                i++;
            }
            return true;
        }
         

        void Reload(string param1,string param2)
        {
            textBox3.Text = param1;//name
            textBox4.Text = param2;//ves
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
                      
    }
}
