using System;
using System.Collections.Generic;
using System.Text;
using FirebirdSql.Data.Firebird;
using System.Configuration;



namespace Ean13Barcode
{
    class ConnStr
    {
        public string connStr;
        public ConnStr()
        {
            FbConnectionStringBuilder cs = new FbConnectionStringBuilder();

            cs.DataSource = "localhost";
            cs.UserID = "SYSDBA";
            cs.Password = "masterkey";
           // cs.Database = @"w2k3:D:\_Recept_Base\cbd_user.gdb";
            cs.Database = @"w2k3:D:\_Recept_Base\Agro\cbd_user.gdb";
        //    cs.Database = @"d:\Programming\Base\FireBird\cbd_user.gdb"; 

            cs.Port = 3050;
            cs.Charset = "WIN1251";
            cs.Pooling = true;
            cs.ConnectionLifeTime = 30;
            // To use the embedded server set the ServerType to 1
            cs.ConnectionLifeTime = 15;
            cs.Dialect = 3;
            cs.Role = "";
            cs.Pooling = true;
            cs.MinPoolSize = 0;
            cs.MaxPoolSize = 50;
            cs.PacketSize = 8192;
            connStr = cs.ToString();
        }
    }
}
