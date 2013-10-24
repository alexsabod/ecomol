using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Xml;
using System.Drawing.Printing;
using System.Diagnostics;
using System.IO;
using FirebirdSql.Data.Firebird;
using System.Text.RegularExpressions;
using System.Data.SqlClient;


namespace Ean13Barcode
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	partial class frmEan13 : Form
	{
        public Ean13 ean13 = null;       
        private bool is_Draw;
        ConnStr connstr = new ConnStr();
        string[] arrVit = new string[40];   // массив для хранения витаминов в премиксе, для исключения из витаминного концентрата
        private string shortName,kindName;  // глобальная Short имя рецепта RAW_DICT.KPROD
        bool typeRecept = true;             //тип рецепта  true - Премикс False - БВМД      
        
        public frmEan13( )
		{
           
           
            is_Draw = true;
			InitializeComponent( );
            readXMLPrint();
		}

		private void CreateEan13( )
		{
            ean13 = new Ean13();
            
            //парсим всю строку по элементно
            string code;
            code = tb_code.Text;
            is_Draw = false;
            int i = 0;
            char[] ton_mas = code.ToCharArray();//koll tonn

            foreach (char temp in ton_mas)
            {
               
                if (i < 2)
                    ean13.CountryCode += temp;
                if (i > 1 && i < 7)
                    ean13.ManufacturerCode += temp;
                if (i > 6 && i < 12)
                    ean13.ProductCode += temp;
                 
                i++;
            }		
		}

		private void butDraw_Click(object sender, EventArgs e)
		{
			System.Drawing.Graphics g = this.picBarcode.CreateGraphics( );

			g.FillRectangle( new System.Drawing.SolidBrush( System.Drawing.Color.White),
				new Rectangle( 0, 0, picBarcode.Width, picBarcode.Height ) );

			CreateEan13( );
			ean13.DrawEan13Barcode( g, new System.Drawing.Point( 0, 0 ) );
			g.Dispose( );
            is_Draw = true;
		}       
		private void butCreateBitmap_Click(object sender, EventArgs e)
		{
			CreateEan13( );

			System.Drawing.Bitmap bmp = ean13.CreateBitmap( );
            bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
           
			this.picBarcode.Image = bmp;

		}

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern long BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
        
        private Bitmap memoryImage;
        //рабочая область печати
        private void CaptureScreen()
        {
            Graphics mygraphics;
            Size s;
            if (tp2.Focus())
            {
                mygraphics = panelBVMD_old.CreateGraphics();
                s = panelBVMD_old.Size;
            }
            else
            {
                mygraphics = panelBVMD.CreateGraphics();
                s = panelBVMD.Size;
            }

            memoryImage = new Bitmap(s.Width, s.Height, mygraphics);
            Graphics memoryGraphics = Graphics.FromImage(memoryImage);
            IntPtr dc1 = mygraphics.GetHdc();
            IntPtr dc2 = memoryGraphics.GetHdc();         
            BitBlt(dc2, -2, -1, this.ClientRectangle.Width-2,
                this.ClientRectangle.Height-2, dc1, -2, -2, 13369376);
           
            mygraphics.ReleaseHdc(dc1);
            memoryGraphics.ReleaseHdc(dc2);         
        }

        private void printDocument1_PrintPage(System.Object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            memoryImage.RotateFlip(RotateFlipType.Rotate90FlipNone);// поворот документа на 90 град
            e.Graphics.DrawImage(memoryImage, 0, 0); //смещение документа на наклейке
        }

        private void button1_Click(object sender, EventArgs e)
        {
          //  writeXMLprintSet();//пишем параметры положения карточки на бумаге в XML
           
            CaptureScreen();// берем скрин рабочей области
            if (printDialog1.ShowDialog() == DialogResult.OK)
            {
                string printer = printDialog1.PrinterSettings.PrinterName;
                printDocument1.DefaultPageSettings.Margins = new Margins(Convert.ToInt32( tbLeftPrint.Text), 0, Convert.ToInt32( tbTopPrint.Text), 0);
                printDocument1.PrinterSettings.PrinterName = printer;
                printDocument1.PrinterSettings = printDialog1.PrinterSettings;
                printDocument1.DefaultPageSettings.Landscape = true;
        //        printDocument1.DefaultPageSettings.PrinterResolution.Y = -3;
                printDocument1.Print();
            }
        }
        //запись параметров отступа на бумаге
        private void writeXMLprintSet() 
        {
            XmlTextWriter XmlWriter = new XmlTextWriter("PrintSet.xml", null);
            XmlWriter.WriteStartDocument();
                XmlWriter.WriteStartElement("PrintSettings");
                    XmlWriter.WriteStartAttribute("Xpos", null);
                        XmlWriter.WriteString(tbLeftPrint.Text);
                    XmlWriter.WriteEndAttribute();
                    XmlWriter.WriteStartAttribute("Ypos", null);
                        XmlWriter.WriteString(tbTopPrint.Text);
                    XmlWriter.WriteEndAttribute();
                XmlWriter.WriteEndElement();
            XmlWriter.Close();
        }
       // читаем с XML параметры принтера
        private void readXMLPrint()
        {
            XmlTextReader XmlReader = new XmlTextReader("PrintSet.xml");
                while (XmlReader.Read())
                {
                    if (XmlReader.NodeType == XmlNodeType.Element)
                    {
                        if (XmlReader.Name.Equals("PrintSettings"))
                         
                        {
                            tbLeftPrint.Text = XmlReader.GetAttribute("Xpos").ToString();
                            tbTopPrint.Text = XmlReader.GetAttribute("Ypos").ToString();                           
                        }
                    }
                }
             XmlReader.Close();
        }     
        private float covertStringDotToFloat(string param)
        {
            int i = 0;
            char[] ton_mas = param.ToCharArray();//koll tonn
            string str_tmp = ""; // расчет на  ХХ тонн
            foreach (char temp in ton_mas)
            {
                if (temp == '.') ton_mas[i] = ',';
                str_tmp += ton_mas.GetValue(i).ToString();
                i++;
            }
            return float.Parse(str_tmp);
        }

        //save dialog
        private void button2_Click(object sender, EventArgs e)
        {
           // string file;
            // Create new SaveFileDialog object
            SaveFileDialog DialogSave = new SaveFileDialog();

            // Default file extension
            DialogSave.DefaultExt = "xml";

            // Available file extensions
            DialogSave.Filter = "Excel file (*.xml)|*.xml";

            // Adds a extension if the user does not
            DialogSave.AddExtension = true;

            // Restores the selected directory, next time
            DialogSave.RestoreDirectory = true;

            // Dialog title
            DialogSave.Title = "Where do you want to save the file?";

            // Startup directory
            // DialogSave.InitialDirectory = "c:\\Documents and Settings\\Admin\\Мои документы\\Экомол\\TechKard\\";

 
          //  DialogSave.FileName = tb_id.Text + "   " + strvalue;
            // Show the dialog and process the result
            if (DialogSave.ShowDialog() == DialogResult.OK)
            {
                // MessageBox.Show("You selected the file: " + DialogSave.FileName);             
             writeInXML(DialogSave.FileName);
            }
            else
            {
                //  MessageBox.Show("You hit cancel or closed the dialog.");
            }

            DialogSave.Dispose();
            DialogSave = null;
        }
        private void writeInXML(string file)
        {
                XmlTextWriter XmlWriter = new XmlTextWriter(file, null); 
                XmlWriter.WriteStartDocument();
                XmlWriter.WriteStartElement("Cards");
                    XmlWriter.WriteStartElement("Premix");
                        XmlWriter.WriteStartAttribute("Name_", null);
                            XmlWriter.WriteString(tb8.Text);
                        XmlWriter.WriteEndAttribute();
                            XmlWriter.WriteStartElement("Data_card");
                                XmlWriter.WriteStartAttribute("fullName", null);
                                    XmlWriter.WriteString(tb2.Text);
                                XmlWriter.WriteEndAttribute();
                                XmlWriter.WriteStartAttribute("Companents", null);
                                    XmlWriter.WriteString(tb6.Text);
                                XmlWriter.WriteEndAttribute();
                                XmlWriter.WriteStartAttribute("Barcode", null);
                                    XmlWriter.WriteString(ean13.CountryCode + ean13.ManufacturerCode + ean13.ProductCode + ean13.ChecksumDigit);
                                XmlWriter.WriteEndAttribute();
                                XmlWriter.WriteStartAttribute("Companents1", null);
                                 XmlWriter.WriteString(tb4.Text);
                                XmlWriter.WriteEndAttribute();
                                XmlWriter.WriteStartAttribute("Companents2", null);
                                    XmlWriter.WriteString(tb12.Text);
                                XmlWriter.WriteEndAttribute();
                                XmlWriter.WriteStartAttribute("DateTime", null);
                                    XmlWriter.WriteString(tb13.Text);
                                XmlWriter.WriteEndAttribute();
                                XmlWriter.WriteStartAttribute("Nom_Partii", null);
                                    XmlWriter.WriteString(tb14.Text);
                                XmlWriter.WriteEndAttribute();
                                XmlWriter.WriteStartAttribute("Nom_smen", null);
                                    XmlWriter.WriteString(textBox15.Text);
                                XmlWriter.WriteEndAttribute();
                                XmlWriter.WriteStartAttribute("Massa", null);
                                    XmlWriter.WriteString(tb10.Text);
                                XmlWriter.WriteEndAttribute();
                            XmlWriter.WriteEndElement();
                    XmlWriter.WriteEndElement();
                XmlWriter.WriteEndElement(); 
                XmlWriter.Close();
        }
        //
        //          Open File dialog vizov  
        //
        private void button3_Click(object sender, EventArgs e)
        {
            dialog_file_open();
        }
        private void dialog_file_open()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open file dialog";
            //openFileDialog.InitialDirectory = @"c:\Documents and Settings\Admin\Мои документы\Экомол\";
            openFileDialog.Filter = "xml files| *.xml";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                XmlTextReader XmlReader = new XmlTextReader(openFileDialog.FileName);
                while (XmlReader.Read())
                {
                    if (XmlReader.NodeType == XmlNodeType.Element)
                    {
                        if (XmlReader.Name.Equals("Premix")) 
                            tb8.Text = XmlReader.GetAttribute("Name_").ToString();
                        if (XmlReader.Name.Equals("Data_card"))
                        {
                            tb2.Text = XmlReader.GetAttribute("fullName").ToString();
                            tb6.Text = XmlReader.GetAttribute("Companents").ToString();
                            tb4.Text = XmlReader.GetAttribute("Companents1").ToString();
                            tb12.Text = XmlReader.GetAttribute("Companents2").ToString();
                            tb13.Text = XmlReader.GetAttribute("DateTime").ToString();
                            tb14.Text = XmlReader.GetAttribute("Nom_Partii").ToString();
                            textBox15.Text = XmlReader.GetAttribute("Nom_smen").ToString();
                        }
                    }
                }
            }
            CreateEan13();
            System.Drawing.Bitmap bmp = ean13.CreateBitmap();
            bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            this.picBarcode.Image = bmp;

            bmp.Save(Application.StartupPath + "\\1.bmp");
        }

        private void frmEan13_Load(object sender, EventArgs e)
        {
            tb30_old.Text = textBox45_old.Text = tb13_old.Text = tb30.Text = textBox45.Text = tb13.Text = DateTime.Now.Day + "/" + DateTime.Now.Month + "/" + DateTime.Now.Year;
        }
        //
        //start
        //
        private void button4_Click(object sender, EventArgs e)
        {
            tb6_old.Text = tb6.Text = "";
            tb26_old.Text = tb26.Text = "";
            tb10_old.Text = tb22_old.Text = textBox37_old.Text = tb10.Text = tb22.Text = textBox37.Text = tbVes.Text;
                        
            if (find_java())
            {          
                serch_BD_ather_item();
                check_type();               //определяем Премикс или БВМД 
                tb14_old.Text = tb14.Text = tb_id.Text;          
                getEAN13code();

                //Draw the EAN 13 code
                if (is_Draw)
                {
                    CreateEan13();                                          //считаем проверяем
                    System.Drawing.Bitmap bmp = ean13.CreateBitmap();       //рисуем
                    bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    this.picBarcode_old.Image = this.pictureBox1_old.Image = this.picBarcode_kom_old.Image =
                        this.picBarcode.Image = this.pictureBox1.Image = this.picBarcode_kom.Image = bmp;//выводим на форму
                }
                else//added to base EAN13 bar code
                {
                    var result = MessageBox.Show("Не нашел штрих код, ДОБАВИМ в базу ??", "EAN13 added", MessageBoxButtons.YesNo,
                                     MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        add_EAN13 form2 = new add_EAN13();
                        form2.Visible = true;
                        form2.Owner = this;
                        if (panelBVMD.Visible == true)
                        {
                            CallBackMy.callbackEventHandler(tb8.Text, tbVes.Text);
                        }
                        else CallBackMy.callbackEventHandler(tb8_kom.Text, tbVes.Text);

                        form2.Show();
                    }
                }
            }
                         

        }
        //проверка на первоначальный ввод входных данных ЭКМ и вес
        /*
        private bool checkIn()
        {
            if (tb_id.Text.ToString() == "" || tbVes.Text.ToString() == "" || )
            {
                return false;
            }
            return true;
        }
*/
//проверка типа карточки
        private void check_type()
        {         
            FbConnection connection = new FbConnection(connstr.connStr);
            FbCommand command = new FbCommand();
            double groupId;
            try
            {
                connection.Open();
                FbTransaction transaction = connection.BeginTransaction();
                //определяем тип рецепта ПРЕМИКС/ БМВД

                command = new FbCommand("SELECT DOC_DCT.DOC_GROUP_ID FROM DOC_DCT WHERE DOC_DCT.DOC_ID = " + ID + " ", connection);
                command.Transaction = transaction;
                groupId = double.Parse( command.ExecuteScalar().ToString());
          
                do
                {
                    command = new FbCommand("select \"Doc Groups\".\"Parent\" FROM \"Doc Groups\" " +
                                            " WHERE \"Doc Groups\".DOC_GROUP_ID = " + groupId + "", connection);
                    command.Transaction = transaction;
                    if (command.ExecuteScalar().ToString() != "")
                            groupId = double.Parse(command.ExecuteScalar().ToString());
                    else break;//не нашли
                } while (groupId != -12 && groupId != -47 && groupId != 4 && groupId != 2);
                if(groupId == 2)
                {
                    textBox38_old.Text = textBox38.Text = "Хлопья кормовые";
                    this.panelHlopia_old.Visible = this.panelHlopia.Visible = true;
                    this.panelKombicorm_old.Visible = this.panelKombicorm.Visible = false;
                    this.panelBVMD_old.Visible = this.panelBVMD.Visible = false;
                    this.tb5_size.Visible = false;
                    this.tb35_old.Text = this.tb35.Text = tb_id.Text;
                    tbVes.Text = textBox37.Text = textBox37_old.Text = "15";

                }
                if (groupId == 4)//ПРЕПМИКС
                {
                    this.panelKombicorm_old.Visible = this.panelKombicorm.Visible = false;
                    this.panelBVMD_old.Visible = this.panelBVMD.Visible = true;
                    this.panelHlopia_old.Visible = this.panelHlopia.Visible = false;
                    this.tb5_size.Visible = true;
                    tb14_old.Text = tb14.Text = tb_id.Text;
                    typeRecept = true; //тип рецепта true - Премикс False - БВМД 
                    tb4_old.Visible = tb4.Visible = true;
                     tb6.Height = 230;
                     tb6_old.Height = 180;
                    tb7.Text = tb7_old.Text = "ПРЕМИКС";
                    tb7_old.Font = this.tb7.Font = new System.Drawing.Font("Tahoma", 19, FontStyle.Bold); 
                    tb9_old.Text = tb9.Text = "Состав биологически активных веществ";

                    if (tb8.Text.Contains("ПА"))
                    {
                        tb12_old.Text = tb12.Text = "TY BY 3000732103.003-2011";
                        tb20_old.Text = tb20.Text = "Срок хранения не более 6 месяцев";
                        tb7_old.Text = tb7.Text = "ПРЕМИКС АДРЕСНЫЙ";
                    }
                    else
                    {
                        tb12_old.Text = tb12.Text = "СТБ 1079-97";
                        tb20_old.Text = tb20.Text = "Срок хранения не более 4 месяцев";
                        func_set_month();
                    }                    
                    funcSetPRMXConc();
                }

                if (groupId == -12)//БВМД
                {
                    tb23_size.Text = "23"; 
                    this.panelKombicorm_old.Visible = this.panelKombicorm.Visible = false;
                    this.panelBVMD_old.Visible = this.panelBVMD.Visible = true;
                    this.panelHlopia_old.Visible = this.panelHlopia.Visible = false;
                    this.tb5_size.Visible = false;
                    tb14_old.Text = tb14.Text = tb_id.Text;
                    typeRecept = false; //тип рецепта  true - Премикс False - БВМД 
                    tb4_old.Visible = tb4.Visible = false;
                    command = new FbCommand("SELECT PRMXRESQ.QLT_VAL FROM PRMXRESQ WHERE PRMXRESQ.RCP_ID =" + ID +" and PRMXRESQ.qlt_id = 1", connection);
                    command.Transaction = transaction;
                    tb6.Height = 240;
                    tb6_old.Height = 195;
                    tb9_old.Text = tb9.Text = "Состав: ";
                    tb7.Text = tb7_old.Text = "Белково-витаминно-минеральная добавка";

                    tb12.Text = tb12_old.Text = "СТБ 1150-2007";
                    tb20.Text = tb20_old.Text = "Срок хранения не более 2 месяцев";
                    this.tb7.Font = tb7_old.Font = new System.Drawing.Font("Tahoma", 12, FontStyle.Bold);
                    tb6_old.Text = tb6.Text += "\r\n" + "Обменная энергия не менее ККал/100г  - " + obmenEnergy()+
                        "\r\n" + "Кормовых ед. в 100 кг комбикорма  - " + command.ExecuteScalar().ToString() +
                        "\r\n" + " Норма ввода в кормосмесь  " + ReturnConc() + "%" +
                        "\r\n Продукцию хранят в сухих чистых, незараженных вредителями хлебных запасов, закрытых складских помещениях." +
                        "\r\n Продукцию, упакованную в мешки, укладывают на плоские поддоны штабелем высотой не более 14 рядов.";
                }

                if(groupId == -47)// КОМБИКОРМ
                {

                    tb23_size.Text = "18"; 
                    this.panelKombicorm_old.Visible = this.panelKombicorm.Visible = true;
                    this.panelBVMD_old.Visible = this.panelBVMD.Visible = false;
                    this.panelHlopia_old.Visible = this.panelHlopia.Visible = false;
                  //  isKombikorm = true;
                    command = new FbCommand("SELECT RAW_DICT.gost, RAW_DICT.\"DefaultStorageTime\",RCPLIST.PRODTYPE_ID" +
                                           " FROM doc_dct, raw_dict, rcplist" +
                                           " WHERE RCPLIST.RCP_ID=" + ID + " and DOC_DCT.DOC_ID = RCPLIST.RCP_ID" +
                                           " AND RCPLIST.PROD_ID = RAW_DICT.RAW_ID" +
                                            " ", connection);
                    command.Transaction = transaction;
                    FbDataReader rd = command.ExecuteReader();
                    tb7_kom_old.Text = tb7_kom.Text = "Комбикорм";
                    while (rd.Read())
                    {
                       tb_STB_old.Text = tb_STB.Text = rd[0].ToString();
                       tb16_old.Text = tb16.Text = "Срок хранения не более  " + rd[1].ToString();
                        if (rd[2].ToString() == "1")
                        {
                          tb2_kom_old.Text =  tb2_kom.Text += "   (КРУПКА)";
                        }
                        if (rd[2].ToString() == "2")
                        {
                            tb2_kom_old.Text = tb2_kom.Text += "   (ГРАНУЛЫ)";
                        }
                        if (rd[2].ToString() == "4")
                        {
                            tb2_kom_old.Text = tb2_kom.Text += "   (РОССЫПНОЙ)";
                        }
                    }
                    rd.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        //обменная энерния
        private string obmenEnergy()
        {
            string result = "0";
            FbConnection connection = new FbConnection(connstr.connStr);                        
            try
            {
                FbCommand command = new FbCommand();               
                connection.Open();
               
                FbTransaction transaction = connection.BeginTransaction();
                command = new FbCommand("SELECT rcpcorrq.qlt_in_calc,rcpcorrq.qlt_id " +
                                                    " from rcpcorrq " +
                                                    " where rcpcorrq.rcp_id = " + ID, connection);
                
                command.Transaction = transaction;
                FbDataReader rd = command.ExecuteReader();
       
                while (rd.Read())
                {
                    if (rd[1].ToString() == "2" || rd[1].ToString() == "3" || rd[1].ToString() == "4" || rd[1].ToString() == "5")
                    {
                       result = rd[0].ToString();
                    }
                }
                rd.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return "0";
            }
            finally
            {
                connection.Close();                
            }
            return result;
        }
        //поиск необходимого штрих кода
        private void getEAN13code()
        {
            int[] stage1 = new int[5]; //степень вероятности соответствия
            int[] stage2 = new int[5];

            string sCxn = "Data Source = w2k3\\ADIFO;Initial Catalog = Bunkers; Persist Security Info = True;User ID = sa;Password = Adifosa1";
            //string insertQuery = "select name, ves, shortName, nameParam, code, dopParam from ean13bar";
            string insertQuery = "SELECT name, ves, shortName, nameParam, code, dopParam "+
              //"FROM    ean13bar "+
               "FROM  ean13ZAO " +
               "WHERE name = '" + shortName.TrimStart(' ') + "' and ves =" + tbVes.Text.ToString( ) + "";

            SqlConnection myConnection = new SqlConnection(sCxn);

            try
            {
                SqlCommand myCommand = new SqlCommand(insertQuery, myConnection);
                myConnection.Open();

                SqlDataReader rd = myCommand.ExecuteReader();

                is_Draw = false; // определяем будем рисовать или нет штрих
                while (rd.Read())
                {
                    if (rd[2].ToString() != "")//сложный элемент ВАЖНО!!!!!!!!!
                    {
                     //   if ( shortName.TrimStart(' ') == rd[0].ToString() && Regex.IsMatch(tbVes.Text.ToString(), rd[1].ToString()) && rd[1].ToString() != "" )
                     //   { 
                            tb_code.Text = rd[4].ToString();
                            is_Draw = true;
                      //  }
                    }
                    else
                    {
                     //   if(shortName == rd[0].ToString() && Regex.IsMatch(tbVes.Text.ToString(), rd[1].ToString()) && (rd[1].ToString() != ""))
                     //  {
                           tb_code.Text = rd[4].ToString();
                           is_Draw = true;
                     //  }
                    }       
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                is_Draw = false;
            }
            finally
            {
                myConnection.Close();
            }
        }
  
        void funcSetPRMXConc()
        {
            double val = ReturnConc();
            if (val > 0)
                tb4_old.Text = tb4.Text = "вводить в количестве  " + val + "% к весу комбикорма или зерновой смеси ";
                else
                tb4_old.Text = tb4.Text = "процент ввода не определён базой данных";
        }
        //поиск концентрации элемента
        double ReturnConc()
        {
            double val;
            
            val = 0;
            FbConnection connection = new FbConnection(connstr.connStr);
            FbCommand command = new FbCommand();

            if (typeRecept) //тип рецепта  true - Премикс False - БВМД 
            {
                command = new FbCommand("SELECT \"Rcp Attributes\".\"AttribValue\" " +
                                            "From \"Rcp Attributes\" " +
                                            "where \"Rcp Attributes\".\"AttribID\" = 1 " +
                                             "AND \"Rcp Attributes\".\"RCP_ID\" =" + ID + "" +
                                          "", connection);
            }
            else
            {
                command = new FbCommand("SELECT \"Rcp Attributes\".\"AttribValue\" " +
                                           "From \"Rcp Attributes\" " +
                                           "where \"Rcp Attributes\".\"AttribID\" = 212 " +
                                            "AND \"Rcp Attributes\".\"RCP_ID\" =" + ID + "" +
                                         "", connection);
            }
            try
            {
                connection.Open();
                FbTransaction transaction = connection.BeginTransaction();
                command.Transaction = transaction;
                //string buf;
                if (command.ExecuteScalar() != null)
                {
                    string a = command.ExecuteScalar().ToString().Replace('.', ',');
                    val = double.Parse(a);
                }
                //buf = command.ExecuteScalar().ToString();
                else
                    val = 0;
            }//try
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }

            return val; 
        }
        
        void func_set_month()
        {
            FbConnection connection = new FbConnection(connstr.connStr);
            FbCommand command = new FbCommand();

            command = new FbCommand("select count( raw_dict.raw_short_name) "+
                " from rcprep , raw_dict "+
                " where rcprep.rcp_id="+ID+" and rcprep.raw_id = raw_dict.raw_id "+
                " and (raw_dict.raw_group_id = 460" +
                " or raw_dict.raw_group_id = 461" +
                " or raw_dict.raw_group_id = 455)", connection);

            try
            {
                connection.Open();
                FbTransaction transaction = connection.BeginTransaction();
                command.Transaction = transaction;
                
                if (int.Parse(command.ExecuteScalar().ToString()) > 0 )
                    tb20_old.Text = tb20.Text = "Срок хранения не более 4 месяцев";
                else
                    tb20_old.Text = tb20.Text = "Срок хранения не более 5 месяцев";
            }//try
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        void func_find_chrotSoi()
        {
            FbConnection connection = new FbConnection(connstr.connStr);
            FbCommand command = new FbCommand();

            command = new FbCommand("select count( raw_dict.raw_short_name) " +
                " from rcprep , raw_dict " +
                " where rcprep.rcp_id=" + ID + " and rcprep.raw_id = raw_dict.raw_id " +
                " and (raw_dict.raw_group_id = 552)", connection);

            try
            {
                connection.Open();
                FbTransaction transaction = connection.BeginTransaction();
                command.Transaction = transaction;

                if (int.Parse(command.ExecuteScalar().ToString()) > 0)
                    tb20_old.Text = tb20.Text = "Срок хранения не более 4 месяцев";
                else
                    tb20_old.Text = tb20.Text = "Срок хранения не более 5 месяцев";
            }//try
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        string replaseNameOfElemetByGroup(string name, string group, string id)
        {
            switch (group)
            {
                case "571":
                    {
                        if (id == "-432959" || id == "-433003" || id == "-432870")
                        {
                            return "ПРОФИШ";
                        }
                        else
                            return "МУКА РЫБНАЯ";
                    } 
                case "572": return "МУКА РЫБНАЯ";
                case "569": return "МУКА РЫБНАЯ";
                case "455": return "МОНОКАЛЬЦИЙ ФОСФАТ";
                case "584": return "МУКА КОРМОВАЯ";
              //  case "284": return "СОДА";
               // case "411": return "ЛИЗИН";
            }
            return name;
 
        }
        string replaseNameOfElemet(string name, string group)
        {
            switch (group)
            {
                case "419419": return "КОБАЛЬТ";
                case "16": return "КОБАЛЬТ";
                case "419418": return "КОБАЛЬТ";
                case "8": return "КОБАЛЬТ";
                case "59": return "КОБАЛЬТ";
                case "17": return "КОБАЛЬТ";
                // case "75": return "КОБАЛЬТ";
                case "51": return "МЕДЬ";
                case "53": return "МЕДЬ";
                case "5": return "МЕДЬ";
                case "52": return "МЕДЬ";
                // case "72": return "МЕДЬ";
                case "242500": return "МЕТИОНИН";
                case "-432787": return "МЕТИОНИН";
                // case "22": return "МЕТИОНИН";
                case "49": return "ЖЕЛЕЗО";
                case "7": return "ЖЕЛЕЗО";
                // case "71": return "ЖЕЛЕЗО";
                case "18": return "ЙОД";
                case "9": return "ЙОД";
                case "50": return "ЙОД";
                // case "76": return "ЙОД";
                case "429874": return "ТРЕОНИН";
                case "439875": return "ТРИПТОФАН";
                case "63": return "МАГНИЙ";
                case "419420": return "МАГНИЙ";
                case "64": return "МАГНИЙ";
                case "61": return "МАГНИЙ";
                case "62": return "МАГНИЙ";
                case "-432578": return "МАГНИЙ";
                // case "53": return "МАГНИЙ";
                case "56": return "МАРГАНЕЦ";
                case "11": return "МАРГАНЕЦ";
                case "57": return "МАРГАНЕЦ";
                case "12": return "МАРГАНЕЦ";
                case "4": return "МАРГАНЕЦ";
                case "58": return "МАРГАНЕЦ";
                // case "74": return "МАРГАНЕЦ";
                case "67": return "НАТРИЙ ТИОСУЛЬФАТ";
                // case "56": return "НАТРИЙ ТИОСУЛЬФАТ";
                case "6": return "ЦИНК";
                case "54": return "ЦИНК";
                case "13": return "ЦИНК";
                case "14": return "ЦИНК";
                case "73": return "ЦИНК";
                // case "73": return "ЦИНК";
                case "-432031": return "МЕЛ";
                case "-432791": return "МЕЛ";
                case "-432487": return "МЕЛ";
                case "431583": return "МЕЛ";
                case "431584": return "МЕЛ";
                // case "49": return "МЕЛ";
                case "-432356": return "СОДА";
                case "-432713": return "СОДА";               
                case "-432296": return "СЕЛЕН";
                case "419423": return "ВИТАМИН В4";
                case "30": return "ВИТАМИН В4";
                case "250120": return "СОДА";
                case "-432032": return "СОДА";
                case "-432818": return "ЛИЗИН";
                case "-432830": return "ЛИЗИН";
                case "242310": return "ЛИЗИН";
                case "-432744": return "ЛИЗИН";
                case "-432843": return "НАТРИЙ СЕРНОКИСЛЫЙ";
                case "-432617": return "ЛАДОЗИМ";
                case "-432729": return "ЛАДОЗИМ";
                case "-432616": return "ЛАДОЗИМ";
                case "-432104": return "ИН-АДСОРБИН";
                case "-432782": return "БЕЛКОВАЯ КОРМОСМЕСЬ";
                case "-433234": return "МЕЛ";
            }
            return name;
        }
        string[] arr = new string[40];

        void serch_BD_ather_item()
        {
            int i = 0;
            bool isExist = false;
            FbConnection connection = new FbConnection(connstr.connStr);
            FbCommand command = new FbCommand();
          
            try
            {
                connection.Open ();
                FbTransaction transaction = connection.BeginTransaction ();
                // поиск всех элементов за исключением витаминных канцентратов , т.е. гр 29
                command = new FbCommand ("SELECT RAW_DICT.RAW_SHORT_NAME, RAW_DICT.RAW_ID, RAW_DICT.RAW_GROUP_ID, RAW_DICT.PRIMARY_QLT_ID " +
                    " FROM RCPREP,RAW_DICT " +
                    " WHERE RCPREP.RCP_ID= " + ID + " AND RCPREP.RAW_ID = RAW_DICT.RAW_ID " +
                    //исключаем Витаминные концентраты
                     "and RCPREP.RAW_ID <>-432390 AND " +//КС-4 (1.5%)
                     "RCPREP.RAW_ID <>-432162 AND " +//КС-1 А
                     "RCPREP.RAW_ID <>-432398 AND " +//КС-3(3 %)
                     "RCPREP.RAW_ID <>-432161 AND " +//КС-3 А
                     "RCPREP.RAW_ID <>-432160 AND " +//КС-4 А
                     "RCPREP.RAW_ID <>-432396 AND " +//П1-1Кобб(2,5%)
                     "RCPREP.RAW_ID <>-432397 AND " +//П1-2(0,8%)                         
                     "RCPREP.RAW_ID <>-432596 AND " +//П6-1(15%)
                     "RCPREP.RAW_ID <>-432597 AND " +//П6-1(15%)ФИН-2
                     "RCPREP.RAW_ID <>-432393 AND " +//П6-1(2,5%)
                     "RCPREP.RAW_ID <>-432493 AND " +//П60-1(2,5%)
                     "RCPREP.RAW_ID <>-432394 AND " +//П60-3(2,5%)
                     "RCPREP.RAW_ID <>-432494 AND " +//П60-3(2,5%)
                     "RCPREP.RAW_ID <>-432389 AND " +//ПКР-2(1%)
                     "RCPREP.RAW_ID <>-432392 AND " +//П5-1(3,5%)
                     "RCPREP.RAW_ID <>-432712 AND " +//П5-1(4,5%)
                     "RCPREP.RAW_ID <>-432703 AND " +//КС-4(2%) 
                     "RCPREP.RAW_ID <>-432714 AND " +//П1-1Кобб(3%)Troun Nu
                     "RCPREP.RAW_ID <>-432758 AND " +//П5-1(15%)МИАВИТ
                     "RCPREP.RAW_ID <>-432918 AND " +
                     "RCPREP.RAW_ID <>-433035 AND " +
                     "RCPREP.RAW_ID <>-432977 AND " +
                     "RCPREP.RAW_ID <>-432811 AND " +
                     "RCPREP.RAW_ID <>-432812 AND " +
                     "RCPREP.RAW_ID <>-432969 AND " +
                     "RCPREP.RAW_ID <>-432805 AND " +
                     "RCPREP.RAW_ID <>-432819 AND " +
                     "RCPREP.RAW_ID <>-433209 AND " +
                     "RCPREP.RAW_ID <>-433074 AND " +
                     "RCPREP.RAW_ID <>-433036 "+
                     " AND RCPREP.RAW_PERCENT <> 0  " +
                "", connection);
    
                command.Transaction = transaction;
                FbDataReader rd = command.ExecuteReader();

              //  dataGridView1.Rows.Clear();  потом открыть, проверка содержимого

                while (rd.Read())
                {
                    //    dataGridView1.Rows.Add(new Object[] { rd[0], rd[1] });   потом открыть, проверка содержимого
                    
                    if (rd[2].ToString() == "17")//если витамин(17) размещаем вверху
                    {
                       tb17_old.Text = tb6_old.Text = tb17.Text = tb6.Text =  tb6.Text.Insert(0, rd[0].ToString()+", ");
                    }
                    else if (replaseNameOfElemetByGroup(rd[0].ToString(), rd[2].ToString(), rd[1].ToString()) != rd[0].ToString())
                    {
                        if (rd[0].ToString().Contains("ПРЕМИКС"))
                        {
                            tb17_old.Text = tb6_old.Text = tb17.Text = tb6.Text += "ПРЕМИКС";
                        }
                        else
                        {
                            tb17_old.Text = tb6_old.Text = tb17.Text = tb6.Text += replaseNameOfElemetByGroup(rd[0].ToString(), rd[2].ToString(), rd[1].ToString()) + ", ";
                        }
                    }
                    else
                    {
                        if (rd[0].ToString().Contains("ПРЕМИКС"))
                        {
                            tb17_old.Text = tb6_old.Text = tb17.Text = tb6.Text += "ПРЕМИКС, ";
                        }
                        else
                        {
                            tb17_old.Text = tb6_old.Text = tb17.Text = tb6.Text += replaseNameOfElemet(rd[0].ToString(), rd[1].ToString()) + " , ";
                        }
                    }
                     if (rd[3].ToString() != "")
                    {
                        arr[i++] = rd[3].ToString(); // сохраняем RAW_DICT.RAW_ID  элементы витамины в массив для Вит Концентратов
                    }
                }
                rd.Close();
// поиск питательности комбикорма
                 
                    command = new FbCommand("SELECT rcpcorrq.qlt_in_calc,rcpcorrq.qlt_id " +
                                                " from rcpcorrq " +
                                                " where rcpcorrq.rcp_id = " + ID , connection);
                    command.Transaction = transaction;
                    rd = command.ExecuteReader();
                    i = 0;
                    while (rd.Read())
                    {
                        if (rd[1].ToString() == "65" || rd[1].ToString() == "3" || rd[1].ToString() == "4" || rd[1].ToString() == "5")
                        {
                           tb26_old.Text = tb26.Text += "\r\n" + "Обменная энергия   " + rd[0].ToString()+ "Мдж/кг";
                        }
                        if (rd[1].ToString() == "7" && rd[0].ToString() != "" )
                        {
                            tb26_old.Text = tb26.Text += "\r\n" + "Мас. доля сырого протеина  " + rd[0].ToString() + "%";
                        }
                        if (rd[1].ToString() == "50" && rd[0].ToString() != "")
                        {
                            tb26_old.Text = tb26.Text += "\r\n" + "Мас. доля общего фосфора  " + rd[0].ToString() + "%";
                        }
                        if (rd[1].ToString() == "49" && rd[0].ToString() != "")
                        {
                            tb26_old.Text = tb26.Text += "\r\n" + "Массовая доля кальция   " + rd[0].ToString() + "%";
                        }
                        if (rd[1].ToString() == "9" && rd[0].ToString() != "")
                        {
                            tb26_old.Text = tb26.Text += "\r\n" + "Массовая доля сырого жира   " + rd[0].ToString() + "%";
                        }
                        if (rd[1].ToString() == "10" && rd[0].ToString() != "")
                        {
                            tb26_old.Text = tb26.Text += "\r\n" + "Массовая доля сырой клетчатки   " + rd[0].ToString() + "%";
                        }
                        if (rd[1].ToString() == "55" && rd[0].ToString() != "")
                        {
                            tb26_old.Text = tb26.Text += "\r\n" + "Массовая доля натрия   " + rd[0].ToString() + "%";
                        }
                    }
                    rd.Close();                  
                //ищем элементы витаминных канцентратов
                command = new FbCommand("select qm_dict.qlt_short_name,qm_dict.qlt_id " +
                                    " from rawqdict ,qm_dict "+
                                    " where rawqdict.qlt_id <> 104 and rawqdict.qlt_id <> 101 and rawqdict.qlt_id <> 49 and " +
                                    " rawqdict.qlt_id <> 102 and rawqdict.qlt_id <> 50 and rawqdict.qlt_id <> 77  and  rawqdict.qlt_id = qm_dict.qlt_id " +
                                    "  AND qm_dict.qm_group_id <> 5 and rawqdict.qlt_val > 0 and rawqdict.raw_id = ( " +
                                    " select raw_dict.raw_id "+
                                    " from raw_dict,rcprep " +
                                    " where raw_dict.raw_group_id = 29  and  rcprep.rcp_id = "+ ID + " " +
                                    " and raw_dict.raw_id <>-432725"+
                                    " and raw_dict.raw_id <>-433121"+
                                    " and raw_dict.raw_id <>-433120"+
                                    " and raw_dict.raw_id <>-432915"+
                                    " and raw_dict.raw_id <>-432916"+
                                    " and raw_dict.raw_id <>-432964"+
                                    " and raw_dict.raw_id <>-432931"+
                                    " and raw_dict.raw_id <>-432932"+
                                    " and raw_dict.raw_id <>-432639"+
                                    " and raw_dict.raw_id <>-432414"+
                                    " and raw_dict.raw_id <>-432930"+
                                    " and raw_dict.raw_id <>-432933"+
                                    " and raw_dict.raw_id <>-432580"+
                                    " and raw_dict.raw_id <>-433122"+
                                    " and raw_dict.raw_id <>-432727"+
                                    " and rcprep.raw_id = raw_dict.raw_id  )", connection);

                command.Transaction = transaction;
                rd = command.ExecuteReader();
                i = 0;
                while (rd.Read())
                {                  
                    for (int j=0; j < arr.Length; j++)
                    {
                        if (arr[j] == rd[1].ToString())
                        {
                            isExist = true; break;
                        }
                    }
                    if (!isExist)
                     tb17_old.Text = tb6_old.Text =  tb17.Text = tb6.Text = tb6.Text.Insert(0, replaseNameOfElemet(rd[0].ToString(), rd[1].ToString()) + " , ");//kombikorm
                        isExist = false;
                }
                rd.Close();

                //находим название рецепта
                command = new FbCommand("SELECT  " +
                   "CAST(RAW_DICT.KPROD AS varchar(120)),raw_dict.\"ProdDestination\", RAW_DICT.RAW_NAME " +
                   "FROM RCPLIST,DOC_DCT, RAW_DICT " +
                   "WHERE  RCPLIST.RCP_ID = " + ID + " AND " +
                   "DOC_DCT.DOC_ID = RCPLIST.RCP_ID AND RCPLIST.PROD_ID = RAW_DICT.RAW_ID", connection);


                command.Transaction = transaction;
                rd = command.ExecuteReader();

                while (rd.Read())
                {
                   tb8_old.Text = tb8.Text = rd[0].ToString();
                   tb8_kom_old.Text = tb8_kom.Text = rd[0].ToString() + "    (экм " + tb_id.Text + ")";
                   tb2_old.Text = tb2.Text = rd[1].ToString();
                   tb2_kom_old.Text = tb2_kom.Text = rd[1].ToString();// + " (экм " + tb_id.Text + ")"; //kombikorm!!!

                    shortName = rd[0].ToString();
                    kindName = rd[2].ToString();
                }
                rd.Close();             

            }//try
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: Нет Названия рецепта" + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }

      /*  private void numerOfBarcode(string nameOfRecept)
        {
            if (Regex.IsMatch(nameOfRecept, "ДП5-1")) { MessageBox.Show("Ура! Слово найдено!"); }
        }
        */
   private bool find_java1() 
   {            
            FbConnection connection = new FbConnection(connstr.connStr);
            FbCommand command = new FbCommand();

            command = new FbCommand("SELECT max(doc_dct.DOC_ID) FROM DOC_DCT WHERE doc_dct.DOC_NUM=" + tb_id.Text + " and doc_dct.doc_group_id  < 0  ",connection);

            try
            {
                connection.Open();
                FbTransaction transaction = connection.BeginTransaction();
                command.Transaction = transaction;
                FbDataReader rd = command.ExecuteReader();

              //  dataGridView1.Rows.Clear();  потом открыть, проверка содержимого

                while (rd.Read())
                {
                    ID = int.Parse(rd[0].ToString());
                }
                
            }
           catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
                return false;
            }
            finally
            {
                connection.Close();               
            }
        return true;
}

   private bool find_java2()
   {   
    
       using (StreamReader Reader = new StreamReader(Application.StartupPath + "\\run.bat"))   // (@"d:\Programming\Экомол\EAN13\Ean13Barcode\bin\Debug\runzao.bat")
       {
           while (!Reader.EndOfStream)
           {
               ID = int.Parse(Reader.ReadLine());
           }

       } 
       return true;
   }

   private int ID;
   
       private bool find_java()
       {

           /*  psI.RedirectStandardInput = true;
            psI.UseShellExecute = false;
            p.StartInfo = psI;
            p.Start();
            using (StreamWriter sw = p.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine("2080");                   
                }
            }
            using (StreamReader sr = p.StandardOutput)
            {
                int i = 0;
                char[] tmp_mas = sr.ReadToEnd().ToCharArray();//koll tonn

                i = 0;
                string str_tmp = ""; // расчет на  ХХ тонн
                foreach (char temp in tmp_mas)
                {
                    if (char.IsNumber(temp)) str_tmp += tmp_mas.GetValue(i).ToString();
                    i++;
                }
                if (str_tmp == "")
                {
                    MessageBox.Show("Нет такого в базе");
                    return false;
                }

                ID = int.Parse(str_tmp);
            }*/


           Process p = new Process();
           StreamWriter sw;
           StreamReader sr;
           ProcessStartInfo psI = new ProcessStartInfo(Application.StartupPath + "\\runZAO.bat");
           psI.UseShellExecute = false;
           psI.RedirectStandardInput = true;
           psI.RedirectStandardOutput = true;
           psI.RedirectStandardError = true;
           psI.CreateNoWindow = true;
           p.StartInfo = psI;
           p.Start();
           sw = p.StandardInput;
           sr = p.StandardOutput;
           sw.AutoFlush = true;

           sw.WriteLine(tb_id.Text);

           sw.Close();
         
           int i = 0;
           char[] tmp_mas = sr.ReadToEnd().ToCharArray();//koll tonn
           i = 0;
           string str_tmp = ""; 
           foreach (char temp in tmp_mas)
           {
               if (char.IsNumber(temp)) str_tmp += tmp_mas.GetValue(i).ToString();
               i++;
           }
           if (str_tmp == "")
           {
               MessageBox.Show("карточка не найдена");
               return false;
           }

           ID = int.Parse(str_tmp);

           sr.Close();
           return true;
        }

     private void textBox11_TextChanged(object sender, EventArgs e)
        {
            if (tb11_size.Text != "")
            {
                string a = tb11_size.Text.Replace('.', ',');
                if (panelBVMD.Visible == true || panelBVMD_old.Visible == true)
                {
                    this.tb6_old.Font = this.tb6.Font = new System.Drawing.Font("Times New Roman", float.Parse(a), FontStyle.Bold);
                }
                if (panelKombicorm.Visible == true || panelKombicorm_old.Visible == true)
                {
                    this.tb26_old.Font = this.tb26.Font = new System.Drawing.Font("Times New Roman", float.Parse(a), FontStyle.Bold);
                }
            }

        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (tb3_size.Text != "")
            {
                string a = tb3_size.Text.Replace('.', ',');
                if (panelBVMD.Visible == true || panelBVMD_old.Visible == true)
                {
                    this.tb2_old.Font = this.tb2.Font = new System.Drawing.Font("Times New Roman", float.Parse(a), FontStyle.Bold);
                }
                if (panelKombicorm.Visible == true || panelKombicorm_old.Visible == true)
                {
                    this.tb2_kom_old.Font = this.tb2_kom.Font = new System.Drawing.Font("Times New Roman", float.Parse(a), FontStyle.Bold);
                }
            }
        }

        private void textBox23_TextChanged(object sender, EventArgs e)
        {
            if (tb23_size.Text != "")
            {
                string a = tb23_size.Text.Replace('.', ',');
                if (panelBVMD.Visible == true || panelBVMD_old.Visible == true)
                {
                    this.tb8_old.Font = this.tb8.Font = new System.Drawing.Font("Times New Roman", float.Parse(a), FontStyle.Bold);
                }
                if (panelKombicorm.Visible == true || panelKombicorm_old.Visible == true)
                {
                    this.tb8_kom_old.Font = this.tb8_kom.Font = new System.Drawing.Font("Times New Roman", float.Parse(a), FontStyle.Bold);
                }
            }
        }
        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (tb5_size.Text != "")
            {
                string a = tb5_size.Text.Replace('.', ',');
                if (panelBVMD.Visible == true || panelBVMD_old.Visible == true)
                {
                    this.tb4_old.Font = this.tb4.Font = new System.Drawing.Font("Times New Roman", float.Parse(a), FontStyle.Bold);
                }
                if (panelKombicorm.Visible == true || panelKombicorm_old.Visible == true)
                {
                    this.tb17_old.Font = this.tb17.Font = new System.Drawing.Font("Times New Roman", float.Parse(a), FontStyle.Bold);
                }
            }
        }

        private void tb_id_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                tb6_old.Text = tb6.Text = "";
                tb26_old.Text = tb26.Text = "";
                tb10_old.Text = tb22_old.Text = textBox37_old.Text = tb10.Text = tb22.Text = textBox37.Text = tbVes.Text;
                if (find_java())
                {
                    serch_BD_ather_item();
                    check_type(); //определяем Премикс или БВМД  
                    tb14_old.Text = tb14.Text = tb_id.Text;
                    getEAN13code();
                }

                    //Draw the EAN 13 code
                    if (is_Draw)
                    {
                        CreateEan13();//считаем проверяем
                        System.Drawing.Bitmap bmp = ean13.CreateBitmap();//рисуем
                        bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        this.picBarcode.Image = pictureBox1.Image = this.picBarcode_kom.Image = bmp;
                    }
                    else
                    {
                        var result = MessageBox.Show("Не нашел штрих код, ДОБАВИМ в базу ??", "EAN13 added", MessageBoxButtons.YesNo,
                                  MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            add_EAN13 form2 = new add_EAN13();
                            form2.Visible = true;
                            form2.Owner = this;
                            if(panelBVMD.Visible == true)
                            {
                                CallBackMy.callbackEventHandler(tb8.Text, tbVes.Text);
                            }
                            else CallBackMy.callbackEventHandler(tb8_kom.Text, tbVes.Text);

                            
                            form2.Show();
                        }
                    }
                    // End draw EAN13 code
            }
        }
       
        private void tbVes_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                tb6_old.Text = tb6.Text = "";
                tb26_old.Text = tb26.Text = "";
                tb10_old.Text = tb22_old.Text = textBox37_old.Text = tb10.Text = tb22.Text = textBox37.Text = tbVes.Text;
                if (find_java())
                {
                    serch_BD_ather_item();
                    check_type(); //определяем Премикс или БВМД  
                    tb14_old.Text = tb14.Text = tb_id.Text;
                }
                //get EAN13 code
                getEAN13code();
                //Draw the EAN 13 code
                if (is_Draw)
                {
                    CreateEan13();//считаем проверяем
                    System.Drawing.Bitmap bmp = ean13.CreateBitmap();//рисуем
                    bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    this.picBarcode.Image  = pictureBox1.Image = this.picBarcode_kom.Image = bmp;
                }
                else
                {
                    var result = MessageBox.Show("Не нашел штрих код, ДОБАВИМ в базу ??", "EAN13 added", MessageBoxButtons.YesNo,
                              MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        add_EAN13 form2 = new add_EAN13();
                        form2.Visible = true;
                        form2.Owner = this;
                        if (panelBVMD.Visible == true)
                        {
                            CallBackMy.callbackEventHandler(tb8.Text, tbVes.Text);
                        }
                        else CallBackMy.callbackEventHandler(tb8_kom.Text, tbVes.Text);
                            
                        form2.Show();
                    }
                }
            }
                    // End draw EAN13 code
        }
      
	}
}
//PARSING SYMBOL
/*     void code_pars(string code)
     { 
                 int i = 0;
                 char[] tmp_mas = code.ToCharArray();//koll tonn
                 string str_tmp = "";

                  foreach (char temp in tmp_mas)
             {
                 if (char.IsNumber(temp)) str_tmp += tmp_mas.GetValue(i).ToString();
                 i++;
             }
     }
  */